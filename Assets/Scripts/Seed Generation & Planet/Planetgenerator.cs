using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// PlanetGenerator: Attach to a GameObject in each planet scene.
/// On Start it reads the sub-seed from GalaxyManager and generates:
///   - Terrain heightmap (Earth style or Moon crater style)
///   - Biome map (temperature + humidity noise)
///   - Structure placement with terrain carving
///   - Road painting between Pizzeria and nearby structures
/// </summary>
[RequireComponent(typeof(Terrain))]
public class PlanetGenerator : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────────
    [Header("Planet Settings")]
    public PlanetType planetType = PlanetType.Earth;

    [Header("Terrain")]
    [Tooltip("Height scale multiplier for the terrain")]
    public float heightScale       = 50f;

    [Tooltip("Noise frequency – smaller = smoother terrain")]
    public float noiseFrequency    = 0.003f;

    [Tooltip("Number of octaves for fractal noise")]
    [Range(1, 8)]
    public int   noiseOctaves      = 5;

    [Header("Moon-Specific")]
    [Tooltip("How deep moon craters are (0-1)")]
    [Range(0f, 1f)]
    public float craterDepth       = 0.35f;

    [Tooltip("Number of craters to stamp on the moon")]
    public int   craterCount       = 40;

    [Header("Structures")]
    [Tooltip("All StructureDefinition assets in the game. Drag them all here.")]
    public List<StructureDefinition> allStructures = new List<StructureDefinition>();

    [Tooltip("Pizzeria prefab – hardcoded at origin on Earth")]
    public GameObject pizzeriaPrefab;

    [Tooltip("How many candidate points to test for structure placement")]
    public int structureCandidateCount = 200;

    [Header("Roads")]
    [Tooltip("Terrain texture index to use for the road (must exist in terrain layers)")]
    public int roadTerrainLayerIndex = 1;

    [Tooltip("Width of the road in world units")]
    public float roadWidth = 4f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Terrain    _terrain;
    private TerrainData _data;
    private int        _heightmapRes;
    private int        _alphamapRes;
    private System.Random _rng;
    private long       _seed;

    // Tracks placed structure world positions for clearance checks and road painting
    private readonly List<(Vector3 position, StructureDefinition def)> _placed
        = new List<(Vector3, StructureDefinition)>();

    private Vector3 _pizzeriaWorldPos = Vector3.zero;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _terrain      = GetComponent<Terrain>();
        _data         = _terrain.terrainData;
        _heightmapRes = _data.heightmapResolution;
        _alphamapRes  = _data.alphamapResolution;

        // Pull sub-seed from GalaxyManager if present, otherwise use inspector fallback
        _seed = GalaxyManager.Instance != null
            ? GalaxyManager.Instance.CurrentPlanetSubSeed
            : System.DateTime.Now.Ticks;

        _rng = new System.Random((int)(_seed & 0x7FFFFFFF));

        StartCoroutine(GeneratePlanet());
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Generation Pipeline

    private IEnumerator GeneratePlanet()
    {
        Debug.Log($"[PlanetGenerator] Generating {planetType} with seed {_seed}");

        // Step 1 – Terrain heightmap
        yield return StartCoroutine(GenerateTerrain());

        // Step 2 – Moon-specific: adjust gravity
        if (planetType == PlanetType.Moon)
            Physics.gravity = new Vector3(0f, -1.62f, 0f);

        // Step 3 – Paint biomes onto alphamap so they are visually distinct
        yield return StartCoroutine(PaintBiomes());

        // Step 4 – Place structures (carving happens after terrain is final)
        PlaceStructures();

        // Step 5 – Paint roads
        yield return StartCoroutine(PaintRoads());

        Debug.Log($"[PlanetGenerator] Generation complete.");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Terrain Generation

    private IEnumerator GenerateTerrain()
    {
        float[,] heights = new float[_heightmapRes, _heightmapRes];

        // Offsets derived from seed so each world looks different
        float offsetX = (float)((_seed >> 16) & 0xFFFF);
        float offsetZ = (float)((_seed)        & 0xFFFF);

        for (int z = 0; z < _heightmapRes; z++)
        {
            for (int x = 0; x < _heightmapRes; x++)
            {
                float nx = (x + offsetX) * noiseFrequency;
                float nz = (z + offsetZ) * noiseFrequency;

                heights[z, x] = planetType == PlanetType.Moon
                    ? SampleMoonHeight(nx, nz)
                    : SampleEarthHeight(nx, nz);
            }

            // Yield every 64 rows to avoid hitching
            if (z % 64 == 0) yield return null;
        }

        _data.SetHeights(0, 0, heights);
        Debug.Log("[PlanetGenerator] Heightmap generated.");
    }

    /// <summary>Fractal Perlin noise for Earth-style rolling hills.</summary>
    private float SampleEarthHeight(float nx, float nz)
    {
        float value     = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue  = 0f;

        for (int i = 0; i < noiseOctaves; i++)
        {
            value    += Mathf.PerlinNoise(nx * frequency, nz * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return (value / maxValue) * (heightScale / _data.size.y);
    }

    /// <summary>
    /// Crater-style noise for the Moon.
    /// Uses inverted radial falloff stamps to simulate crater shapes.
    /// </summary>
    private float SampleMoonHeight(float nx, float nz)
    {
        // Base bumpy surface
        float base_ = Mathf.PerlinNoise(nx * 2f, nz * 2f) * 0.15f;

        // Pre-generate crater centres once per world (cached in seed-derived list)
        float craterValue = 0f;
        System.Random craterRng = new System.Random((int)(_seed & 0x7FFFFFFF));

        for (int i = 0; i < craterCount; i++)
        {
            float cx = (float)craterRng.NextDouble();
            float cz = (float)craterRng.NextDouble();
            float radius = 0.02f + (float)craterRng.NextDouble() * 0.08f;

            float dist = Mathf.Sqrt((nx - cx) * (nx - cx) + (nz - cz) * (nz - cz));
            if (dist < radius)
            {
                // Crater bowl: inverted smooth step
                float t = dist / radius;
                float bowl = 1f - Mathf.SmoothStep(0f, 1f, t);
                craterValue = Mathf.Max(craterValue, bowl * craterDepth);
            }
        }

        return Mathf.Clamp01(base_ - craterValue) * (heightScale / _data.size.y);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Structure Placement

    private void PlaceStructures()
    {
        // ── Step A: Pizzeria at origin (Earth only) ───────────────────────────
        if (planetType == PlanetType.Earth && pizzeriaPrefab != null)
        {
            CarveFlat(Vector3.zero, 100f);
            float y = _terrain.SampleHeight(Vector3.zero) + _terrain.transform.position.y;
            _pizzeriaWorldPos = new Vector3(0f, y + 0.1f, 0f);
            Instantiate(pizzeriaPrefab, _pizzeriaWorldPos, Quaternion.identity);
            Debug.Log($"[PlanetGenerator] Pizzeria placed at {_pizzeriaWorldPos}");
        }

        // ── Step B: ScriptableObject-driven structures ─────────────────────────
        Vector3 terrainSize = _data.size;
        Vector3 terrainPos  = _terrain.transform.position;

        foreach (StructureDefinition def in allStructures)
        {
            // Skip if this planet type isn't allowed
            if (!def.allowedPlanets.Contains(planetType)) continue;

            // Skip moon Pizzeria-style force-origin structures on Moon
            if (def.forceSpawnAtOrigin && planetType == PlanetType.Moon) continue;

            int placedCount = 0;

            for (int attempt = 0; attempt < structureCandidateCount; attempt++)
            {
                // Singleton: only place once
                if (def.isSingleton && placedCount > 0) break;

                // Deterministic candidate position
                float cx = (float)_rng.NextDouble() * terrainSize.x + terrainPos.x;
                float cz = (float)_rng.NextDouble() * terrainSize.z + terrainPos.z;
                Vector3 candidate = new Vector3(cx, 0f, cz);

                // Probability check
                if (_rng.NextDouble() > def.spawnProbability) continue;

                // Biome check
                Biome biome = SampleBiome(cx / terrainSize.x, cz / terrainSize.z);
                if (!def.allowedBiomes.Contains(biome)) continue;

                // Clearance check
                if (IsTooCloseToExisting(candidate, def.clearanceRadius)) continue;

                // Carve terrain flat if required
                if (def.carveTerrainFlat)
                    CarveFlat(candidate, def.clearanceRadius);

                float worldY = _terrain.SampleHeight(candidate) + terrainPos.y;
                candidate.y  = worldY;

                Quaternion rot = Quaternion.Euler(0f, (float)(_rng.NextDouble() * 360f), 0f);
                Instantiate(def.prefab, candidate, rot);
                _placed.Add((candidate, def));
                placedCount++;
            }
        }

        Debug.Log($"[PlanetGenerator] Placed {_placed.Count} structures.");
    }

    /// <summary>
    /// Samples the biome at a normalised coordinate (0-1) using two Perlin layers
    /// for temperature and humidity. Deterministic based on seed offsets.
    /// </summary>
    private Biome SampleBiome(float nx, float nz)
    {
        float tempOffset  = (float)((_seed >> 32) & 0xFFFF);
        float humidOffset = (float)((_seed >> 48) & 0xFFFF);

        float temperature = Mathf.PerlinNoise(nx * 2f + tempOffset,  nz * 2f + tempOffset);
        float humidity    = Mathf.PerlinNoise(nx * 2f + humidOffset, nz * 2f + humidOffset);

        if (planetType == PlanetType.Moon) return Biome.Lunar;

        if (temperature > 0.7f && humidity < 0.3f) return Biome.Desert;
        if (temperature < 0.3f)                     return Biome.Tundra;
        if (humidity > 0.7f)                         return Biome.Forest;
        if (temperature > 0.6f && humidity > 0.6f)  return Biome.Volcanic;
        return Biome.Grassland;
    }

    /// <summary>Returns true if any already-placed structure is within radius of candidate.</summary>
    private bool IsTooCloseToExisting(Vector3 candidate, float radius)
    {
        foreach (var (pos, _) in _placed)
        {
            if (Vector3.Distance(new Vector3(candidate.x, 0f, candidate.z),
                                 new Vector3(pos.x,       0f, pos.z)) < radius)
                return true;
        }
        // Also check distance from Pizzeria
        if (Vector3.Distance(new Vector3(candidate.x, 0f, candidate.z), Vector3.zero) < radius)
            return true;

        return false;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Biome Painting

    /// <summary>
    /// Paints terrain alphamap textures based on biome at each point.
    /// Requires terrain layers set up in this order:
    ///   0 = Grassland (green)
    ///   1 = Road / path
    ///   2 = Desert (sand)
    ///   3 = Tundra (snow)
    ///   4 = Forest (dark green)
    ///   5 = Volcanic (dark rock)
    ///   6 = Lunar (grey dust) — Moon only
    /// Add as many layers as you have, missing ones are skipped gracefully.
    /// </summary>
    private IEnumerator PaintBiomes()
    {
        int layers = _data.alphamapLayers;
        if (layers == 0)
        {
            Debug.LogWarning("[PlanetGenerator] No terrain layers set up — skipping biome paint.");
            yield break;
        }

        float[,,] alphamap = _data.GetAlphamaps(0, 0, _alphamapRes, _alphamapRes);

        for (int z = 0; z < _alphamapRes; z++)
        {
            for (int x = 0; x < _alphamapRes; x++)
            {
                float nx = (float)x / _alphamapRes;
                float nz = (float)z / _alphamapRes;

                Biome biome = SampleBiome(nx, nz);

                // Clear all layers
                for (int l = 0; l < layers; l++)
                    alphamap[z, x, l] = 0f;

                // Set dominant layer based on biome
                int dominantLayer = BiomeToLayer(biome);
                if (dominantLayer < layers)
                    alphamap[z, x, dominantLayer] = 1f;
                else
                    alphamap[z, x, 0] = 1f; // fallback to layer 0
            }

            if (z % 32 == 0) yield return null;
        }

        _data.SetAlphamaps(0, 0, alphamap);
        Debug.Log("[PlanetGenerator] Biomes painted.");
    }

    private int BiomeToLayer(Biome biome)
    {
        return biome switch
        {
            Biome.Grassland => 0,
            Biome.Desert    => 2,
            Biome.Tundra    => 3,
            Biome.Forest    => 4,
            Biome.Volcanic  => 5,
            Biome.Lunar     => 6,
            _               => 0
        };
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Terrain Carving

    /// <summary>
    /// Flattens a circular area of the heightmap around worldPos.
    /// Uses Unity's TerrainData.GetHeights / SetHeights for direct heightmap access.
    /// </summary>
    private void CarveFlat(Vector3 worldPos, float worldRadius)
    {
        Vector3 terrainPos  = _terrain.transform.position;
        Vector3 terrainSize = _data.size;

        // Convert world position to heightmap coordinates
        int hmX = Mathf.RoundToInt((worldPos.x - terrainPos.x) / terrainSize.x * (_heightmapRes - 1));
        int hmZ = Mathf.RoundToInt((worldPos.z - terrainPos.z) / terrainSize.z * (_heightmapRes - 1));
        hmX = Mathf.Clamp(hmX, 0, _heightmapRes - 1);
        hmZ = Mathf.Clamp(hmZ, 0, _heightmapRes - 1);

        // Convert world radius to heightmap texel radius
        int hmRadius = Mathf.RoundToInt(worldRadius / terrainSize.x * (_heightmapRes - 1));
        hmRadius = Mathf.Max(hmRadius, 1);

        // Clamp sample region to terrain bounds
        int xMin = Mathf.Max(0, hmX - hmRadius);
        int xMax = Mathf.Min(_heightmapRes - 1, hmX + hmRadius);
        int zMin = Mathf.Max(0, hmZ - hmRadius);
        int zMax = Mathf.Min(_heightmapRes - 1, hmZ + hmRadius);

        int w = xMax - xMin + 1;
        int h = zMax - zMin + 1;

        float[,] heights = _data.GetHeights(xMin, zMin, w, h);

        // Use the minimum height in the area so the flat spot
        // sits at the lowest natural point — avoids floating buildings
        float targetHeight = float.MaxValue;
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                if (heights[z, x] < targetHeight)
                    targetHeight = heights[z, x];

        // Apply: fully flat inside, blend only in outer 20% ring
        float blendStart = hmRadius * 0.8f;
        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx   = (xMin + x) - hmX;
                float dz   = (zMin + z) - hmZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist > hmRadius) continue;

                if (dist <= blendStart)
                {
                    // Fully flat
                    heights[z, x] = targetHeight;
                }
                else
                {
                    // Smooth blend into surrounding terrain
                    float t = (dist - blendStart) / (hmRadius - blendStart);
                    heights[z, x] = Mathf.Lerp(targetHeight, heights[z, x], Mathf.SmoothStep(0f, 1f, t));
                }
            }
        }

        _data.SetHeights(xMin, zMin, heights);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Road Painting (Job-based)

    private IEnumerator PaintRoads()
    {
        if (_alphamapRes == 0 || _data.alphamapLayers < roadTerrainLayerIndex + 1)
        {
            Debug.LogWarning("[PlanetGenerator] Skipping road paint: terrain layer not set up.");
            yield break;
        }

        float[,,] alphamap = _data.GetAlphamaps(0, 0, _alphamapRes, _alphamapRes);
        Vector3 terrainPos  = _terrain.transform.position;
        Vector3 terrainSize = _data.size;

        // Find all structures that want a road connection
        foreach (var (structPos, def) in _placed)
        {
            if (!def.connectRoadToHome) continue;

            // Paint road from Pizzeria to this structure
            yield return StartCoroutine(PaintRoadSegment(
                alphamap, terrainPos, terrainSize,
                _pizzeriaWorldPos, structPos));
        }

        _data.SetAlphamaps(0, 0, alphamap);
        Debug.Log("[PlanetGenerator] Roads painted.");
    }

    private IEnumerator PaintRoadSegment(
        float[,,] alphamap,
        Vector3 terrainPos, Vector3 terrainSize,
        Vector3 from, Vector3 to)
    {
        int steps = Mathf.CeilToInt(Vector3.Distance(from, to) * 0.5f);
        float halfWidthNorm = (roadWidth * 0.5f) / terrainSize.x * _alphamapRes;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 point = Vector3.Lerp(from, to, t);

            // Also flatten terrain along the road path
            CarveFlat(point, roadWidth);

            // Convert to alphamap space
            int ax = Mathf.RoundToInt((point.x - terrainPos.x) / terrainSize.x * _alphamapRes);
            int az = Mathf.RoundToInt((point.z - terrainPos.z) / terrainSize.z * _alphamapRes);

            int r = Mathf.CeilToInt(halfWidthNorm);

            for (int dz = -r; dz <= r; dz++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    int px = Mathf.Clamp(ax + dx, 0, _alphamapRes - 1);
                    int pz = Mathf.Clamp(az + dz, 0, _alphamapRes - 1);

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    float blend = 1f - Mathf.Clamp01(dist / halfWidthNorm);

                    // Blend road layer in, reduce all others
                    for (int layer = 0; layer < _data.alphamapLayers; layer++)
                    {
                        if (layer == roadTerrainLayerIndex)
                            alphamap[pz, px, layer] = Mathf.Max(alphamap[pz, px, layer], blend);
                        else
                            alphamap[pz, px, layer] *= (1f - blend);
                    }
                }
            }

            if (i % 10 == 0) yield return null; // yield periodically
        }
    }

    #endregion
}
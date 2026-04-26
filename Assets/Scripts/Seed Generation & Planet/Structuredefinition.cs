using System.Collections.Generic;
using UnityEngine;

// ── Enums shared across the system ───────────────────────────────────────────

public enum PlanetType { Earth, Moon }

public enum Biome
{
    Grassland,
    Desert,
    Tundra,
    Forest,
    Volcanic,
    Lunar // Moon-only biome
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// StructureDefinition: ScriptableObject data card for a placeable building.
/// 
/// HOW TO CREATE
///     Right-click in Project window → Create → Galaxy / Structure Definition
/// 
/// Add as many of these as you like. PlanetGenerator will loop through all of them and place whichever ones match the current planet and biome - no code changes required
/// </summary>
[CreateAssetMenu
    (
        fileName = "NewStructureDefinition",
        menuName = "Galaxy/Structure Definition",
        order = 1
    )
]
public class StructureDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name for debugging purposes")]
    public string structureName = "Unnamed Structure";

    [Tooltip("The prefab to spawn in the world")]
    public GameObject prefab;

    // ── Placement Rules ───────────────────────────────────────────────────────
    [Header("Placement Rules")]

    [Tooltip("Which planet types this structure can appear on")]
    public List<PlanetType> allowedPlanets = new List<PlanetType> { PlanetType.Earth };

    [Tooltip("Which biomes this structure can appear in")]
    public List<Biome> allowedBiomes = new List<Biome> { Biome.Grassland };

    [Tooltip("0 = never spawn, 1 = always spawn (checked per candidate point)")]
    [Range(0f, 1f)]
    public float spawnProbability = 0.05f;

    [Tooltip("Radius in world units within which no other structure may spawn")]
    public float clearanceRadius = 10f;

    [Tooltip("If true, the terrain is flattened to a circle of clearanceRadius before spawning")]
    public bool carveTerrainFlat = true;

    // ── Special Flags ─────────────────────────────────────────────────────────
    [Header("Special Flags")]

    [Tooltip("If true this structure is hardcoded to spawn at the world orgin (0,0,0). " + "Used for the Pizzeria on Earth.")]
    public bool forceSpawnAtOrigin = false;

    [Tooltip("If true only ONE instance of this structure exists per world")]
    public bool isSingleton = true;

    // ── Road Connection ───────────────────────────────────────────────────────
    [Header("Road Connector")]

    [Tooltip("If true a road will be painted from the Pizzeria to this structure after placement")]
    public bool connectRoadToHome = false;
}
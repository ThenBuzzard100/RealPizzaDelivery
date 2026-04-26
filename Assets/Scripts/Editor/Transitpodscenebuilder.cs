// Place in: Assets/Scripts/Editor/TransitPodSceneBuilder.cs
// Access via: Tools → TransitPodCreation → Build Transit Pod Scene

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public static class TransitPodSceneBuilder
{
    // ── Dimensions ────────────────────────────────────────────────────────────
    private const float POD_LENGTH = 18f;   // Z axis — front to back
    private const float POD_WIDTH  = 4.5f;  // X axis
    private const float POD_HEIGHT = 3.2f;  // Y axis

    // ── Colour Palette (matches dark industrial sci-fi) ───────────────────────
    // Hull: near-black dark steel
    private static readonly Color COL_HULL         = new Color(0.08f, 0.08f, 0.09f);
    // Grime: slightly warmer dark grey for variety panels
    private static readonly Color COL_PANEL_DARK   = new Color(0.12f, 0.11f, 0.10f);
    // Accent stripe: dark gunmetal
    private static readonly Color COL_ACCENT       = new Color(0.18f, 0.17f, 0.16f);
    // Emergency red glow
    private static readonly Color COL_RED_LIGHT    = new Color(1.0f,  0.05f, 0.02f);
    // Dim amber warning
    private static readonly Color COL_AMBER_LIGHT  = new Color(1.0f,  0.45f, 0.02f);
    // Cold blue console
    private static readonly Color COL_BLUE_LIGHT   = new Color(0.1f,  0.4f,  1.0f);
    // Grate/floor: dark iron
    private static readonly Color COL_GRATE        = new Color(0.10f, 0.10f, 0.10f);
    // Pipe: oxidised copper-green tint
    private static readonly Color COL_PIPE         = new Color(0.15f, 0.20f, 0.14f);
    // Rust/damage streaks
    private static readonly Color COL_RUST         = new Color(0.35f, 0.15f, 0.05f);

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TransitPodCreation/Build Transit Pod Scene")]
    public static void BuildTransitPodScene()
    {
        // Create or open the TransitPod scene
        string scenePath = "Assets/Scenes/TransitPod.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "TransitPod";

        // ── Root container ────────────────────────────────────────────────────
        GameObject root = new GameObject("TransitPod_Root");

        // ── Build everything ──────────────────────────────────────────────────
        BuildStructure(root);
        BuildFloor(root);
        BuildCeiling(root);
        BuildWalls(root);
        BuildPanelDetails(root);
        BuildCeilingPipes(root);
        BuildLighting(root);
        BuildConsoles(root);
        BuildPhysicsProps(root);
        BuildEmergencyFixtures(root);
        BuildCamera(root);

        // ── Ambient / fog setup ───────────────────────────────────────────────
        SetupEnvironment();

        // Save scene
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log("[TransitPodBuilder] Scene built and saved to " + scenePath);
        EditorUtility.DisplayDialog("Transit Pod Builder",
            "Transit Pod scene built successfully!\nSaved to: " + scenePath, "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Structure

    /// <summary>Invisible collider shell — the pod hull boundary.</summary>
    static void BuildStructure(GameObject root)
    {
        GameObject hull = new GameObject("Hull_Colliders");
        hull.transform.SetParent(root.transform);

        // We'll use actual mesh walls instead, so hull just marks the container
        hull.transform.localPosition = Vector3.zero;
    }

    /// <summary>Grated metal floor with collider.</summary>
    static void BuildFloor(GameObject root)
    {
        GameObject floor = MakeBox(
            "Floor",
            root,
            new Vector3(0f, -0.05f, POD_LENGTH * 0.5f),
            new Vector3(POD_WIDTH, 0.1f, POD_LENGTH),
            COL_GRATE,
            0f); // static — no rigidbody

        // Add grate-like sub-panels for visual detail
        GameObject grateParent = new GameObject("Floor_GrateDetails");
        grateParent.transform.SetParent(floor.transform);
        grateParent.transform.localPosition = Vector3.zero;

        int grateCount = 8;
        float grateSpacing = POD_LENGTH / grateCount;
        for (int i = 0; i < grateCount; i++)
        {
            float z = i * grateSpacing + grateSpacing * 0.5f;
            // Raised grate strip
            GameObject strip = MakeBox(
                $"GrateStrip_{i}",
                grateParent,
                new Vector3(0f, 0.06f, z - POD_LENGTH * 0.5f),
                new Vector3(POD_WIDTH - 0.1f, 0.02f, grateSpacing * 0.85f),
                i % 2 == 0 ? COL_GRATE : COL_PANEL_DARK,
                0f);
        }

        // Red centre stripe on floor
        MakeBox("Floor_CentreStripe", grateParent,
            new Vector3(0f, 0.07f, 0f),
            new Vector3(0.12f, 0.01f, POD_LENGTH - 0.2f),
            COL_RED_LIGHT * 0.3f, 0f);
    }

    /// <summary>Ceiling with panel seams and pipe channels.</summary>
    static void BuildCeiling(GameObject root)
    {
        MakeBox("Ceiling", root,
            new Vector3(0f, POD_HEIGHT + 0.05f, POD_LENGTH * 0.5f),
            new Vector3(POD_WIDTH, 0.1f, POD_LENGTH),
            COL_HULL, 0f);

        // Ceiling panel strips
        GameObject ceilParent = new GameObject("Ceiling_Panels");
        ceilParent.transform.SetParent(root.transform);
        ceilParent.transform.localPosition = Vector3.zero;

        for (int i = 0; i < 6; i++)
        {
            float z = (i / 5f) * (POD_LENGTH - 1f) + 0.5f;
            MakeBox($"CeilPanel_{i}", ceilParent,
                new Vector3(0f, POD_HEIGHT - 0.01f, z),
                new Vector3(POD_WIDTH - 0.2f, 0.04f, (POD_LENGTH / 6f) - 0.1f),
                COL_PANEL_DARK, 0f);
        }
    }

    /// <summary>Walls — left, right, front cap, back cap with rivet-panel detail.</summary>
    static void BuildWalls(GameObject root)
    {
        GameObject wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(root.transform);

        float halfW = POD_WIDTH  * 0.5f;
        float halfH = POD_HEIGHT * 0.5f;

        // Left wall
        MakeBox("Wall_Left",  wallParent,
            new Vector3(-halfW - 0.05f, halfH, POD_LENGTH * 0.5f),
            new Vector3(0.1f, POD_HEIGHT, POD_LENGTH), COL_HULL, 0f);

        // Right wall
        MakeBox("Wall_Right", wallParent,
            new Vector3(halfW + 0.05f, halfH, POD_LENGTH * 0.5f),
            new Vector3(0.1f, POD_HEIGHT, POD_LENGTH), COL_HULL, 0f);

        // Front wall (door end — far end of pod)
        MakeBox("Wall_Front", wallParent,
            new Vector3(0f, halfH, POD_LENGTH + 0.05f),
            new Vector3(POD_WIDTH, POD_HEIGHT, 0.1f), COL_PANEL_DARK, 0f);

        // Back wall (entry hatch)
        MakeBox("Wall_Back", wallParent,
            new Vector3(0f, halfH, -0.05f),
            new Vector3(POD_WIDTH, POD_HEIGHT, 0.1f), COL_HULL, 0f);

        // Wall panel detail strips (both sides)
        BuildWallPanelStrips(wallParent, -halfW, "Left");
        BuildWallPanelStrips(wallParent,  halfW, "Right");
    }

    static void BuildWallPanelStrips(GameObject parent, float xPos, string side)
    {
        float sign   = xPos < 0f ? 1f : -1f;
        int   panels = 6;
        float panH   = (POD_HEIGHT - 0.3f) / 2f;
        float panZ   = POD_LENGTH / panels;

        for (int i = 0; i < panels; i++)
        {
            float z = i * panZ + panZ * 0.5f;

            // Lower panel
            MakeBox($"WallPanel_{side}_Low_{i}", parent,
                new Vector3(xPos + sign * 0.06f, panH * 0.5f + 0.1f, z),
                new Vector3(0.08f, panH, panZ - 0.12f),
                i % 3 == 0 ? COL_ACCENT : COL_PANEL_DARK, 0f);

            // Upper panel
            MakeBox($"WallPanel_{side}_High_{i}", parent,
                new Vector3(xPos + sign * 0.06f, panH + panH * 0.5f + 0.2f, z),
                new Vector3(0.08f, panH, panZ - 0.12f),
                i % 2 == 0 ? COL_PANEL_DARK : COL_ACCENT, 0f);

            // Rivet strip between panels (thin vertical seam)
            MakeBox($"WallSeam_{side}_{i}", parent,
                new Vector3(xPos + sign * 0.05f, POD_HEIGHT * 0.5f, i * panZ),
                new Vector3(0.06f, POD_HEIGHT - 0.1f, 0.06f),
                COL_HULL, 0f);
        }

        // Horizontal railing at waist height
        MakeBox($"Rail_{side}", parent,
            new Vector3(xPos + sign * 0.08f, 0.95f, POD_LENGTH * 0.5f),
            new Vector3(0.06f, 0.08f, POD_LENGTH - 0.3f),
            COL_ACCENT, 0f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Detail Geometry

    static void BuildPanelDetails(GameObject root)
    {
        GameObject details = new GameObject("PanelDetails");
        details.transform.SetParent(root.transform);

        // Ceiling cross-beams (structural ribs like the image)
        int ribCount = 7;
        for (int i = 0; i <= ribCount; i++)
        {
            float z = (i / (float)ribCount) * POD_LENGTH;

            // Left arm of rib
            MakeBox($"Rib_Left_{i}", details,
                new Vector3(-POD_WIDTH * 0.35f, POD_HEIGHT * 0.5f, z),
                new Vector3(0.1f, POD_HEIGHT, 0.18f), COL_HULL, 0f);

            // Right arm of rib
            MakeBox($"Rib_Right_{i}", details,
                new Vector3(POD_WIDTH * 0.35f, POD_HEIGHT * 0.5f, z),
                new Vector3(0.1f, POD_HEIGHT, 0.18f), COL_HULL, 0f);

            // Top cap connecting arms
            MakeBox($"Rib_Top_{i}", details,
                new Vector3(0f, POD_HEIGHT - 0.09f, z),
                new Vector3(POD_WIDTH * 0.75f, 0.18f, 0.18f), COL_HULL, 0f);
        }

        // Floor edge trim both sides
        MakeBox("FloorTrim_Left", details,
            new Vector3(-POD_WIDTH * 0.5f + 0.08f, 0.06f, POD_LENGTH * 0.5f),
            new Vector3(0.12f, 0.12f, POD_LENGTH), COL_ACCENT, 0f);
        MakeBox("FloorTrim_Right", details,
            new Vector3(POD_WIDTH * 0.5f - 0.08f, 0.06f, POD_LENGTH * 0.5f),
            new Vector3(0.12f, 0.12f, POD_LENGTH), COL_ACCENT, 0f);

        // Warning chevron stripes near front wall
        for (int i = 0; i < 5; i++)
        {
            MakeBox($"Chevron_{i}", details,
                new Vector3(-POD_WIDTH * 0.4f + i * (POD_WIDTH * 0.2f), 0.02f, POD_LENGTH - 0.5f),
                new Vector3(0.25f, 0.03f, 0.6f),
                i % 2 == 0 ? COL_RED_LIGHT * 0.5f : COL_AMBER_LIGHT * 0.3f, 0f);
        }
    }

    static void BuildCeilingPipes(GameObject root)
    {
        GameObject pipes = new GameObject("CeilingPipes");
        pipes.transform.SetParent(root.transform);

        // Three main runs of pipes along the ceiling
        float[] pipeX = { -POD_WIDTH * 0.3f, 0f, POD_WIDTH * 0.3f };
        float[] pipeDiameter = { 0.12f, 0.20f, 0.10f };
        Color[] pipeColors = { COL_PIPE, COL_HULL, COL_RUST };

        for (int p = 0; p < 3; p++)
        {
            // Main pipe run
            GameObject pipe = MakeBox($"Pipe_Main_{p}", pipes,
                new Vector3(pipeX[p], POD_HEIGHT - 0.12f, POD_LENGTH * 0.5f),
                new Vector3(pipeDiameter[p], pipeDiameter[p], POD_LENGTH - 0.3f),
                pipeColors[p], 0f);

            // Pipe clamps at each rib
            int ribCount = 7;
            for (int r = 0; r <= ribCount; r++)
            {
                float z = (r / (float)ribCount) * POD_LENGTH;
                MakeBox($"PipeClamp_{p}_{r}", pipes,
                    new Vector3(pipeX[p], POD_HEIGHT - 0.08f, z),
                    new Vector3(pipeDiameter[p] + 0.08f, 0.1f, 0.1f),
                    COL_ACCENT, 0f);
            }
        }

        // Smaller cross pipes connecting the runs
        for (int i = 0; i < 4; i++)
        {
            float z = 2f + i * 4f;
            MakeBox($"Pipe_Cross_{i}", pipes,
                new Vector3(0f, POD_HEIGHT - 0.15f, z),
                new Vector3(POD_WIDTH * 0.6f, 0.08f, 0.08f),
                COL_PIPE, 0f);
        }

        // The burst pipe for the duct tape emergency — marked with a trigger
        GameObject burstPipe = MakeBox("Pipe_BurstLeak", pipes,
            new Vector3(POD_WIDTH * 0.3f, POD_HEIGHT - 0.12f, POD_LENGTH * 0.35f),
            new Vector3(0.12f, 0.12f, 1.2f),
            COL_RUST, 0f);
        // Add a child trigger marking the leak point
        GameObject leakPoint = new GameObject("LeakTrigger");
        leakPoint.transform.SetParent(burstPipe.transform);
        leakPoint.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        SphereCollider leakCol = leakPoint.AddComponent<SphereCollider>();
        leakCol.radius    = 0.25f;
        leakCol.isTrigger = true;
        // Tag it so TransitEmergencyManager can find it
        leakPoint.name = "LeakTrigger";
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Lighting

    static void BuildLighting(GameObject root)
    {
        GameObject lighting = new GameObject("Lighting");
        lighting.transform.SetParent(root.transform);

        // Ambient is nearly black — all light comes from practicals
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.02f);

        // ── Red emergency strip lights (ceiling, both sides) ──────────────────
        int redCount = 6;
        for (int i = 0; i < redCount; i++)
        {
            float z = (i + 0.5f) * (POD_LENGTH / redCount);

            // Left strip
            AddLight($"RedStrip_L_{i}", lighting,
                new Vector3(-POD_WIDTH * 0.38f, POD_HEIGHT - 0.15f, z),
                LightType.Point, COL_RED_LIGHT, 1.8f, 3.5f);

            // Right strip (offset so they alternate)
            AddLight($"RedStrip_R_{i}", lighting,
                new Vector3(POD_WIDTH * 0.38f, POD_HEIGHT - 0.15f, z + POD_LENGTH / redCount * 0.5f),
                LightType.Point, COL_RED_LIGHT, 1.8f, 3.5f);

            // Small red practical box on wall
            MakeBox($"LightFixture_L_{i}", lighting,
                new Vector3(-POD_WIDTH * 0.42f, POD_HEIGHT - 0.18f, z),
                new Vector3(0.08f, 0.06f, 0.25f), COL_RED_LIGHT * 0.8f, 0f);
            MakeBox($"LightFixture_R_{i}", lighting,
                new Vector3(POD_WIDTH * 0.42f, POD_HEIGHT - 0.18f, z + POD_LENGTH / redCount * 0.5f),
                new Vector3(0.08f, 0.06f, 0.25f), COL_RED_LIGHT * 0.8f, 0f);
        }

        // ── Amber warning light near front (engine room area) ─────────────────
        AddLight("AmberWarning_Front", lighting,
            new Vector3(0f, POD_HEIGHT - 0.3f, POD_LENGTH - 1.5f),
            LightType.Point, COL_AMBER_LIGHT, 2.5f, 5f);
        MakeBox("AmberFixture_Front", lighting,
            new Vector3(0f, POD_HEIGHT - 0.12f, POD_LENGTH - 1.5f),
            new Vector3(0.2f, 0.08f, 0.2f), COL_AMBER_LIGHT * 0.6f, 0f);

        // ── Blue console glow (back half) ─────────────────────────────────────
        AddLight("BlueConsole_Glow", lighting,
            new Vector3(POD_WIDTH * 0.4f, 0.8f, 3.5f),
            LightType.Point, COL_BLUE_LIGHT, 1.2f, 2.5f);

        // ── Directional fill — barely visible, cold tint ──────────────────────
        AddLight("AmbientFill", lighting,
            new Vector3(0f, 10f, 5f),
            LightType.Directional, new Color(0.04f, 0.04f, 0.08f), 0.15f, 0f);
    }

    static void AddLight(string name, GameObject parent,
        Vector3 localPos, LightType type, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        Light l = go.AddComponent<Light>();
        l.type      = type;
        l.color     = color;
        l.intensity = intensity;
        if (type != LightType.Directional) l.range = range;
        l.shadows   = LightShadows.Soft;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Consoles & Fixtures

    static void BuildConsoles(GameObject root)
    {
        GameObject consoles = new GameObject("Consoles");
        consoles.transform.SetParent(root.transform);

        // ── Main engine console (right wall, back half) ───────────────────────
        GameObject mainConsole = MakeBox("Console_Engine", consoles,
            new Vector3(POD_WIDTH * 0.5f - 0.2f, 0.7f, POD_LENGTH - 3f),
            new Vector3(0.3f, 1.0f, 1.8f), COL_PANEL_DARK, 0f);
        // Console screen glow
        MakeBox("Console_Screen", mainConsole,
            new Vector3(-0.16f, 0.2f, 0f),
            new Vector3(0.02f, 0.5f, 1.4f), COL_BLUE_LIGHT * 0.4f, 0f);
        // Buttons row
        for (int b = 0; b < 5; b++)
        {
            Color btnCol = b == 2 ? COL_RED_LIGHT : COL_ACCENT;
            MakeBox($"Btn_{b}", mainConsole,
                new Vector3(-0.16f, -0.25f, -0.6f + b * 0.3f),
                new Vector3(0.04f, 0.06f, 0.08f), btnCol * 0.7f, 0f);
        }

        // ── Secondary panel (left wall, mid) ──────────────────────────────────
        MakeBox("Console_Secondary", consoles,
            new Vector3(-POD_WIDTH * 0.5f + 0.2f, 0.65f, POD_LENGTH * 0.5f),
            new Vector3(0.3f, 0.9f, 1.2f), COL_PANEL_DARK, 0f);

        // ── Fan / vent (for Slop Clog emergency) ─────────────────────────────
        GameObject ventFrame = MakeBox("Vent_FanFrame", consoles,
            new Vector3(0f, POD_HEIGHT - 0.25f, POD_LENGTH * 0.6f),
            new Vector3(0.7f, 0.7f, 0.1f), COL_ACCENT, 0f);
        // Fan blades represented as crossed boxes
        GameObject fan = new GameObject("Fan_Blades");
        fan.transform.SetParent(ventFrame.transform);
        fan.transform.localPosition = new Vector3(0f, 0f, -0.06f);
        MakeBox("Blade_H", fan, Vector3.zero, new Vector3(0.55f, 0.08f, 0.04f), COL_HULL, 0f);
        MakeBox("Blade_V", fan, Vector3.zero, new Vector3(0.08f, 0.55f, 0.04f), COL_HULL, 0f);

        // ── Hull breach wall socket (for Hull Breach emergency) ───────────────
        GameObject breachSocket = MakeBox("HullBreach_Cover", consoles,
            new Vector3(POD_WIDTH * 0.5f - 0.06f, 0.5f, POD_LENGTH * 0.75f),
            new Vector3(0.08f, 0.3f, 0.3f), COL_RUST, 0f);
        // Trigger collider marking the breach
        SphereCollider breachTrigger = breachSocket.AddComponent<SphereCollider>();
        breachTrigger.radius    = 0.2f;
        breachTrigger.isTrigger = true;
        breachSocket.name = "HullBreachPoint";

        // ── Lug nut wall socket ───────────────────────────────────────────────
        MakeBox("LugNut_Socket", consoles,
            new Vector3(-POD_WIDTH * 0.5f + 0.12f, 1.2f, POD_LENGTH * 0.4f),
            new Vector3(0.15f, 0.2f, 0.2f), COL_ACCENT, 0f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Physics Props

    static void BuildPhysicsProps(GameObject root)
    {
        GameObject props = new GameObject("PhysicsProps");
        props.transform.SetParent(root.transform);

        // All props have Rigidbodies with mass proportional to visual size

        // ── Fire extinguisher (1.5kg — handheld) ─────────────────────────────
        GameObject extinguisher = MakePhysicsProp("FireExtinguisher", props,
            new Vector3(-POD_WIDTH * 0.4f, 0.55f, POD_LENGTH * 0.3f),
            new Vector3(0.18f, 0.5f, 0.18f), COL_RED_LIGHT * 0.6f, 1.5f);
        // Handle
        MakeBox("Handle", extinguisher,
            new Vector3(0f, 0.3f, 0.1f), new Vector3(0.05f, 0.1f, 0.05f), COL_ACCENT, 0f);

        // ── Duct tape roll (0.15kg — very light) ─────────────────────────────
        MakePhysicsProp("DuctTape", props,
            new Vector3(0.3f, 0.06f, POD_LENGTH * 0.25f),
            new Vector3(0.15f, 0.07f, 0.15f), new Color(0.5f, 0.5f, 0.4f), 0.15f);

        // ── Engine battery / lug nut (12kg — heavy, bouncy) ──────────────────
        GameObject battery = MakePhysicsProp("EngineBattery_LugNut", props,
            new Vector3(-POD_WIDTH * 0.5f + 0.15f, 1.35f, POD_LENGTH * 0.4f),
            new Vector3(0.22f, 0.28f, 0.18f), new Color(0.2f, 0.2f, 0.25f), 12f);
        battery.GetComponent<Rigidbody>().isKinematic = true; // starts in socket
        // Terminal details
        MakeBox("Terminal_Pos", battery, new Vector3(0.05f, 0.15f, 0f),
            new Vector3(0.04f, 0.06f, 0.04f), new Color(0.8f, 0.6f, 0.1f), 0f);
        MakeBox("Terminal_Neg", battery, new Vector3(-0.05f, 0.15f, 0f),
            new Vector3(0.04f, 0.06f, 0.04f), new Color(0.6f, 0.1f, 0.1f), 0f);

        // ── Space trash / slop prop (0.4kg — light, kickable) ────────────────
        MakePhysicsProp("SlopProp_Trash", props,
            new Vector3(0f, 0.12f, POD_LENGTH * 0.58f),
            new Vector3(0.25f, 0.15f, 0.3f), new Color(0.3f, 0.28f, 0.2f), 0.4f);

        // ── Heavy chair (8kg — can plug hull breach) ──────────────────────────
        BuildChair(props, new Vector3(POD_WIDTH * 0.2f, 0f, POD_LENGTH * 0.5f));

        // ── Loose crate (6kg) ─────────────────────────────────────────────────
        MakePhysicsProp("Crate_A", props,
            new Vector3(-POD_WIDTH * 0.3f, 0.2f, POD_LENGTH * 0.7f),
            new Vector3(0.4f, 0.4f, 0.4f), COL_PANEL_DARK, 6f);

        // ── Small loose crate (3kg) ───────────────────────────────────────────
        MakePhysicsProp("Crate_B", props,
            new Vector3(POD_WIDTH * 0.25f, 0.15f, POD_LENGTH * 0.2f),
            new Vector3(0.3f, 0.3f, 0.3f), COL_ACCENT, 3f);

        // ── Tool wrench (0.8kg) ───────────────────────────────────────────────
        GameObject wrench = MakePhysicsProp("Wrench", props,
            new Vector3(0.1f, 0.06f, POD_LENGTH * 0.45f),
            new Vector3(0.05f, 0.04f, 0.35f), new Color(0.4f, 0.4f, 0.42f), 0.8f);
        MakeBox("WrenchHead", wrench, new Vector3(0f, 0f, 0.17f),
            new Vector3(0.12f, 0.06f, 0.07f), new Color(0.4f, 0.4f, 0.42f), 0f);

        // ── Loose panel fragment (2kg) ────────────────────────────────────────
        MakePhysicsProp("LoosePanel", props,
            new Vector3(-0.5f, 0.02f, POD_LENGTH * 0.8f),
            new Vector3(0.6f, 0.04f, 0.4f), COL_ACCENT, 2f);

        // ── Oxygen canister (1.2kg) ───────────────────────────────────────────
        MakePhysicsProp("OxygenCanister", props,
            new Vector3(POD_WIDTH * 0.35f, 0.22f, POD_LENGTH * 0.15f),
            new Vector3(0.12f, 0.42f, 0.12f), new Color(0.1f, 0.3f, 0.5f), 1.2f);

        // ── Data pad (0.08kg — very light, floats dramatically in zero-g) ────
        MakePhysicsProp("DataPad", props,
            new Vector3(-0.2f, 0.06f, POD_LENGTH * 0.35f),
            new Vector3(0.18f, 0.02f, 0.25f), new Color(0.05f, 0.05f, 0.08f), 0.08f);
    }

    /// <summary>Builds a chair made of multiple boxes — 8kg total.</summary>
    static void BuildChair(GameObject parent, Vector3 pos)
    {
        GameObject chair = new GameObject("Chair");
        chair.transform.SetParent(parent.transform);
        chair.transform.localPosition = pos;

        // Add single Rigidbody to the root
        Rigidbody rb = chair.AddComponent<Rigidbody>();
        rb.mass = 8f;
        rb.linearDamping  = 0.3f;
        rb.angularDamping = 0.5f;

        // Seat
        MakeBox("Seat", chair, new Vector3(0f, 0.45f, 0f),
            new Vector3(0.5f, 0.06f, 0.5f), COL_PANEL_DARK, 0f);
        // Back
        MakeBox("Back", chair, new Vector3(0f, 0.82f, -0.22f),
            new Vector3(0.5f, 0.72f, 0.06f), COL_PANEL_DARK, 0f);
        // Legs
        float[] lx = { -0.2f,  0.2f, -0.2f,  0.2f };
        float[] lz = { -0.2f, -0.2f,  0.2f,  0.2f };
        for (int i = 0; i < 4; i++)
            MakeBox($"Leg_{i}", chair, new Vector3(lx[i], 0.22f, lz[i]),
                new Vector3(0.05f, 0.44f, 0.05f), COL_ACCENT, 0f);

        // Compound colliders
        BoxCollider seatCol = chair.AddComponent<BoxCollider>();
        seatCol.center = new Vector3(0f, 0.45f, 0f);
        seatCol.size   = new Vector3(0.5f, 0.06f, 0.5f);
        BoxCollider backCol = chair.AddComponent<BoxCollider>();
        backCol.center = new Vector3(0f, 0.82f, -0.22f);
        backCol.size   = new Vector3(0.5f, 0.72f, 0.06f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Emergency Fixtures

    /// <summary>
    /// Builds the non-physics emergency fixtures — fire console glow box,
    /// pipe steam origin marker, gravity warning sign.
    /// </summary>
    static void BuildEmergencyFixtures(GameObject root)
    {
        GameObject fixtures = new GameObject("EmergencyFixtures");
        fixtures.transform.SetParent(root.transform);

        // ── Fire console (Engine Grease Fire) ─────────────────────────────────
        GameObject fireConsole = MakeBox("FireConsole", fixtures,
            new Vector3(POD_WIDTH * 0.45f, 0.55f, POD_LENGTH - 2f),
            new Vector3(0.25f, 0.7f, 0.9f), COL_PANEL_DARK, 0f);
        MakeBox("FireConsole_Screen", fireConsole,
            new Vector3(-0.14f, 0.1f, 0f),
            new Vector3(0.02f, 0.4f, 0.7f), COL_AMBER_LIGHT * 0.3f, 0f);

        // ── Gravity warning sign ──────────────────────────────────────────────
        MakeBox("GravityWarningSign", fixtures,
            new Vector3(0f, POD_HEIGHT - 0.35f, POD_LENGTH * 0.5f),
            new Vector3(0.6f, 0.25f, 0.04f),
            new Color(0.8f, 0.6f, 0f) * 0.4f, 0f);

        // ── Hazard stripes near hull breach area ──────────────────────────────
        for (int i = 0; i < 3; i++)
        {
            MakeBox($"HazardStripe_{i}", fixtures,
                new Vector3(POD_WIDTH * 0.45f, 0.35f + i * 0.15f, POD_LENGTH * 0.75f),
                new Vector3(0.04f, 0.08f, 0.25f),
                i % 2 == 0 ? COL_AMBER_LIGHT * 0.4f : COL_HULL, 0f);
        }

        // ── Damage rust streaks on walls (atmospheric) ────────────────────────
        MakeBox("RustStreak_L1", fixtures,
            new Vector3(-POD_WIDTH * 0.48f, 1.4f, POD_LENGTH * 0.45f),
            new Vector3(0.04f, 0.6f, 0.18f), COL_RUST * 0.5f, 0f);
        MakeBox("RustStreak_R1", fixtures,
            new Vector3(POD_WIDTH * 0.48f, 0.9f, POD_LENGTH * 0.65f),
            new Vector3(0.04f, 0.4f, 0.12f), COL_RUST * 0.4f, 0f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Camera & Player Spawn

    static void BuildCamera(GameObject root)
    {
        // Player spawn marker
        GameObject playerSpawn = new GameObject("PlayerSpawnPoint");
        playerSpawn.transform.SetParent(root.transform);
        playerSpawn.transform.localPosition = new Vector3(0f, 0.9f, 1.5f);
        playerSpawn.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        // Placeholder camera (player camera will attach here at runtime)
        GameObject camGO = new GameObject("PodCamera_Placeholder");
        camGO.transform.SetParent(playerSpawn.transform);
        camGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        Camera cam = camGO.AddComponent<Camera>();
        cam.fieldOfView   = 75f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane  = 30f;
        cam.backgroundColor = new Color(0.02f, 0.01f, 0.01f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Environment

    static void SetupEnvironment()
    {
        // Fog — thick, claustrophobic, red-tinted
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.ExponentialSquared;
        RenderSettings.fogColor     = new Color(0.06f, 0.01f, 0.01f);
        RenderSettings.fogDensity   = 0.08f;

        // Skybox off — solid dark colour behind everything
        RenderSettings.skybox       = null;
        RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.02f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Factory Helpers

    /// <summary>
    /// Creates a primitive cube with a material, collider, and optional static flag.
    /// Mass = 0 means no Rigidbody (static geometry).
    /// </summary>
    static GameObject MakeBox(string name, GameObject parent,
        Vector3 localPos, Vector3 size, Color color, float mass)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;

        // Apply material colour
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;

            // Metallic / roughness settings for sci-fi industrial look
            mat.SetFloat("_Metallic",   0.75f);
            mat.SetFloat("_Glossiness", 0.25f); // rough metal — not shiny

            // Emissive for light fixtures and glowing elements
            if (color.r > 0.4f || color.g > 0.4f || color.b > 0.4f)
            {
                // Bright colours get a subtle emissive so they glow against darkness
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.15f);
            }

            r.sharedMaterial = mat;
        }

        // Add Rigidbody if mass > 0
        if (mass > 0f)
        {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass           = mass;
            rb.linearDamping  = Mathf.Lerp(0.5f, 0.05f, Mathf.Clamp01(mass / 10f));
            rb.angularDamping = 0.3f;
        }
        else
        {
            // Static — mark as static for batching
            go.isStatic = true;
        }

        return go;
    }

    /// <summary>
    /// Creates a physics prop with a Rigidbody. Mass drives damping:
    /// lighter objects bounce more, heavier objects settle faster.
    /// </summary>
    static GameObject MakePhysicsProp(string name, GameObject parent,
        Vector3 localPos, Vector3 size, Color color, float mass)
    {
        GameObject go = MakeBox(name, parent, localPos, size, color, mass);

        // Physics material — light objects are bouncier
        PhysicsMaterial pm = new PhysicsMaterial(name + "_PhysMat");
        pm.bounciness         = Mathf.Lerp(0.5f, 0.1f, Mathf.Clamp01(mass / 15f));
        pm.dynamicFriction    = Mathf.Lerp(0.2f, 0.6f, Mathf.Clamp01(mass / 15f));
        pm.staticFriction     = pm.dynamicFriction + 0.1f;
        pm.bounceCombine      = PhysicsMaterialCombine.Average;
        pm.frictionCombine    = PhysicsMaterialCombine.Average;

        Collider col = go.GetComponent<Collider>();
        if (col != null) col.material = pm;

        return go;
    }

    #endregion
}
#endif
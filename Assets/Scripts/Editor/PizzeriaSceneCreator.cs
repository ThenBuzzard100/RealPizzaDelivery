#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// One-click builder: creates a single detailed scene with the full pizzeria,
/// swinging kitchen doors, and a smooth ~60s intro camera path.
///
/// Access: Tools → Pizzeria → Build Everything
/// </summary>
public class PizzeriaFullBuilder : EditorWindow
{
    private float introDuration = 60f;
    private float rotationSmooth = 4f;
    private float lookAhead = 0.04f;
    private bool loopIntro = false;
    private Vector2 scrollPos;
    private string status = "";
    private MessageType statusType = MessageType.None;

    // Material cache
    private Dictionary<string, Material> mats = new Dictionary<string, Material>();
    private Shader shader;

    [MenuItem("Tools/Pizzeria/Build Everything")]
    public static void ShowWindow()
    {
        var w = GetWindow<PizzeriaFullBuilder>("🍕 Build Everything");
        w.minSize = new Vector2(380, 420);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(10);
        GUILayout.Label("🍕 Pizzeria — Build Everything", new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 15, alignment = TextAnchor.MiddleCenter });
        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Builds ONE detailed scene:\n\n" +
            "• Lobby — counter, tables, chairs, booths, decorations\n" +
            "• Kitchen — oven, prep, fridge, swinging doors\n" +
            "• Street — road, buildings with windows/doors/awnings\n\n" +
            "Smooth ~60s intro camera. Hit Play to watch.",
            MessageType.Info);

        EditorGUILayout.Space(10);
        GUILayout.Label("Intro Camera", EditorStyles.boldLabel);
        introDuration = EditorGUILayout.Slider("Duration (seconds)", introDuration, 10f, 180f);
        rotationSmooth = EditorGUILayout.Slider("Rotation Smoothing", rotationSmooth, 0.5f, 20f);
        lookAhead = EditorGUILayout.Slider("Look Ahead", lookAhead, 0.01f, 0.15f);
        loopIntro = EditorGUILayout.Toggle("Loop", loopIntro);

        EditorGUILayout.Space(16);
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.3f);
        if (GUILayout.Button("BUILD EVERYTHING", GUILayout.Height(40)))
            Build();
        GUI.backgroundColor = oldBg;

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(status, statusType);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    // ════════════════════════════════════════════════════════════
    private void Build()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string folder = "Assets/Scenes";
        EnsureFolder(folder);
        string matFolder = "Assets/Materials";
        EnsureFolder(matFolder);
        string path = $"{folder}/PizzeriaWorld.unity";

        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            CreateMaterials(matFolder);
            BuildLobby();
            BuildKitchen();
            BuildKitchenDoors();
            BuildStreet();
            BuildIntroCamera();
            BuildLighting();

            EditorSceneManager.SaveScene(scene, path);

            var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in existing) if (s.path == path) { found = true; break; }
            if (!found) existing.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = existing.ToArray();

            EditorSceneManager.OpenScene(path);

            status = "✅ Scene built! Hit Play to watch the intro.\nAssets/Scenes/PizzeriaWorld.unity";
            statusType = MessageType.Info;
        }
        catch (System.Exception ex)
        {
            status = $"❌ {ex.Message}";
            statusType = MessageType.Error;
            Debug.LogException(ex);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  MATERIALS
    // ════════════════════════════════════════════════════════════

    private void CreateMaterials(string folder)
    {
        shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");

        mats.Clear();
        // Lobby
        Mat(folder, "LobbyFloor",     new Color(0.55f, 0.35f, 0.22f));
        Mat(folder, "LobbyCeiling",   new Color(0.92f, 0.90f, 0.85f));
        Mat(folder, "LobbyWall",      new Color(0.95f, 0.88f, 0.72f));
        Mat(folder, "Counter",        new Color(0.45f, 0.28f, 0.15f));
        Mat(folder, "CounterTop",     new Color(0.30f, 0.30f, 0.30f));
        Mat(folder, "Table",          new Color(0.60f, 0.40f, 0.22f));
        Mat(folder, "Chair",          new Color(0.35f, 0.20f, 0.10f));
        Mat(folder, "ChairSeat",      new Color(0.65f, 0.12f, 0.12f));
        Mat(folder, "BoothSeat",      new Color(0.60f, 0.10f, 0.10f));
        Mat(folder, "MenuBoard",      new Color(0.12f, 0.12f, 0.12f));
        Mat(folder, "MenuScreen",     new Color(0.9f, 0.6f, 0.1f)); // High attention bright orange
        Mat(folder, "LightBulb",      new Color(1f, 1f, 1f));
        Mat(folder, "CashRegister",   new Color(0.25f, 0.25f, 0.28f));
        Mat(folder, "TipJar",         new Color(0.7f, 0.85f, 0.9f));
        Mat(folder, "Plant",          new Color(0.20f, 0.50f, 0.15f));
        Mat(folder, "Pot",            new Color(0.55f, 0.30f, 0.18f));
        Mat(folder, "PictureFrame",   new Color(0.40f, 0.30f, 0.18f));
        Mat(folder, "WelcomeMat",     new Color(0.35f, 0.15f, 0.10f));
        Mat(folder, "NapkinDispenser",new Color(0.80f, 0.80f, 0.82f));
        Mat(folder, "SaltPepper",     new Color(0.90f, 0.90f, 0.85f));
        Mat(folder, "DrinkStation",   new Color(0.30f, 0.30f, 0.32f));
        Mat(folder, "GlassCase",      new Color(0.75f, 0.88f, 0.92f));
        // Kitchen
        Mat(folder, "KitchenFloor",   new Color(0.78f, 0.78f, 0.78f));
        Mat(folder, "KitchenWall",    new Color(0.95f, 0.95f, 0.93f));
        Mat(folder, "KitchenCeiling", new Color(0.95f, 0.95f, 0.93f));
        Mat(folder, "Oven",           new Color(0.20f, 0.20f, 0.22f));
        Mat(folder, "OvenMouth",      new Color(0.85f, 0.30f, 0.05f));
        Mat(folder, "PrepTable",      new Color(0.80f, 0.80f, 0.82f));
        Mat(folder, "Dough",          new Color(0.95f, 0.90f, 0.75f));
        Mat(folder, "CuttingBoard",   new Color(0.55f, 0.40f, 0.20f));
        Mat(folder, "Shelf",          new Color(0.50f, 0.35f, 0.20f));
        Mat(folder, "Fridge",         new Color(0.85f, 0.85f, 0.88f));
        Mat(folder, "Sink",           new Color(0.75f, 0.75f, 0.78f));
        Mat(folder, "PizzaBox",       new Color(0.82f, 0.68f, 0.45f));
        Mat(folder, "PotRack",        new Color(0.30f, 0.30f, 0.32f));
        Mat(folder, "FireExt",        new Color(0.80f, 0.10f, 0.08f));
        Mat(folder, "TrashCan",       new Color(0.35f, 0.35f, 0.38f));
        Mat(folder, "SwingDoor",      new Color(0.50f, 0.35f, 0.20f));
        Mat(folder, "DoorWindow",     new Color(0.65f, 0.80f, 0.85f));
        // Street
        Mat(folder, "Ground",         new Color(0.30f, 0.42f, 0.18f));
        Mat(folder, "Road",           new Color(0.22f, 0.22f, 0.25f));
        Mat(folder, "Sidewalk",       new Color(0.65f, 0.63f, 0.60f));
        Mat(folder, "LaneMark",       new Color(0.95f, 0.90f, 0.20f));
        Mat(folder, "Building1",      new Color(0.70f, 0.55f, 0.45f));
        Mat(folder, "Building2",      new Color(0.55f, 0.50f, 0.50f));
        Mat(folder, "Building3",      new Color(0.60f, 0.42f, 0.35f));
        Mat(folder, "BuildingTrim",   new Color(0.40f, 0.38f, 0.35f));
        Mat(folder, "Window",         new Color(0.55f, 0.72f, 0.85f));
        Mat(folder, "WindowFrame",    new Color(0.35f, 0.33f, 0.30f));
        Mat(folder, "Awning",         new Color(0.70f, 0.15f, 0.10f));
        Mat(folder, "BldgDoor",       new Color(0.35f, 0.22f, 0.12f));
        Mat(folder, "House",          new Color(0.85f, 0.82f, 0.70f));
        Mat(folder, "Roof",           new Color(0.45f, 0.18f, 0.12f));
        Mat(folder, "Door",           new Color(0.40f, 0.25f, 0.12f));
        Mat(folder, "Facade",         new Color(0.75f, 0.15f, 0.12f));
        Mat(folder, "Sign",           new Color(0.90f, 0.85f, 0.20f));
        Mat(folder, "Pole",           new Color(0.30f, 0.30f, 0.32f));
        Mat(folder, "Bench",          new Color(0.45f, 0.30f, 0.18f));
        Mat(folder, "Hydrant",        new Color(0.80f, 0.15f, 0.08f));
        Mat(folder, "Mailbox",        new Color(0.15f, 0.20f, 0.55f));
        Mat(folder, "Crosswalk",      new Color(0.95f, 0.95f, 0.95f));
        Mat(folder, "Tree",           new Color(0.25f, 0.45f, 0.15f));
        Mat(folder, "Trunk",          new Color(0.40f, 0.28f, 0.15f));
        Mat(folder, "ACUnit",         new Color(0.60f, 0.60f, 0.62f));
    }

    private void Mat(string folder, string name, Color color)
    {
        string path = $"{folder}/Mat_{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        mats[name] = mat;
    }

    private Material M(string name) => mats.ContainsKey(name) ? mats[name] : null;

    // ════════════════════════════════════════════════════════════
    //  LOBBY (Z = 0 to 20, X = -10 to 10)
    // ════════════════════════════════════════════════════════════

    private void BuildLobby()
    {
        var p = new GameObject("--- LOBBY ---");

        // Floor & ceiling
        C("Lobby_Floor", V(0, -0.05f, 10), V(20, 0.1f, 20), "LobbyFloor", p);
        C("Lobby_Ceiling", V(0, 4, 10), V(20, 0.15f, 20), "LobbyCeiling", p);

        // ── WALLS ──
        // Front wall
        C("Wall_Front", V(0, 2, 20), V(20, 4, 0.3f), "LobbyWall", p);
        // Right wall
        C("Wall_Right", V(10, 2, 10), V(0.3f, 4, 20), "LobbyWall", p);
        // Back wall (gap Z = -2 to 2 center for street entrance)
        C("Wall_Back_L", V(-7, 2, 0), V(6, 4, 0.3f), "LobbyWall", p);
        C("Wall_Back_R", V(7, 2, 0), V(6, 4, 0.3f), "LobbyWall", p);
        // Left wall — split with kitchen doorway (gap Z = 9 to 15)
        C("Wall_Left_A", V(-10, 2, 4.5f), V(0.3f, 4, 9f), "LobbyWall", p);
        C("Wall_Left_Above", V(-10, 3.5f, 12), V(0.3f, 1, 6), "LobbyWall", p);
        C("Wall_Left_B", V(-10, 2, 17.5f), V(0.3f, 4, 5), "LobbyWall", p);

        // ── COUNTER AREA ──
        C("Counter_Base", V(0, 0.45f, 16), V(8, 0.9f, 1.5f), "Counter", p);
        C("Counter_Top", V(0, 0.95f, 16), V(8.1f, 0.1f, 1.6f), "CounterTop", p);
        C("CashRegister", V(2.5f, 1.25f, 16), V(0.6f, 0.5f, 0.5f), "CashRegister", p);
        C("CashRegister_Screen", V(2.5f, 1.55f, 15.85f), V(0.4f, 0.15f, 0.05f), "MenuBoard", p);
        C("TipJar", V(1, 1.15f, 15.5f), V(0.25f, 0.35f, 0.25f), "TipJar", p);

        // Glass display case
        C("DisplayCase_Base", V(-2.5f, 0.5f, 15.5f), V(2.5f, 1, 0.8f), "CounterTop", p);
        C("DisplayCase_Glass", V(-2.5f, 1.15f, 15.5f), V(2.4f, 0.6f, 0.7f), "GlassCase", p);

        // Menu board (High attention)
        C("MenuBoard_Backing", V(0, 3.3f, 19.85f), V(6.5f, 1.8f, 0.1f), "CounterTop", p);
        C("MenuBoard_Frame", V(0, 3.3f, 19.82f), V(6.7f, 2f, 0.05f), "MenuScreen", p); // Bright glowing trim
        // 3 screen panels
        C("MenuScreen_1", V(-2f, 3.3f, 19.83f), V(1.9f, 1.6f, 0.05f), "MenuScreen", p);
        C("MenuScreen_2", V(0f, 3.3f, 19.83f), V(1.9f, 1.6f, 0.05f), "MenuScreen", p);
        C("MenuScreen_3", V(2f, 3.3f, 19.83f), V(1.9f, 1.6f, 0.05f), "MenuScreen", p);

        // ── TABLES & CHAIRS ──
        BuildTable(V(-6f, 0, 5), p);       // Left front
        BuildTable(V(-6f, 0, 11), p);      // Left back
        BuildTable(V(5.5f, 0, 13), p);     // Right side (past the booths)
        BuildTable(V(-1.5f, 0, 8), p);     // Center-left

        // ── BOOTH SEATING (along right wall) ──
        C("Booth_Back_1", V(8.8f, 0.6f, 4), V(0.6f, 1.2f, 3), "BoothSeat", p);
        C("Booth_Seat_1", V(8, 0.3f, 4), V(1.2f, 0.6f, 3), "BoothSeat", p);
        C("Booth_Table_1", V(6.5f, 0.4f, 4), V(1.2f, 0.8f, 2.5f), "Table", p);

        C("Booth_Back_2", V(8.8f, 0.6f, 8), V(0.6f, 1.2f, 3), "BoothSeat", p);
        C("Booth_Seat_2", V(8, 0.3f, 8), V(1.2f, 0.6f, 3), "BoothSeat", p);
        C("Booth_Table_2", V(6.5f, 0.4f, 8), V(1.2f, 0.8f, 2.5f), "Table", p);

        // ── DECORATIONS ──
        // Welcome mat
        C("WelcomeMat", V(0, 0.01f, 1), V(2, 0.02f, 1.2f), "WelcomeMat", p);

        // Potted plants in corners
        BuildPlant(V(8.5f, 0, 18.5f), p, "Plant_1");
        BuildPlant(V(-8.5f, 0, 18.5f), p, "Plant_2");
        BuildPlant(V(8.5f, 0, 1), p, "Plant_3");

        // Picture frames on walls
        C("Picture_1", V(9.85f, 2.5f, 6), V(0.05f, 0.8f, 1.2f), "PictureFrame", p);
        C("Picture_2", V(9.85f, 2.5f, 12), V(0.05f, 1, 0.8f), "PictureFrame", p);
        C("Picture_3", V(-3, 2.5f, 19.85f), V(1, 0.8f, 0.05f), "PictureFrame", p);

        // Drink station (left wall near entrance)
        C("DrinkStation_Base", V(-8.5f, 0.5f, 2), V(2, 1, 1), "DrinkStation", p);
        C("DrinkStation_Top", V(-8.5f, 1.05f, 2), V(2.1f, 0.1f, 1.1f), "PrepTable", p);
        C("Cups_Stack", V(-8.5f, 1.3f, 2), V(0.3f, 0.4f, 0.3f), "TipJar", p);

        // Hanging pendant light fixtures (decorative geometry)
        C("Pendant_1", V(-4, 3.5f, 7), V(0.05f, 0.5f, 0.05f), "Pole", p);
        C("Pendant_1_Shade", V(-4, 3.2f, 7), V(0.5f, 0.2f, 0.5f), "CounterTop", p);
        C("Pendant_2", V(4, 3.5f, 7), V(0.05f, 0.5f, 0.05f), "Pole", p);
        C("Pendant_2_Shade", V(4, 3.2f, 7), V(0.5f, 0.2f, 0.5f), "CounterTop", p);
    }

    private void BuildTable(Vector3 center, GameObject parent)
    {
        string id = $"{center.x:F0}_{center.z:F0}";
        var root = new GameObject($"TableGroup_{id}");
        root.transform.parent = parent.transform;
        root.transform.position = center;

        // Table top
        C($"Table", V(0, 0.4f, 0), V(1.8f, 0.08f, 1.8f), "Table", root);
        // Table leg
        C($"TableLeg", V(0, 0.2f, 0), V(0.15f, 0.4f, 0.15f), "Counter", root);
        
        // 4 chairs (properly rotated)
        float off = 1.2f;
        BuildChair(V(-off, 0, 0), 90f, root, $"Chair_L");
        BuildChair(V(off, 0, 0), -90f, root, $"Chair_R");
        BuildChair(V(0, 0, -off), 0f, root, $"Chair_B");
        BuildChair(V(0, 0, off), 180f, root, $"Chair_F");
        
        // Napkin dispenser
        C($"Napkin", V(0.5f, 0.48f, 0), V(0.15f, 0.12f, 0.08f), "NapkinDispenser", root);
        // Salt & pepper
        C($"Salt", V(-0.3f, 0.5f, 0.3f), V(0.06f, 0.12f, 0.06f), "SaltPepper", root);
        C($"Pepper", V(-0.15f, 0.5f, 0.3f), V(0.06f, 0.12f, 0.06f), "CounterTop", root);
    }

    private void BuildChair(Vector3 localPos, float rotY, GameObject parent, string name)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = localPos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        C("Seat", V(0, 0.25f, 0), V(0.5f, 0.05f, 0.5f), "Chair", root);
        C("Back", V(0, 0.45f, -0.2f), V(0.5f, 0.35f, 0.05f), "Chair", root);
        
        // 4 legs for the chair
        C("LegFL", V(-0.2f, 0.12f, 0.2f), V(0.05f, 0.25f, 0.05f), "Chair", root);
        C("LegFR", V(0.2f, 0.12f, 0.2f), V(0.05f, 0.25f, 0.05f), "Chair", root);
        C("LegBL", V(-0.2f, 0.12f, -0.2f), V(0.05f, 0.25f, 0.05f), "Chair", root);
        C("LegBR", V(0.2f, 0.12f, -0.2f), V(0.05f, 0.25f, 0.05f), "Chair", root);
    }

    private void BuildPlant(Vector3 pos, GameObject parent, string name)
    {
        C(name + "_Pot", V(pos.x, 0.2f, pos.z), V(0.5f, 0.4f, 0.5f), "Pot", parent);
        C(name + "_Plant", V(pos.x, 0.6f, pos.z), V(0.6f, 0.5f, 0.6f), "Plant", parent);
    }

    // ════════════════════════════════════════════════════════════
    //  KITCHEN (X = -22 to -10, Z = 6 to 18)
    // ════════════════════════════════════════════════════════════

    private void BuildKitchen()
    {
        var p = new GameObject("--- KITCHEN ---");
        float cx = -16f, cz = 12f;

        // Floor & ceiling (offset Y to avoid Z-fight)
        C("Kitchen_Floor", V(cx, -0.06f, cz), V(12, 0.1f, 12), "KitchenFloor", p);
        C("Kitchen_Ceiling", V(cx, 3.99f, cz), V(12, 0.15f, 12), "KitchenCeiling", p);

        // Walls (NO right wall — lobby left wall is the boundary with doorway)
        C("K_Wall_Front", V(cx, 2, 18), V(12, 4, 0.3f), "KitchenWall", p);
        C("K_Wall_Back", V(cx, 2, 6), V(12, 4, 0.3f), "KitchenWall", p);
        C("K_Wall_Left", V(-22, 2, cz), V(0.3f, 4, 12), "KitchenWall", p);

        // ── PIZZA OVEN ──
        C("Oven_Body", V(cx, 0.75f, 17.2f), V(3, 1.5f, 1.5f), "Oven", p);
        C("Oven_Hood", V(cx, 1.8f, 17.2f), V(3.4f, 0.4f, 1.8f), "Oven", p);
        C("Oven_Mouth", V(cx, 0.6f, 16.35f), V(1.6f, 0.7f, 0.1f), "OvenMouth", p);
        C("Oven_Shelf", V(cx, 0.2f, 16.3f), V(2, 0.05f, 0.3f), "PrepTable", p);

        // Pizza paddle leaning against oven
        C("PizzaPaddle_Handle", V(cx + 2, 0.8f, 17.5f), V(0.08f, 1.5f, 0.08f), "Table", p);
        C("PizzaPaddle_Head", V(cx + 2, 1.6f, 17.5f), V(0.5f, 0.03f, 0.6f), "Table", p);

        // ── PREP TABLE ──
        C("PrepTable_Top", V(cx - 2, 0.9f, cz), V(3.5f, 0.08f, 2), "PrepTable", p);
        C("PrepTable_LegA", V(cx - 3.5f, 0.45f, cz - 0.8f), V(0.08f, 0.9f, 0.08f), "PrepTable", p);
        C("PrepTable_LegB", V(cx - 0.5f, 0.45f, cz - 0.8f), V(0.08f, 0.9f, 0.08f), "PrepTable", p);
        C("PrepTable_LegC", V(cx - 3.5f, 0.45f, cz + 0.8f), V(0.08f, 0.9f, 0.08f), "PrepTable", p);
        C("PrepTable_LegD", V(cx - 0.5f, 0.45f, cz + 0.8f), V(0.08f, 0.9f, 0.08f), "PrepTable", p);
        C("PrepTable_Shelf", V(cx - 2, 0.3f, cz), V(3.3f, 0.05f, 1.8f), "PrepTable", p);

        // Prep items
        C("DoughBall", V(cx - 2, 0.97f, cz), V(0.5f, 0.12f, 0.5f), "Dough", p);
        C("CuttingBoard", V(cx - 1, 0.97f, cz + 0.5f), V(0.6f, 0.03f, 0.4f), "CuttingBoard", p);
        C("CuttingBoard_2", V(cx - 3, 0.97f, cz - 0.5f), V(0.5f, 0.03f, 0.35f), "CuttingBoard", p);

        // ── INGREDIENT SHELF ──
        C("Shelf_Frame", V(-21.3f, 1, 9), V(1, 2, 3), "Shelf", p);
        C("Shelf_1", V(-21.3f, 0.5f, 9), V(0.9f, 0.05f, 2.8f), "Shelf", p);
        C("Shelf_2", V(-21.3f, 1.0f, 9), V(0.9f, 0.05f, 2.8f), "Shelf", p);
        C("Shelf_3", V(-21.3f, 1.5f, 9), V(0.9f, 0.05f, 2.8f), "Shelf", p);
        // Jars/containers on shelves
        for (int i = 0; i < 4; i++)
        {
            float z = 7.8f + i * 0.7f;
            C($"Jar_{i}", V(-21.3f, 0.65f, z), V(0.2f, 0.25f, 0.2f), "TipJar", p);
            C($"Jar2_{i}", V(-21.3f, 1.15f, z), V(0.18f, 0.22f, 0.18f), "GlassCase", p);
        }

        // ── FRIDGE ──
        C("Fridge_Body", V(-21.3f, 1.1f, 15), V(1.4f, 2.2f, 1.4f), "Fridge", p);
        C("Fridge_Handle", V(-20.55f, 1.2f, 15), V(0.05f, 0.6f, 0.05f), "Pole", p);
        C("Fridge_Top", V(-21.3f, 2.25f, 15), V(1.5f, 0.1f, 1.5f), "Fridge", p);

        // ── SINK ──
        C("Sink_Cabinet", V(cx + 3, 0.4f, 17.3f), V(1.8f, 0.8f, 1), "KitchenWall", p);
        C("Sink_Basin", V(cx + 3, 0.85f, 17.3f), V(1.5f, 0.2f, 0.8f), "Sink", p);
        C("Sink_Faucet", V(cx + 3, 1.15f, 17.6f), V(0.05f, 0.4f, 0.05f), "Pole", p);

        // ── PIZZA BOX STACK ──
        for (int i = 0; i < 5; i++)
        {
            C($"PizzaBox_{i}", V(-12, 0.08f + i * 0.12f, 8), V(0.8f, 0.1f, 0.8f), "PizzaBox", p);
        }
        // Second smaller stack
        for (int i = 0; i < 3; i++)
        {
            C($"PizzaBox2_{i}", V(-12.5f, 0.08f + i * 0.12f, 7.3f), V(0.65f, 0.1f, 0.65f), "PizzaBox", p);
        }

        // ── HANGING POT RACK ──
        C("PotRack_Bar", V(cx, 3.2f, 13), V(3, 0.08f, 0.08f), "PotRack", p);
        C("PotRack_Chain_L", V(cx - 1.3f, 3.6f, 13), V(0.03f, 0.8f, 0.03f), "PotRack", p);
        C("PotRack_Chain_R", V(cx + 1.3f, 3.6f, 13), V(0.03f, 0.8f, 0.03f), "PotRack", p);
        // Hanging pots
        C("Pot_1", V(cx - 0.8f, 2.9f, 13), V(0.35f, 0.2f, 0.35f), "PotRack", p);
        C("Pot_2", V(cx, 2.85f, 13), V(0.4f, 0.22f, 0.4f), "PotRack", p);
        C("Pot_3", V(cx + 0.8f, 2.9f, 13), V(0.3f, 0.18f, 0.3f), "PotRack", p);

        // ── FIRE EXTINGUISHER ──
        C("FireExt", V(-21.5f, 1, 17), V(0.2f, 0.5f, 0.2f), "FireExt", p);

        // ── TRASH CAN ──
        C("TrashCan", V(-12, 0.35f, 17), V(0.6f, 0.7f, 0.6f), "TrashCan", p);

        // ── WALL CLOCK (on back wall) ──
        C("Clock_Face", V(cx, 3, 6.15f), V(0.6f, 0.6f, 0.05f), "SaltPepper", p);
        C("Clock_Rim", V(cx, 3, 6.12f), V(0.7f, 0.7f, 0.03f), "CounterTop", p);
    }

    // ════════════════════════════════════════════════════════════
    //  SWINGING KITCHEN DOORS
    // ════════════════════════════════════════════════════════════

    private void BuildKitchenDoors()
    {
        var p = new GameObject("--- KITCHEN DOORS ---");

        // Doorway is at X = -10, Z = 9 to 15 (6 units wide, split into two 3-unit doors)
        float doorWidth = 2.8f;
        float doorHeight = 2.8f;
        float doorThickness = 0.1f;
        float doorY = doorHeight / 2f;
        float doorwayCenter = 12f; // center Z of doorway

        // Trim border for the doorway
        C("KitchenDoor_Trim_Top", V(-10f, 3f, doorwayCenter), V(0.4f, 0.2f, 6.2f), "BuildingTrim", p);
        C("KitchenDoor_Trim_L", V(-10f, 1.5f, doorwayCenter - 3.1f), V(0.4f, 3.2f, 0.2f), "BuildingTrim", p);
        C("KitchenDoor_Trim_R", V(-10f, 1.5f, doorwayCenter + 3.1f), V(0.4f, 3.2f, 0.2f), "BuildingTrim", p);

        // ── LEFT DOOR (swings into kitchen) ──
        // Hinge pivot at Z = 9 side
        var leftPivot = new GameObject("KitchenDoor_Left_Pivot");
        leftPivot.transform.parent = p.transform;
        leftPivot.transform.position = V(-10, 0, doorwayCenter - 3f);

        var leftDoor = C("KitchenDoor_Left", V(0, doorY, doorWidth / 2f),
            V(doorThickness, doorHeight, doorWidth), "SwingDoor", leftPivot);
        leftDoor.transform.localPosition = V(0, doorY, doorWidth / 2f);

        // Door window (round porthole effect)
        var leftWindow = C("KitchenDoor_Left_Window", V(0, 0, 0), V(0.12f, 0.5f, 0.5f), "DoorWindow", leftDoor);
        leftWindow.transform.localPosition = V(0, 0.3f, 0);

        // Add trigger and swing script to pivot
        var leftTrigger = leftPivot.AddComponent<BoxCollider>();
        leftTrigger.isTrigger = true;
        leftTrigger.center = V(0, 1.5f, 1.5f);
        leftTrigger.size = V(2, 3, 4);
        var leftSwing = leftPivot.AddComponent<SwingingDoor>();
        SetSwingDoor(leftSwing, 85f, 200f, 2.5f);

        // ── RIGHT DOOR (swings into kitchen) ──
        var rightPivot = new GameObject("KitchenDoor_Right_Pivot");
        rightPivot.transform.parent = p.transform;
        rightPivot.transform.position = V(-10, 0, doorwayCenter + 3f);

        var rightDoor = C("KitchenDoor_Right", V(0, doorY, -doorWidth / 2f),
            V(doorThickness, doorHeight, doorWidth), "SwingDoor", rightPivot);
        rightDoor.transform.localPosition = V(0, doorY, -doorWidth / 2f);

        // Door window
        var rightWindow = C("KitchenDoor_Right_Window", V(0, 0, 0), V(0.12f, 0.5f, 0.5f), "DoorWindow", rightDoor);
        rightWindow.transform.localPosition = V(0, 0.3f, 0);

        // Trigger and swing script
        var rightTrigger = rightPivot.AddComponent<BoxCollider>();
        rightTrigger.isTrigger = true;
        rightTrigger.center = V(0, 1.5f, -1.5f);
        rightTrigger.size = V(2, 3, 4);
        var rightSwing = rightPivot.AddComponent<SwingingDoor>();
        SetSwingDoor(rightSwing, -85f, 200f, 2.5f);
    }

    private void SetSwingDoor(SwingingDoor door, float angle, float speed, float delay)
    {
        var so = new SerializedObject(door);
        so.FindProperty("swingAngle").floatValue = angle;
        so.FindProperty("swingSpeed").floatValue = speed;
        so.FindProperty("closeDelay").floatValue = delay;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ════════════════════════════════════════════════════════════
    //  STREET (Z = -60 to 0)
    // ════════════════════════════════════════════════════════════

    private void BuildStreet()
    {
        var p = new GameObject("--- STREET ---");

        // Ground
        C("Ground", V(0, -0.2f, -30), V(60, 0.1f, 60), "Ground", p);

        // Road
        C("Road", V(0, -0.08f, -30), V(8, 0.06f, 60), "Road", p);

        // Lane markings
        for (int i = 0; i < 8; i++)
        {
            float z = -55f + i * 8f;
            C($"LaneMark_{i}", V(0, -0.04f, z), V(0.2f, 0.02f, 3.5f), "LaneMark", p);
        }

        // Crosswalk in front of pizzeria
        for (int i = 0; i < 6; i++)
        {
            float x = -3f + i * 1.2f;
            C($"Crosswalk_{i}", V(x, -0.04f, -3), V(0.5f, 0.02f, 2.5f), "Crosswalk", p);
        }

        // Sidewalks
        C("Sidewalk_L", V(-5.5f, -0.1f, -30), V(3, 0.12f, 60), "Sidewalk", p);
        C("Sidewalk_R", V(5.5f, -0.1f, -30), V(3, 0.12f, 60), "Sidewalk", p);

        // ── DETAILED BUILDINGS ──
        Material[] bldgMats = { M("Building1"), M("Building2"), M("Building3") };
        Random.InitState(42);

        for (int i = 0; i < 6; i++)
        {
            float z = -55f + i * 10f;
            float hL = Random.Range(5f, 10f);
            float hR = Random.Range(5f, 10f);
            float wL = Random.Range(5f, 7.5f);
            float wR = Random.Range(5f, 7.5f);

            // Left building
            BuildDetailedBuilding($"BldgL_{i}", V(-13, 0, z), wL, hL, 7, true, bldgMats[i % 3], p);
            // Right building
            BuildDetailedBuilding($"BldgR_{i}", V(13, 0, z), wR, hR, 7, false, bldgMats[(i + 1) % 3], p);
        }

        // ── DELIVERY HOUSE ──
        C("House_Body", V(13, 2.5f, -55), V(8, 5, 7), "House", p);
        C("House_Roof", V(13, 5.3f, -55), V(9, 0.5f, 8), "Roof", p);
        C("House_Door", V(9.1f, 1.3f, -55), V(0.1f, 2.6f, 1.3f), "Door", p);
        C("House_DoorFrame", V(9.1f, 1.3f, -55), V(0.12f, 2.8f, 1.5f), "BuildingTrim", p);
        // House windows
        C("House_Window_1", V(9.1f, 3, -53.5f), V(0.08f, 1, 0.8f), "Window", p);
        C("House_WinFrame_1", V(9.1f, 3, -53.5f), V(0.1f, 1.1f, 0.9f), "WindowFrame", p);
        C("House_Window_2", V(9.1f, 3, -56.5f), V(0.08f, 1, 0.8f), "Window", p);
        C("House_WinFrame_2", V(9.1f, 3, -56.5f), V(0.1f, 1.1f, 0.9f), "WindowFrame", p);
        // Porch
        C("House_Porch", V(9, 0.05f, -55), V(2, 0.1f, 3), "Sidewalk", p);

        // ── PIZZERIA FACADE ──
        C("Facade_Top", V(0, 4.5f, -0.3f), V(10, 2, 0.5f), "Facade", p);
        C("Facade_Sign", V(0, 5.2f, -0.65f), V(6, 0.8f, 0.1f), "Sign", p);
        C("Facade_Trim", V(0, 3.55f, -0.3f), V(10.2f, 0.15f, 0.55f), "BuildingTrim", p);

        // ── STREET FURNITURE ──
        // Light poles
        for (int i = 0; i < 6; i++)
        {
            float z = -55f + i * 10f;
            C($"Pole_L_{i}", V(-4.5f, 2.5f, z), V(0.12f, 5, 0.12f), "Pole", p);
            C($"PoleArm_L_{i}", V(-4.2f, 4.8f, z), V(0.6f, 0.08f, 0.08f), "Pole", p);
            C($"Pole_R_{i}", V(4.5f, 2.5f, z), V(0.12f, 5, 0.12f), "Pole", p);
            C($"PoleArm_R_{i}", V(4.2f, 4.8f, z), V(0.6f, 0.08f, 0.08f), "Pole", p);
        }

        // Benches
        BuildBench(V(-5.5f, 0, -15), 90f, p, "Bench_1");
        BuildBench(V(5.5f, 0, -25), -90f, p, "Bench_2");

        // Fire hydrant
        C("Hydrant_Body", V(-4, 0.25f, -20), V(0.25f, 0.5f, 0.25f), "Hydrant", p);
        C("Hydrant_Top", V(-4, 0.55f, -20), V(0.3f, 0.1f, 0.15f), "Hydrant", p);

        // Mailbox
        C("Mailbox_Post", V(5.5f, 0.5f, -10), V(0.1f, 1, 0.1f), "Pole", p);
        C("Mailbox_Box", V(5.5f, 1.15f, -10), V(0.5f, 0.35f, 0.3f), "Mailbox", p);

        // Trees
        BuildTree(V(-8, 0, -8), p, "Tree_1");
        BuildTree(V(8, 0, -18), p, "Tree_2");
        BuildTree(V(-8, 0, -35), p, "Tree_3");
        BuildTree(V(8, 0, -45), p, "Tree_4");
    }

    private void BuildDetailedBuilding(string name, Vector3 basePos, float w, float h, float d, bool facingRight, Material bodyMat, GameObject parent)
    {
        float x = basePos.x, z = basePos.z;

        // Main body
        var body = C(name, V(x, h / 2, z), V(w, h, d), bodyMat, parent);

        // Trim at top
        C(name + "_Trim", V(x, h + 0.05f, z), V(w + 0.2f, 0.15f, d + 0.1f), "BuildingTrim", parent);

        // Base trim
        C(name + "_BaseTrim", V(x, 0.15f, z), V(w + 0.1f, 0.3f, d + 0.05f), "BuildingTrim", parent);

        // Windows (2 rows, 2 columns per floor)
        float faceSide = facingRight ? (x + w / 2f + 0.06f) : (x - w / 2f - 0.06f);
        int floors = Mathf.FloorToInt(h / 3f);

        for (int floor = 0; floor < floors; floor++)
        {
            float winY = 2f + floor * 3f;
            for (int col = 0; col < 2; col++)
            {
                float winZ = z - 1.5f + col * 3f;
                string wn = $"{name}_Win_{floor}_{col}";
                C(wn, V(faceSide, winY, winZ), V(0.08f, 1, 0.7f), "Window", parent);
                C(wn + "_Frame", V(faceSide, winY, winZ), V(0.1f, 1.15f, 0.85f), "WindowFrame", parent);
            }
        }

        // Door on ground floor
        float doorX = facingRight ? (x + w / 2f + 0.06f) : (x - w / 2f - 0.06f);
        C(name + "_Door", V(doorX, 1, z), V(0.08f, 2, 1), "BldgDoor", parent);

        // Awning above door
        C(name + "_Awning", V(doorX + (facingRight ? 0.3f : -0.3f), 2.2f, z),
            V(0.8f, 0.08f, 1.6f), "Awning", parent);

        // AC unit on side
        if (h > 6)
        {
            C(name + "_AC", V(doorX + (facingRight ? 0.3f : -0.3f), h * 0.6f, z + d/2f - 0.5f),
                V(0.5f, 0.4f, 0.6f), "ACUnit", parent);
        }
    }

    private void BuildBench(Vector3 pos, float rotY, GameObject parent, string name)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        C("Seat", V(0, 0.3f, 0), V(1.8f, 0.08f, 0.5f), "Bench", root);
        C("Back", V(0, 0.55f, -0.22f), V(1.8f, 0.4f, 0.06f), "Bench", root);
        
        // 4 defined legs for the bench
        C("LegFL", V(-0.7f, 0.15f, 0.2f), V(0.08f, 0.3f, 0.08f), "Pole", root);
        C("LegFR", V(0.7f, 0.15f, 0.2f), V(0.08f, 0.3f, 0.08f), "Pole", root);
        C("LegBL", V(-0.7f, 0.15f, -0.2f), V(0.08f, 0.3f, 0.08f), "Pole", root);
        C("LegBR", V(0.7f, 0.15f, -0.2f), V(0.08f, 0.3f, 0.08f), "Pole", root);
    }

    private void BuildTree(Vector3 pos, GameObject parent, string name)
    {
        C(name + "_Trunk", V(pos.x, 1.2f, pos.z), V(0.3f, 2.4f, 0.3f), "Trunk", parent);
        C(name + "_Canopy", V(pos.x, 3, pos.z), V(2.5f, 2, 2.5f), "Tree", parent);
    }

    // ════════════════════════════════════════════════════════════
    //  INTRO CAMERA PATH — smoother kitchen section
    // ════════════════════════════════════════════════════════════

    private void BuildIntroCamera()
    {
        var pathObj = new GameObject("IntroPath");

        // Smooth gentle arc through kitchen — no more back-and-forth zigzag
        Vector3[] points = new Vector3[]
        {
            // Phase 1: Street fly-in
            new Vector3(0f, 8f, -58f),
            new Vector3(2f, 6f, -45f),
            new Vector3(-1f, 4.5f, -32f),
            new Vector3(1f, 3f, -18f),
            new Vector3(0f, 2.5f, -8f),

            // Phase 2: Enter the lobby
            new Vector3(0f, 2f, -1f),
            new Vector3(0f, 1.8f, 4f),
            new Vector3(3f, 1.7f, 7f),
            new Vector3(6f, 1.6f, 10f),
            new Vector3(5f, 1.7f, 13f),

            // Phase 3: Counter
            new Vector3(2f, 1.6f, 14.5f),
            new Vector3(-1f, 1.6f, 14.5f),
            new Vector3(-4f, 1.7f, 13f),

            // Phase 4: Kitchen — SMOOTH ARC (no zigzag)
            // Enter through doorway, gentle counter-clockwise loop
            new Vector3(-8f, 1.8f, 12f),           // approach doorway
            new Vector3(-12f, 1.8f, 12f),           // enter kitchen
            new Vector3(-14f, 1.7f, 13.5f),         // drift left and forward
            new Vector3(-15f, 1.7f, 15f),           // toward oven area (safe distance)
            new Vector3(-17f, 1.7f, 15.5f),         // past oven, continuing left
            new Vector3(-19.5f, 1.7f, 14f),         // along left wall
            new Vector3(-19.5f, 1.7f, 11f),         // down the left wall
            new Vector3(-18f, 1.7f, 9f),            // past shelves, turning
            new Vector3(-15f, 1.8f, 8.5f),          // heading back toward doorway

            // Phase 5: Exit kitchen, back to lobby
            new Vector3(-12f, 1.8f, 10f),           // near doorway
            new Vector3(-9f, 1.8f, 12f),            // through doorway
            new Vector3(-5f, 1.7f, 13f),            // back in lobby
            new Vector3(-2f, 1.7f, 14f),            // approaching counter
            new Vector3(0f, 1.6f, 14.5f),           // end — facing counter
        };

        for (int i = 0; i < points.Length; i++)
        {
            var wp = new GameObject($"WP_{i:D2}");
            wp.transform.parent = pathObj.transform;
            wp.transform.position = points[i];
        }

        var mgrObj = new GameObject("IntroCameraManager");
        var mgr = mgrObj.AddComponent<IntroCameraManager>();

        var so = new SerializedObject(mgr);
        so.FindProperty("pathName").stringValue = "IntroPath";
        so.FindProperty("totalDuration").floatValue = introDuration;
        so.FindProperty("rotationSmooth").floatValue = rotationSmooth;
        so.FindProperty("lookAheadAmount").floatValue = lookAhead;
        so.FindProperty("loop").boolValue = loopIntro;
        so.FindProperty("splineTension").floatValue = 0f;

        var cpList = so.FindProperty("checkpoints");
        cpList.arraySize = 3;
        SetCheckpoint(cpList, 0, "SkipToLobby", 0.18f);
        SetCheckpoint(cpList, 1, "SkipToKitchen", 0.50f);
        SetCheckpoint(cpList, 2, "SkipToFinale", 0.88f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ════════════════════════════════════════════════════════════
    //  LIGHTING
    // ════════════════════════════════════════════════════════════

    private void BuildLighting()
    {
        var p = new GameObject("--- LIGHTING ---");

        // Lobby
        PL("Lobby_1", V(0, 3.5f, 6), 1.4f, 14, new Color(1, 0.93f, 0.82f), true, p);
        PL("Lobby_2", V(0, 3.5f, 14), 1.2f, 12, new Color(1, 0.93f, 0.82f), true, p);
        PL("Lobby_3", V(-5, 3.2f, 10), 0.6f, 8, new Color(1, 0.93f, 0.82f), true, p);
        PL("Lobby_4", V(5, 3.2f, 10), 0.6f, 8, new Color(1, 0.93f, 0.82f), true, p);

        // Kitchen
        PL("Kitchen_1", V(-16, 3.5f, 12), 1.6f, 14, new Color(1, 1, 0.97f), true, p);
        PL("Kitchen_2", V(-16, 3.5f, 16), 1, 10, new Color(1, 0.95f, 0.9f), true, p);
        PL("OvenGlow", V(-16, 0.6f, 16.5f), 0.7f, 4, new Color(1, 0.45f, 0.1f), false, p);

        // Street
        for (int i = 0; i < 6; i++)
        {
            float z = -55f + i * 10f;
            PL($"SL_L_{i}", V(-4.2f, 4.8f, z), 0.8f, 10, new Color(1, 0.88f, 0.65f), true, p);
            PL($"SL_R_{i}", V(4.2f, 4.8f, z), 0.8f, 10, new Color(1, 0.88f, 0.65f), true, p);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    private GameObject C(string name, Vector3 pos, Vector3 scale, string matName, GameObject parent)
    {
        return C(name, pos, scale, M(matName), parent);
    }

    private static GameObject C(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        if (parent != null) obj.transform.parent = parent.transform;
        obj.transform.localPosition = pos; // Fixed: Use localPosition so groups don't clump at origin!
        obj.transform.localScale = scale;
        if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    private void PL(string name, Vector3 pos, float intensity, float range, Color color, bool visibleBulb, GameObject parent)
    {
        var obj = new GameObject(name);
        var l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.intensity = intensity;
        l.range = range;
        l.color = color;
        if (parent != null) obj.transform.parent = parent.transform;
        obj.transform.localPosition = pos;

        if (visibleBulb)
        {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = name + "_Bulb";
            bulb.transform.position = pos;
            bulb.transform.localScale = Vector3.one * 0.2f;
            bulb.transform.parent = obj.transform;
            Renderer r = bulb.GetComponent<Renderer>();
            r.sharedMaterial = M("LightBulb");
            // Set emission color to match the light if not already done
            r.sharedMaterial.EnableKeyword("_EMISSION");
            r.sharedMaterial.SetColor("_EmissionColor", color * 2.0f); 
            // Also turn off shadow casting for the bulb
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private static void SetCheckpoint(SerializedProperty list, int index, string trigName, float pathPos)
    {
        var el = list.GetArrayElementAtIndex(index);
        el.FindPropertyRelative("triggerName").stringValue = trigName;
        el.FindPropertyRelative("pathPosition").floatValue = pathPos;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif

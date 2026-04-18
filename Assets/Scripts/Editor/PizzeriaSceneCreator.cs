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
    private float introDuration = 75f;
    private float rotationSmooth = 4f;
    private float lookAhead = 0.04f;
    private bool loopIntro = false;
    private Vector2 scrollPos;
    private string status = "";
    private MessageType statusType = MessageType.None;

    // Material cache
    private Dictionary<string, Material> mats = new Dictionary<string, Material>();
    private Shader shader;

    [MenuItem("Tools/Pizzeria/IntroSequence Builder")]
    public static void ShowWindow()
    {
        var w = GetWindow<PizzeriaFullBuilder>("🍕 Intro Builder");
        w.minSize = new Vector2(380, 420);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(10);
        GUILayout.Label("🍕 IntroSequence Builder", new GUIStyle(EditorStyles.boldLabel)
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
            var p = new GameObject("--- WORLD ---");
            // Heavy hardware details on the roof
            BuildHVAC(V(4, 6, 12), 0, p, "HVAC_1");
            BuildHVAC(V(-4, 6, 12), 45, p, "HVAC_2");
            BuildHVAC(V(10, 6, 15), 90, p, "HVAC_3");

            // Epic Neon Spinning Sign Core
            var neonRoot = new GameObject("NeonSignSystem");
            neonRoot.transform.parent = p.transform;
            neonRoot.transform.localPosition = V(0, 7f, -1f);
            C("SignMount", V(0, -0.5f, 0), V(0.3f, 1.5f, 0.3f), "Pole", neonRoot);
            
            var neonCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neonCyl.transform.parent = neonRoot.transform;
            neonCyl.transform.localPosition = V(0, 0.7f, 0);
            neonCyl.transform.localScale = V(4f, 0.4f, 4f);
            neonCyl.transform.localRotation = Quaternion.Euler(90, 0, 0);
            neonCyl.GetComponent<Renderer>().sharedMaterial = M("Awning");
            neonCyl.AddComponent<NeonRotator>();
            
            BuildLobby(p);
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
        MatE(folder, "MenuScreen",    new Color(0.9f, 0.6f, 0.1f), new Color(1f, 0.7f, 0.1f) * 2.5f); // High attention glowing orange
        MatE(folder, "LightBulb",     new Color(1f, 1f, 1f), new Color(1f, 0.95f, 0.8f) * 2f);
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
        MatE(folder, "OvenMouth",     new Color(0.85f, 0.30f, 0.05f), new Color(1f, 0.4f, 0.1f) * 3f);
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
        Mat(folder, "Window",         new Color(0.55f, 0.72f, 0.85f)); // Daylight window
        Mat(folder, "WindowFrame",    new Color(0.15f, 0.13f, 0.10f));
        Mat(folder, "Awning",         new Color(0.70f, 0.15f, 0.10f));
        Mat(folder, "BldgDoor",       new Color(0.35f, 0.22f, 0.12f));
        Mat(folder, "House",          new Color(0.85f, 0.82f, 0.70f));
        Mat(folder, "Roof",           new Color(0.45f, 0.18f, 0.12f));
        Mat(folder, "Door",           new Color(0.40f, 0.25f, 0.12f));
        Mat(folder, "Facade",         new Color(0.75f, 0.15f, 0.12f));
        Mat(folder, "Sign",           new Color(0.90f, 0.85f, 0.20f));
        Mat(folder, "Pole",           new Color(0.30f, 0.30f, 0.32f));
        Mat(folder, "Bench",          new Color(0.45f, 0.30f, 0.18f));
        Mat(folder, "BenchFrame",     new Color(0.20f, 0.20f, 0.20f));
        Mat(folder, "Mailbox",        new Color(0.15f, 0.20f, 0.55f));
        Mat(folder, "Crosswalk",      new Color(0.95f, 0.95f, 0.95f));
        Mat(folder, "Tree",           new Color(0.25f, 0.45f, 0.15f));
        Mat(folder, "Trunk",          new Color(0.40f, 0.28f, 0.15f));
        Mat(folder, "ACUnit",         new Color(0.60f, 0.60f, 0.62f));
        
        // City Details
        Mat(folder, "CarRed",         new Color(0.7f, 0.05f, 0.05f));
        Mat(folder, "CarSilver",      new Color(0.6f, 0.6f, 0.65f));
        Mat(folder, "CarYellow",      new Color(0.9f, 0.7f, 0.1f));
        Mat(folder, "Tires",          new Color(0.1f, 0.1f, 0.1f));
        Mat(folder, "NPCShirt1",      new Color(0.2f, 0.3f, 0.6f));
        Mat(folder, "NPCShirt2",      new Color(0.6f, 0.2f, 0.3f));
        Mat(folder, "NPCPants",       new Color(0.2f, 0.2f, 0.2f));
        Mat(folder, "NPCSkin",        new Color(0.9f, 0.8f, 0.7f));
        Mat(folder, "Dumpster",       new Color(0.2f, 0.4f, 0.2f));
        Mat(folder, "TrafficLight",   new Color(0.15f, 0.15f, 0.15f));
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
            mat.DisableKeyword("_EMISSION"); // Disable by default unless explicitly enabled
            EditorUtility.SetDirty(mat);
        }
        mats[name] = mat;
    }

    private void MatE(string folder, string name, Color color, Color emission)
    {
        Mat(folder, name, color);
        if (mats.ContainsKey(name))
        {
            mats[name].EnableKeyword("_EMISSION");
            mats[name].SetColor("_EmissionColor", emission);
            EditorUtility.SetDirty(mats[name]);
        }
    }

    private Material M(string name) => mats.ContainsKey(name) ? mats[name] : null;

    // ════════════════════════════════════════════════════════════
    //  LOBBY (Z = 0 to 20, X = -10 to 10)
    // ════════════════════════════════════════════════════════════

    private void BuildLobby(GameObject p)
    {
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

        // Ground - massive concrete block to support layout
        C("Ground", V(-10f, -0.2f, -10f), V(160, 0.1f, 160), "Ground", p);

        // Main Street (Far left: X = -45) (Spans Z = -70 to 30)
        C("MainRoad", V(-45, -0.08f, -20), V(8, 0.06f, 120), "Road", p);
        for (int i = 0; i < 15; i++)
        {
            float z = -75f + i * 8f;
            C($"LaneMark_{i}", V(-45, -0.04f, z), V(0.2f, 0.02f, 3.5f), "LaneMark", p);
        }

        // Sidewalks running along Main Street
        C("Sidewalk_Main_L", V(-50.5f, -0.1f, -20), V(3, 0.12f, 120), "Sidewalk", p);
        
        // Right sidewalk cut out for the intersection (Intersection is at Z = -25)
        C("Sidewalk_Main_R_Front", V(-39.5f, -0.1f, -55), V(3, 0.12f, 50), "Sidewalk", p);
        C("Sidewalk_Main_R_Back", V(-39.5f, -0.1f, 15), V(3, 0.12f, 70), "Sidewalk", p);

        // Driveway Intersection connecting Main Street tightly into the Lot
        C("Intersection", V(-26.5f, -0.08f, -25f), V(29, 0.06f, 8), "Road", p);

        // ── FRONT PARKING LOT ──
        // Spans X = -12 to 12. Z = -35 to -2. Width 24, Length 33.
        C("Lot_Asphalt", V(0f, -0.15f, -18.5f), V(24, 0.1f, 33), "Road", p);
        
        // Front Parking Spaces (Facing lobby)
        for (int i = 0; i < 5; i++)
        {
            float x = -8f + i * 4f;
            C($"LotLine_L_{i}", V(x, -0.09f, -6), V(0.2f, 0.02f, 5f), "LaneMark", p);
            C($"LotBlocker_{i}", V(x + 2f, -0.05f, -3f), V(2f, 0.15f, 0.5f), "Sidewalk", p);
        }
        C($"LotLine_L_5", V(12f, -0.09f, -6), V(0.2f, 0.02f, 5f), "LaneMark", p);

        // ── 5-LAYER CITY BUILDINGS ──
        Material[] bldgMats = { M("Building1"), M("Building2"), M("Building3") };
        Random.InitState(42);

        // Left side 5-layer thick
        for (int layer = 0; layer < 5; layer++)
        {
            float curX = -60f - (layer * 14f);
            for (int i = -2; i < 7; i++)
            {
                float z = -65f + i * 18f;
                float hL = Random.Range(15f + layer * 15f, 75f + layer * 25f);
                if (layer == 0)
                    BuildDetailedBuilding($"BldgL_L0_{i}", V(curX, 0, z), 12, hL, 14, 0f, bldgMats[Mathf.Abs(i) % 3], p);
                else {
                    float jX = Random.Range(-3f, 3f);
                    float jZ = Random.Range(-4f, 4f);
                    BuildProxyBuilding($"BldgL_L{layer}_{i}", V(curX + jX, 0, z + jZ), 12, hL, 14, 0f, bldgMats[(Mathf.Abs(i) + layer) % 3], p);
                }
            }
        }
        
        // Right side 5-layer thick
        for (int layer = 0; layer < 5; layer++)
        {
            float curX = 30f + (layer * 14f);
            for (int i = -2; i < 7; i++)
            {
                float z = -65f + i * 18f;
                float hR = Random.Range(20f + layer * 15f, 65f + layer * 25f);
                if (layer == 0)
                    BuildDetailedBuilding($"BldgR_L0_{i}", V(curX, 0, z), 12, hR, 14, 180f, bldgMats[(Mathf.Abs(i) + 1) % 3], p);
                else {
                    float jX = Random.Range(-3f, 3f);
                    float jZ = Random.Range(-4f, 4f);
                    BuildProxyBuilding($"BldgR_L{layer}_{i}", V(curX + jX, 0, z + jZ), 12, hR, 14, 180f, bldgMats[(Mathf.Abs(i) + layer + 1) % 3], p);
                }
            }
        }

        // Back wall 5-layer thick (Z=45 outwards)
        for (int layer = 0; layer < 5; layer++)
        {
            float curZ = 45f + (layer * 14f);
            for (int i = 0; i < 15; i++)
            {
                float x = -116f + i * 14.5f; // Span entire interlock
                float hCenter = Random.Range(35f + layer * 15f, 95f + layer * 25f);
                
                if (layer == 0 && x > -60f && x < 30f) // Detailed only inside view
                    BuildDetailedBuilding($"BldgBack_L0_{i}", V(x, 0, curZ), 13, hCenter, 14, 90f, bldgMats[i % 3], p);
                else {
                    float jX = Random.Range(-5f, 5f);
                    float jZ = Random.Range(-3f, 3f);
                    BuildProxyBuilding($"BldgBack_L{layer}_{i}", V(x + jX, 0, curZ + jZ), 13, hCenter, 14, 90f, bldgMats[(i + layer) % 3], p);
                }
            }
        }

        // Front wall 5-layer thick (Z=-90 outwards)
        for (int layer = 0; layer < 5; layer++)
        {
            float curZ = -90f - (layer * 14f);
            for (int i = 0; i < 15; i++)
            {
                float x = -116f + i * 14.5f; 
                float hFront = Random.Range(40f + layer * 15f, 100f + layer * 25f);
                
                if (layer == 0 && x > -60f && x < 30f)
                    BuildDetailedBuilding($"BldgFront_L0_{i}", V(x, 0, curZ), 13, hFront, 14, -90f, bldgMats[i % 3], p);
                else {
                    float jX = Random.Range(-5f, 5f);
                    float jZ = Random.Range(-3f, 3f);
                    BuildProxyBuilding($"BldgFront_L{layer}_{i}", V(x + jX, 0, curZ + jZ), 13, hFront, 14, -90f, bldgMats[(i + layer) % 3], p);
                }
            }
        }

        // ── PIZZERIA CROSSWALK & SIDEWALK ──
        C("Lobby_Sidewalk", V(0, -0.1f, -1.5f), V(26, 0.12f, 3), "Sidewalk", p);
        C("Facade_Top", V(0, 4.5f, -0.3f), V(10, 2, 0.5f), "Facade", p);
        C("Facade_Sign", V(0, 5.2f, -0.65f), V(6, 0.8f, 0.1f), "Sign", p);
        C("Facade_Trim", V(0, 3.55f, -0.3f), V(10.2f, 0.15f, 0.55f), "BuildingTrim", p);

        // ── CITY PROPS ──
        // Dumpsters tucked securely around the far back left of the lot
        C("Dumpster_1", V(-10f, 1f, -32f), V(3, 2, 2), "Dumpster", p);
        C("Dumpster_2", V(-6f, 1f, -32f), V(3, 2, 2), "Dumpster", p);
        
        // Stop Signs rotated across the intersection
        BuildStopSign(V(-40.5f, 0, -21f), 0f, p, "StopSign_1");
        BuildStopSign(V(-40.5f, 0, -29f), 180f, p, "StopSign_2");

        // Light poles outlining the long main street axis evenly
        for (int i = -2; i < 8; i++)
        {
            float z = -65f + i * 15f;
            C($"Pole_L_{i}", V(-49.5f, 2.5f, z), V(0.12f, 5, 0.12f), "Pole", p);
            C($"PoleArm_L_{i}", V(-49.2f, 4.8f, z), V(0.6f, 0.08f, 0.08f), "Pole", p);
            C($"Pole_R_{i}", V(-40.5f, 2.5f, z), V(0.12f, 5, 0.12f), "Pole", p);
            C($"PoleArm_R_{i}", V(-40.8f, 4.8f, z), V(0.6f, 0.08f, 0.08f), "Pole", p);
        }

        // Detailed Street Decor and Props
        BuildMicroDetails(p);

        // Populate dynamic City Park in the massive gap on the right
        BuildPark(p);

        // ── DYNAMIC CITY ENTITIES ──
        // Cars now loop smoothly along the side street only, avoiding collisions completely
        BuildVehicle(V(-47f, 0, -60), 0f, "CarRed", false, p);       // Inner lane (+Z)
        BuildVehicle(V(-47f, 0, -30), 0f, "CarYellow", false, p);    // Inner lane (+Z)
        BuildVehicle(V(-43f, 0, -10), 180f, "CarSilver", false, p);  // Outer lane (-Z)
        BuildVehicle(V(-43f, 0, 20), 180f, "CarRed", false, p);      // Outer lane (-Z)
        
        // The Hero Vehicle (parks dynamically from Left Street -> Driveway -> Lot -> Spot 2)
        BuildHeroVehicle(V(-47f, 0, -65), p);

        // Parked Cars permanently resting in front of the lobby
        BuildVehicle(V(-6, 0, -6f), 0f, "CarSilver", true, p);  // Spot 1
        BuildVehicle(V(2, 0, -6f), 0f, "CarRed", true, p);      // Spot 3
        BuildVehicle(V(6, 0, -6f), 0f, "CarYellow", true, p);   // Spot 4
        BuildVehicle(V(10, 0, -6f), 0f, "CarSilver", true, p);   // Spot 5

        // Pedestrians sticking specifically to main sidewalks
        BuildNPC(V(-50.5f, 0, -50), 0f, "NPCShirt1", p);
        BuildNPC(V(-39.5f, 0, 10), 180f, "NPCShirt2", p);
    }

    private void BuildPark(GameObject parent)
    {
        var root = new GameObject("CityPark");
        root.transform.parent = parent.transform;
        
        // Grass Base
        C("Park_Grass", V(15, -0.1f, -42.5f), V(22, 0.15f, 57), "Ground", root);

        // Walking Paths
        C("Path_V", V(15, -0.05f, -42.5f), V(4, 0.1f, 57), "Sidewalk", root); // Vertical path down middle
        C("Path_H1", V(15, -0.05f, -25f), V(22, 0.1f, 4), "Sidewalk", root); // Horizontal path top
        C("Path_H2", V(15, -0.05f, -60f), V(22, 0.1f, 4), "Sidewalk", root); // Horizontal path bottom

        // Benches gracefully centered on concrete pads, completely facing path inwards
        BuildBench(V(10.5f, 0, -32f), 0f, root, "ParkBench_1");
        BuildBench(V(19.5f, 0, -32f), 180f, root, "ParkBench_2");
        BuildBench(V(10.5f, 0, -52f), 0f, root, "ParkBench_3");
        BuildBench(V(19.5f, 0, -52f), 180f, root, "ParkBench_4");
        
        // Fountain seating
        BuildBench(V(15f, 0, -38f), 90f, root, "ParkBench_5");
        BuildBench(V(15f, 0, -47f), -90f, root, "ParkBench_6");

        // Park Fountain / Monument
        C("FountainBase", V(15, 0f, -42.5f), V(6, 0.4f, 6), "Sidewalk", root);
        C("FountainTier1", V(15, 0.4f, -42.5f), V(4, 0.4f, 4), "BuildingTrim", root);
        C("FountainTier2", V(15, 0.8f, -42.5f), V(2, 0.8f, 2), "BuildingTrim", root);
        C("FountainTop", V(15, 1.6f, -42.5f), V(1, 1f, 1), "Sidewalk", root);

        // Playground Area
        C("Sandbox", V(19, 0f, -65f), V(4, 0.2f, 4), "Dough", root);
        C("SlideStairs", V(19, 0.8f, -64f), V(1, 1.6f, 1), "Awning", root);
        C("SlideRamp", V(19, 0.4f, -66f), V(1, 0.8f, 2), "BoothSeat", root);

        // Trash Cans & Street Lights inside park
        C("TrashCan_1", V(12.5f, 0.5f, -28f), V(0.6f, 1f, 0.6f), "Dumpster", root);
        C("TrashCan_2", V(12.5f, 0.5f, -57f), V(0.6f, 1f, 0.6f), "Dumpster", root);
        
        C("ParkLight_1_Pole", V(10f, 2.5f, -42.5f), V(0.12f, 5, 0.12f), "Pole", root);
        C("ParkLight_1_Globe", V(10f, 5.2f, -42.5f), V(0.5f, 0.5f, 0.5f), "LightBulb", root);
        
        C("ParkLight_2_Pole", V(20f, 2.5f, -42.5f), V(0.12f, 5, 0.12f), "Pole", root);
        C("ParkLight_2_Globe", V(20f, 5.2f, -42.5f), V(0.5f, 0.5f, 0.5f), "LightBulb", root);

        // High Density Forest Blocks (Added strict collision bounds for paths)
        Random.InitState(12345);
        for (int x = 6; x < 26; x += 4)
        {
            for (int z = -68; z < -16; z += 5)
            {
                float jx = Random.Range(-1.5f, 1.5f);
                float jz = Random.Range(-1.5f, 1.5f);
                float treeX = x + jx;
                float treeZ = z + jz;
                
                // Ensure trees don't overlap the intersecting walking paths or fountain
                if (treeX > 11.5f && treeX < 18.5f) continue;
                if (treeZ > -29f && treeZ < -21f) continue;
                if (treeZ > -64f && treeZ < -56f) continue;
                
                BuildTree(V(treeX, 0, treeZ), root, $"ParkTree_{x}_{z}");
                
                float bushX = treeX + 1.2f;
                float bushZ = treeZ + 0.8f;
                
                // Ensure bushes do not bleed onto sidewalks
                if (bushX > 12f && bushX < 18f) continue;
                if (bushZ > -28f && bushZ < -22f) continue;
                if (bushZ > -63f && bushZ < -57f) continue;

                C($"Bush_{x}_{z}", V(bushX, 0.5f, bushZ), V(1.5f, 1f, 1.5f), "Plant", root);
            }
        }
    }

    private void BuildStopSign(Vector3 pos, float rotY, GameObject parent, string name)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        C("Pole", V(0, 1.5f, 0), V(0.12f, 3, 0.12f), "Pole", root);
        
        var sign = C("SignBd", V(0, 2.7f, 0.08f), V(0.8f, 0.8f, 0.05f), "BoothSeat", root);
        sign.transform.localRotation = Quaternion.Euler(0, 0, 45); // Diamond shaped Stop Sign
    }

    private void BuildProxyBuilding(string name, Vector3 basePos, float w, float h, float d, float rotY, Material bodyMat, GameObject parent)
    {
        var root = new GameObject(name + "_Proxy");
        root.transform.parent = parent.transform;
        root.transform.localPosition = basePos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        // Core monolithic structure mimicking detail volume roughly
        C("Body", V(0, h / 2, 0), V(w - 0.5f, h, d - 0.5f), bodyMat, root); // Slight gap scaling
        
        if (Random.value > 0.4f) 
        {
            C("Trim", V(0, h + 0.05f, 0), V(w - 0.2f, 0.15f, d - 0.2f), "BuildingTrim", root);
        }
    }

    private void BuildDetailedBuilding(string name, Vector3 basePos, float w, float h, float d, float rotY, Material bodyMat, GameObject parent)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = basePos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        // Core structure
        C("Body", V(0, h / 2, 0), V(w, h, d), bodyMat, root);
        C("Trim", V(0, h + 0.05f, 0), V(w + 0.2f, 0.15f, d + 0.1f), "BuildingTrim", root);
        C("BaseTrim", V(0, 0.15f, 0), V(w + 0.1f, 0.3f, d + 0.05f), "BuildingTrim", root);

        // Windows (Front face locally +X)
        float faceSide = w / 2f + 0.06f; 
        int floors = Mathf.FloorToInt(h / 3f);

        for (int floor = 0; floor < floors; floor++)
        {
            float winY = 2f + floor * 3f;
            for (int col = 0; col < 2; col++)
            {
                float winZ = -1.5f + col * 3f;
                string wn = $"Win_{floor}_{col}";
                
                // Restored to standard daylight blue windows (no emission)
                C(wn, V(faceSide, winY, winZ), V(0.08f, 1, 0.7f), "Window", root);
                C(wn + "_Frame", V(faceSide, winY, winZ), V(0.1f, 1.15f, 0.85f), "WindowFrame", root);
            }
        }

        // Street Door
        C("Door", V(faceSide, 1, 0), V(0.08f, 2, 1), "BldgDoor", root);
        C("Awning", V(faceSide + 0.3f, 2.2f, 0), V(0.8f, 0.08f, 1.6f), "Awning", root);

        if (h > 6)
        {
            C("AC", V(faceSide + 0.3f, h * 0.6f, d / 2f - 0.5f), V(0.5f, 0.4f, 0.6f), "ACUnit", root);
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

    private void BuildVehicle(Vector3 startPos, float rotY, string colorMat, bool parked, GameObject parent)
    {
        var root = new GameObject("CityVehicle_" + colorMat);
        root.transform.parent = parent.transform;
        root.transform.localPosition = startPos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        C("Body", V(0, 0.5f, 0), V(1.8f, 0.6f, 4f), colorMat, root);
        C("Cab", V(0, 1.1f, -0.2f), V(1.6f, 0.5f, 2f), colorMat, root);
        C("Windshield", V(0, 1.1f, 0.85f), V(1.5f, 0.4f, 0.1f), "Window", root);
        
        // Tires
        C("TireFR", V(0.9f, 0.3f, 1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);
        C("TireFL", V(-0.9f, 0.3f, 1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);
        C("TireBR", V(0.9f, 0.3f, -1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);
        C("TireBL", V(-0.9f, 0.3f, -1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);

        if (!parked)
        {
            var mover = root.AddComponent<CityEntityBehavior>();
            mover.type = CityEntityBehavior.EntityType.Vehicle;
            mover.speed = 12f;
            mover.travelDistance = 150f;
        }
    }

    private void BuildHeroVehicle(Vector3 startPos, GameObject parent)
    {
        var root = new GameObject("HeroCar_Parking");
        root.transform.parent = parent.transform;
        root.transform.localPosition = startPos;
        root.transform.localRotation = Quaternion.Euler(0, 0, 0);

        string colorMat = "CarRed";
        C("Body", V(0, 0.5f, 0), V(1.8f, 0.6f, 4f), colorMat, root);
        C("Cab", V(0, 1.1f, -0.2f), V(1.6f, 0.5f, 2f), colorMat, root);
        C("Windshield", V(0, 1.1f, 0.85f), V(1.5f, 0.4f, 0.1f), "Window", root);
        
        C("TireFR", V(0.9f, 0.3f, 1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);
        C("TireFL", V(-0.9f, 0.3f, 1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);
        C("TireBR", V(0.9f, 0.3f, -1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);
        C("TireBL", V(-0.9f, 0.3f, -1.2f), V(0.2f, 0.6f, 0.6f), "Tires", root);

        var mover = root.AddComponent<CinematicParkingCar>();
        mover.speed = 4f;
        mover.turnSpeed = 60f;
        mover.waypoints = new Vector3[] {
            V(-47f, 0, -28f),      // 1. Cruise down main street
            V(-43f, 0, -25f),      // 2. Begin right turn intersection
            V(-10f, 0, -25f),      // 3. Clear majestic driveway approach
            V(-2f, 0, -25f),       // 4. Center of the lot lane
            V(-2f, 0, -18f),       // 5. Straighten out pointing towards lobby
            V(-2f, 0, -6f)         // 6. Pulled securely into Spot 2!
        };
    }

    private void BuildNPC(Vector3 startPos, float rotY, string shirtMat, GameObject parent)
    {
        var root = new GameObject("CityPedestrian");
        root.transform.parent = parent.transform;
        root.transform.localPosition = startPos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        C("LegL", V(-0.2f, 0.5f, 0), V(0.3f, 1f, 0.3f), "NPCPants", root);
        C("LegR", V(0.2f, 0.5f, 0), V(0.3f, 1f, 0.3f), "NPCPants", root);
        C("Torso", V(0, 1.4f, 0), V(0.8f, 0.8f, 0.4f), shirtMat, root);
        C("Head", V(0, 2f, 0), V(0.4f, 0.4f, 0.4f), "NPCSkin", root);

        var mover = root.AddComponent<CityEntityBehavior>();
        mover.type = CityEntityBehavior.EntityType.Pedestrian;
        mover.speed = 2.5f;
        mover.travelDistance = 80f;
    }

    // ════════════════════════════════════════════════════════════
    //  INTRO CAMERA PATH — smoother kitchen section
    // ════════════════════════════════════════════════════════════

    private void BuildIntroCamera()
    {
        var pathObj = new GameObject("IntroPath");

        // Sweeping tour from far-left street corner, through parking lot, ending at lobby doors
        Vector3[] points = new Vector3[]
        {
            // Phase 1: Cruising alongside Hero Car on Main Street (X=-47)
            new Vector3(-49f, 8f, -68f),
            new Vector3(-48f, 6.5f, -50f),
            new Vector3(-48f, 5f, -38f),
            new Vector3(-47f, 4f, -32f),
            
            // Phase 2: The Right Turn into the Massive Driveway Connection
            new Vector3(-35f, 3.5f, -28f),
            new Vector3(-20f, 2.8f, -26f),
            new Vector3(-5f, 2.6f, -26f),
            
            // Phase 3: Trailing entirely across the lot, watching pulling into Spot 2
            new Vector3(-1f, 2.4f, -24f),
            new Vector3(1f, 2.2f, -20f),
            new Vector3(2f, 2.0f, -14f),
            
            // Phase 4: Sweeping curve Detaching, gliding toward the entrance
            new Vector3(3f, 1.8f, -8f),
            new Vector3(2f, 1.8f, -4f),

            // Phase 5: Approach Lobby Doors and exactly resume interior sweep
            new Vector3(1f, 1.8f, 2f),
            new Vector3(0f, 2f, -1f),
            new Vector3(0f, 1.8f, 4f),
            new Vector3(3f, 1.7f, 7f),
            new Vector3(6f, 1.6f, 10f),
            new Vector3(5f, 1.7f, 13f),

            // Phase 6: Counter
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
        // Neon Sign glow illumination overlapping the parking lot
        PL("NeonAmbientGlow", V(0, 7f, 0), 2f, 20f, new Color(1f, 0.3f, 0.2f), true, p);
    }

    private void BuildMicroDetails(GameObject p)
    {
        // Trash Bags near dumpsters
        C("TrashBag_1", V(-8f, 0.3f, -32f), V(0.8f, 0.6f, 0.8f), "BuildingTrim", p).transform.localRotation = Quaternion.Euler(15, 45, 15);
        C("TrashBag_2", V(-7f, 0.4f, -31.5f), V(0.9f, 0.8f, 0.9f), "BuildingTrim", p).transform.localRotation = Quaternion.Euler(-10, 80, -5);
        C("TrashBag_3", V(-16f, 0.3f, 4f), V(0.7f, 0.6f, 0.7f), "BuildingTrim", p);

        // Cardboard boxes in alley
        C("Box_1", V(-18f, 0.5f, 6f), V(1, 1, 1), "Dough", p).transform.localRotation = Quaternion.Euler(0, 15, 0);
        C("Box_2", V(-17.5f, 0.3f, 6.5f), V(0.8f, 0.6f, 0.8f), "Dough", p).transform.localRotation = Quaternion.Euler(0, -25, 0);

        // Mailbox on street intersection corner
        var mbox = new GameObject("Mailbox");
        mbox.transform.parent = p.transform;
        mbox.transform.localPosition = V(-15f, 0, -20.5f);
        C("Post", V(0, 0.75f, 0), V(0.2f, 1.5f, 0.2f), "Pole", mbox);
        C("Box", V(0, 1.6f, 0), V(0.6f, 0.6f, 1f), "Window", mbox);

        // Street Drain Grates
        C("Drain_1", V(-44.8f, 0.01f, -15f), V(0.8f, 0.05f, 1.5f), "BenchFrame", p);
        C("Drain_2", V(-44.8f, 0.01f, -40f), V(0.8f, 0.05f, 1.5f), "BenchFrame", p);
        C("Drain_3", V(-25f, 0.01f, -20.8f), V(1.5f, 0.05f, 0.8f), "BenchFrame", p);

        // Fire Hydrants
        BuildHydrant(V(-46.5f, 0, -18f), p, "Hydrant_1");
        BuildHydrant(V(4f, 0, -22f), p, "Hydrant_2");

        // Planter Boxes tracing the new pedestrian walk
        for (int x = -15; x < 25; x += 8)
        {
            if (x > -2 && x < 4) continue; // Pizzeria door gap
            BuildPlanterBox(V(x, 0, -11.5f), p, $"Planter_{x}");
        }
    }

    private void BuildHVAC(Vector3 pos, float rotY, GameObject parent, string name)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        C("AC_Base", V(0, 0.5f, 0), V(2, 1, 1.5f), "Dumpster", root);
        C("AC_FanFan", V(0, 1.05f, 0), V(1.2f, 0.1f, 1.2f), "Pole", root);
        C("AC_Vent", V(0, 0.5f, 0.76f), V(1f, 0.6f, 0.2f), "BenchFrame", root);
    }

    private void BuildPlanterBox(Vector3 pos, GameObject parent, string name)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = pos;

        C("PlanterBase", V(0, 0.4f, 0), V(4f, 0.8f, 1.5f), "BuildingTrim", root);
        C("PlanterDirt", V(0, 0.75f, 0), V(3.8f, 0.8f, 1.3f), "BldgDoor", root);
        C("PBush_1", V(-1f, 1f, 0), V(1.2f, 1f, 1.2f), "Plant", root);
        C("PBush_2", V(1f, 1f, 0), V(1.2f, 1.2f, 1.2f), "Plant", root);
    }

    private void BuildHydrant(Vector3 pos, GameObject parent, string name)
    {
        var root = new GameObject(name);
        root.transform.parent = parent.transform;
        root.transform.localPosition = pos;

        C("Base", V(0, 0.1f, 0), V(0.6f, 0.2f, 0.6f), "Awning", root);
        C("Body", V(0, 0.6f, 0), V(0.4f, 0.8f, 0.4f), "Awning", root);
        C("Dome", V(0, 1.1f, 0), V(0.45f, 0.3f, 0.45f), "Awning", root);
        C("NozzleL", V(-0.25f, 0.7f, 0), V(0.2f, 0.15f, 0.15f), "BuildingTrim", root);
        C("NozzleR", V(0.25f, 0.7f, 0), V(0.2f, 0.15f, 0.15f), "BuildingTrim", root);
        C("NozzleF", V(0, 0.7f, 0.25f), V(0.15f, 0.15f, 0.3f), "BuildingTrim", root);
    }

    // ════════════════════════════════════════════════════════════
    //  LIGHTING
    // ════════════════════════════════════════════════════════════

    private void BuildLighting()
    {
        var p = new GameObject("--- LIGHTING ---");

        // Daylight Setting Adjustments
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f); // Bright daylight base

        // Chimney Exhaust rising over the back of the pizzeria roof
        BuildParticleSystem(p, "RoofSmoke", V(-5, 6.5f, 15f), V(-90, 0, 0), new Color(0.2f, 0.2f, 0.2f, 0.4f), 3f, 2.5f, 6f, 8, V(1, 1, 1));
        
        // Deep Oven Embers violently sparking up
        BuildParticleSystem(p, "OvenEmbers", V(-16, 0.8f, 16.5f), V(-80, 0, 0), new Color(1f, 0.5f, 0.1f, 0.9f), 0.12f, 3f, 1.5f, 25, V(1.5f, 0.5f, 1));

        // Lobby primary lighting
        PL("Lobby_1", V(0, 3.5f, 6), 1.8f, 15, new Color(1, 0.93f, 0.82f), true, p);
        PL("Lobby_2", V(0, 3.5f, 14), 1.8f, 15, new Color(1, 0.93f, 0.82f), true, p);
        
        // Pendants in Lobby (explicit matching to geometry built in BuildLobby)
        PL("Pendant_L1", V(-4, 3.1f, 7), 1.2f, 8, new Color(1, 0.93f, 0.82f), true, p);
        PL("Pendant_R1", V(4, 3.1f, 7), 1.2f, 8, new Color(1, 0.93f, 0.82f), true, p);

        // Kitchen lighting
        PL("Kitchen_1", V(-16, 3.5f, 12), 2f, 15, new Color(1, 1, 0.97f), true, p);
        PL("Kitchen_2", V(-16, 3.5f, 16), 1.5f, 12, new Color(1, 0.95f, 0.9f), true, p);
        PL("OvenGlow", V(-16, 0.6f, 16.5f), 1f, 6, new Color(1, 0.35f, 0.05f), false, p);

        // Street Lights reverted to general ambient point lights matching daylight scheme
        for (int i = -2; i < 8; i++)
        {
            float z = -65f + i * 15f;
            PL($"SL_L_{i}", V(-49.2f, 4.8f, z), 1.5f, 15f, new Color(1, 0.95f, 0.8f), true, p);
            PL($"SL_R_{i}", V(-40.8f, 4.8f, z), 1.5f, 15f, new Color(1, 0.95f, 0.8f), true, p);
        }

        // Park Ambient Lights mounted inside the fountain square
        PL("ParkLightAmbient_1", V(10f, 4.5f, -42.5f), 1.2f, 12f, new Color(1, 0.95f, 0.85f), false, p);
        PL("ParkLightAmbient_2", V(20f, 4.5f, -42.5f), 1.2f, 12f, new Color(1, 0.95f, 0.85f), false, p);
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
        obj.transform.localRotation = Quaternion.identity; // Fixed: Reset local rotation so it rotates WITH the parent
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
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private void SL(string name, Vector3 pos, float range, GameObject parent)
    {
        var obj = new GameObject(name);
        var l = obj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = 65f;
        l.intensity = 4f;
        l.range = range;
        l.color = new Color(1, 0.85f, 0.55f);
        if (parent != null) obj.transform.parent = parent.transform;
        obj.transform.localPosition = pos;
        obj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Point straight down
        
        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.transform.parent = obj.transform;
        bulb.transform.localPosition = Vector3.zero;
        bulb.transform.localScale = Vector3.one * 0.2f;
        Renderer r = bulb.GetComponent<Renderer>();
        r.sharedMaterial = M("LightBulb");
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void BuildParticleSystem(GameObject parent, string name, Vector3 pos, Vector3 rot, Color color, float size, float speed, float lifetime, int rate, Vector3 shapeScale)
    {
        var psObj = new GameObject(name);
        psObj.transform.parent = parent.transform;
        psObj.transform.localPosition = pos;
        psObj.transform.localRotation = Quaternion.Euler(rot);

        var ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = size;
        main.startSpeed = speed;
        main.startLifetime = lifetime;

        var em = ps.emission;
        em.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = shapeScale;
        
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
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

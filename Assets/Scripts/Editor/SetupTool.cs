using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SetupTool : EditorWindow
{
    private List<ScriptSelection> availableScripts = new List<ScriptSelection>();
    private Vector2 scrollPos;
    private string searchFilter = "";
    private const string TargetFolder = "Assets/Scripts"; // Target directory

    [System.Serializable]
    public class ScriptSelection
    {
        public MonoScript script;
        public bool isSelected;
        public string name;
    }

    [MenuItem("Tools/Setup/Script Applier")]
    public static void ShowWindow()
    {
        GetWindow<SetupTool>("Script Applier");
    }

    private void OnEnable()
    {
        RefreshScriptList();
    }

    private void OnGUI()
    {
        GUILayout.Label($"Scripts in {TargetFolder}", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Scripts List"))
        {
            RefreshScriptList();
        }

        searchFilter = EditorGUILayout.TextField("Filter", searchFilter).ToLower();

        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All")) SetAllSelected(true);
        if (GUILayout.Button("Deselect All")) SetAllSelected(false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        
        if (availableScripts.Count == 0)
        {
            EditorGUILayout.HelpBox($"No scripts found in {TargetFolder}. Make sure the folder exists!", MessageType.Info);
        }

        foreach (var item in availableScripts)
        {
            if (string.IsNullOrEmpty(searchFilter) || item.name.ToLower().Contains(searchFilter))
            {
                item.isSelected = EditorGUILayout.ToggleLeft(item.name, item.isSelected);
            }
        }
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Selected to Selected Object", GUILayout.Height(30)))
        {
            ApplyScripts();
        }
    }

    private void RefreshScriptList()
    {
        availableScripts.Clear();

        // FindAssets takes a filter and an array of folder paths to search within
        string[] searchFolders = new string[] { TargetFolder };
        
        // Ensure the folder exists before searching to avoid errors
        if (!AssetDatabase.IsValidFolder(TargetFolder))
        {
            Debug.LogWarning($"The folder {TargetFolder} does not exist. Please create it.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:MonoScript", searchFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

            if (script != null && script.GetClass() != null && typeof(MonoBehaviour).IsAssignableFrom(script.GetClass()))
            {
                availableScripts.Add(new ScriptSelection { 
                    script = script, 
                    isSelected = false, 
                    name = script.name 
                });
            }
        }
        
        availableScripts = availableScripts.OrderBy(s => s.name).ToList();
    }

    private void SetAllSelected(bool state)
    {
        foreach (var item in availableScripts) item.isSelected = state;
    }

    private void ApplyScripts()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Selection Required", "Please select a GameObject in the scene.", "OK");
            return;
        }

        int count = 0;
        Undo.RegisterCompleteObjectUndo(selected, "Add Scripts");

        foreach (var item in availableScripts)
        {
            if (item.isSelected)
            {
                System.Type scriptType = item.script.GetClass();
                if (selected.GetComponent(scriptType) == null)
                {
                    selected.AddComponent(scriptType);
                    count++;
                }
            }
        }

        Debug.Log($"Added {count} components to {selected.name}.");
    }
}
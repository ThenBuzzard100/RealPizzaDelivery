using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// PlayPanelController: Manages the Play sub-menu.
///
/// Sub-panels:
///   • Choice view  – "New World" / "Load World" / "Back"
///   • New World    – enter a world name and confirm
///   • Load World   – scrollable list of saved worlds
///
/// Attach to the PlayPanel GameObject.
/// Wire up all references in the Inspector.
/// </summary>
public class PlayPanelController : MonoBehaviour
{
    // ── Sub-panel GameObjects ─────────────────────────────────────────────────
    [Header("Sub-Panels")]
    public GameObject choiceView;       // Initial "New World" / "Load World" choice
    public GameObject newWorldView;     // New world creation form
    public GameObject loadWorldView;    // Saved worlds list

    // ── New World View ────────────────────────────────────────────────────────
    [Header("New World")]
    [Tooltip("TMP InputField where the player types the world name")]
    public TMP_InputField worldNameInput;

    [Tooltip("Button that confirms world creation")]
    public Button createWorldButton;

    [Tooltip("Error text shown when the name is blank")]
    public TextMeshProUGUI newWorldErrorText;

    [Tooltip("Name of the scene to load for a new game")]
    public string newGameSceneName = "GameScene";

    // ── Load World View ───────────────────────────────────────────────────────
    [Header("Load World")]
    [Tooltip("ScrollRect content transform where saved-world entries are instantiated")]
    public Transform savedWorldListContent;

    [Tooltip("Prefab for each saved-world row (should have a SavedWorldEntry component)")]
    public GameObject savedWorldEntryPrefab;

    [Tooltip("Text shown when no saves are found")]
    public TextMeshProUGUI noSavesText;

    // ─────────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        // Every time this panel becomes visible, reset to the choice view
        ShowChoiceView();
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Sub-panel navigation

    public void ShowChoiceView()
    {
        SetActive(choiceView,    true);
        SetActive(newWorldView,  false);
        SetActive(loadWorldView, false);
    }

    public void ShowNewWorldView()
    {
        SetActive(choiceView,    false);
        SetActive(newWorldView,  true);
        SetActive(loadWorldView, false);

        // Clear previous input
        if (worldNameInput  != null) worldNameInput.text = string.Empty;
        if (newWorldErrorText != null) newWorldErrorText.gameObject.SetActive(false);
    }

    public void ShowLoadWorldView()
    {
        SetActive(choiceView,    false);
        SetActive(newWorldView,  false);
        SetActive(loadWorldView, true);

        PopulateSavedWorldList();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region New World Logic

    /// <summary>Called by the "Create" / "Confirm" button inside New World view.</summary>
    public void OnCreateWorldConfirmed()
    {
        string name = worldNameInput != null ? worldNameInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            if (newWorldErrorText != null)
            {
                newWorldErrorText.text = "Please enter a world name.";
                newWorldErrorText.gameObject.SetActive(true);
            }
            return;
        }

        // Persist the chosen name so the game scene can read it
        PlayerPrefs.SetString("NewWorldName", name);
        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene(newGameSceneName);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Load World Logic

    private void PopulateSavedWorldList()
    {
        // Clear existing entries
        foreach (Transform child in savedWorldListContent)
            Destroy(child.gameObject);

        List<string> saves = SaveDataHelper.GetAllSaveNames();

        bool hasSaves = saves != null && saves.Count > 0;
        if (noSavesText != null) noSavesText.gameObject.SetActive(!hasSaves);

        if (!hasSaves) return;

        foreach (string saveName in saves)
        {
            GameObject entry = Instantiate(savedWorldEntryPrefab, savedWorldListContent, false);
            SavedWorldEntry component = entry.GetComponent<SavedWorldEntry>();
            if (component != null)
                component.Initialise(saveName, OnSaveSelected);
        }
    }

    private void OnSaveSelected(string saveName)
    {
        PlayerPrefs.SetString("LoadWorldName", saveName);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(newGameSceneName);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    private static void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }
}





// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Minimal save-data helper. Replace the body of GetAllSaveNames() with your
/// actual save system (e.g. reading files from Application.persistentDataPath).
/// </summary>
public static class SaveDataHelper
{
    private const string SaveListKey = "SavedWorldsList";

    /// <summary>Returns all save names stored via PlayerPrefs (comma-separated).</summary>
    public static List<string> GetAllSaveNames()
    {
        string raw = PlayerPrefs.GetString(SaveListKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return new List<string>();

        var list = new List<string>(raw.Split(','));
        list.RemoveAll(string.IsNullOrWhiteSpace);
        return list;
    }

    /// <summary>Adds a new save name to the list.</summary>
    public static void RegisterSave(string saveName)
    {
        List<string> saves = GetAllSaveNames();
        if (!saves.Contains(saveName))
        {
            saves.Add(saveName);
            PlayerPrefs.SetString(SaveListKey, string.Join(",", saves));
            PlayerPrefs.Save();
        }
    }
}
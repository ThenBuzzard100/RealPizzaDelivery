using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the SavedWorldEntry prefab.
/// The prefab needs: a TextMeshProUGUI for the name, and a Button to load it.
/// </summary>
public class SavedWorldEntry : MonoBehaviour
{
    [Tooltip("Displays the save name")]
    public TextMeshProUGUI nameLabel;

    [Tooltip("Button the player clicks to load this save")]
    public Button loadButton;

    private System.Action<string> _onSelected;
    private string _saveName;

    public void Initialise(string saveName, System.Action<string> onSelected)
    {
        _saveName   = saveName;
        _onSelected = onSelected;

        if (nameLabel  != null) nameLabel.text = saveName;
        if (loadButton != null) loadButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked() => _onSelected?.Invoke(_saveName);
}
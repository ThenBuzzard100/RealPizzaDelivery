using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SplitScreenLayout: Handles the split-screen layout and gradient at runtime.
/// Attach to the Canvas root. Assign leftPanel, rightPanel, gradientStrip, backgroundCamera.
/// </summary>
public class SplitScreenLayout : MonoBehaviour
{
    [Header("Panel Transforms")]
    public RectTransform leftPanel;
    public RectTransform rightPanel;
    public RectTransform gradientStrip;

    [Header("Split Settings")]
    [Range(0f, 1f)]
    public float splitPosition = 0.45f;

    [Range(0f, 0.25f)]
    public float gradientWidth = 0.08f;

    [Header("Background Camera")]
    public Camera backgroundCamera;

    [Header("Gradient")]
    public Image gradientImage;

    private void Start()
    {
        GenerateGradient();
        ApplyLayout();
    }

    private void GenerateGradient()
    {
        if (gradientImage == null) return;

        // Build a 64x1 gradient texture: black opaque → transparent
        int w = 64;
        Texture2D tex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < w; x++)
        {
            float alpha = 1f - (x / (float)(w - 1));
            tex.SetPixel(x, 0, new Color(0f, 0f, 0f, alpha));
        }
        tex.Apply();

        Sprite spr = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 1f);
        gradientImage.sprite = spr;
        gradientImage.color  = Color.white;
        gradientImage.type   = Image.Type.Simple;
        gradientImage.preserveAspect = false;
    }

    private void ApplyLayout()
    {
        // Left panel
        if (leftPanel != null)
        {
            leftPanel.anchorMin = Vector2.zero;
            leftPanel.anchorMax = new Vector2(splitPosition, 1f);
            leftPanel.offsetMin = leftPanel.offsetMax = Vector2.zero;
        }

        // Right panel
        if (rightPanel != null)
        {
            rightPanel.anchorMin = new Vector2(splitPosition, 0f);
            rightPanel.anchorMax = Vector2.one;
            rightPanel.offsetMin = rightPanel.offsetMax = Vector2.zero;
        }

        // Gradient strip - sits at the seam
        if (gradientStrip != null)
        {
            gradientStrip.anchorMin = new Vector2(splitPosition - gradientWidth, 0f);
            gradientStrip.anchorMax = new Vector2(splitPosition,                 1f);
            gradientStrip.offsetMin = gradientStrip.offsetMax = Vector2.zero;
        }

        // Background camera viewport - right half only
        if (backgroundCamera != null)
        {
            backgroundCamera.rect = new Rect(splitPosition, 0f, 1f - splitPosition, 1f);
        }
    }
}
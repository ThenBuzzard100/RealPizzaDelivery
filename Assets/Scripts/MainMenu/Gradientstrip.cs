using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the GradientStrip GameObject.
/// Generates a black left-to-transparent right gradient at runtime.
/// </summary>
[RequireComponent(typeof(Image))]
public class GradientStrip : MonoBehaviour
{
    private void Awake()
    {
        Image img = GetComponent<Image>();

        Texture2D tex = new Texture2D(64, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color targetColor = new Color(13f/255f, 13f/255f, 13f/255f);

        for (int x = 0; x < 64; x++)
        {
            // This creates the transparency fade (1 is solid, 0 is clear)
            float alpha = 1f - (x / 63f);
            
            // Apply the RGB (13,13,13) with the calculated alpha
            tex.SetPixel(x, 0, new Color(targetColor.r, targetColor.g, targetColor.b, alpha));
        }
        tex.Apply();

        img.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 1), Vector2.one * 0.5f, 1f);
        img.color  = Color.white;
        img.type   = Image.Type.Simple;
        img.preserveAspect = false;
    }
}
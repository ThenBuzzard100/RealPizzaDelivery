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

        for (int x = 0; x < 64; x++)
        {
            float alpha = 1f - (x / 63f);
            tex.SetPixel(x, 0, new Color(0f, 0f, 0f, alpha));
        }
        tex.Apply();

        img.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 1), Vector2.one * 0.5f, 1f);
        img.color  = Color.white;
        img.type   = Image.Type.Simple;
        img.preserveAspect = false;
    }
}
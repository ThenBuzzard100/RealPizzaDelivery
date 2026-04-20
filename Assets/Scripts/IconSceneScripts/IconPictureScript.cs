using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraScreenshot : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera targetCamera;
    public int width = 1920;
    public int height = 1080;

    [Header("Save Settings")]
    // Path relative to the project root (Assets/../)
    // Example: "Assets/Screenshots/capture.png"
    public string outputPath = "Assets/Screenshots/capture.png";

    [Header("Trigger Settings")]
    public KeyCode screenshotKey = KeyCode.F12;

    private void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            CaptureScreenshot();
        }
    }

    public void CaptureScreenshot()
    {
        if (targetCamera == null)
        {
            Debug.LogError("[CameraScreenshot] No target camera assigned!");
            return;
        }

        // Create a RenderTexture for the camera to render into
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        RenderTexture previousTarget = targetCamera.targetTexture;

        targetCamera.targetTexture = renderTexture;
        targetCamera.Render();

        // Read pixels from the RenderTexture into a Texture2D
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // Encode to PNG
        byte[] pngData = screenshot.EncodeToPNG();

        // Ensure the directory exists
        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write the file
        File.WriteAllBytes(outputPath, pngData);
        Debug.Log($"[CameraScreenshot] Screenshot saved to: {Path.GetFullPath(outputPath)}");

        // Cleanup
        targetCamera.targetTexture = previousTarget;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);
        DestroyImmediate(screenshot);

#if UNITY_EDITOR
        // Refresh the Asset Database so the file appears in the Project window
        AssetDatabase.Refresh();
#endif
    }
}

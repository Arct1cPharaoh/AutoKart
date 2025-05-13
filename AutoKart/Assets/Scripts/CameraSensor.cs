using UnityEngine;

public class CameraSensor : MonoBehaviour
{
    [Header("Camera Settings")]
    public int width = 640;
    public int height = 480;

    private Camera cam;
    private RenderTexture tex;
    private Texture2D img;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraSensor: No Camera component found!");
            enabled = false;
            return;
        }
        tex = new RenderTexture(width, height, 24);
        img = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.targetTexture = tex;
    }

    public Texture2D CaptureFrame() {
        RenderTexture curRT = RenderTexture.active;
        RenderTexture.active = tex;

        cam.Render();
        img.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        img.Apply();

        RenderTexture.active = curRT;

        return img;
    }
}

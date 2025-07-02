using UnityEngine;
using System.Collections.Generic;

public struct DetectedCone
{
    public Rect boundingBox;
    public Color color;
}

public class ConeDetector : MonoBehaviour
{
    [Header("Capture Settings")]
    [SerializeField] private CameraSensor cameraSensor;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private string imageSaveFolder = "CapturedFrames";
    [SerializeField] private bool captureOnStart = false;
    [SerializeField] private bool saveFrames = false;

    [Header("Filter Settings")]
    [SerializeField] private int contourMergePad = 20;
    [SerializeField] private int minPoints = 10;
    [SerializeField] private int minSize = 5;

    private float frameTimer = 0f;
    private int frameCounter = 0;

    private ContourDetector contourDetector;
    private ConeFilter coneFilter;

    void Awake()
    {
        contourDetector = new ContourDetector();
        coneFilter = new ConeFilter(minPoints, minSize, contourMergePad);
    }

    void Start()
    {
        if (captureOnStart)
        {
            Texture2D initialFrame = cameraSensor.CaptureFrame();
            DetectCones(initialFrame);
        }
    }

    public int GetCameraWidth() => cameraSensor.GetCameraWidth();
    public int GetCameraHeight() => cameraSensor.GetCameraHeight();


    // ------------------------------------------------------------------------
    // Masking
    // ------------------------------------------------------------------------

    List<Vector2> carMask = new List<Vector2>
    {
        new Vector2(0.2f, 0.25f),  // bottom left
        new Vector2(0.26f, 0.34f), // mid left
        new Vector2(0.37f, 0.37f),  // mid left
        new Vector2(0.5f, 0.43f),  // center
        new Vector2(0.63f, 0.37f),  // mid right
        new Vector2(0.74f, 0.34f), // mid right
        new Vector2(0.8f, 0.25f)   // bottom right
    };

    // ------------------------------------------------------------------------
    // End Masking
    // ------------------------------------------------------------------------

    private List<DetectedCone> DetectCones(Texture2D img)
    {
        Texture2D gray = Image.ToGrayScale(img);
        // Image.ApplyPolygonMask(gray, carMask);

        // Crop out top (sky) and bottom (car)
        Vector2 cropOffset;
        Texture2D cropped = Image.Crop(
            gray,
            out cropOffset,
            topPercent: 0.29f,
            bottomPercent: 0.45f
        );

        Image.SaveAsync(cropped, "CapturedFrames", -1);

        List<Contour> rawContours = contourDetector.Detect(cropped);
        List<DetectedCone> detected = coneFilter.FilterContours(
            rawContours, img, cropOffset
        );

        if (!saveFrames)
        {
            return detected;
        }

        // Draw cone bboxes
        foreach (var cone in detected)
        {
            Rect box = cone.boundingBox;
            Color col = cone.color;
            box.position += cropOffset;
            Draw.Box(img, box, col);
        }

        // Image.ApplyPolygonMask(img, carMask);
        Image.SaveAsync(img, imageSaveFolder, frameCounter);
        frameCounter++;

        return detected;
    }

    public List<DetectedCone> TryDetectFrame(float deltaTime)
    {
        frameTimer += deltaTime;

        if (captureOnStart || frameTimer < 1f / frameRate)
            return null;

        frameTimer = 0f;

        Texture2D frame = cameraSensor.CaptureFrame();
        return DetectCones(frame);
    }
}

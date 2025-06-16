using UnityEngine;
using System.Collections.Generic;

public struct DetectedCone
{
    public Rect boundingBox;
    public string color;
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

    private List<DetectedCone> DetectCones(Texture2D img)
    {
        Texture2D gray = Image.ToGrayScale(img);

        List<Contour> rawContours = contourDetector.DetectContoursFromRaw(gray);
        List<DetectedCone> detected = coneFilter.FilterContours(rawContours, "raw");


        if (!saveFrames)
        {
            return detected;
        }

        // Draw raw cone bboxes
        foreach (var cone in detected)
            Draw.Box(img, cone.boundingBox, Color.green);

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

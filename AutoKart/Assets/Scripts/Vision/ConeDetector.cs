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

/*
public class ConeDetector : MonoBehaviour
{
    [SerializeField] private CameraSensor cameraSensor;
    [SerializeField] private bool captureOnStart = false;
    [SerializeField] private string imageSaveFolder = "CapturedFrames";

    [Range(1, 60)] public float frameRate = 30f;
    private float frameTimer;
    private int frameCounter = 0;

    // Contour Detector Config
    [SerializeField] private int contourMergePad = 20;
    [SerializeField] private int minPoints = 10;
    [SerializeField] private int minSize = 5;

    [SerializeField] ConeMapper coneMapper;
    SelfDriving core;

    public struct DetectedCone
    {
        public Rect boundingBox;
        public string color;
    }

    void Start()
    {
        if (captureOnStart)
        {
            Texture2D img = cameraSensor.CaptureFrame();
            DetectCones(img);
        }

        core = GetComponentInParent<SelfDriving>();
    }

    void Update()
    {
        if (captureOnStart) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            Texture2D img = cameraSensor.CaptureFrame();
            DetectCones(img);
            frameTimer = 0f;
        }
    }

    List<Contour> MergeWhiteContours(List<Contour> baseContours,
        List<Contour> whiteContours, int pad)
    {
        var merged = new List<Contour>();

        foreach (var baseContour in baseContours)
        {
            var baseBox = baseContour.GetBoundingBox();
            var mergedPoints = new List<Vector2Int>(baseContour.points);

            foreach (var white in whiteContours)
            {
                var whiteBox = white.GetBoundingBox();
                if (ContourDetector.Overlaps(baseBox, whiteBox, pad))
                    mergedPoints.AddRange(white.points);
            }

            merged.Add(new Contour { points = mergedPoints });
        }

        return merged;
    }

    List<Contour> FilterContainedContours(List<Contour> contours)
    {
        int count = contours.Count;
        var keep = new bool[count];
        Array.Fill(keep, true);

        var boxes = new RectInt[count];
        for (int i = 0; i < count; i++)
            boxes[i] = contours[i].GetBoundingBox();

        for (int i = 0; i < count; i++)
        {
            if (!keep[i]) continue;

            for (int j = 0; j < count; j++)
            {
                if (i == j || !keep[j]) continue;
                if (ContourDetector.IsInside(boxes[i], boxes[j]))
                {
                    keep[i] = false;
                    break;
                }
            }
        }

        var filtered = new List<Contour>();
        for (int i = 0; i < count; i++)
            if (keep[i])
                filtered.Add(contours[i]);

        return filtered;
    }

    List<DetectedCone> CreateDetectedCones(List<Contour> contours, string label,
        int minPoints, int minSize)
    {
        var cones = new List<DetectedCone>();

        foreach (var c in contours)
        {
            if (c.points.Count < minPoints)
                continue;

            var box = c.GetBoundingBox();
            if (box.width < minSize || box.height < minSize)
                continue;

            cones.Add(new DetectedCone
            {
                boundingBox = new Rect(box.x, box.y, box.width, box.height),
                color = label
            });
        }

        return cones;
    }


    List<DetectedCone> ProcessRawContours(Texture2D img, ContourDetector detector)
    {
        // Stopwatch stopwatch = Stopwatch.StartNew();

        var rawContours = detector.DetectContoursFromRaw(img);
        // stopwatch.Stop();
        // UnityEngine.Debug.Log($"raw grayscale contour detection took {stopwatch.ElapsedMilliseconds} ms");

        // stopwatch = Stopwatch.StartNew();
        var merged = ContourDetector.MergeContours(rawContours, contourMergePad);
        var filtered = FilterContainedContours(merged);
        // stopwatch.Stop();
        // UnityEngine.Debug.Log($"merging + filtering took {stopwatch.ElapsedMilliseconds} ms");

        return CreateDetectedCones(filtered, "raw", minPoints, minSize);
    }

    void SaveFrameImage(Texture2D img)
    {
        byte[] bytes = img.EncodeToJPG(75);

        string folderPath = Path.Combine(Application.dataPath, imageSaveFolder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filename = $"frame_{frameCounter:D5}.jpg";
        string filePath = Path.Combine(folderPath, filename);

        Task.Run(() => File.WriteAllBytes(filePath, bytes));

        // Debug.Log($"Saved frame {frameCounter} to: {filePath}");
        frameCounter++;
    }

    Texture2D ToGrayScale(Texture2D img)
    {
        // Convert to grayscale in-place
        int width = img.width;
        int height = img.height;
        Color32[] pixels = img.GetPixels32();
        byte[] gray = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            gray[i] = (byte)((c.r * 0.3f) + (c.g * 0.59f) + (c.b * 0.11f));
        }

        Texture2D grayTex = new Texture2D(width, height, TextureFormat.R8, false);
        grayTex.LoadRawTextureData(gray);
        grayTex.Apply();
        return grayTex;
    }

    // Temp
    public Camera cam;

    void DetectCones(Texture2D img)
    {
        Texture2D grayTex = ToGrayScale(img);

        var detector = new ContourDetector();
        var detectedCones = ProcessRawContours(grayTex, detector);

        // Draw raw cones
        foreach (var cone in detectedCones)
        {
            Draw.Box(img, cone.boundingBox, Color.green);

            Vector3 worldPos = EstimateConePos(
                cone.boundingBox, cam.transform, img
            );

           Vector3 carPos = core.GetEstimatedPosition();
           coneMapper.RegisterConeEstimate(worldPos, carPos);
        }

        // SaveFrameImage(img);
    }
}
*/

using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.Collections.Generic;

public struct DetectedCone
{
    public Rect boundingBox;
    public Color color;
}

public struct StereoDetectedCone
{
    public DetectedCone leftFrame;
    public DetectedCone rightFrame;
    public Color color;
}

public class ConeDetector : MonoBehaviour
{
    [Header("Capture Settings")]
    [SerializeField] private CameraSensor cameraSensorLeft;
    [SerializeField] private CameraSensor cameraSensorRight;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private string imageSaveFolder = "CapturedFrames";
    [SerializeField] private bool captureOnStart = false;
    [SerializeField] private bool saveFrames = false;

    private float frameTimer = 0f;
    private int frameCounter = 0;

    [Header("Model Settings")]
    [SerializeField] private ModelAsset onnxModel;
    private Model runtimeModel;
    private Worker worker;

    void Awake()
    {
        runtimeModel = ModelLoader.Load(onnxModel);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    void Start()
    {
        if (captureOnStart)
        {
            Texture2D leftFrame = cameraSensorLeft.CaptureFrame();
            Texture2D rightFrame = cameraSensorRight.CaptureFrame();
            DetectConesStereo(leftFrame, rightFrame);
        }
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    public int GetCameraWidth() => cameraSensorLeft.GetCameraWidth();
    public int GetCameraHeight() => cameraSensorLeft.GetCameraHeight();

    Tensor<float> PreprocessImage(Texture2D img, int width, int height)
    {
        Texture2D resized = Image.Resize(img, width, height);
        Image.SaveAsync(resized, imageSaveFolder, -2);
        return TextureConverter.ToTensor(resized);
    }

    Tensor<float> RunInference(Tensor<float> input)
    {
        worker.Schedule(input);
        return worker.PeekOutput() as Tensor<float>;
    }

    // ------------------------------------------ put in helper class

    float[] Softmax(float[] logits)
    {
        float max = logits.Max();
        float sum = 0f;
        float[] exp = new float[logits.Length];
        for (int i = 0; i < logits.Length; i++)
        {
            exp[i] = Mathf.Exp(logits[i] - max);
            sum += exp[i];
        }

        for (int i = 0; i < exp.Length; i++)
            exp[i] /= sum;

        return exp;
    }

    private List<DetectedCone>
    ApplyNMS(List<DetectedCone> detections, float iouThreshold = 0.45f)
    {
        if (detections.Count == 0) return detections;

        // Sort by confidence (stored in color.a as alpha, if needed)
        detections = detections.OrderByDescending(d => d.color.a).ToList();
        List<DetectedCone> results = new List<DetectedCone>();

        while (detections.Count > 0)
        {
            DetectedCone best = detections[0];
            results.Add(best);
            detections.RemoveAt(0);

            detections = detections.Where(
                d => IoU(best.boundingBox, d.boundingBox) < iouThreshold
            ).ToList();
        }

        return results;
    }

    private float IoU(Rect a, Rect b)
    {
        float x1 = Mathf.Max(a.xMin, b.xMin);
        float y1 = Mathf.Max(a.yMin, b.yMin);
        float x2 = Mathf.Min(a.xMax, b.xMax);
        float y2 = Mathf.Min(a.yMax, b.yMax);

        float intersection = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float union = a.width * a.height + b.width * b.height - intersection;
        return intersection / union;
    }

    // --------------------------------------------------------

    List<DetectedCone>
    ParseDetections(Tensor<float> output, int imageWidth, int imageHeight)
    {
        List<DetectedCone> detected = new List<DetectedCone>();

        int channels = output.shape[1]; // Should be 9
        int numBoxes = output.shape[2]; // Should be 8400

        // Class color map
        Color[] classColors = new Color[]
        {
            Color.yellow,            // yellow_cone
            Color.blue,              // blue_cone
            new Color(1f, 0.5f, 0f), // orange_cone
            new Color(1f, 0.3f, 0f), // large_orange_cone
            Color.gray               // unknown_cone
        };

        for (int i = 0; i < numBoxes; i++)
        {
            float x = output[0, 0, i];
            float y = output[0, 1, i];
            float w = output[0, 2, i];
            float h = output[0, 3, i];

            float[] classLogits = new float[5];
            for (int j = 0; j < 5; j++)
                classLogits[j] = output[0, 4 + j, i];

            float[] classProbs = Softmax(classLogits);

            for (int classId = 0; classId < classProbs.Length; classId++)
            {
                if (classProbs[classId] < 0.33f)
                    continue;

                float left = (x - w / 2) * imageWidth / 640f;
                float width = w * imageWidth / 640f;
                float height = h * imageHeight / 640f;
                float top = (y - h / 2) * imageHeight / 640f;
                float flippedTop = imageHeight - top - height;

                Rect rect = new Rect(left, flippedTop, width, height);

                Color color = classColors[classId];

                detected.Add(new DetectedCone { boundingBox = rect, color = color });
            }
        }

        return detected;
    }

    void SaveAnnotatedImage(Texture2D img, List<DetectedCone> cones)
    {
        foreach (var cone in cones)
        {
            Draw.Box(img, cone.boundingBox, cone.color);
        }

        Image.SaveAsync(img, imageSaveFolder, frameCounter++);
    }

    List<DetectedCone> DetectCones(Texture2D img)
    {
        using Tensor<float> input = PreprocessImage(img, 640, 640);
        using Tensor<float> rawOutput = RunInference(input);
        using Tensor<float> output = rawOutput.ReadbackAndClone();

        List<DetectedCone> cones = ParseDetections(output, img.width, img.height);

        // Apply Non-Maximum Suppression to merge overlapping boxes
        cones = ApplyNMS(cones, 0.45f);

        return cones;
    }

    // ---------------- Stereo Matching ----------------

    int FindBestRightMatch(DetectedCone left, List<DetectedCone> rightCones,
        HashSet<int> unmatchedRight, int imageWidth, float baseThreshold = 0.05f,
        float maxThreshold = 0.3f)
    {
        // Adaptive threshold scales with cone width (closer cones get larger threshold)
        float widthRatio = left.boundingBox.width / imageWidth;
        float adaptiveThreshold = Mathf.Lerp(
            baseThreshold, maxThreshold, Mathf.Clamp01(widthRatio * 10f)
        );

        float xLeftNorm = left.boundingBox.center.x / imageWidth;

        float bestDist = float.MaxValue;
        int bestIndex = -1;

        foreach (int i in unmatchedRight)
        {
            float xRightNorm = rightCones[i].boundingBox.center.x / imageWidth;
            float dist = Mathf.Abs(xLeftNorm - xRightNorm);

            if (dist <= adaptiveThreshold && dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    bool ColorsMatch(DetectedCone a, DetectedCone b) => a.color == b.color;

    StereoDetectedCone CreateStereoCone(DetectedCone left, DetectedCone right)
    {
        return new StereoDetectedCone { leftFrame = left, rightFrame = right, color = left.color };
    }

    List<StereoDetectedCone> MatchStereoCones(List<DetectedCone> leftCones,
            List<DetectedCone> rightCones, int imageWidth, float maxXDistNorm = 0.05f)
    {
        List<StereoDetectedCone> matched = new List<StereoDetectedCone>();
        HashSet<int> unmatchedRight = new HashSet<int>(Enumerable.Range(0, rightCones.Count));

        foreach (DetectedCone left in leftCones)
        {
            int bestIndex = FindBestRightMatch(left, rightCones, unmatchedRight, imageWidth, maxXDistNorm);
            if (bestIndex != -1 && ColorsMatch(left, rightCones[bestIndex]))
            {
                matched.Add(CreateStereoCone(left, rightCones[bestIndex]));
                unmatchedRight.Remove(bestIndex);
            }
        }
        return matched;
    }

    void AnnotateImages(Texture2D leftImg, Texture2D rightImg,
        List<DetectedCone> leftCones, List<DetectedCone> rightCones)
    {
        foreach (var cone in leftCones)
            Draw.Box(leftImg, cone.boundingBox, cone.color);

        foreach (var cone in rightCones)
            Draw.Box(rightImg, cone.boundingBox, cone.color);

        // Merge both views horizontally (side by side)
        Texture2D merged = Image.MergeHori(leftImg, rightImg);
        Image.SaveAsync(merged, imageSaveFolder, frameCounter++);
    }

    private List<StereoDetectedCone>
    DetectConesStereo(Texture2D leftImg, Texture2D rightImg)
    {
        var leftCones = DetectCones(leftImg);
        var rightCones = DetectCones(rightImg);
        var stereoCones = MatchStereoCones(leftCones, rightCones, leftImg.width);

        if (saveFrames)
            AnnotateImages(leftImg, rightImg, leftCones, rightCones);

        return stereoCones;
    }

    public List<StereoDetectedCone> TryDetectFrame(float deltaTime)
    {
        frameTimer += deltaTime;
        if (captureOnStart || frameTimer < 1f / frameRate)
            return null;

        frameTimer = 0f;
        Texture2D leftFrame = cameraSensorLeft.CaptureFrame();
        Texture2D rightFrame = cameraSensorRight.CaptureFrame();
        return DetectConesStereo(leftFrame, rightFrame);
    }
}

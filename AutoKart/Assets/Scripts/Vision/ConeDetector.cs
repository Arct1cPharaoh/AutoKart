using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    [Header("Filter Settings")]
    [SerializeField] private int contourMergePad = 20;
    [SerializeField] private int minPoints = 10;
    [SerializeField] private int minSize = 5;

    private float frameTimer = 0f;
    private int frameCounter = 0;

    private ContourDetector contourDetector;
    private ConeFilter coneFilter;
    private Vector2 cropOffset;

    void Awake()
    {
        contourDetector = new ContourDetector();
        coneFilter = new ConeFilter(minPoints, minSize, contourMergePad);
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

    public int GetCameraWidth() => cameraSensorLeft.GetCameraWidth();
    public int GetCameraHeight() => cameraSensorLeft.GetCameraHeight();


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

    List<DetectedCone> DetectCones(Texture2D img)
    {
        Texture2D gray = Image.ToGrayScale(img);

        // Crop out top (sky) and bottom (car)
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


        return detected;
    }

    int FindBestRightMatch(
        DetectedCone left,
        List<DetectedCone> rightCones,
        HashSet<int> unmatchedRight,
        int imageWidth,
        float maxXDistNorm
    )
    {
        float xLeftNorm = left.boundingBox.center.x / imageWidth;
        float bestDist = float.MaxValue;
        int bestIndex = -1;

        foreach (int i in unmatchedRight)
        {
            float xRightNorm = rightCones[i].boundingBox.center.x / imageWidth;
            float dist = Mathf.Abs(xLeftNorm - xRightNorm);

            if (dist <= maxXDistNorm && dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    bool ColorsMatch(DetectedCone a, DetectedCone b)
    {
        return a.color == b.color;
    }

    StereoDetectedCone CreateStereoCone(DetectedCone left, DetectedCone right)
    {
        return new StereoDetectedCone
        {
            leftFrame = left,
            rightFrame = right,
            color = left.color // safe to use either since they're equal
        };
    }

    List<StereoDetectedCone> MatchStereoCones(
        List<DetectedCone> leftCones,
        List<DetectedCone> rightCones,
        int imageWidth,
        float maxXDistNorm = 0.05f
    )
    {
        List<StereoDetectedCone> matched = new List<StereoDetectedCone>();
        HashSet<int> unmatchedRight = new HashSet<int>(
            Enumerable.Range(0, rightCones.Count)
        );

        foreach (DetectedCone left in leftCones)
        {
            int bestIndex = FindBestRightMatch(
                left, rightCones, unmatchedRight, imageWidth, maxXDistNorm
            );

            if (bestIndex != -1 && ColorsMatch(left, rightCones[bestIndex]))
            {
                StereoDetectedCone stereoCone = CreateStereoCone(
                    left, rightCones[bestIndex]
                );
                matched.Add(stereoCone);
                unmatchedRight.Remove(bestIndex);
            }
        }

        return matched;
    }

    void AnotateImages(Texture2D leftImg, Texture2D rightImg,
        List<DetectedCone> leftCones, List<DetectedCone> rightCones)
    {
        // Annotate boxes on left and right image
        foreach (var cone in leftCones)
        {
            Rect box = cone.boundingBox;
            Color col = cone.color;
            box.position += cropOffset;
            Draw.Box(leftImg, box, col);
        }

        // Annotate right image
        foreach (var cone in rightCones)
        {
            Rect box = cone.boundingBox;
            Color col = cone.color;
            box.position += cropOffset;
            Draw.Box(rightImg, box, col);
        }

        // Texture2D merged = Image.MergeOpacity(
        //     leftImg, rightImg, alpha: 0.5f
        // );
        Texture2D merged = Image.MergeHori(leftImg, rightImg);
        Image.SaveAsync(merged, imageSaveFolder, frameCounter);
        frameCounter++;
    }

    private List<StereoDetectedCone> DetectConesStereo(Texture2D leftImg, Texture2D rightImg)
    {
        var leftCones = DetectCones(leftImg);
        var rightCones = DetectCones(rightImg);

        var stereoCones = MatchStereoCones(leftCones, rightCones, leftImg.width);

        if (saveFrames)
            AnotateImages(leftImg, rightImg, leftCones, rightCones);

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

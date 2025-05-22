using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class ConeDetector : MonoBehaviour
{
    [SerializeField] private CameraSensor cameraSensor;
    [SerializeField] private bool captureOnStart = true;

    [Range(1, 60)] public float frameRate = 30f;
    private float frameTimer;

    // Contour Detector Config
    [SerializeField] private int contourMergePad = 20;
    [SerializeField] private int whiteMergePad = 2;
    [SerializeField] private int minPoints = 10;
    [SerializeField] private int minSize = 5;

    public struct DetectedCone
    {
        public Rect boundingBox;
        public string color;
    }

    void Start()
    {
        // Debugging case TODO: Remove later
        if (captureOnStart)
        {
            Texture2D img = cameraSensor.CaptureFrame();
            DetectCones(img);
        }
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

    List<DetectedCone> ProcessMaskedContours(bool[] colorMask, bool[] whiteMask,
        string colorLabel, int width, int height, ContourDetector detector)
    {
        Texture2D colorTex = ConeColorMask.BoolMaskToTex(colorMask, width, height);
        Texture2D whiteTex = ConeColorMask.BoolMaskToTex(whiteMask, width, height);

        var colorContours = detector.Detect(colorTex);
        var whiteContours = detector.Detect(whiteTex);

        var mergedBase = ContourDetector.MergeContours(colorContours, contourMergePad);
        var withWhite = MergeWhiteContours(mergedBase, whiteContours, whiteMergePad);
        var finalContours = ContourDetector.MergeContours(withWhite, contourMergePad);

        finalContours = FilterContainedContours(finalContours);

        return CreateDetectedCones(finalContours, colorLabel, minPoints, minSize);
    }

    void DetectCones(Texture2D img)
    {
        var mask = ConeColorMask.ExtractConeMask(img);
        var detector = new ContourDetector();
        var detectedCones = new List<DetectedCone>();

        detectedCones.AddRange(ProcessMaskedContours(
            mask.yellowMask, mask.whiteMask, "yellow",
            img.width, img.height, detector));
        detectedCones.AddRange(ProcessMaskedContours(
            mask.blueMask, mask.whiteMask, "blue",
            img.width, img.height, detector));

        // Draw boxes
        foreach (var cone in detectedCones)
        {
            Color color = cone.color == "yellow" ? Color.yellow : Color.blue;
            Draw.Box(img, cone.boundingBox, color);
        }

        // Save img
        byte[] bytes = img.EncodeToPNG();
        string path = Application.dataPath + "/DebugCapturedImage.png";
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"Saved image to: {path}");
    }
}
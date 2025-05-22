using UnityEngine;
using System.Collections.Generic;

public class ContourDetector
{
    private CannyEdgeDetector.CannyParams cannyParams;

    public ContourDetector(CannyEdgeDetector.CannyParams parameters = null)
    {
        this.cannyParams = parameters ?? new CannyEdgeDetector.CannyParams();
    }

    public List<Contour> Detect(Texture2D img)
    {
        int width, height;

        // Edge Detection
        bool[] edgeMap = CannyEdgeDetector.Apply(img, out width, out height,
            cannyParams);

        // Border Following to extract contours
        List<Contour> contours = SuzukiAbeBorderFollower.TraceContours(
            edgeMap, width, height);

        return contours;
    }

    public static bool Overlaps(RectInt a, RectInt b, int pad = 0)
    {
        return a.xMin - pad < b.xMax && a.xMax + pad > b.xMin &&
           a.yMin - pad < b.yMax && a.yMax + pad > b.yMin;
    }

    public static bool IsInside(RectInt a, RectInt b)
    {
        return a.xMin >= b.xMin && a.xMax <= b.xMax &&
            a.yMin >= b.yMin && a.yMax <= b.yMax;
    }

    public static RectInt Expand(RectInt a, RectInt b)
    {
        int xMin = Mathf.Min(a.xMin, b.xMin);
        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    static bool TryMergeContours(RectInt baseBox, Contour other, int pad,
        out RectInt newBox)
    {
        RectInt otherBox = other.GetBoundingBox();

        if (!Overlaps(baseBox, otherBox, pad))
        {
            newBox = default;
            return false;
        }

        Vector2 centerA = baseBox.center;
        Vector2 centerB = otherBox.center;
        float dx = Mathf.Abs(centerA.x - centerB.x);
        float avgWidth = (baseBox.width + otherBox.width) * 0.5f;

        if (dx > avgWidth * 0.25f)
        {
            newBox = default;
            return false;
        }

        newBox = Expand(baseBox, otherBox);
        return true;
    }

    public static List<Contour> MergeContours(List<Contour> contours, int pad)
    {
        var mergedContours = new List<Contour>();
        var used = new bool[contours.Count];

        for (int i = 0; i < contours.Count; i++)
        {
            if (used[i]) continue;

            Contour baseContour = contours[i];
            var groupPoints = new List<Vector2Int>(baseContour.points);
            var groupBox = baseContour.GetBoundingBox();

            used[i] = true;

            for (int j = i + 1; j < contours.Count; j++)
            {
                if (used[j]) continue;

                if (TryMergeContours(groupBox, contours[j], pad, out RectInt expandedBox))
                {
                    groupPoints.AddRange(contours[j].points);
                    groupBox = expandedBox;
                    used[j] = true;
                }
            }

            mergedContours.Add(new Contour { points = groupPoints });
        }

        return mergedContours;
    }
}
using UnityEngine;
using System.Collections.Generic;

public class Contour
{
    public List<Vector2Int> points = new List<Vector2Int>();
    public bool isOuter = true;

    public RectInt GetBoundingBox()
    {
        if (points.Count == 0) return new RectInt();

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int. MinValue;

        foreach (var pt in points)
        {
            minX = Mathf.Min(minX, pt.x);
            minY = Mathf.Min(minY, pt.y);
            maxX = Mathf.Max(maxX, pt.x);
            maxY = Mathf.Max(maxY, pt.y);
        }

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
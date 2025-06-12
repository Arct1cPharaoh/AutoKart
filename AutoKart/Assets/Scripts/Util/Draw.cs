using UnityEngine;
using System.Collections.Generic;

public static class Draw
{
    // Bresenham-like line drawing
    public static void Line(Texture2D tex, Vector2 a, Vector2 b, Color color)
    {
        int x0 = Mathf.RoundToInt(a.x);
        int y0 = Mathf.RoundToInt(a.y);
        int x1 = Mathf.RoundToInt(b.x);
        int y1 = Mathf.RoundToInt(b.y);

        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            tex.SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public static void Triangle(Texture2D img, Vector2[] tri, Color color)
    {
        if (tri.Length != 3) {
            Debug.LogError("Cannot Draw trangle with more than 3 points.");
            return;
        }

        Line(img, tri[0], tri[1], color);
        Line(img, tri[1], tri[2], color);
        Line(img, tri[2], tri[0], color);
        img.Apply();
    }

    public static void Box(Texture2D img, Rect rect, Color color)
    {
        int minX = Mathf.RoundToInt(rect.xMin);
        int maxX = Mathf.RoundToInt(rect.xMax);
        int minY = Mathf.RoundToInt(rect.yMin);
        int maxY = Mathf.RoundToInt(rect.yMax);

        for (int x = minX; x <= maxX; x++)
        {
            img.SetPixel(x, minY, color);
            img.SetPixel(x, maxY, color);
        }

        for (int y = minY; y <= maxY; y++)
        {
            img.SetPixel(minX, y, color);
            img.SetPixel(maxX, y, color);
        }

        img.Apply();
    }
}
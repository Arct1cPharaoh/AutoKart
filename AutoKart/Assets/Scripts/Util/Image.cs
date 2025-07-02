using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public static class Image
{
    public static void Save(Texture2D tex, string folderName, string fileName)
    {
        string folderPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, fileName);
        byte[] bytes = tex.EncodeToJPG(75);
        File.WriteAllBytes(filePath, bytes);
    }

    public static void SaveAsync(Texture2D img, string folder, int index)
    {
        byte[] bytes = img.EncodeToJPG(75);

        string folderPath = Path.Combine(Application.dataPath, folder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filename = $"frame_{index:D5}.jpg";
        string path = Path.Combine(folderPath, filename);

        Task.Run(() => File.WriteAllBytes(path, bytes));
    }

    public static Texture2D ToGrayScale(Texture2D input)
    {
        int width = input.width;
        int height = input.height;
        Color32[] pixels = input.GetPixels32();
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

    public static Texture2D GrayScaleToRGB(Texture2D grayTex)
    {
        int width = grayTex.width;
        int height = grayTex.height;
        byte[] gray = grayTex.GetRawTextureData();

        Color32[] rgbPixels = new Color32[gray.Length];
        for (int i = 0; i < gray.Length; i++)
        {
            byte g = gray[i];
            rgbPixels[i] = new Color32(g, g, g, 255);
        }

        Texture2D rgbTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        rgbTex.SetPixels32(rgbPixels);
        rgbTex.Apply();
        return rgbTex;
    }

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                (point.x < (polygon[j].x - polygon[i].x) *
                 (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public static void ApplyPolygonMask(Texture2D image, List<Vector2> normalizedPoints)
    {
        int width = image.width;
        int height = image.height;

        // Convert normalized coords to pixel-space
        Vector2[] pixelPoints = normalizedPoints
            .Select(p => new Vector2(p.x * width, p.y * height))
            .ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 pt = new Vector2(x, y);
                if (IsPointInPolygon(pt, pixelPoints))
                {
                    image.SetPixel(x, y, Color.black);
                }
            }
        }

        image.Apply();
    }

    public static Texture2D Crop(
        Texture2D input,
        out Vector2 offset,
        float? topPercent = null,
        float? bottomPercent = null,
        float? leftPercent = null,
        float? rightPercent = null)
    {
        int width = input.width;
        int height = input.height;

        int bottomCrop = (int)(bottomPercent.HasValue ? height * bottomPercent.Value : 0);
        int topCrop = (int)(topPercent.HasValue ? height * topPercent.Value : 0);

        int startY = bottomCrop;
        int endY = height - topCrop;

        int startX = (int)(leftPercent.HasValue ? width * leftPercent.Value : 0);
        int endX = (int)(rightPercent.HasValue ? width * (1f - rightPercent.Value) : width);

        int croppedWidth = endX - startX;
        int croppedHeight = endY - startY;

        offset = new Vector2(startX, startY); // 👈 report shift

        Texture2D cropped = new Texture2D(croppedWidth, croppedHeight, input.format, false);
        Color32[] fullPixels = input.GetPixels32();
        Color32[] croppedPixels = new Color32[croppedWidth * croppedHeight];

        for (int y = 0; y < croppedHeight; y++)
        {
            int srcY = startY + y;
            Array.Copy(
                fullPixels, srcY * width + startX,
                croppedPixels, y * croppedWidth,
                croppedWidth);
        }

        cropped.SetPixels32(croppedPixels);
        cropped.Apply();

        return cropped;
    }
}

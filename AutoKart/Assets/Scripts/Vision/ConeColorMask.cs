using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class ConeColorMask
{
    // Yellow Cones (Hue ~30°)
    static bool IsYellowHSV(float h, float s, float v)
    {
        return h > 0.15f && h < 0.18f && s > 0.4f && v > 0.4f;
    }

    // Blue Cones (Hue ~220-240°)
    static bool IsBlueHSV(float h, float s, float v)
    {
        return h > 0.55f && h < 0.72f && s > 0.3f && v > 0.3f;
    }

    // White stripes
    static bool IsWhiteHSV(float h, float s, float v)
    {
        return (v > 0.85f && s < 0.2f) || (v > 0.7f && s < 0.25f);
    }

    static Color? GetConeColor(float h, float s, float v)
    {
        if (IsYellowHSV(h, s, v)) return Color.yellow;
        if (IsBlueHSV(h, s, v)) return Color.blue;
        return null;
    }

    static Vector2Int SampleBoxPoint(RectInt box, Vector2 cropOffset)
    {
        float u = Random.value;
        float v = Random.value;

        int px = Mathf.Clamp((int)(box.x + cropOffset.x + box.width * u), 0, int.MaxValue);
        int py = Mathf.Clamp((int)(box.y + cropOffset.y + box.height * v), 0, int.MaxValue);

        return new Vector2Int(px, py);
    }

    public static Color? SampleConeColor(Texture2D image, RectInt box,
            Vector2 cropOffset, int sampleCount = 50,
            float minVoteFraction = 0.2f, bool debug = false)
    {
        Dictionary<Color, int> voteCounts = new();
        Texture2D debugImg = new Texture2D(image.width, image.height, image.format, false);
        debugImg.SetPixels32(image.GetPixels32());

        for (int i = 0; i < sampleCount; i++)
        {
            Vector2Int p = SampleBoxPoint(box, cropOffset);
            Color32 pixel = image.GetPixel(p.x, p.y);
            Color.RGBToHSV(pixel, out float h, out float s, out float v);

            Color? color = GetConeColor(h, s, v);
            Color debugColor = Color.black;
            if (color.HasValue)
            {
                if (!voteCounts.ContainsKey(color.Value))
                    voteCounts[color.Value] = 0;
                voteCounts[color.Value]++;
                debugColor = color.Value;
            }

        }

        if (debug)
        {
            debugImg.Apply();
            Image.SaveAsync(debugImg, "CapturedFrames", -2);

            foreach (var kvp in voteCounts)
                Debug.Log($"Color: {kvp.Key}, Votes: {kvp.Value}");
        }

        int minVotes = Mathf.CeilToInt(sampleCount * minVoteFraction);
        foreach (var kvp in voteCounts)
        {
            if (kvp.Value >= minVotes)
                return kvp.Key;
        }

        return null;
    }
}

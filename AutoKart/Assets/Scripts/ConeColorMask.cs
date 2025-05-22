using UnityEngine;
using System.IO;

public static class ConeColorMask
{
    public struct ConeMask
    {
        public bool[] yellowMask;
        public bool[] blueMask;
        public bool[] whiteMask;
        public int width;
        public int height;
    }

    // Yellow Cones (Hue ~30°)
    static bool isYellowHSV(float h, float s, float v)
    {
        return h > 0.10f && h < 0.18f && s > 0.4f && v > 0.4f;
    }

    // Blue Cones (Hue ~220-240°)
    static bool isBlueHSV(float h, float s, float v)
    {
        return h > 0.55f && h < 0.72f && s > 0.3f && v > 0.3f;
    }

    // White stripes
    static bool isWhiteHSV(float h, float s, float v)
    {
        return (v > 0.85f && s < 0.2f) || (v > 0.7f && s < 0.25f);
    }

    public static ConeMask ExtractConeMask(Texture2D img)
    {
        Color32[] pixels = img.GetPixels32();
        int width = img.width;
        int height = img.height;

        bool[] yellowMask = new bool[pixels.Length];
        bool[] blueMask = new bool[pixels.Length];
        bool[] whiteMask = new bool[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 rgb = pixels[i];
            // Convert to better format for processing
            Color.RGBToHSV(rgb, out float h, out float s, out float v);

            if (isYellowHSV(h, s, v))
                yellowMask[i] = true;
            else if (isBlueHSV(h, s, v))
                blueMask[i] = true;
            // White Stripes for merging
            else if (isWhiteHSV(h, s, v))
                whiteMask[i] = true;

            // Orange Cones TODO: Add later
        }

        return new ConeMask
        {
            yellowMask = yellowMask,
            blueMask = blueMask,
            whiteMask = whiteMask,
            width = width,
            height = height
        };
    }

    public static Texture2D BoolMaskToTex(bool[] mask, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.R8, false);
        Color32[] pixels = new Color32[mask.Length];

        for (int i = 0; i < mask.Length; i++)
            pixels[i] = mask[i] ? Color.white : Color.black;

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    public static void SaveMask(ConeMask mask)
    {
        Texture2D tex = new Texture2D(mask.width, mask.height, TextureFormat.RGB24, false);
        Color32[] pixels = new Color32[mask.width * mask.height];

        for (int i = 0; i < pixels.Length; i++)
        {
            bool y = mask.yellowMask[i];
            bool b = mask.blueMask[i];
            bool w = mask.whiteMask[i];

            pixels[i] = new Color32(
                (byte)(b ? 255 : 0),
                (byte)(y ? 255 : 0),
                (byte)(w ? 255 : 0),
                255
            );
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/DebugConeMask.png";
        File.WriteAllBytes(path, bytes);
        Debug.Log($"Saved cone mask debug image: {path}");
    }
}
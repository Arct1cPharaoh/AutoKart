using UnityEngine;
using System.IO;
using System.Threading.Tasks;

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
}

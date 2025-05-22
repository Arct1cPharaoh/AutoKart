using UnityEngine;
using System.Collections.Generic;

public class CannyEdgeDetector
{
    public class CannyParams
    {
        public float lowThresh = 0.1f;
        public float highThresh = 0.3f;
        public bool applyGaussian = false;
    }

    private static void ApplyGaussianBlur(float[] buffer, int width, int height,
        float sigma = 1.0f)
    {
        int radius = Mathf.CeilToInt(3 * sigma);
        int size = radius * 2 + 1;
        float[] kernel = new float[size];
        float sum = 0f;

        for (int i = 0; i < size; i++)
        {
            int x = i - radius;
            kernel[i] = Mathf.Exp(-(x * x) / (2 * sigma * sigma));
            sum += kernel[i];
        }

        // Normalize
        for (int i = 0; i < size; i++) kernel[i] /= sum;

        float[] temp = new float[buffer.Length];

        // Horizontal pass
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float acc = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int nx = Mathf.Clamp(x + k, 0, width - 1);
                    acc += buffer[y * width + nx] * kernel[k + radius];
                }

                temp[y * width + x] = acc;
            }
        }

        // Vertical pass
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float acc = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int ny = Mathf.Clamp(y + k, 0, height - 1);
                    acc += temp[ny * width + x] * kernel[k + radius];
                }

                buffer[y * width + x] = acc;
            }
        }
    }

    private static void ApplySobel(float[] src, int width, int height,
        float[] gradient, float[] dir)
    {
        int[] gx = {-1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1};
        int[] gy = {1, 2, 1,
                    0, 0, 0,
                    -1, -2, -1};

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                float sumX = 0, sumY = 0;

                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int pixelIdx = (y + ky) * width + (x + kx);
                        int kernalIdx = (ky + 1) * 3 + (kx + 1);
                        float val = src[pixelIdx];
                        sumX += gx[kernalIdx] * val;
                        sumY += gy[kernalIdx] * val;
                    }
                }

                int idx = y * width + x;
                gradient[idx] = Mathf.Sqrt(sumX * sumX + sumY * sumY);
                dir[idx] = Mathf.Atan2(sumY, sumX);
            }
        }
    }

    private static float[] NonMaxSuppression(float[] grad, float[] dir,
        int width, int height)
    {
        float[] result = new float[grad.Length];
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;
                float angle = dir[idx] * Mathf.Rad2Deg;
                angle = (angle + 180) % 180; // Normalize

                float a = 0, b = 0;

                // Directional suppression
                if (angle < 22.5 || angle >= 157.5)
                {
                    a = grad[idx - 1];
                    b = grad[idx + 1];
                }
                else if (angle < 67.5)
                {
                    a = grad[idx - width - 1];
                    b = grad[idx + width + 1];
                }
                else if (angle < 112.5)
                {
                    a = grad[idx - width];
                    b = grad[idx + width];
                }
                else
                {
                    a = grad[idx - width + 1];
                    b = grad[idx + width - 1];
                }

                result[idx] = (grad[idx] >= a && grad[idx] >= b) ? grad[idx] : 0f;
            }
        }

        return result;
    }

    private static bool[] Hysteresis(float[] nms, int width, int height,
        float low, float high)
    {
        bool[] result = new bool[nms.Length];
        Queue<int> queue = new Queue<int>();

        // First pass: mark stron edges
        for (int i = 0; i < nms.Length; i++)
        {
            if (nms[i] >= high)
            {
                result[i] = true;
                queue.Enqueue(i);
            }
        }

        // Second pass: follow connected weak edges
        int[] offsets = {-1, 1, -width, -width-1, -width+1, width-1, width+1};
        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            foreach (int off in offsets)
            {
                int nei = idx + off;
                if (nei >= 0 && nei < nms.Length && !result[nei] && nms[nei] >= low)
                {
                    result[nei] = true;
                    queue.Enqueue(nei);
                }
            }
        }

        return result;
    }

    public static bool[] Apply(Texture2D img, out int width, out int height,
        CannyParams parameters = null)
    {
        // Use default parameters if none
        if (parameters == null)
            parameters = new CannyParams();

        width = img.width;
        height = img.height;

        Color32[] pixels = img.GetPixels32();
        float[] grayScale = new float[pixels.Length];

        // Conver to grayscale
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            grayScale[i] = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
        }

        // Gaussian blur
        if (parameters.applyGaussian)
            ApplyGaussianBlur(grayScale, width, height, sigma: 1.0f);

        // Sobel filter
        float[] gradient = new float[pixels.Length];
        float[] dir = new float[pixels.Length];
        ApplySobel(grayScale, width, height, gradient, dir);

        // Non-Maximum Suppression
        float[] nms = NonMaxSuppression(gradient, dir, width, height);

        // Hysteresis Thresholding
        bool[] edgeMap = Hysteresis(nms, width, height, parameters.lowThresh,
            parameters.highThresh);

        return edgeMap;
    }
}
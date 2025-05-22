using UnityEngine;
using System.Collections.Generic;

public static class SuzukiAbeBorderFollower
{
    public static List<Contour> TraceContours(bool[] binaryImg, int width,
        int height)
    {
        List<Contour> contours = new();
        bool[] visited = new bool[binaryImg.Length];

        // 8 dirs clockwise (starting top left)
        int[] dx = {-1, 0, 1, 1, 1, 0, -1, -1};
        int[] dy = {-1, -1, -1, 0, 1, 1, 1, 0};

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;

                if (!binaryImg[idx] || visited[idx])
                    continue;

                Contour contour = new();
                Vector2Int start = new(x, y);
                Vector2Int cur = start;
                int dir = 7; // Start by looking left

                do
                {
                    visited[cur.y * width + cur.x] = true;
                    contour.points.Add(cur);

                    bool found = false;
                    for (int i = 0; i < 8; i++)
                    {
                        dir = (dir + 1) % 8;
                        int nx = cur.x + dx[dir];
                        int ny = cur.y + dy[dir];
                        int nidx = ny * width + nx;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                            binaryImg[nidx])
                        {
                            cur = new Vector2Int(nx, ny);
                            found = true;
                            // Rotate back to look around neighbor
                            dir = (dir + 6) % 8;
                            break;
                        }
                    }

                    if (!found)
                        break;
                } while (cur != start && contour.points.Count < 1000); // Prevents infinite loops

                if (contour.points.Count > 3)
                    contours.Add(contour);
            }
        }

        return contours;
    }
}
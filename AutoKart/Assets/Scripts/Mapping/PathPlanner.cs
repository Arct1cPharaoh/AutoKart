using UnityEngine;
using System.Collections.Generic;

public class PathPlanner
{
    public List<Vector3> waypoints = new();
    public List<Vector3> smoothedPath = new();

    private List<Vector3> SmoothPath(List<Vector3> input, int windowSize = 2)
    {
        List<Vector3> smoothed = new List<Vector3>();

        for (int i = 0; i < input.Count; i++)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            for (int j = -windowSize; j <= windowSize; j++)
            {
                int idx = i + j;
                if (idx >= 0 && idx < input.Count)
                {
                    sum += input[idx];
                    count++;
                }
            }

            smoothed.Add(sum / count);
        }

        return smoothed;
    }

    public List<Vector3> UpdatePlan(List<Vector3> blueCones, List<Vector3> yellowCones)
    {
        waypoints.Clear();

        int pairCount = Mathf.Min(blueCones.Count, yellowCones.Count);
        for (int i = 0; i < pairCount; i++)
        {
            Vector3 midpoint = (blueCones[i] + yellowCones[i]) * 0.5f;
            waypoints.Add(midpoint);

            // Debugging
            Debug.DrawLine(blueCones[i], midpoint, Color.blue, 0.1f);
            Debug.DrawLine(yellowCones[i], midpoint, Color.yellow, 0.1f);

            if (i > 0)
            {
                Debug.DrawLine(waypoints[i - 1], waypoints[i], Color.green, 0.1f);
            }
        }

        smoothedPath = SmoothPath(waypoints, 2);

        for (int i = 1; i < smoothedPath.Count; i++)
            Debug.DrawLine(smoothedPath[i - 1], smoothedPath[i], Color.white, 0.1f);

        return smoothedPath.Count > 0 ? smoothedPath : waypoints;
    }
}

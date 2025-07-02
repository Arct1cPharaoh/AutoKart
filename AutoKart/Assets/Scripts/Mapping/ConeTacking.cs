using UnityEngine;
using System.Collections.Generic;

public class ConeTracking
{
    class TrackedCone
    {
        public Vector3 worldPos;
        public Color color;
        public int updateCount = 1;
        public GameObject visual;
    }

    private readonly List<TrackedCone> trackedCones = new();
    private readonly ConeMapper coneMapper;

    private const float POS_THRESHOLD = 2.0f;
    private const int MIN_UPDATES_TO_CONFIRM = 5;
    private const float MAX_RANGE = 5f;

    public ConeTracking(ConeMapper mapper)
    {
        coneMapper = mapper;
    }

    public List<Vector3> GetConesByColor(Color color)
    {
        List<Vector3> result = new();
        // TODO: Make this automatic
        foreach (var cone in trackedCones)
        {
            if (cone.updateCount >= MIN_UPDATES_TO_CONFIRM && cone.color == color)
                result.Add(cone.worldPos);
        }
        return result;
    }

    TrackedCone FindMatchingCone(Vector3 pos, Color color)
    {
        // FIXME: This can be heavily optimized
        foreach (TrackedCone cone in trackedCones)
        {
            if (Vector3.Distance(cone.worldPos, pos) < POS_THRESHOLD &&
               cone.color == color)
                return cone;
        }
        return null;
    }

    public void RegisterCone(Vector3 estWorldPos, Color color)
    {
        TrackedCone match = FindMatchingCone(estWorldPos, color);

        // Create new cone
        if (match == null)
        {
            TrackedCone cone = new TrackedCone{
                worldPos = estWorldPos,
                color = color
            };
            trackedCones.Add(cone);

            if (cone.updateCount >= MIN_UPDATES_TO_CONFIRM)
                cone.visual = coneMapper.PlaceCone(estWorldPos, color);
            return;
        }

        if (match.updateCount == MIN_UPDATES_TO_CONFIRM)
            match.visual = coneMapper.PlaceCone(estWorldPos, color);

        float alpha = 1f / (match.updateCount + 1f); // exponential decay
        match.worldPos = Vector3.Lerp(match.worldPos, estWorldPos, alpha);
        match.updateCount++;

        if (match.visual != null)
            coneMapper.UpdateConePosition(match.visual, match.worldPos);
    }

    public Vector3 ComputePoseCorrection(Vector3 carPos, float carHeading)
    {
        Vector2 totalError = Vector2.zero;
        int count = 0;

        foreach (TrackedCone cone in trackedCones)
        {
            if (cone.updateCount < MIN_UPDATES_TO_CONFIRM) continue;

            // Expected position of cone in world space if the car is correctly located
            Vector2 forward = new Vector2(Mathf.Sin(carHeading), Mathf.Cos(carHeading));
            Vector2 carPos2D = new Vector2(carPos.x, carPos.z);
            Vector2 conePos2D = new Vector2(cone.worldPos.x, cone.worldPos.z);

            Vector2 expectedRel = conePos2D - carPos2D;
            float expectedDist = expectedRel.magnitude;

            if (expectedDist > MAX_RANGE) continue;

            // Add the difference between expected and actual cone position
            Vector2 error = conePos2D - (carPos2D + expectedRel); // simplified form
            totalError += error;
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        Vector2 avgError = totalError / count;
        Vector3 correction = new Vector3(avgError.x, 0f, avgError.y);

        // Apply smoothing (low-pass filter)
        Vector3 poseCorrection = Vector3.Lerp(Vector3.zero, correction, 0.02f);
        return poseCorrection;
    }

    public float ComputeHeadingCorrection(Vector3 carPos, float carHeading)
    {
        float totalHeadingError = 0f;
        int count = 0;

        Vector2 carPos2D = new Vector2(carPos.x, carPos.z);
        Vector2 forward = new Vector2(Mathf.Sin(carHeading), Mathf.Cos(carHeading));

        foreach (TrackedCone cone in trackedCones)
        {
            if (cone.updateCount < MIN_UPDATES_TO_CONFIRM) continue;

            Vector2 conePos2D = new Vector2(cone.worldPos.x, cone.worldPos.z);
            Vector2 toCone = conePos2D - carPos2D;

            float dist = toCone.magnitude;
            if (dist < 1f || dist > 20f) continue; // Filter unreliable cones

            toCone.Normalize();

            // Signed angle error between current forward and actual direction to cone
            float angleError = Mathf.Atan2(
                toCone.x * forward.y - toCone.y * forward.x,
                toCone.x * forward.x + toCone.y * forward.y
            );

            totalHeadingError += angleError;
            count++;
        }

        if (count == 0) return 0f;

        float avgHeadingError = totalHeadingError / count;

        // Clamp large jumps to prevent flipping
        float maxHeadingCorrection = Mathf.Deg2Rad * 2f;
        avgHeadingError = Mathf.Clamp(avgHeadingError, -maxHeadingCorrection, maxHeadingCorrection);

        // Scale correction based on cone confidence
        float trust = Mathf.Clamp01(count / 5f); // Full trust at 5+ cones
        float trustedCorrection = avgHeadingError * trust;

        // Smooth the correction (low-pass)
        return Mathf.Lerp(0f, trustedCorrection, 0.1f);
    }
}

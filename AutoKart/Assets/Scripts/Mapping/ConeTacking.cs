using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ConeTracking
{
    class TrackedCone
    {
        public Vector3 worldPos;
        public Color color;
        public int updateCount = 1;
        public GameObject visual;
        public float lastUpdatedTime;
    }

    struct RemovalRecord
    {
        public Vector3 pos;
        public float time;
    }

    private readonly List<TrackedCone> trackedCones = new();
    private readonly List<RemovalRecord> removalHistory = new();
    private readonly ConeMapper coneMapper;

    private const float POS_THRESHOLD = 1.0f;
    private const int BASE_UPDATES_TO_CONFIRM = 2;
    private const int MAX_UPDATES_TO_CONFIRM = 5;
    private const float MAX_RANGE = 15f;
    private const float CONE_EXPIRE_SECONDS = 3f;
    private const float REMOVAL_HISTORY_WINDOW = 5f; // seconds to keep past removals
    private const float REMOVAL_PROXIMITY = 1.0f; // consider a new cone "suspiciously near" a recently removed one

    public ConeTracking(ConeMapper mapper)
    {
        coneMapper = mapper;
    }

    public List<Vector3> GetConesByColor(Color color)
    {
        List<Vector3> result = new();
        foreach (var cone in trackedCones)
        {
            int required = GetRequiredUpdatesToConfirm(cone.worldPos);
            if (cone.updateCount >= required && cone.color == color)
                result.Add(cone.worldPos);
        }
        return result;
    }

    public List<Vector3> GetAllConePositions()
    {
        List<Vector3> result = new();
        foreach (var cone in trackedCones)
        {
            result.Add(cone.worldPos);
        }
        return result;
    }

    public void ClearTracking()
    {
        foreach (var cone in trackedCones)
        {
            if (cone.visual != null)
                GameObject.Destroy(cone.visual);
        }
        trackedCones.Clear();
        removalHistory.Clear();
    }

    private TrackedCone FindMatchingCone(Vector3 pos, Color color)
    {
        foreach (TrackedCone cone in trackedCones)
        {
            float dist = Vector3.Distance(cone.worldPos, pos);
            if (dist < POS_THRESHOLD && cone.color == color)
                return cone;
        }
        return null;
    }

    public void RegisterCone(Vector3 estWorldPos, Color color, Vector3 carPos)
    {
        // purge stale and old removal history before registering
        PurgeStaleCones();
        PurgeOldRemovalHistory();

        if (Vector3.Distance(estWorldPos, carPos) > MAX_RANGE)
            return;

        TrackedCone match = FindMatchingCone(estWorldPos, color);
        float now = Time.time;

        // Create new cone
        if (match == null)
        {
            TrackedCone cone = new TrackedCone
            {
                worldPos = estWorldPos,
                color = color,
                lastUpdatedTime = now
            };
            trackedCones.Add(cone);

            int required = GetRequiredUpdatesToConfirm(cone.worldPos);
            if (cone.updateCount >= required)
                cone.visual = coneMapper.PlaceCone(estWorldPos, color);
            return;
        }

        // existing cone: update
        if (match.updateCount == BASE_UPDATES_TO_CONFIRM)
            match.visual = coneMapper.PlaceCone(estWorldPos, color);

        float alpha = 1f / (match.updateCount + 1f); // exponential decay-like smoothing
        match.worldPos = Vector3.Lerp(match.worldPos, estWorldPos, alpha);
        match.updateCount++;
        match.lastUpdatedTime = now;

        if (match.visual != null)
            coneMapper.UpdateConePosition(match.visual, match.worldPos);
    }

    private void PurgeStaleCones()
    {
        float now = Time.time;
        for (int i = trackedCones.Count - 1; i >= 0; i--)
        {
            var cone = trackedCones[i];
            // extend expiry for cones with more confirmations
            float extraLifetime = Mathf.Clamp((cone.updateCount - BASE_UPDATES_TO_CONFIRM) * 1f, 0f, 5f); // up to +5s
            float expiryThreshold = CONE_EXPIRE_SECONDS + extraLifetime;
            if (now - cone.lastUpdatedTime > expiryThreshold)
            {
                removalHistory.Add(new RemovalRecord { pos = cone.worldPos, time = now });
                if (cone.visual != null)
                    GameObject.Destroy(cone.visual);
                trackedCones.RemoveAt(i);
            }
        }
    }

    private void PurgeOldRemovalHistory()
    {
        float now = Time.time;
        for (int i = removalHistory.Count - 1; i >= 0; i--)
        {
            if (now - removalHistory[i].time > REMOVAL_HISTORY_WINDOW)
                removalHistory.RemoveAt(i);
        }
    }

    // Increase required confirmations if new detection is near recently removed (suggesting instability)
    private int GetRequiredUpdatesToConfirm(Vector3 candidatePos)
    {
        int extra = 0;
        float now = Time.time;
        foreach (var record in removalHistory)
        {
            if (now - record.time > REMOVAL_HISTORY_WINDOW) continue;
            if (Vector3.Distance(candidatePos, record.pos) <= REMOVAL_PROXIMITY)
                extra++;
        }
        int required = BASE_UPDATES_TO_CONFIRM + extra;
        return Mathf.Clamp(required, BASE_UPDATES_TO_CONFIRM, MAX_UPDATES_TO_CONFIRM);
    }

    public Vector3 ComputePoseCorrection(Vector3 carPos, float carHeading)
    {
        Vector2 totalError = Vector2.zero;
        int count = 0;

        foreach (TrackedCone cone in trackedCones)
        {
            int required = GetRequiredUpdatesToConfirm(cone.worldPos);
            if (cone.updateCount < required) continue;

            Vector2 forward = new Vector2(Mathf.Sin(carHeading), Mathf.Cos(carHeading));
            Vector2 carPos2D = new Vector2(carPos.x, carPos.z);
            Vector2 conePos2D = new Vector2(cone.worldPos.x, cone.worldPos.z);

            Vector2 expectedRel = conePos2D - carPos2D;
            float expectedDist = expectedRel.magnitude;

            if (expectedDist > MAX_RANGE) continue;

            Vector2 error = conePos2D - (carPos2D + expectedRel);
            totalError += error;
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        Vector2 avgError = totalError / count;
        Vector3 correction = new Vector3(avgError.x, 0f, avgError.y);

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
            int required = GetRequiredUpdatesToConfirm(cone.worldPos);
            if (cone.updateCount < required) continue;

            Vector2 conePos2D = new Vector2(cone.worldPos.x, cone.worldPos.z);
            Vector2 toCone = conePos2D - carPos2D;

            float dist = toCone.magnitude;
            if (dist < 1f || dist > 20f) continue;

            toCone.Normalize();

            float angleError = Mathf.Atan2(
                toCone.x * forward.y - toCone.y * forward.x,
                toCone.x * forward.x + toCone.y * forward.y
            );

            totalHeadingError += angleError;
            count++;
        }

        if (count == 0) return 0f;

        float avgHeadingError = totalHeadingError / count;

        float maxHeadingCorrection = Mathf.Deg2Rad * 2f;
        avgHeadingError = Mathf.Clamp(avgHeadingError, -maxHeadingCorrection, maxHeadingCorrection);

        float trust = Mathf.Clamp01(count / 5f);
        float trustedCorrection = avgHeadingError * trust;

        return Mathf.Lerp(0f, trustedCorrection, 0.1f);
    }
}

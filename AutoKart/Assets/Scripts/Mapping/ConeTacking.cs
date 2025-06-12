using UnityEngine;
using System.Collections.Generic;

public class ConeTracking
{
    class TrackedCone
    {
        public Vector3 carPos;
        public Vector3 worldPos;
        public int updateCount = 1;
        public GameObject visual;
    }

    private readonly List<TrackedCone> trackedCones = new();
    private readonly ConeMapper coneMapper;

    private const float POS_THRESHOLD = 1.0f;
    private const int MIN_UPDATES_TO_CONFIRM = 2;

    private Vector3 lastPoseCorrection = Vector3.zero;

    public ConeTracking(ConeMapper mapper)
    {
        coneMapper = mapper;
    }

    TrackedCone FindMatchingCone(Vector3 pos)
    {
        // FIXME: This can be heavily optimized
        foreach (TrackedCone cone in trackedCones)
        {
            if (Vector3.Distance(cone.worldPos, pos) < POS_THRESHOLD)
                return cone;
        }
        return null;
    }

    public void RegisterCone(Vector3 estWorldPos, Vector3 estCarPos)
    {
        TrackedCone match = FindMatchingCone(estWorldPos);

        // Create new cone
        if (match == null)
        {
            TrackedCone cone = new TrackedCone{
                worldPos = estWorldPos,
                carPos = estCarPos
            };
            trackedCones.Add(cone);

            if (cone.updateCount >= MIN_UPDATES_TO_CONFIRM)
                cone.visual = coneMapper.PlaceCone(estWorldPos);
            return;
        }

        if (match.updateCount == MIN_UPDATES_TO_CONFIRM)
            match.visual = coneMapper.PlaceCone(estWorldPos);

        match.worldPos = Vector3.Lerp(match.worldPos, estWorldPos, 0.5f);
        match.carPos = Vector3.Lerp(match.carPos, estCarPos, 0.99f);
        match.updateCount++;

        if (match.visual != null)
            coneMapper.UpdateConePosition(match.visual, match.worldPos);
    }

    public Vector3 ComputePoseCorrection(Vector3 currentPose)
    {
        Vector3 totalOffset = Vector3.zero;
        int count = 0;

        foreach (TrackedCone cone in trackedCones)
        {
            if (cone.updateCount < MIN_UPDATES_TO_CONFIRM) continue;

            Vector3 offset = currentPose - cone.carPos;
            totalOffset += offset;
            count++;
        }

        if (count == 0)
        {
            lastPoseCorrection = currentPose;
            return lastPoseCorrection;
        }

        Vector3 avgOffset = totalOffset / count;
        lastPoseCorrection = Vector3.Lerp(currentPose, currentPose - avgOffset, 0.02f);
        return lastPoseCorrection;
    }
}

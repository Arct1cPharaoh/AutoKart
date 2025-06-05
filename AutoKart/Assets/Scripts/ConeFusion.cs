using UnityEngine;
using System.Collections.Generic;

public class ConeFusion
{
    class TrackedCone
    {
        public Vector3 worldPos;
        public int updateCount = 1;
        public GameObject visual;
    }

    private readonly List<TrackedCone> trackedCones = new();
    private readonly ConeMapper coneMapper;

    private const float POS_THRESHOLD = 1.0f;
    private const int MIN_UPDATES_TO_CONFIRM = 2;

    public ConeFusion(ConeMapper mapper)
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

    public void RegisterConeEstimate(Vector3 estimatedWorldPos)
    {
        TrackedCone match = FindMatchingCone(estimatedWorldPos);

        // Create new cone
        if (match == null)
        {
            TrackedCone cone = new TrackedCone{worldPos = estimatedWorldPos};
            trackedCones.Add(cone);

            if (cone.updateCount >= MIN_UPDATES_TO_CONFIRM)
                cone.visual = coneMapper.PlaceCone(estimatedWorldPos);
            return;
        }
        if (match.updateCount == MIN_UPDATES_TO_CONFIRM)
            match.visual = coneMapper.PlaceCone(estimatedWorldPos);

        match.worldPos = Vector3.Lerp(match.worldPos, estimatedWorldPos, 0.5f);
        match.updateCount++;

        if (match.visual != null)
            coneMapper.UpdateConePosition(match.visual, match.worldPos);
    }
}

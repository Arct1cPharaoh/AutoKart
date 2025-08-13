using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathPlanner
{
    public List<Vector3> previousPath = new();
    public List<Vector3> carHistory = new();

    private const float minSpacing = 0.5f;
    private const float cornerAngleThresholdDeg = 45f;
    private const float clearance = 0.5f;
    private const float historySpacing = 0.5f;

    private const float updateRadius = 10f;
    private int lapCount = 0;
    private Vector3 lapStartPos = Vector3.zero;
    private float lastLapTime = -10f;
    private const float lapDetectRadius = 1f;
    private const float lapDetectHeadingCosThreshold = 0.8f;
    private const float lapCooldown = 5f;
    private Vector3 firstLapDir = Vector3.zero;

    private GameObject debugRoot;
    private LineRenderer pathRenderer;
    private LineRenderer historyRenderer;
    private GameObject startMarker;
    private GameObject endMarker;

    private Vector3 lastDir = Vector3.forward;

    public List<Vector3> UpdatePlan(List<Vector3> left, List<Vector3> right, Vector3 currentPos)
    {
        if ((left == null || left.Count == 0) && (right == null || right.Count == 0))
            return new List<Vector3>();

        if (lapStartPos == Vector3.zero)
            lapStartPos = currentPos;

        // build centerline from colored sides
        List<Vector3> center = BuildCenter(left, right, currentPos);
        if (center == null || center.Count < 2)
            return new List<Vector3>();

        // update history
        if (carHistory.Count == 0 || Vector3.Distance(currentPos, carHistory[^1]) >= historySpacing)
            carHistory.Add(currentPos);

        // capture initial direction
        if (firstLapDir == Vector3.zero && carHistory.Count >= 2)
            firstLapDir = (carHistory[^1] - carHistory[^2]).normalized;

        // lap detection
        if (carHistory.Count >= 2 &&
            Vector3.Distance(currentPos, lapStartPos) <= lapDetectRadius &&
            Time.time - lastLapTime >= lapCooldown)
        {
            Vector3 recentDir = (carHistory[^1] - carHistory[^2]).normalized;
            if (firstLapDir.sqrMagnitude > 0f &&
                Vector3.Dot(recentDir, firstLapDir) >= lapDetectHeadingCosThreshold)
            {
                lapCount++;
                lastLapTime = Time.time;
            }
        }

        // history bias
        List<Vector3> biased = Bias(center);

        // smooth but preserve sharp turns
        List<Vector3> smooth = Smooth(biased);

        // clearance using both sides
        var all = new List<Vector3>();
        if (left != null) all.AddRange(left);
        if (right != null) all.AddRange(right);
        List<Vector3> final = Clear(smooth, all, clearance);

        Draw(final);
        previousPath = final;
        return final;
    }

    // centerline from left/right by pairing along direction
    private List<Vector3> BuildCenter(List<Vector3> L, List<Vector3> R, Vector3 pos)
    {
        // compute rough direction: prefer history
        Vector3 dir;
        if (carHistory.Count >= 2)
        {
            dir = (carHistory[^1] - carHistory[Mathf.Max(0, carHistory.Count - 2)]).normalized;
        }
        else
        {
            // fallback: from average of L/R centroid progression
            Vector3 mL = L != null && L.Count > 0 ? Mean(L) : Vector3.zero;
            Vector3 mR = R != null && R.Count > 0 ? Mean(R) : Vector3.zero;
            dir = ( (mL + mR) * 0.5f - pos );
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = lastDir;
            dir.Normalize();
        }

        // smooth direction to avoid jitter
        dir = Vector3.Slerp(lastDir, dir, 0.2f).normalized;
        lastDir = dir;

        // project and sort each side along dir
        List<Vector3> sortedL = (L != null) ? L.OrderBy(c => Vector3.Dot(c - pos, dir)).ToList() : new();
        List<Vector3> sortedR = (R != null) ? R.OrderBy(c => Vector3.Dot(c - pos, dir)).ToList() : new();

        int count = Mathf.Max(sortedL.Count, sortedR.Count);
        if (count == 0) return null;

        List<Vector3> center = new();
        for (int i = 0; i < count; i++)
        {
            Vector3 a = i < sortedL.Count ? sortedL[i] : sortedL.LastOrDefault();
            Vector3 b = i < sortedR.Count ? sortedR[i] : sortedR.LastOrDefault();

            if (sortedL.Count == 0) a = b;
            if (sortedR.Count == 0) b = a;

            Vector3 mid = (a + b) * 0.5f;
            if (center.Count == 0 || Vector3.Distance(mid, center[^1]) >= minSpacing)
                center.Add(mid);
        }

        return center;
    }

    private List<Vector3> Bias(List<Vector3> path)
    {
        if (path == null || path.Count == 0 || carHistory.Count < 1)
            return new List<Vector3>(path);

        List<Vector3> outp = new();
        foreach (var p in path)
        {
            Vector3 candidate = p;
            // find nearest history point within radius
            float snapRadius = 1.5f + 0.3f * lapCount;
            float best = float.MaxValue;
            Vector3 nearest = candidate;
            for (int i = Mathf.Max(0, carHistory.Count - 5); i < carHistory.Count; i++)
            {
                Vector3 h = carHistory[i];
                float d = Vector3.Distance(candidate, h);
                if (d < best && d < snapRadius)
                {
                    best = d;
                    nearest = h;
                }
            }
            if (best < snapRadius)
            {
                float influence = Mathf.Clamp(0.2f + 0.05f * lapCount - (best / snapRadius) * 0.1f, 0.1f, 0.5f);
                candidate = Vector3.Lerp(candidate, nearest, influence);
            }

            if (outp.Count == 0 || Vector3.Distance(candidate, outp[^1]) >= minSpacing)
                outp.Add(candidate);
        }
        return outp;
    }

    private List<Vector3> Smooth(List<Vector3> raw)
    {
        if (raw == null || raw.Count == 0) return new List<Vector3>();
        if (raw.Count < 5) return new List<Vector3>(raw);

        int n = raw.Count;
        List<Vector3> sg = new List<Vector3>(n);
        float[] coeffs = new float[] { -3f, 12f, 17f, 12f, -3f };
        const float norm = 35f;

        for (int i = 0; i < n; i++)
        {
            Vector3 acc = Vector3.zero;
            for (int k = -2; k <= 2; k++)
            {
                int idx = Mathf.Clamp(i + k, 0, n - 1);
                acc += raw[idx] * coeffs[k + 2];
            }
            acc /= norm;
            acc.y = raw[i].y;
            sg.Add(acc);
        }

        List<Vector3> blended = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
        {
            if (i == 0 || i == n - 1)
            {
                blended.Add(sg[i]);
                continue;
            }

            Vector3 prev = raw[i - 1];
            Vector3 curr = raw[i];
            Vector3 next = raw[i + 1];

            Vector3 dirBefore = (curr - prev).normalized;
            Vector3 dirAfter = (next - curr).normalized;
            float angle = Vector3.Angle(dirBefore, dirAfter);

            // adapt smoothing strength: sharper turns get less smoothing
            float t = Mathf.Clamp01((cornerAngleThresholdDeg - angle) / cornerAngleThresholdDeg);
            Vector3 finalPt = Vector3.Lerp(raw[i], sg[i], t);
            blended.Add(finalPt);
        }

        List<Vector3> result = new();
        foreach (var p in blended)
        {
            if (result.Count == 0 || Vector3.Distance(p, result[^1]) >= minSpacing)
                result.Add(p);
        }

        return result;
    }

    private List<Vector3> Clear(List<Vector3> pathPoints, List<Vector3> cones, float minClearance)
    {
        if (pathPoints == null || pathPoints.Count < 2 || cones == null || cones.Count == 0)
            return new List<Vector3>(pathPoints);

        int n = pathPoints.Count;
        Vector3[] accum = new Vector3[n];
        int[] counts = new int[n];

        for (int segIdx = 1; segIdx < n; segIdx++)
        {
            Vector3 a = pathPoints[segIdx - 1];
            Vector3 b = pathPoints[segIdx];
            Vector3 segDir = (b - a);
            if (segDir.sqrMagnitude < 1e-6f) continue;
            segDir.y = 0f;
            segDir.Normalize();
            Vector3 perp = new Vector3(-segDir.z, 0f, segDir.x);

            for (int ci = 0; ci < cones.Count; ci++)
            {
                Vector3 cone = cones[ci];
                Vector3 closest = Closest(a, b, cone);
                Vector3 diff = cone - closest;
                diff.y = 0f;
                float dist = diff.magnitude;
                if (dist >= minClearance || dist < 1e-6f) continue;

                float sign = Vector3.Dot(perp, diff.normalized) >= 0f ? 1f : -1f;
                Vector3 adjustment = perp * sign * (minClearance - dist);

                accum[segIdx - 1] += adjustment * 0.5f;
                counts[segIdx - 1]++;
                accum[segIdx] += adjustment * 0.5f;
                counts[segIdx]++;
            }
        }

        List<Vector3> adjusted = new List<Vector3>(pathPoints);
        for (int i = 0; i < n; i++)
        {
            if (counts[i] > 0)
            {
                Vector3 delta = accum[i] / counts[i];
                adjusted[i] += delta;
            }
        }

        return adjusted;
    }

    private Vector3 Closest(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        ab.y = 0f;
        ap.y = 0f;
        float ab2 = Vector3.Dot(ab, ab);
        if (ab2 < 1e-8f) return a;
        float t = Vector3.Dot(ap, ab) / ab2;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    private Vector3 Mean(List<Vector3> pts)
    {
        Vector3 sum = Vector3.zero;
        foreach (var v in pts) sum += v;
        return sum / pts.Count;
    }

    private void Draw(List<Vector3> pathPoints)
    {
        Ensure();
        if (pathPoints != null && pathPoints.Count > 0)
        {
            pathRenderer.positionCount = pathPoints.Count;
            pathRenderer.SetPositions(pathPoints.ToArray());
            startMarker.transform.position = pathPoints[0] + Vector3.up * 0.1f;
            endMarker.transform.position = pathPoints[^1] + Vector3.up * 0.1f;
        }
        else
        {
            pathRenderer.positionCount = 0;
        }

        if (carHistory != null && carHistory.Count >= 2)
        {
            historyRenderer.positionCount = carHistory.Count;
            historyRenderer.SetPositions(carHistory.ToArray());
        }
        else
        {
            historyRenderer.positionCount = 0;
        }

        // fallback scene debug
        if (pathPoints != null)
        {
            for (int i = 1; i < pathPoints.Count; i++)
                Debug.DrawLine(pathPoints[i - 1], pathPoints[i], Color.white, 0.1f);
            if (pathPoints.Count > 0)
            {
                Debug.DrawLine(pathPoints[0], pathPoints[0] + Vector3.up * 0.2f, Color.green, 0.1f);
                Debug.DrawLine(pathPoints[^1], pathPoints[^1] + Vector3.up * 0.2f, Color.red, 0.1f);
            }
        }

        for (int i = 1; i < carHistory.Count; i++)
            Debug.DrawLine(carHistory[i - 1], carHistory[i], Color.yellow, 0.1f);
    }

    private void Ensure()
    {
        if (debugRoot == null)
        {
            debugRoot = new GameObject("PathPlannerDebug");
            Object.DontDestroyOnLoad(debugRoot);

            pathRenderer = debugRoot.AddComponent<LineRenderer>();
            pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathRenderer.widthCurve = AnimationCurve.Constant(0, 1, 0.1f);
            pathRenderer.numCapVertices = 4;
            pathRenderer.useWorldSpace = true;
            pathRenderer.startColor = pathRenderer.endColor = Color.white;
            pathRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pathRenderer.receiveShadows = false;

            historyRenderer = new GameObject("CarHistoryRenderer").AddComponent<LineRenderer>();
            historyRenderer.transform.SetParent(debugRoot.transform);
            historyRenderer.material = new Material(Shader.Find("Sprites/Default"));
            historyRenderer.widthCurve = AnimationCurve.Constant(0, 1, 0.05f);
            historyRenderer.numCapVertices = 4;
            historyRenderer.useWorldSpace = true;
            historyRenderer.startColor = historyRenderer.endColor = Color.yellow;
            historyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            historyRenderer.receiveShadows = false;

            startMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startMarker.name = "Start";
            startMarker.transform.SetParent(debugRoot.transform);
            startMarker.transform.localScale = Vector3.one * 0.2f;
            var smr = startMarker.GetComponent<Renderer>();
            smr.material = new Material(Shader.Find("Standard")) { color = Color.green };
            Object.Destroy(startMarker.GetComponent<Collider>());

            endMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            endMarker.name = "End";
            endMarker.transform.SetParent(debugRoot.transform);
            endMarker.transform.localScale = Vector3.one * 0.2f;
            var emr = endMarker.GetComponent<Renderer>();
            emr.material = new Material(Shader.Find("Standard")) { color = Color.red };
            Object.Destroy(endMarker.GetComponent<Collider>());
        }
    }
}

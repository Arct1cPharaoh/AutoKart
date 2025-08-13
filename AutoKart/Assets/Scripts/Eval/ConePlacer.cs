using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ConePlacer : MonoBehaviour
{
    [SerializeField] private GameObject conePrefab;
    [SerializeField] private SelfDriving selfDriving;
    private float evaluationTime = 1.0f;

    private GameObject currentCone;
    private int currentConeIndex = 0;

    private Vector3[] conePositions =
    {
        new Vector3(-1, 0, 0),
        new Vector3(-2, 0, 0),
        new Vector3(-3, 0, 0),
        new Vector3(-4, 0, 0),
        new Vector3(-5, 0, 0),
        new Vector3(-6, 0, 0),
        new Vector3(-7, 0, 0),
        new Vector3(-8, 0, 0),
        new Vector3(-9, 0, 0),
        new Vector3(-10, 0, 0),
        new Vector3(-11, 0, 0),
        new Vector3(-12, 0, 0),
        new Vector3(-13, 0, 0),
        new Vector3(-14, 0, 0),
        new Vector3(-15, 0, 0),
        new Vector3(-16, 0, 0),
        new Vector3(-17, 0, 0),
        new Vector3(-18, 0, 0),
        new Vector3(-19, 0, 0),
        new Vector3(-20, 0, 0),
        new Vector3(-21, 0, 0),
        new Vector3(-22, 0, 0),
        new Vector3(-23, 0, 0),
        new Vector3(-24, 0, 0),
        new Vector3(-25, 0, 0),
        new Vector3(-26, 0, 0),
        new Vector3(-27, 0, 0),
        new Vector3(-28, 0, 0),
        new Vector3(-29, 0, 0),
        new Vector3(-30, 0, 0),
    };

    // Store all metrics for final summary
    private List<float> allXErrors = new List<float>();
    private List<float> allZErrors = new List<float>();
    private List<float> allDistanceErrors = new List<float>();

    void Start()
    {
        StartCoroutine(EvaluateConesRoutine());
    }

    private IEnumerator EvaluateConesRoutine()
    {
        while (currentConeIndex < conePositions.Length)
        {
            SpawnCurrentCone();
            yield return new WaitForSeconds(evaluationTime);

            EvaluateCurrentCone();

            DeleteCurrentCone();
            selfDriving.tracking.ClearTracking();
            currentConeIndex++;
        }

        PrintOverallMetrics();
        Debug.Log("[Evaluator] All cones evaluated.");
    }

    private void SpawnCurrentCone()
    {
        if (currentCone != null)
            Destroy(currentCone);

        Vector3 pos = conePositions[currentConeIndex];
        currentCone = Instantiate(
            conePrefab,
            transform.TransformPoint(pos),
            Quaternion.Euler(-90, 0, 0)
        );
    }

    private void DeleteCurrentCone()
    {
        if (currentCone != null)
            Destroy(currentCone);
        currentCone = null;
    }

    private void EvaluateCurrentCone()
    {
        var trackedCones = selfDriving.tracking.GetConesByColor(Color.blue)
            .Concat(selfDriving.tracking.GetConesByColor(Color.yellow))
            .ToList();

        if (trackedCones.Count == 0)
        {
            Debug.LogWarning($"[Evaluator] No tracked cones for Cone #{currentConeIndex}.");
            return;
        }

        Vector3 groundTruth = currentCone.transform.position;
        Vector3 bestCone = trackedCones.OrderBy(c => Vector3.Distance(c, groundTruth)).First();

        // Calculate errors
        float xError = bestCone.x - groundTruth.x;
        float zError = bestCone.z - groundTruth.z;
        float distanceError = Vector3.Distance(bestCone, groundTruth);

        // Save metrics
        allXErrors.Add(Mathf.Abs(xError));
        allZErrors.Add(Mathf.Abs(zError));
        allDistanceErrors.Add(distanceError);

        Debug.Log($"[Evaluator] Cone #{currentConeIndex} - XError: {xError:F3}m, ZError: {zError:F3}m, Distance: {distanceError:F3}m");

        // Visualize
        Debug.DrawLine(groundTruth, bestCone, Color.green, 1.0f);
    }

    private void PrintOverallMetrics()
    {
        if (allDistanceErrors.Count == 0)
        {
            Debug.LogWarning("[Evaluator] No cones were detected during evaluation.");
            return;
        }

        float avgSignedX = allXErrors.Sum() / allXErrors.Count;  // current is abs
        float avgSignedZ = allZErrors.Sum() / allZErrors.Count;
        float stdX = Mathf.Sqrt(allXErrors.Average(v => Mathf.Pow(v - avgSignedX, 2)));
        float stdZ = Mathf.Sqrt(allZErrors.Average(v => Mathf.Pow(v - avgSignedZ, 2)));

        float avgDistError = allDistanceErrors.Average();
        float maxDistError = allDistanceErrors.Max();
        float minDistError = allDistanceErrors.Min();

        Debug.Log($"[Evaluator] Final Metrics:\n" +
                  $"   Avg X Error = {avgSignedX:F3}m (std: {stdX:F3})\n" +
                  $"   Avg Z Error = {avgSignedZ:F3}m (std: {stdZ:F3})\n" +
                  $"   Distance Error: avg = {avgDistError:F3}m, min = {minDistError:F3}m, max = {maxDistError:F3}m");
    }
}

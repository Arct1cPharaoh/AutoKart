using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CarController), typeof(VehicleSpecs))]
public class PathFollower : MonoBehaviour
{
    [Header("Lookahead")]
    [SerializeField] private float lookaheadDistance = 2.0f;

    private CarController car;
    private VehicleSpecs specs;

    void Start()
    {
        car = GetComponent<CarController>();
        specs = GetComponent<VehicleSpecs>();
    }

    private Vector3 FindLookaheadTarget(List<Vector3> path, Vector3 carPos)
    {
        Vector3 forward = transform.forward;

        foreach (var point in path)
        {
            Vector3 toPoint = point - carPos;
            float dist = toPoint.magnitude;

            if (dist >= lookaheadDistance && Vector3.Dot(forward, toPoint.normalized) > 0.5f)
            {
                return point;
            }
        }

        // If none ahead, fallback to last valid point
        return path[path.Count - 1];
    }

    public void FollowPath(List<Vector3> path, Vector3 carPos, float carHeading)
    {
        if (path == null || path.Count == 0)
            return;

        // Find the lookahead target
        Vector3 target = FindLookaheadTarget(path, carPos);
        Vector3 carForward = new Vector3(Mathf.Sin(carHeading), 0f, Mathf.Cos(carHeading));
        Vector3 toTarget = target - carPos;
        Vector2 toTarget2D = new Vector2(toTarget.x, toTarget.z).normalized;
        Vector2 forward2D = new Vector2(carForward.x, carForward.z).normalized;

        // Signed angle between car forward and target direction
        float angle = Mathf.Atan2(
            toTarget2D.x * forward2D.y - toTarget2D.y * forward2D.x,
            toTarget2D.x * forward2D.x + toTarget2D.y * forward2D.y
        );
        float steeringDeg = Mathf.Rad2Deg * angle;

        // Apply control
        car.steeringAngle = Mathf.Clamp(steeringDeg, -specs.maxSteeringAngle, specs.maxSteeringAngle);
        car.throttlePos = 0.5f;
        car.brakePos = 0.0f;

        Debug.DrawLine(carPos, target, Color.green, 0.1f);
    }
}

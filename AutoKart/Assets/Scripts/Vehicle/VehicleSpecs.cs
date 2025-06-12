using UnityEngine;

[DisallowMultipleComponent]
public class VehicleSpecs : MonoBehaviour
{
    [Header("Mass Properties (kg)")]
    public float massCar = 100f;
    public float massBattery = 60f;
    public float massDriver = 70f;

    [Header("Dimensions (meters)")]
    [Tooltip("Distance between front and rear axle centers.")]
    public float wheelbase = 1.6f;

    [Tooltip("Height of the center of gravity above the ground.")]
    public float cgHeight = 0.254f;

    [Tooltip("0 = 100% rear, 1 = 100% front.")]
    [Range(0f, 1f)] public float frontWeightDistribution = 0.5f;

    [Tooltip("Distance between the front-left and front-right wheels.")]
    public float frontTrackWidth = 1.2f;

    [Tooltip("Distance between the rear-left and rear-right wheels.")]
    public float rearTrackWidth = 1.2f;

    [Tooltip("Radius of tire from center to road contact patch.")]
    public float tireRadius = 0.09597f;

    [Header("Motor")]
    [Tooltip("Maximum torque produced by the motor")]
    public float motorMaxTorque = 230;

    [Tooltip("Maximum RPM produced by the motor")]
    public float motorMaxRPM = 8000;

    [Header("Aerodynamics")]
    public float frontalArea = 1.0f;
    public float dragCoefficient = 0.8f;
    public float liftCoefficient = 1.6f;
    public float airDensity = 1.162f;

    [Header("Tire Model")]
    public float coefficientOfFriction = 1.4f;
    public float loadSensitivity = 0.0004f;

    [Header("Breaking")]
    public float maxBrakeForce = 1000;
    [Range(0f, 100f)] public float brakeBias = 70;

    [Header("Steering")]
    public float maxSteeringAngle = 30;
    public float steeringSpeed = 90;

    [Header("Runtime Settings")]
    public bool autoDeriveGeometry = true;

    // Computed properties
    public float TotalMass => massCar + massBattery + massDriver;
    public float RearAxlePosition => wheelbase * (1f - frontWeightDistribution);
    public float FrontAxlePosition => wheelbase * frontWeightDistribution;

    private SteeringRig steering;

    private void TryInitializeFromWheels()
    {
        if (steering == null)
        {
            Debug.LogWarning("SteeringRig not found. Cannot auto-initialize geometry.");
            return;
        }

        InitializeFromWheelTransforms(
            steering.frontLeftWheel,
            steering.frontRightWheel,
            steering.rearLeftWheel,
            steering.rearRightWheel
        );
    }

    void Start()
    {
        steering = GetComponentInChildren<SteeringRig>();

        if (autoDeriveGeometry)
            TryInitializeFromWheels();
    }

    private bool AllWheelsPresent(
        Transform fl, Transform fr, Transform rl, Transform rr)
    {
        return fl && fr && rl && rr;
    }

    private void TryAutoDetectTireRadius(Transform wheel)
    {
        Renderer renderer = wheel.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("No renderer found to detect tire radius.");
            return;
        }

        float height = renderer.bounds.size.y;
        if (height > 0.01f)
        {
            tireRadius = height * 0.5f;
        }
    }

    public void InitializeFromWheelTransforms(
        Transform frontLeft, Transform frontRight,
        Transform rearLeft, Transform rearRight
    )
    {
        if (!AllWheelsPresent(frontLeft, frontRight, rearLeft, rearRight))
        {
            Debug.LogWarning("Missing wheel transforms.");
            return;
        }

        frontTrackWidth = Vector3.Distance(frontLeft.position, frontRight.position);
        rearTrackWidth = Vector3.Distance(rearLeft.position, rearRight.position);

        Vector3 frontMid = (frontLeft.position + frontRight.position) * 0.5f;
        Vector3 rearMid = (rearLeft.position + rearRight.position) * 0.5f;
        wheelbase = Vector3.Distance(frontMid, rearMid);

        TryAutoDetectTireRadius(frontLeft);
    }
}

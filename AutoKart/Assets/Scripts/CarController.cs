using UnityEngine;

public class CarController : MonoBehaviour
{
    [Range(0.0f, 1.0f)] public float throttlePos = 0.0f;
    [Range(-30.0f, 30.0f)] public float steeringAngle = 0.0f; // Deg
    [Range(0.0f, 1.0f)] public float brakePos = 0.0f;

    private float curAngle = 0.0f;
    private Vector3 lastVelocity;

    private Rigidbody rb;
    private VehicleSpecs specs;
    private ChassisDynamics chassis;
    private SteeringRigController steering;

    private WheelPhysics wheelFLPhy;
    private WheelPhysics wheelFRPhy;
    private WheelPhysics wheelRLPhy;
    private WheelPhysics wheelRRPhy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        steering = GetComponent<SteeringRigController>();
        curAngle = transform.eulerAngles.y;

        specs = GetComponent<VehicleSpecs>();
        if (specs == null)
        {
            Debug.LogError("VehicleSpecs not found on this GameObject");
            return;
        }

        rb.mass = specs.TotalMass;
        chassis = new ChassisDynamics(specs);
        wheelFLPhy = new WheelPhysics(steering.frontLeftWheel, rb, specs);
        wheelFRPhy = new WheelPhysics(steering.frontRightWheel, rb, specs);
        wheelRLPhy = new WheelPhysics(steering.rearLeftWheel, rb, specs);
        wheelRRPhy = new WheelPhysics(steering.rearRightWheel, rb, specs);
    }

    bool ShouldOverrideMotion()
    {
        // If both brake and throttle are significantly applied, stop everthing
        return throttlePos > 0.2f && brakePos > 0.2f;
    }

    void ApplyFullStop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Estimate current acceleration from last velocity change
    float EstimateAcceleration()
    {
        float accel = (rb.linearVelocity - lastVelocity).magnitude / Time.fixedDeltaTime;
        lastVelocity = rb.linearVelocity;
        return accel;
    }

    void ApplyNormalForcesToTwoWheels(float load, WheelPhysics w1, WheelPhysics w2)
    {
        // Divide load equally
        float splitLoad = load * 0.5f;
        w1.SetNormalForce(splitLoad);
        w2.SetNormalForce(splitLoad);
    }

    float ComputeAvalibleTorque()
    {
        float wheelCircumference = 2 * Mathf.PI * specs.tireRadius;
        float curRPM = (rb.linearVelocity.magnitude / wheelCircumference) * 60f;
        float clampedRPM = Mathf.Clamp01(1.0f - (curRPM / specs.motorMaxRPM));
        float torqueAvailable = specs.motorMaxTorque * clampedRPM;
        float torque = throttlePos * torqueAvailable;
        return torque;
    }

    Vector3 ComputeForceOnCar(float torque)
    {
        Vector3 deltaVelL = wheelRLPhy.ComputeTractionVelocity(torque);
        Vector3 deltaVelR = wheelRRPhy.ComputeTractionVelocity(torque);
        Vector3 netDeltaVelocity = (deltaVelL + deltaVelR) * 0.5f;
        return netDeltaVelocity;
    }

    void ApplyThrottleForce()
    {
        float accel = EstimateAcceleration();
        var (frontLoad, rearLoad) = chassis.ComputeLongitudinalLoadTransfer(accel);
        ApplyNormalForcesToTwoWheels(rearLoad, wheelRLPhy, wheelRRPhy);
        float torque = ComputeAvalibleTorque();
        Vector3 netForce = ComputeForceOnCar(torque);

        // Apply the force
        rb.AddForce(netForce, ForceMode.Force);
        wheelRLPhy.ApplyLateralGripForce();
        wheelRRPhy.ApplyLateralGripForce();
    }

    void ApplySteering()
    {
        float accel = EstimateAcceleration();
        var (frontLoad, rearLoad) = chassis.ComputeLongitudinalLoadTransfer(accel);
        ApplyNormalForcesToTwoWheels(frontLoad, wheelFLPhy, wheelFRPhy);
        wheelFLPhy.ApplyLateralGripForce();
        wheelFRPhy.ApplyLateralGripForce();
    }

    void ApplyBraking()
    {
        if (brakePos <= 0.01f)
            return;

        // Split force by bias
        float totalBrakeForce = brakePos * specs.maxBrakeForce;
        float frontBias = specs.brakeBias / 100f;
        float rearBias = 1f - frontBias;

        float brakeFront = totalBrakeForce * 0.5f * frontBias; // per wheel
        float brakeRear = totalBrakeForce * 0.5f * rearBias;   // per wheel

        // Apply to each wheel
        if (wheelFLPhy != null)
            wheelFLPhy.ApplyBrakeForce(brakeFront);
        if (wheelFRPhy != null)
            wheelFRPhy.ApplyBrakeForce(brakeFront);
        if (wheelRLPhy != null)
            wheelRLPhy.ApplyBrakeForce(brakeRear);
        if (wheelRRPhy != null)
            wheelRRPhy.ApplyBrakeForce(brakeRear);
    }

    // FixedUpdate is called once per phyisics frame
    void FixedUpdate()
    {
        if (ShouldOverrideMotion())
        {
            ApplyFullStop();
            return;
        }

        ApplyThrottleForce();
        ApplySteering();
        ApplyBraking();
    }
}

using UnityEngine;

public class CarController : MonoBehaviour
{
    [Range(0.0f, 1.0f)] public float throttlePos = 0.0f;
    [Range(-30.0f, 30.0f)] public float steeringAngle = 0.0f; // Deg
    [Range(0.0f, 1.0f)] public float brakePos = 0.0f;

    // Throttle
    public float wheelRadius = 0.15f; // meters
    public float motorMaxTorque = 5f; // Nm
    public float motorMaxRPM = 3000f; // rpm
    public float gearRatio = 2.75f; // 1:1
    public float vehicleMass = 200f; // kg

    // Steering
    private float curAngle = 0.0f;
    public float maxSteeringAngle = 30f; // degrees
    public float steeringSpeed = 90f; // degrees per second

    // Brakes
    public float maxBrakeForce = 1000f;


    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        curAngle = transform.eulerAngles.y;
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

    void ApplyThrottleForce()
    {
        // Simple linear fall off
        float wheelCircumference = 2 * Mathf.PI * wheelRadius;
        float curRPM = (rb.linearVelocity.magnitude / wheelCircumference) * 60f;
        float clampedRPM = Mathf.Clamp01(1.0f - (curRPM / motorMaxRPM));
        float torqueAvailable = motorMaxTorque * clampedRPM;
        float torque = throttlePos * torqueAvailable;

        // Apply force to car
        float driveForce = torque / wheelRadius;
        rb.AddForce(transform.forward * driveForce);
    }

    void ApplySteering()
    {
        // Clamp steering
        steeringAngle = Mathf.Clamp(
            steeringAngle, -maxSteeringAngle, maxSteeringAngle
        );

        // Compute rotation delta for this frame
        float step = steeringSpeed * Time.deltaTime;
        // limit how fast it turns
        float delta = Mathf.Clamp(steeringAngle, -step, step);
        curAngle += delta;

        // Apply rotation
        transform.rotation = Quaternion.Euler(0f, curAngle, 0f);
    }

    void ApplyBraking()
    {
        // Apply braking
        if (rb.linearVelocity.magnitude > 0.01) {
            Vector3 brakeForce =
                -rb.linearVelocity.normalized * brakePos * maxBrakeForce;
            rb.AddForce(brakeForce);
        }
        else
        {
            ApplyFullStop();
        }
    }

    // FixedUpdate is called once per phisics frame
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

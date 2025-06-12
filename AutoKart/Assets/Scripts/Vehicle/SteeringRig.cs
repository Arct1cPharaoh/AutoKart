using UnityEngine;

public class SteeringRig : MonoBehaviour
{
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    private VehicleSpecs specs;
    private CarController car;
    private Rigidbody rb;

    private void Start()
    {
        specs = GetComponentInParent<VehicleSpecs>();
        car = GetComponentInParent<CarController>();
        rb = GetComponentInParent<Rigidbody>();
    }

    void UpdateFrontWheelRotation()
    {
        float speed = rb.linearVelocity.magnitude; // m/s
        float wheelCircumference = 2 * Mathf.PI * specs.tireRadius;
        float rpm = (speed / wheelCircumference) * 60f;
        float degreesPerFrame = (rpm / 60f) * 360f * Time.deltaTime;

        if (frontLeftWheel != null)
            frontLeftWheel.Rotate(Vector3.right, degreesPerFrame, Space.Self);

        if (frontRightWheel != null)
            frontRightWheel.Rotate(Vector3.right, degreesPerFrame, Space.Self);
    }

    void UpdateSteeringVisuals()
    {
        float angle = Mathf.Clamp(
            car.steeringAngle,
            -specs.maxSteeringAngle,
            specs.maxSteeringAngle
        );

        if (frontLeftWheel != null)
        {
            float spin = frontLeftWheel.localEulerAngles.x;
            frontLeftWheel.localRotation = Quaternion.Euler(spin, angle, 0f);
        }
        if (frontRightWheel != null)
        {
            float spin = frontRightWheel.localEulerAngles.x;
            frontRightWheel.localRotation = Quaternion.Euler(spin, angle, 0f);
        }
    }

    void Update()
    {
        UpdateSteeringVisuals();
    }
}

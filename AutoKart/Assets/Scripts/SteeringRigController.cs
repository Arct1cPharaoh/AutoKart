using UnityEngine;

[RequireComponent(typeof(CarController))]
public class SteeringRigController : MonoBehaviour
{
    [Header("Front Wheel Visual Meshes")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;

    [Header("Rear Wheels")]
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Driveshaft")]
    public Transform driveShaft;

    [Header("Steering Visuals")]
    public Transform steeringWheel;
    public float steeringWheelMultiplier = 1.0f; // How much to spin the steering wheel visually

    private CarController car;
    private Rigidbody rb;

    private void Start()
    {
        car = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
    }

    void UpdateRearWheelRotation()
    {
        float speed = rb.linearVelocity.magnitude; // m/s
        float wheelCircumference = 2 * Mathf.PI * car.wheelRadius;
        float rpm = (speed / wheelCircumference) * 60f;
        float degreesPerFrame = (rpm / 60f) * 360f * Time.deltaTime;

        if (rearLeftWheel != null)
            rearLeftWheel.Rotate(Vector3.right, degreesPerFrame, Space.Self);

        if (rearRightWheel != null)
            rearRightWheel.Rotate(Vector3.right, degreesPerFrame, Space.Self);

        if (driveShaft != null)
            driveShaft.Rotate(Vector3.right, degreesPerFrame, Space.Self);
    }

    void UpdateFrontWheelRotation()
    {
        float speed = rb.linearVelocity.magnitude; // m/s
        float wheelCircumference = 2 * Mathf.PI * car.wheelRadius;
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
            -car.maxSteeringAngle,
            car.maxSteeringAngle
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
        UpdateRearWheelRotation();
        UpdateSteeringVisuals();
        UpdateFrontWheelRotation();
    }
}

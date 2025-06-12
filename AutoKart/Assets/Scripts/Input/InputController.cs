using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(VehicleSpecs))]
public class InputController : MonoBehaviour
{
    public PlayerControls controls;

    [Header("Config")]
    [SerializeField] private float centeringStrength = 1.0f;
    [SerializeField] private float steeringDeadzone = 0.01f;

    private CarController car;
    private VehicleSpecs specs;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        car = GetComponent<CarController>();
        specs = GetComponent<VehicleSpecs>();
    }

    private void UpdateThrottle()
    {
        float throttle = controls.Drive.Throttle.ReadValue<float>();
        car.throttlePos = Mathf.Clamp01(throttle);
    }

    private void UpdateBraking()
    {
        float brake = controls.Drive.Brake.ReadValue<float>();
        car.brakePos = Mathf.Clamp01(brake);
    }

    private void ApplyCenteringForce()
    {
        float strength = Mathf.Abs(car.steeringAngle / specs.maxSteeringAngle);

        float centeringRate = centeringStrength * specs.steeringSpeed *
                              strength * Time.deltaTime;

        car.steeringAngle = Mathf.MoveTowards(
            car.steeringAngle,
            0f,
            centeringRate
        );
    }

    private void ClampSteering()
    {
        car.steeringAngle = Mathf.Clamp(
            car.steeringAngle,
            -specs.maxSteeringAngle,
            specs.maxSteeringAngle
        );
    }

    private void UpdateSteering()
    {
        float steeringInput = controls.Drive.Steering.ReadValue<float>();
        float deltaAngle = steeringInput * specs.steeringSpeed * Time.deltaTime;

        // Center deadzone
        if (Mathf.Abs(steeringInput) > steeringDeadzone)
        {
            car.steeringAngle += deltaAngle;
        }
        else
        {
            ApplyCenteringForce();
        }

        ClampSteering();
    }

    private void Update()
    {
        UpdateThrottle();
        UpdateBraking();
        UpdateSteering();
    }
}

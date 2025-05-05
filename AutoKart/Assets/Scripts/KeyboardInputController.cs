using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarController))]
public class KeyboardInputController : MonoBehaviour
{
    public PlayerControls controls;

    private CarController car;

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
    }

    private void Update()
    {
        float throttleInput = controls.Drive.Throttle.ReadValue<float>();
        float brakeInput = controls.Drive.Brake.ReadValue<float>();
        float steeringInput = controls.Drive.Steering.ReadValue<float>();

        car.throttlePos = Mathf.Clamp01(throttleInput);
        car.brakePos = Mathf.Clamp01(brakeInput);

        float deltaAngle = steeringInput * car.steeringSpeed * Time.deltaTime;

        if (Mathf.Abs(steeringInput) > 0.01f)
        {
            // Apply steering input
            car.steeringAngle += deltaAngle;
        }
        else
        {
            // Return to center
            car.steeringAngle = Mathf.MoveTowards(
                car.steeringAngle,
                0f,
                car.steeringSpeed * Time.deltaTime
            );
        }

        car.steeringAngle = Mathf.Clamp(
            car.steeringAngle,
            -car.maxSteeringAngle,
            car.maxSteeringAngle
        );
    }
}

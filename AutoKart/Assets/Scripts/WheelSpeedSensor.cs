using UnityEngine;

public class WheelSpeedSensor : MonoBehaviour
{
    [Tooltip("Simulated high voltage output (V)")]
    public float highVoltage = 5.0f;

    [Tooltip("Simulated low voltage output (V)")]
    public float lowVoltage = 0.0f;

    [Tooltip("Number of digital pulses per full wheel revolution")]
    public int pulsesPerRevolution = 20;

    public float OutputVoltage => outputState ? highVoltage : lowVoltage;
    public bool IsHigh => outputState;

    public delegate void PulseEvent();
    public event PulseEvent OnRisingEdge;

    private VehicleSpecs specs;
    private Rigidbody rb;

    private float wheelCircumference;
    private float nextToggleTime;
    private bool outputState = false;

    private void Start()
    {
        specs = GetComponentInParent<VehicleSpecs>();
        rb = GetComponentInParent<Rigidbody>();

        if (specs == null)
        {
            Debug.LogError("VehicleSpecs not found in parent.");
            enabled = false;
            return;
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody not found in parent.");
            enabled = false;
            return;
        }

        wheelCircumference = 2f * Mathf.PI * specs.tireRadius;
        nextToggleTime = Time.time;
    }

    private void FixedUpdate()
    {
        Vector3 contactVelocity = rb.GetPointVelocity(transform.position);
        // Debug.Log($"MPH (raw): {(rb.GetPointVelocity(transform.position).magnitude * 2.23694f):F2}");

        float wheelSpeed = contactVelocity.magnitude; // m/s

        if (wheelSpeed < 0.01f)
        {
            outputState = false;
            return;
        }

        float revsPerSec = wheelSpeed / wheelCircumference;
        float pulsesPerSec = revsPerSec * pulsesPerRevolution;

        if (pulsesPerSec <= 0f)
            return;

        float halfPulsePeriod = 1f / (pulsesPerSec * 2f);
        float now = Time.time;

        // Toggle signal as many times as needed (in case frame skipped multiple toggles)
        while (now >= nextToggleTime)
        {
            outputState = !outputState;
            nextToggleTime += halfPulsePeriod;

            if (outputState && OnRisingEdge != null)
                OnRisingEdge.Invoke();
        }
    }
}

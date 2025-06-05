using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Speedometer : MonoBehaviour
{
    public WheelSpeedSensor sensor;
    public TextMeshProUGUI uiText;

    [Tooltip("Time window to average pulse count over (seconds)")]
    public float sampleWindow = 1f;

    [Tooltip("Max time allowed without a pulse before assuming zero speed")]
    public float pulseTimeout = 0.3f;

    public float currentSpeedMPH = 0f;

    private Queue<float> pulseTimestamps = new Queue<float>();
    private float lastPulseTime = -999f;

    private void Start()
    {
        if (sensor != null)
        {
            sensor.OnRisingEdge += HandlePulse;
        }
    }

    private void HandlePulse()
    {
        float now = Time.time;
        pulseTimestamps.Enqueue(now);
        lastPulseTime = now;

        // Remove old pulses outside of sample window
        while (pulseTimestamps.Count > 0 && now - pulseTimestamps.Peek() > sampleWindow)
            pulseTimestamps.Dequeue();
    }

    private void Update()
    {
        float now = Time.time;

        if (pulseTimestamps.Count == 0 || (now - lastPulseTime) > pulseTimeout)
        {
            currentSpeedMPH = 0f;
        }
        else
        {
            float pulseRate = pulseTimestamps.Count / sampleWindow;
            float wheelCircumference = 2f * Mathf.PI * sensor.GetComponentInParent<VehicleSpecs>().tireRadius;
            float revsPerSecond = pulseRate / sensor.pulsesPerRevolution;
            float linearSpeed = revsPerSecond * wheelCircumference; // m/s
            currentSpeedMPH = linearSpeed * 2.23694f;
        }

        if (uiText != null)
            uiText.text = $"{currentSpeedMPH:F1} MPH";
    }
}

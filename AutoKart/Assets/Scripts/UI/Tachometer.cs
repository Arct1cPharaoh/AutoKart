using UnityEngine;
using TMPro;

public class Tachometer : MonoBehaviour
{
    public TextMeshProUGUI uiText;

    private Rigidbody rb;
    private VehicleSpecs specs;
    private float wheelCircumference;

    private void Start()
    {
        // Find Rigidbody and VehicleSpecs in the root body
        rb = GetComponentInParent<Rigidbody>();
        specs = GetComponentInParent<VehicleSpecs>();

        if (rb == null)
        {
            Debug.LogError("Tachometer: Rigidbody not found in parent hierarchy.");
            enabled = false;
            return;
        }

        if (specs == null)
        {
            Debug.LogError("Tachometer: VehicleSpecs not found in parent hierarchy.");
            enabled = false;
            return;
        }

        wheelCircumference = 2f * Mathf.PI * specs.tireRadius;
    }

    private void Update()
    {
        float speed = rb.linearVelocity.magnitude; // m/s
        float rpm = (speed / wheelCircumference) * 60f;

        if (uiText != null)
            uiText.text = $"{rpm:F0} RPM";
    }
}

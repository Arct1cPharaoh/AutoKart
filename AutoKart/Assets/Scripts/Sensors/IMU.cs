using UnityEngine;

public class IMU : MonoBehaviour
{
    // Local Frame
    public Vector3 linearAccel;
    public Vector3 angularVelocity;

    private Vector3 lastVelocity;
    private Quaternion lastRotation;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        lastVelocity = rb.linearVelocity;
        lastRotation = transform.rotation;
    }

    public Vector3 GetLinearAcceleration()
    {
        return linearAccel;
    }

    public Vector3 GetAngularVelocity()
    {
        return angularVelocity;
    }

    void FixedUpdate()
    {
        // linear acceleration in world space
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 worldAccel = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;

        // Convert to lagrangian
        linearAccel = transform.InverseTransformDirection(worldAccel);

        // Compute angular velocity
        Quaternion deltaRot = transform.rotation * Quaternion.Inverse(lastRotation);
        deltaRot.ToAngleAxis(out float angleDeg, out Vector3 axis);

        // safety for zero-rotation case
        if (float.IsInfinity(axis.x)) axis = Vector3.zero;

        if (angleDeg > 0.01f && !float.IsInfinity(axis.x))
        {
            float angleRad = angleDeg * Mathf.Deg2Rad;
            angularVelocity = axis * angleRad / Time.fixedDeltaTime;
        }
        else
        {
            angularVelocity = Vector3.zero;
        }

        // Update stored state
        lastVelocity = currentVelocity;
        lastRotation = transform.rotation;
    }
}

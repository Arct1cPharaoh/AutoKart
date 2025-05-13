using UnityEngine;

public class WheelPhysics
{
    public Transform wheelTransform;
    private VehicleSpecs specs;

    private Rigidbody rb;
    private float surfaceMu = 1.0f;
    private float normalForce = 0f;

    // Contact cache
    private bool isGrounded = false;
    private Vector3 contactPoint;
    private Vector3 contactNormal;

    private float angularVelocity = 0.0f; // rad/s
    private float currentAngle = 0.0f;    // degrees, for visual rotation

    public WheelPhysics(Transform wheelTransform, Rigidbody rb, VehicleSpecs specs)
    {
        this.specs = specs;
        this.wheelTransform = wheelTransform;
        this.rb = rb;
    }

    public void UpdateContact()
    {
        RaycastHit hit;
        Vector3 origin = wheelTransform.position;
        Vector3 direction = Vector3.down;

        // Visualize the ray — green if hit, red if not
        Color debugColor = Color.red;
        // 0.01 is just the right length to reach the ground (slightly higher than epsilon)
        if (Physics.Raycast(origin, direction, out hit, specs.tireRadius + 0.01f))
        {
            isGrounded = true;
            contactPoint = hit.point;
            contactNormal = hit.normal;

            var surface = hit.collider.GetComponent<TractionSurface>();
            surfaceMu = surface != null ? surface.frictionCoefficient : 1.0f;

            debugColor = Color.green;
        }
        else
        {
            isGrounded = false;
            surfaceMu = 0.0f;
        }

        // Draw the ray in the Scene view
        Debug.DrawRay(origin, direction * specs.tireRadius, debugColor);
    }

    public void SetNormalForce(float force)
    {
        normalForce = Mathf.Max(force, 0f);
    }

    private float ComputeWheelInertia()
    {
        float mass = 5.0f; // TODO: Replace with realistic wheel mass
        // I = 1/2 * m * r^2
        float inertia = 0.5f * mass * Mathf.Pow(specs.tireRadius, 2);
        return inertia;
    }

    private float ComputeTotalTorque(float driveTorque)
    {
        // --- Passive drivetrain drag (always present) ---
        float drivetrainDrag = 0.1f; // N·m·s — tweak as needed
        float passiveTorque = -angularVelocity * drivetrainDrag;

        // Ground rolling resistance torque (always opposes spin direction)
        float groundTorque = 0f;
        if (isGrounded && normalForce > 0f)
        {
            // Rolling resistance coefficient (tiny value, typical ~0.01–0.015)
            float crr = 0.00015f;

            // Rolling resistance force: F_rr = crr * normalForce
            float rollingResistanceForce = crr * normalForce;

            // Convert to torque: T = F * r
            float groundForce = -Mathf.Sign(angularVelocity) * rollingResistanceForce;
            groundTorque = groundForce * specs.tireRadius;
        }


        return driveTorque + passiveTorque + groundTorque;
    }

    private void UpdateAngularVelocity(float inertia, float torque)
    {
        float angularAccel = torque / inertia;
        angularVelocity += angularAccel * Time.fixedDeltaTime;

        // Integrate spin angle (degrees)
        currentAngle += angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime;
        currentAngle %= 360f;
    }

    private Vector3 ComputeDeltaVelocityFromSlip()
    {
        Vector3 contactVelocity = rb.GetPointVelocity(contactPoint);
        Vector3 rawForward = rb.transform.forward;
        Vector3 groundNormal = contactNormal; // from raycast
        Vector3 rollDir = Vector3.ProjectOnPlane(rawForward, groundNormal).normalized;

        float vehicleSpeed = Vector3.Dot(contactVelocity, rollDir);
        float wheelSpeed = angularVelocity * specs.tireRadius;

        float denom = Mathf.Max(Mathf.Abs(vehicleSpeed), Mathf.Abs(wheelSpeed), 0.1f);
        float slipRatio = (wheelSpeed - vehicleSpeed) / denom;

        float maxGrip = surfaceMu * normalForce;
        float tractionForce = Mathf.Clamp(slipRatio * maxGrip, -maxGrip, maxGrip);

        Vector3 tractionDirection = (tractionForce >= 0f) ? rollDir : -rollDir;
        Vector3 force = tractionDirection * Mathf.Abs(tractionForce);

        return force;
    }


    public Vector3 ComputeTractionVelocity(float torque)
    {
        UpdateContact();
        float interia = ComputeWheelInertia();
        float totalTorque = ComputeTotalTorque(torque);
        UpdateAngularVelocity(interia, totalTorque);

        // Apply rotation cleanly (avoid Euler flip)
        wheelTransform.localRotation = Quaternion.AngleAxis(currentAngle, Vector3.right);

        if (!isGrounded || normalForce <= 0f)
            return Vector3.zero;

        return ComputeDeltaVelocityFromSlip();
    }

    public void ApplyLateralGripForce()
    {
        UpdateContact();
        if (!isGrounded  || normalForce <= 0f)
            return;

        // Lateral direction relative to ground
        Vector3 lateralDir = wheelTransform.right;
        Vector3 groundNormal = contactNormal;
        lateralDir = Vector3.ProjectOnPlane(lateralDir, groundNormal).normalized;

        // Velocity at contact point
        Vector3 contactVelocity = rb.GetPointVelocity(contactPoint);
        float lateralSpeed = Vector3.Dot(contactVelocity, lateralDir);

        // Compute slip angle
        float forwardSpeed = Mathf.Max(Vector3.Dot(rb.linearVelocity, wheelTransform.forward), 0.1f);
        float slipAngle = Mathf.Atan2(lateralSpeed, forwardSpeed); // radians

        // Tire model
        float grip = specs.coefficientOfFriction * (1f - specs.loadSensitivity * normalForce);
        float lateralForceMag = -grip * normalForce * slipAngle; // Negative = restoring force

        // Apply lateral force
        Vector3 force = lateralDir * lateralForceMag;
        rb.AddForceAtPosition(force, contactPoint);

        // Debugging
        Debug.DrawRay(contactPoint, force * 0.001f, Color.cyan);
        Debug.DrawRay(contactPoint, lateralDir * 0.25f, Color.magenta);

        // Optional: visualize yaw torque line from CG
        Vector3 cg = rb.worldCenterOfMass;
        Vector3 leverArm = contactPoint - cg;
        Vector3 torqueVec = Vector3.Cross(leverArm, force);
        Debug.DrawRay(cg, torqueVec.normalized * 0.2f, Color.yellow);
    }

    public void ApplyBrakeForce(float forceMagnitude)
    {
        UpdateContact();

        if (!isGrounded || normalForce <= 0f)
            return;

        // Get local forward direction projected onto ground
        Vector3 forwardDir = wheelTransform.forward;
        Vector3 groundNormal = contactNormal;
        forwardDir = Vector3.ProjectOnPlane(forwardDir, groundNormal).normalized;

        // Get velocity at wheel contact
        Vector3 contactVelocity = rb.GetPointVelocity(contactPoint);
        float rollingSpeed = Vector3.Dot(contactVelocity, forwardDir);

        if (Mathf.Abs(rollingSpeed) < 0.05f)
            return; // avoid jitter when stopped

        // Clamp to direction of motion
        Vector3 brakeDir = -Mathf.Sign(rollingSpeed) * forwardDir;

        // Final brake force
        Vector3 brakeForce = brakeDir * forceMagnitude;

        rb.AddForceAtPosition(brakeForce, contactPoint);
        Debug.DrawRay(contactPoint, brakeForce * 0.001f, Color.red);
    }
}

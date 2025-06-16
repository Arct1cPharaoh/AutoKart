using UnityEngine;

public class PoseEstimator
{
    public struct Pose2D
    {
        public Vector2 position;
        public float heading; // radians
    }

    Pose2D pose;
    float imuVelocity = 0f;

    private const float initialHeadingDeg = -90f;
    private const float MPH_TO_MPS = 0.44704f;
    private const float SPEED_BLEND_WEIGHT = 0.7f;
    private const float POSITION_CORRECTION_GAIN = 0.5f;

    public PoseEstimator()
    {
        pose.heading = initialHeadingDeg * Mathf.Deg2Rad;
    }

    public Pose2D GetPose() => pose;

    // Helper: Fuse wheel speed and IMU acceleration
    private float FuseSpeed(float deltaTime, float speedMPH, IMU imu)
    {
        float wheelSpeed = speedMPH * MPH_TO_MPS;
        Vector3 linearAccel = imu.GetLinearAcceleration();
        imuVelocity += linearAccel.z * deltaTime;
        return Mathf.Lerp(wheelSpeed, imuVelocity, SPEED_BLEND_WEIGHT);
    }

    // Helper: Update heading with gyro and correction
    private void UpdateHeading(float deltaTime, IMU imu, float headingCorrection)
    {
        float yawRate = imu.GetAngularVelocity().y;
        pose.heading += yawRate * deltaTime;
        pose.heading += headingCorrection;
    }

    // Helper: Dead-reckon position
    private void UpdatePosition(float deltaTime, float speed)
    {
        Vector2 forward = new Vector2(Mathf.Sin(pose.heading), Mathf.Cos(pose.heading));
        pose.position += forward * speed * deltaTime;
    }

    // Helper: Apply visual correction offset
    private void ApplyPositionCorrection(Vector3 posOffset)
    {
        Vector2 offset2D = new Vector2(posOffset.x, posOffset.z);
        pose.position = Vector2.Lerp(
            pose.position, pose.position - offset2D, POSITION_CORRECTION_GAIN
        );
    }

    public void Update(float deltaTime, float speedMPH, IMU imu, Vector3 posOffset, float headingCorrection)
    {
        float fusedSpeed = FuseSpeed(deltaTime, speedMPH, imu);
        UpdateHeading(deltaTime, imu, headingCorrection);
        UpdatePosition(deltaTime, fusedSpeed);
        ApplyPositionCorrection(posOffset);
    }

    public void OverridePose(Vector2 position, float heading)
    {
        pose.position = position;
        pose.heading = heading;
    }
}

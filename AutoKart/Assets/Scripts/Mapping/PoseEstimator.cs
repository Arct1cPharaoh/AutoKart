using UnityEngine;

public class PoseEstimator
{
    public struct Pose2D
    {
        public Vector2 position;
        public float heading; // radians
    }

    Pose2D pose;
    float initialHeadingDeg = -90f;
    float imuVelocity = 0f;

    public PoseEstimator()
    {
        pose.heading = initialHeadingDeg * Mathf.Deg2Rad;
    }

    public Pose2D GetPose() => pose;

    public void Update(float deltaTime, float speedMPH, IMU imu, Vector3 posOffset)
    {
        float yawRate = imu.GetAngularVelocity().y;
        Vector3 linearAccel = imu.GetLinearAcceleration();

        float wheelSpeed = speedMPH * 0.44704f;
        imuVelocity += linearAccel.z * deltaTime;
        float fusedSpeed = Mathf.Lerp(wheelSpeed, imuVelocity, 0.7f);

        pose.heading += yawRate * deltaTime;

        Vector2 forward = new Vector2(Mathf.Sin(pose.heading), Mathf.Cos(pose.heading));
        pose.position += forward * imuVelocity * deltaTime;

        Vector2 offset2D = new Vector2(posOffset.x, posOffset.z);
        float correctionStrength = 0.05f; // FIXME: This needs to move
        pose.position = Vector2.Lerp(
            pose.position, pose.position - offset2D, correctionStrength
        );
    }
}

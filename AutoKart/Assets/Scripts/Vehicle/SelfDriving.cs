using UnityEngine;
using System.Collections.Generic;

public class SelfDriving : MonoBehaviour
{
    PoseEstimator poseEstimator;
    ConeDetector detector;
    ConeProjector projector;
    ConeTracking tracking;
    [SerializeField] ConeMapper mapper;

    Speedometer wheel;
    IMU imu;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0.3986f, 0.1971f);
    [SerializeField] private float horizontalFOV = 61.38998f;
    [SerializeField] private float verticalFOV = 48f;
    [SerializeField] private float coneHeightM = 0.45f;

    void Start()
    {
        detector = GetComponentInChildren<ConeDetector>();
        int width = detector.GetCameraWidth();
        int height = detector.GetCameraHeight();

        wheel = GetComponentInChildren<Speedometer>();
        imu = GetComponentInChildren<IMU>();

        poseEstimator = new PoseEstimator();
        tracking = new ConeTracking(mapper);
        projector = new ConeProjector(
            width, height, cameraOffset, horizontalFOV, verticalFOV, coneHeightM
        );
    }


    public Vector3 GetEstimatedPosition()
    {
        var pose = poseEstimator.GetPose();
        return new Vector3(pose.position.x, 0f, pose.position.y);
    }

    public float GetHeadingRadians()
    {
        return poseEstimator.GetPose().heading;
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        float speed = wheel.GetSpeedMPH();

        Vector3 carPos = GetEstimatedPosition();
        float carHeading = GetHeadingRadians();
        List<DetectedCone> cones = detector.TryDetectFrame(Time.deltaTime);
        if (cones != null)
        {
            foreach (DetectedCone cone in cones)
            {
                Vector3 world = projector.Project(cone.boundingBox, carPos, carHeading);
                tracking.RegisterCone(world, carPos);
            }
        }

        Vector3 offset = tracking.ComputePoseCorrection(carPos);
        poseEstimator.Update(deltaTime, speed, imu, offset);
    }
}

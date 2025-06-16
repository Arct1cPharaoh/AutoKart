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

    [Header("Debug Options")]
    [SerializeField] private bool debugOverridePose = false;

    void Start()
    {
        detector = GetComponentInChildren<ConeDetector>();
        int width = detector.GetCameraWidth();
        int height = detector.GetCameraHeight();

        wheel = GetComponentInChildren<Speedometer>();
        imu = GetComponentInChildren<IMU>();

        poseEstimator = new PoseEstimator();
    }

    public float GetHeadingRadians()
    {
        return poseEstimator.GetPose().heading;
    }

    public void OverrideEstimatedPoseWithTruePose()
    {
        Vector3 truePos = transform.position;
        float trueHeading = transform.eulerAngles.y * Mathf.Deg2Rad;
        poseEstimator.OverridePose(new Vector2(truePos.x, truePos.z), trueHeading);
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
                tracking.RegisterCone(world);
            }

            if (debugOverridePose)
            {
                OverrideEstimatedPoseWithTruePose();
            }
        }

        Vector3 offset = tracking.ComputePoseCorrection(carPos, carHeading);
        // float headingCorrection = tracking.ComputeHeadingCorrection(carPos, carHeading);
        poseEstimator.Update(deltaTime, speed, imu, offset, 0.0f);
    }
}

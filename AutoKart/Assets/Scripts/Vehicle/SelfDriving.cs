using UnityEngine;
using System.Collections.Generic;

public class SelfDriving : MonoBehaviour
{
    PoseEstimator poseEstimator;
    ConeDetector detector;
    ConeProjector projector;
    public ConeTracking tracking; // FIXME: Make not public
    PathPlanner pathPlanner;
    PathFollower follower;
    [SerializeField] ConeMapper mapper;

    Speedometer wheel;
    IMU imu;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0.1922f, 0.4241f, 0.0f);
    [SerializeField] private float horizontalFOV = 136.0f; // degrees
    [SerializeField] private float stereoBaseline = 0.2f; // meters

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
        tracking = new ConeTracking(mapper);

        // compute focal length (fx) from horizontal FOV, assume principal point cx is image center
        float hFovRad = horizontalFOV * Mathf.Deg2Rad;
        float fx = width / (2f * Mathf.Tan(hFovRad * 0.5f));
        float cx = width * 0.5f;
        projector = new ConeProjector(
            width, cameraOffset, fx, cx, stereoBaseline
        );
        pathPlanner = new PathPlanner();
        follower = GetComponent<PathFollower>();
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
        List<StereoDetectedCone> cones = detector.TryDetectFrame(deltaTime);
        if (cones != null && cones.Count != 0)
        {
            foreach (StereoDetectedCone cone in cones)
            {
                Vector3? conePos = projector.Project(cone, carPos, carHeading);
                if (conePos.HasValue)
                    tracking.RegisterCone(conePos.Value, cone.color, carPos);
            }

            List<Vector3> blues = tracking.GetConesByColor(Color.blue);
            List<Vector3> yellows = tracking.GetConesByColor(Color.yellow);
            List<Vector3> path = pathPlanner.UpdatePlan(blues, yellows, carPos);
            if (follower.isActiveAndEnabled)
                follower.FollowPath(path, carPos, carHeading);

            if (debugOverridePose)
                OverrideEstimatedPoseWithTruePose();
        }

        Vector3 offset = tracking.ComputePoseCorrection(carPos, carHeading);
        // float headingCorrection = tracking.ComputeHeadingCorrection(carPos, carHeading);
        poseEstimator.Update(deltaTime, speed, imu, offset, 0.0f);
    }
}

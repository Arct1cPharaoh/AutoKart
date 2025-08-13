using UnityEngine;

public class ConeProjector
{
    private readonly float fx; // focal length in pixels (x)
    private readonly float cx; // principal point x
    private readonly int imageWidth;
    private readonly float baseline;
    private readonly Vector3 cameraOffset;

    public ConeProjector(
        int imageWidth,
        Vector3 cameraOffset,
        float fx,
        float cx,
        float baseline
    )
    {
        this.imageWidth = imageWidth;
        this.cameraOffset = cameraOffset;
        this.fx = fx;
        this.cx = cx;
        this.baseline = baseline;
    }

    // Simple sanity: size consistency between left/right bboxes
    bool ValidateBoxScale(Rect left, Rect right, float maxRatioDiff = 0.3f)
    {
        float scaleL = left.width * left.height;
        float scaleR = right.width * right.height;
        if (scaleR <= 0f) return false;
        float ratio = Mathf.Abs(scaleL / scaleR - 1f);
        return ratio <= maxRatioDiff;
    }

    // Returns null if invalid / unreliable
    public Vector3? Project(StereoDetectedCone cone, Vector3 carPos, float heading)
    {
        var leftBox = cone.leftFrame.boundingBox;
        var rightBox = cone.rightFrame.boundingBox;

        if (!ValidateBoxScale(leftBox, rightBox)) {
            return null; // inconsistent stereo pair
        }

        float xL = leftBox.center.x;
        float xR = rightBox.center.x;
        float disparity = Mathf.Abs(xL - xR);
        if (disparity < 1e-1f) // threshold tuned to expected scale (avoid near-zero)
            return null;

        // Depth from standard formula: Z = f * B / disparity
        float depth = (fx * baseline) / disparity;

        // Bearing: account for principal point
        float xCenter = 0.5f * (xL + xR);
        float xOffset = xCenter - cx;
        float bearing = Mathf.Atan2(xOffset, fx); // radians

        Vector3 localDir = new Vector3(Mathf.Sin(bearing), 0f, Mathf.Cos(bearing));
        Quaternion carRot = Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f);
        Vector3 cameraWorldPos = carPos + carRot * cameraOffset;

        Vector3 coneWorld = cameraWorldPos + carRot * (localDir * depth);
        return coneWorld;
    }
}

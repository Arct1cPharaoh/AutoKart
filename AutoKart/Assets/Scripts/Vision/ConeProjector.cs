using UnityEngine;

public class ConeProjector
{
    private readonly int imageWidth;
    private readonly int imageHeight;
    private readonly float horizontalFOV;
    private readonly float verticalFOV;
    private readonly float stereoDiff;
    private readonly Vector3 cameraOffset;

    public ConeProjector(
        int imageWidth,
        int imageHeight,
        Vector3 cameraOffset,
        float horizontalFOV,
        float verticalFOV,
        float stereoDiff
    )
    {
        this.imageWidth = imageWidth;
        this.imageHeight = imageHeight;
        this.cameraOffset = cameraOffset;
        this.horizontalFOV = horizontalFOV;
        this.verticalFOV = verticalFOV;
        this.stereoDiff = stereoDiff;
    }

    float EstimateDisparity(StereoDetectedCone cone)
    {
        float xLeft = cone.leftFrame.boundingBox.center.x;
        float xRight = cone.rightFrame.boundingBox.center.x;
        return Mathf.Abs(xLeft - xRight);
    }

    float EstimateFocalLengthPixels()
    {
        float fovRadians = horizontalFOV * Mathf.Deg2Rad;
        return imageWidth / (2f * Mathf.Tan(fovRadians / 2f));
    }

    float EstimateDist(float disparity, float focalLengthPixels)
    {
        float distance = (stereoDiff * focalLengthPixels) / disparity;

        // Debug.Log($"[Triangulation] disparity: {disparity:F2}, focalLen(px): {focalLengthPixels:F2}, baseline: {stereoDiff:F2}, estDist: {distance:F2}");
        return distance;
    }

    float EstimateBearing(float xCenter)
    {
        float fovRadians = horizontalFOV * Mathf.Deg2Rad;
        float focalLengthPixels = imageWidth / (2f * Mathf.Tan(fovRadians / 2f));

        float xOffset = xCenter - (imageWidth / 2f);
        float bearing = Mathf.Atan2(xOffset, focalLengthPixels);

        // Debug.Log($"[BearingEst] xCenter: {xCenter:F2}, xOffset: {xOffset:F2}, focalLen: {focalLengthPixels:F2}, bearing(deg): {bearing * Mathf.Rad2Deg:F2}");
        return bearing;
    }

    public Vector3? Project(StereoDetectedCone cone, Vector3 carPos, float heading)
    {
        float disparity = EstimateDisparity(cone);

        if (disparity < 1e-5f) // FIXME: Use epsilon later
            return null;

        float focalLengthPixels = EstimateFocalLengthPixels();
        float distance = EstimateDist(disparity, focalLengthPixels);

        float xLeft = cone.leftFrame.boundingBox.center.x;
        float xRight = cone.rightFrame.boundingBox.center.x;
        float xCenter = (xLeft + xRight) * 0.5f;
        float bearing = EstimateBearing(xCenter);

        Vector3 localDirection = new Vector3(Mathf.Sin(bearing), 0f, Mathf.Cos(bearing));
        Quaternion carRotation = Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f);

        Vector3 cameraWorldPosition = carPos + carRotation * cameraOffset;
        Vector3 projectedWorldPos = cameraWorldPosition + carRotation * (localDirection * distance);

        // Debug.Log($"[Project] camWorldPos: {cameraWorldPosition}, coneWorldPos: {projectedWorldPos}");
        // Debug.DrawLine(cameraWorldPosition, projectedWorldPos, Color.magenta, 0.25f, false);

        return projectedWorldPos;
    }
}

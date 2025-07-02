using UnityEngine;

public class ConeProjector
{
    private readonly int imageWidth;
    private readonly int imageHeight;
    private readonly float horizontalFOV;
    private readonly float verticalFOV;
    private readonly float coneHeightMeters;
    private readonly Vector3 cameraOffset;

    public ConeProjector(
        int imageWidth,
        int imageHeight,
        Vector3 cameraOffset,
        float horizontalFOV,
        float verticalFOV,
        float coneHeightMeters)
    {
        this.imageWidth = imageWidth;
        this.imageHeight = imageHeight;
        this.cameraOffset = cameraOffset;
        this.horizontalFOV = horizontalFOV;
        this.verticalFOV = verticalFOV;
        this.coneHeightMeters = coneHeightMeters;
    }

    private float EstimateConeDistance(float pixelHeight)
    {
        float fovRadians = 0.5f * verticalFOV * Mathf.Deg2Rad;
        float focalLengthPixels = imageHeight / (2f * Mathf.Tan(fovRadians));
        float distance = (focalLengthPixels * coneHeightMeters) / pixelHeight;

        // Debug.Log($"[DistanceEst] pixelHeight: {pixelHeight:F2}, focalLen(px): {focalLengthPixels:F2}, estDist: {distance:F2}");

        return distance;
    }

    private float EstimateBearing(float xCenter)
    {
        float fovRadians = horizontalFOV * Mathf.Deg2Rad;
        float focalLengthPixels = imageWidth / (2f * Mathf.Tan(fovRadians / 2f));

        float xOffset = xCenter - (imageWidth / 2f);
        float bearing = Mathf.Atan2(xOffset, focalLengthPixels);

        // Debug.Log($"[BearingEst] xCenter: {xCenter:F2}, xOffset: {xOffset:F2}, focalLen: {focalLengthPixels:F2}, bearing(deg): {bearing * Mathf.Rad2Deg:F2}");
        return bearing;
    }

    public Vector3 Project(Rect bbox, Vector3 carPos, float heading)
    {
        float pixelHeight = bbox.height;
        float xCenter = bbox.center.x;

        float distance = EstimateConeDistance(pixelHeight);
        float bearing = EstimateBearing(xCenter);

        // Local direction vector
        Vector3 localDirection = new Vector3(Mathf.Sin(bearing), 0f, Mathf.Cos(bearing));
        Quaternion carRotation = Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f);

        Vector3 cameraWorldPosition = carPos + carRotation * cameraOffset;
        Vector3 projectedWorldPos = cameraWorldPosition + carRotation * (localDirection * distance);

        // Debug.Log($"[Project] camWorldPos: {cameraWorldPosition}, coneWorldPos: {projectedWorldPos}");

        // Debug.DrawLine(cameraWorldPosition, projectedWorldPos, Color.magenta, 0.25f, false);

        return projectedWorldPos;
    }
}

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
        return (focalLengthPixels * coneHeightMeters) / pixelHeight;
    }

    private float EstimateBearing(float xCenter)
    {
        float normalizedX = (xCenter - imageWidth / 2f) / (imageWidth / 2f);
        return normalizedX * (horizontalFOV / 2f) * Mathf.Deg2Rad;
    }

    public Vector3 Project(Rect bbox, Vector3 carPos, float heading)
    {
        float pixelHeight = bbox.height;
        float xCenter = bbox.center.x;

        float distance = EstimateConeDistance(pixelHeight);
        float bearing = EstimateBearing(xCenter);

        Vector3 localDirection = new Vector3(Mathf.Sin(bearing), 0f, Mathf.Cos(bearing));
        Quaternion carRotation = Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f);

        Vector3 cameraWorldPosition = carPos + carRotation * cameraOffset;
        return cameraWorldPosition + carRotation * (localDirection * distance);
    }
}

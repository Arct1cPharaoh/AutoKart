using UnityEngine;

public class MapKartController : MonoBehaviour
{
    public SelfDriving core;

    // Late Update is called once per frame after update
    void LateUpdate()
    {
        if (core == null) return;

        // Get estimated pose
        Vector3 estPos = core.GetEstimatedPosition();
        float heading = core.GetHeadingRadians();

        // Apply directly to position and yaw rotation
        transform.position = new Vector3(estPos.x, transform.position.y, estPos.z);
        transform.rotation = Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f);
    }
}

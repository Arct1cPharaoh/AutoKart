using UnityEngine;
using UnityEngine.UI;

public class GForceMeter : MonoBehaviour
{
    public RectTransform dot;
    public float scale = 50f; // Pixels per G

    private IMU imu;

    void Start()
    {
        imu = GetComponent<IMU>();
    }

    void Update()
    {
        if (imu == null || dot == null)
        {
            Debug.LogError("Missing GForceMeter required componets");
            return;
        }

        // Get hori accel in Gs
        float gravity = 9.81f;
        Vector2 gForce = new Vector2(imu.linearAccel.x, imu.linearAccel.z) / gravity;

        // Scale and apply to UI
        dot.anchoredPosition = gForce * scale;
    }
}
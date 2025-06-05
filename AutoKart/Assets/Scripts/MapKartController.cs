using UnityEngine;

public class MapKartController : MonoBehaviour
{
    public Transform realKart;
    public float scale = 0.1f;

    // Late Update is called once per frame after update
    void LateUpdate()
    {
        if (realKart == null) return;

        // Position
        Vector3 scaledPos = realKart.position * scale;
        float posY = transform.position.y;
        transform.position = new Vector3(scaledPos.x, posY, scaledPos.z);

        // Rotation (only yaw)
        Vector3 euler = realKart.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);
    }
}

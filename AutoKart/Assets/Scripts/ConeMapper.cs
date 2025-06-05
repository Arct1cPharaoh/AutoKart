using UnityEngine;

public class ConeMapper : MonoBehaviour
{
    public GameObject conePrefab;
    public bool placeInRealWorld = false;

    private Transform miniKart;
    private Transform coneRoot;
    private MapKartController controller;
    private ConeFusion fusion;
    private Transform realKart;
    private float scale;

    void Awake()
    {
        miniKart = transform;
        coneRoot = transform.parent;
        controller = GetComponent<MapKartController>();
        fusion = new ConeFusion(this);
        realKart = controller.realKart;
        scale = controller.scale;
    }

    Vector3 GetMinimapPosition(Vector3 realWorldPos)
    {
        Vector3 offset = realWorldPos - realKart.position;
        Vector3 scaledOffset = offset * scale;
        scaledOffset = new Vector3(scaledOffset.x, 0f, scaledOffset.z);
        return miniKart.position + scaledOffset;
    }

    GameObject PlaceConeOnMinimap(Vector3 realWorldPos)
    {
        Vector3 minimapPos = GetMinimapPosition(realWorldPos);

        // Create cone
        GameObject cone = Instantiate(
            conePrefab,
            minimapPos,
            conePrefab.transform.rotation,
            coneRoot
        );
        cone.transform.localScale = Vector3.one * scale;
        cone.layer = LayerMask.NameToLayer("SLAMMap");
        return cone;
    }

    GameObject PlaceConeInRealWorld(Vector3 realWorldPos)
    {
        GameObject cone = Instantiate(
            conePrefab,
            new Vector3(realWorldPos.x, 0f, realWorldPos.z),
            conePrefab.transform.rotation,
            coneRoot
        );
        cone.layer = LayerMask.NameToLayer("SLAMMap");
        return cone;
    }

    public GameObject PlaceCone(Vector3 realWorldPos)
    {
        if (realKart == null || miniKart == null ||
            coneRoot == null || conePrefab == null)
        {
            Debug.LogError("Cone Mapper Missing Assets");
            return null;
        }

        // PlaceConeOnMinimap(realWorldPos);

        // if (placeInRealWorld)
        return PlaceConeInRealWorld(realWorldPos);
    }

    public void UpdateConePosition(GameObject cone, Vector3 realWorldPos)
    {
        cone.transform.position = new Vector3(realWorldPos.x, 0f, realWorldPos.z);
    }

    // Wrapper for fusion code
    public void RegisterConeEstimate(Vector3 worldPos)
    {
        fusion.RegisterConeEstimate(worldPos);
    }
}

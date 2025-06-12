using UnityEngine;

public class ConeMapper : MonoBehaviour
{
    [SerializeField] private GameObject conePrefab;
    private Transform coneRoot;

    void Awake()
    {
        coneRoot = transform.parent;
    }

    public GameObject PlaceCone(Vector3 pos)
    {
        GameObject cone = Instantiate(
            conePrefab,
            new Vector3(pos.x, 0f, pos.z),
            conePrefab.transform.rotation,
            coneRoot
        );
        cone.layer = LayerMask.NameToLayer("SLAMMap");
        return cone;
    }

    public void UpdateConePosition(GameObject cone, Vector3 pos)
    {
        cone.transform.position = new Vector3(pos.x, 0f, pos.z);
    }
}

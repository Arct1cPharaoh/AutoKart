using UnityEngine;

public class ConeMapper : MonoBehaviour
{
    [SerializeField] private GameObject yellowConePrefab;
    [SerializeField] private GameObject blueConePrefab;
    private Transform coneRoot;

    void Awake()
    {
        coneRoot = transform.parent;
    }

    public GameObject PlaceCone(Vector3 pos, Color color)
    {
        GameObject prefab = color == Color.blue ? blueConePrefab : yellowConePrefab;

        GameObject cone = Instantiate(
            prefab,
            new Vector3(pos.x, 0f, pos.z),
            prefab.transform.rotation,
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

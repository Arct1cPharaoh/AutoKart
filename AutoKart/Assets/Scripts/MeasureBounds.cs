using UnityEngine;

// A tool for confirming realistic dimensions
public class MeasureBounds : MonoBehaviour
{
    // On start we print the Dimensions and center.
    void Start()
    {
        var bounds = new Bounds(transform.position, Vector3.zero);
        var renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        Debug.Log(gameObject.name + " Bounds Center: " + bounds.center);
        Debug.Log(gameObject.name + " Size (W x H x L): " + bounds.size.x + " x " + bounds.size.y + " x " + bounds.size.z);
    }
}

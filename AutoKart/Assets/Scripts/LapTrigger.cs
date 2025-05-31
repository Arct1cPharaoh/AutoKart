using UnityEngine;

public class LapTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        LapTimer timer = other.GetComponentInParent<LapTimer>();
        if (timer != null)
        {
            timer.TriggerLap();
        }
    }
}

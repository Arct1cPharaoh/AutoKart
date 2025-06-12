using UnityEngine;
using TMPro;

public class LapTimer : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI lastTimeText;
    public TextMeshProUGUI bestTimeText;

    private float currentTime = 0f;
    private float lastLapTime = 0f;
    private float bestLapTime = float.MaxValue;

    private bool timerRunning = false;

    private void Update()
    {
        if (timerRunning)
        {
            currentTime += Time.deltaTime;

            if (timeText != null)
                timeText.text = $"Time: {FormatTime(currentTime)}";
        }
    }

    public void TriggerLap()
    {
        if (!timerRunning)
        {
            // First trigger hit — start timing
            timerRunning = true;
            currentTime = 0f;
            Debug.Log("Lap timing started");
            return;
        }

        // Lap completed
        lastLapTime = currentTime;
        if (lastLapTime < bestLapTime)
            bestLapTime = lastLapTime;

        if (lastTimeText != null)
            lastTimeText.text = $"Last: {FormatTime(lastLapTime)}";

        if (bestTimeText != null)
            bestTimeText.text = $"Best: {FormatTime(bestLapTime)}";

        currentTime = 0f; // restart for next lap
        Debug.Log($"Lap completed: {FormatTime(lastLapTime)}");
    }

    private string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t % 60f;
        return $"{minutes:00}:{seconds:00.000}";
    }
}

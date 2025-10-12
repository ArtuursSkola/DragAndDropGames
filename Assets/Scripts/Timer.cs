using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    private float elapsedTime = 0f;
    private bool isPaused = false;

    public float ElapsedTime => elapsedTime; // allows ObjectScript to read final time

    void Start()
    {
        if (timerText == null)
            Debug.LogError("Timer text is not assigned in the Inspector!");
    }

    void Update()
    {
        if (isPaused || timerText == null)
            return;

        elapsedTime += Time.deltaTime;

        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void PauseTimer()
    {
        isPaused = true;
    }

    public void ResumeTimer()
    {
        isPaused = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        if (timerText != null)
            timerText.text = "00:00";
    }
}

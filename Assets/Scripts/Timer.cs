using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;  // Reference to the TextMeshProUGUI object
    float elapsedTime;

    void Start()
    {
        if (timerText == null)
        {
            Debug.LogError("Timer text is not assigned in the Inspector!");
        }
    }

    void Update()
    {
        if (timerText != null)
        {
            elapsedTime += Time.deltaTime;  // Increment time by deltaTime
            int minutes = Mathf.FloorToInt(elapsedTime / 60);  // Convert to minutes
            int seconds = Mathf.FloorToInt(elapsedTime % 60);  // Convert to seconds
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);  // Update text
        }
        else
        {
            Debug.LogError("Timer text is not assigned!");
        }
    }
}

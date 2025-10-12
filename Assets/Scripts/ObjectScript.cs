using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ObjectScript : MonoBehaviour
{
    [Header("Vehicles")]
    public GameObject[] vehicles;
    [HideInInspector] public Vector2[] startCoordinates;

    [Header("Audio")]
    public Canvas can;
    public AudioSource effects;
    public AudioClip[] audioCli;

    [HideInInspector] public bool rightPlace = false;
    public static GameObject lastDragged = null;
    public static bool drag = false;

    [Header("Stars UI")]
    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

    [Header("Winning UI")]
    public GameObject winningWindow;
    public Text scoreText;

    [Header("Win/Lose Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Timer References")]
    public Timer timer;                   // Reference to Timer script
    public TextMeshProUGUI timeText;      // Reference to "TimeText" inside WinningWindow

    private int totalVehicles;
    private int placedVehicles = 0;
    private int destroyedVehicles = 0;

    public void Initialize()
    {
        if (vehicles == null || vehicles.Length == 0)
        {
            Debug.LogError("[ObjectScript] No vehicles assigned to initialize!");
            return;
        }

        startCoordinates = new Vector2[vehicles.Length];
        totalVehicles = vehicles.Length;
        placedVehicles = 0;
        destroyedVehicles = 0;

        for (int i = 0; i < vehicles.Length; i++)
        {
            RectTransform rt = vehicles[i].GetComponent<RectTransform>();
            startCoordinates[i] = rt.anchoredPosition;
        }

        Debug.Log($"[ObjectScript] Initialized with {vehicles.Length} vehicles");
    }

    public void CarPlaced()
    {
        placedVehicles++;
        CheckWin();
    }

    public void CarDestroyed()
    {
        destroyedVehicles++;
        CheckWin();
    }

    private void CheckWin()
    {
        if (placedVehicles + destroyedVehicles >= totalVehicles)
        {
            ShowWinningWindow();
        }
    }

    private void ShowWinningWindow()
    {
        if (winningWindow != null && scoreText != null)
        {
            winningWindow.SetActive(true);

            // ✅ Pause timer and the game
            if (timer != null)
                timer.PauseTimer();
            Time.timeScale = 0f;

            // Background
            Image bg = winningWindow.GetComponent<Image>();

            // Reset panels
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            // Score text
            scoreText.text = $"Score: {placedVehicles}/{totalVehicles}";

            // Determine win or lose
            bool won = placedVehicles >= 8;

            if (won)
            {
                if (bg != null)
                    bg.color = new Color(0f, 1f, 0f, 0.4f); // green
                if (winPanel != null)
                    winPanel.SetActive(true);
            }
            else
            {
                if (bg != null)
                    bg.color = new Color(1f, 0f, 0f, 0.4f); // red
                if (losePanel != null)
                    losePanel.SetActive(true);
            }

            // Get and show elapsed time
            float totalTime = timer != null ? timer.ElapsedTime : 0f;
            int minutes = Mathf.FloorToInt(totalTime / 60);
            int seconds = Mathf.FloorToInt(totalTime % 60);

            if (timeText != null)
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            else
                Debug.LogWarning("[ObjectScript] TimeText not assigned!");

            // ⭐ Update stars based on score + time
            UpdateStars(placedVehicles, totalTime);
        }
        else
        {
            Debug.Log("Winning window or score text not assigned!");
        }
    }

    private void UpdateStars(int score, float time)
    {
        if (star1 == null || star2 == null || star3 == null)
        {
            Debug.LogWarning("[ObjectScript] One or more star GameObjects are not assigned!");
            return;
        }

        // Hide all stars first
        star1.SetActive(false);
        star2.SetActive(false);
        star3.SetActive(false);

        // Time thresholds (in seconds)
        float twoMinutes = 120f;
        float threeMinutes = 180f;

        // --- Star logic ---
        int stars = 0;

        if (score >= 12)
        {
            if (time < twoMinutes)
                stars = 3;
            else if (time < threeMinutes)
                stars = 2;
            else
                stars = 1;
        }
        else if (score >= 10 && score <= 11)
        {
            if (time < twoMinutes)
                stars = 2;
            else
                stars = 1;
        }
        else if (score >= 8 && score < 10)
        {
            if (time < twoMinutes)
                stars = 1;
            else
                stars = 0;
        }
        else
        {
            stars = 0;
        }

        // Activate the correct number of stars
        if (stars >= 1) star1.SetActive(true);
        if (stars >= 2) star2.SetActive(true);
        if (stars >= 3) star3.SetActive(true);
    }

    public void LeaveGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

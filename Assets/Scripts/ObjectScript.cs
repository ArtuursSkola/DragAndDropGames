using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class ObjectScript : MonoBehaviour
{
    public GameObject[] vehicles;
    [HideInInspector]
    public Vector2[] startCoordinates;
    public Canvas can;
    public AudioSource effects;
    public AudioClip[] audioCli;
    [HideInInspector]
    public bool rightPlace = false;
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

    // Call this when a car is placed correctly
    public void CarPlaced()
    {
        placedVehicles++;
        CheckWin();
    }

    // Call this when a car is destroyed
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

      
        Image bg = winningWindow.GetComponent<Image>();

        
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        
        scoreText.text = $"Score: {placedVehicles}/{totalVehicles}";

        
        bool won = placedVehicles >= 8;

        if (won)
        {
            if (bg != null)
                bg.color = new Color(0f, 1f, 0f, 0.4f); 
            if (winPanel != null)
                winPanel.SetActive(true);
        }
        else
        {
            if (bg != null)
                bg.color = new Color(1f, 0f, 0f, 0.4f); 
            if (losePanel != null)
                losePanel.SetActive(true);
        }

        
        UpdateStars(placedVehicles);
    }
    else
    {
        Debug.Log("Winning window or score text not assigned!");
    }
}

private void UpdateStars(int score)
{
    
    if (star1 == null || star2 == null || star3 == null)
    {
        Debug.LogWarning("[ObjectScript] One or more star GameObjects are not assigned!");
        return;
    }

    // Hide all stars by default
    star1.SetActive(false);
    star2.SetActive(false);
    star3.SetActive(false);

    // Show stars based on score
    if (score >= 8 && score < 10)
    {
        star1.SetActive(true);
    }
    else if (score >= 10 && score < 12)
    {
        star1.SetActive(true);
        star2.SetActive(true);
    }
    else if (score >= 12)
    {
        star1.SetActive(true);
        star2.SetActive(true);
        star3.SetActive(true);
    }
}

// Called when the "Leave" button is clicked
public void LeaveGame()
{
    // Assuming your main menu scene is called "MainMenu"
    SceneManager.LoadScene("MainMenu");
}

// Called when the "Restart" button is clicked
public void RestartGame()
{
    // Reloads the currently active scene
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

}
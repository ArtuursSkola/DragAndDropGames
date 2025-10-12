using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    public GameObject settingsPanel; // Assign your SettingsPanel here
    public Slider volumeSlider;      // Assign the slider here
    public AudioSource backgroundMusic; // Assign your background music AudioSource here

    private void Start()
    {
        // Hide the settings panel at start
        settingsPanel.SetActive(false);

        // Set slider to current volume value (0-1)
        volumeSlider.value = backgroundMusic.volume;

        // Listen for slider value changes
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    // Toggle settings panel visibility - link this to your button OnClick()
    public void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // Change volume based on slider value
    public void ChangeVolume(float value)
    {
        backgroundMusic.volume = value;
    }
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

}

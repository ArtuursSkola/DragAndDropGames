using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InterstitialAd : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    string _adUnitId;

    public event Action OnInterstitialAdReady;
    public bool isReady = false;
    [SerializeField] Button _interstitialAdButton;
    // if we tried to show an interstitial but it wasn't ready, remember to show it when loaded
    private bool pendingShowOnLoad = false;
    private Coroutine sceneShowCoroutine = null;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // attempt to show an interstitial every time a new scene is loaded
        Debug.Log("InterstitialAd: Scene loaded (" + scene.name + ") - attempting to show interstitial.");
        // stop any previous coroutine and start a fresh attempt with retries
        if (sceneShowCoroutine != null)
            StopCoroutine(sceneShowCoroutine);
        sceneShowCoroutine = StartCoroutine(AttemptShowWithRetries(10, 0.5f));
    }

    void Awake()
    {
        _adUnitId = _androidAdUnitId;
        // begin loading an interstitial immediately
        LoadAd();
    }

    private void Update()
    {
        if (AdManager.Instance != null && AdManager.Instance.interstitialAd != null)
        {
            if (_interstitialAdButton != null)
                _interstitialAdButton.interactable = isReady;
            else
                Debug.LogWarning("InterstitialAd: _interstitialAdButton is not assigned in the Inspector.");
        }
    }

    public void OnInterstitialAdButtonClicked()
    {
        Debug.Log("Interstitial ad button clicked!");
        ShowInterstitial();
    }

    public void LoadAd()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Tried to load interstitial ad before Unity ads was initialized!");
            return;
        }

        Debug.Log("Loading interstitial ad");
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd()
    {
        if (isReady)
        {
            //
            Advertisement.Show(_adUnitId, this);
            isReady = false;
            pendingShowOnLoad = false;
        } else {
            Debug.LogWarning("Interestitial ad is not ready yet!");
            // mark that we want to show once it finishes loading
            pendingShowOnLoad = true;
            LoadAd();
    }
    }

    public void ShowInterstitial()
    {
        // allow manual show attempt regardless of AdManager presence
        if (isReady)
        {
            Debug.Log("Showing interstitial ad manually!");
            ShowAd();
        }
        else
        {
            Debug.Log("Interstitial ad not ready yet, loading and will show when ready.");
            pendingShowOnLoad = true;
            LoadAd();
        }
    }

    // Called from SceneLoaded handler to attempt showing during scene change
    private void ShowInterstitialOnSceneChange()
    {
        if (isReady)
            ShowAd();
        else
        {
            pendingShowOnLoad = true;
            LoadAd();
        }
    }

    private System.Collections.IEnumerator AttemptShowWithRetries(int maxAttempts, float delaySeconds)
    {
        int attempts = 0;
        while (attempts < maxAttempts)
        {
            attempts++;
            if (isReady)
            {
                Debug.Log($"InterstitialAd: Ready on attempt {attempts} - showing ad.");
                ShowAd();
                yield break;
            }

            Debug.Log($"InterstitialAd: Not ready on attempt {attempts}, loading and will retry in {delaySeconds}s.");
            pendingShowOnLoad = true;
            LoadAd();

            yield return new WaitForSeconds(delaySeconds);
        }

        Debug.LogWarning("InterstitialAd: Exhausted attempts to show ad on scene load.");
        pendingShowOnLoad = false;
        sceneShowCoroutine = null;
    }


    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log("Interstitial ad loaded!");
        if (_interstitialAdButton != null)
            _interstitialAdButton.interactable = true;
        else
            Debug.LogWarning("InterstitialAd: loaded but _interstitialAdButton is not assigned.");
        isReady = true;
        OnInterstitialAdReady?.Invoke();
        if (pendingShowOnLoad)
        {
            Debug.Log("InterstitialAd: pending show detected - showing now.");
            ShowAd();
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning("Failed to load interstitial ad!");
        LoadAd();
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log("User clicked on interstitial ad!");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Interstitial ad watched completely!");
            // Restore time before applying the slow-down effect
            Time.timeScale = 1f;
            StartCoroutine(SlowDownTimeTemporarily(30f));
            LoadAd();
        }
        else
        {
            Debug.Log("Interstitial ad skipped or not fully watched.");
            // Ensure the game is not left paused when the ad is skipped
            Time.timeScale = 1f;
            LoadAd();
        }
    }
    private IEnumerator SlowDownTimeTemporarily(float seconds)
    {
        Time.timeScale = 0.4f;
        Debug.Log("Time slowed down to 0.4x for " + seconds + " seconds.");
        yield return new WaitForSecondsRealtime(seconds);

        Time.timeScale = 1f;
        Debug.Log("Time scale restored to normal.");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.Log("Error showing interstitial ad!");
        // Ensure the game is not left paused on failure
        Time.timeScale = 1f;
        LoadAd();
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log("Showing intersstitial ad at this moment!");
        Time.timeScale = 0f;
    }
    public void SetButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnInterstitialAdButtonClicked);
        _interstitialAdButton = button;
        _interstitialAdButton.interactable = false;
    }
}
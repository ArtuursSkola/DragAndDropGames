using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    string _adUnitId;
    [SerializeField] Button _rewardedAdButton;
    [SerializeField] FlyingObjectsManager flyingObjectManager;
    // whether the rewarded ad is loaded and ready to show
    public bool isReady = false;
    public static event Action OnRewardGranted;

    private void Awake()
    {
        _adUnitId = _androidAdUnitId;

        if (flyingObjectManager == null)
            flyingObjectManager = UnityEngine.Object.FindFirstObjectByType<FlyingObjectsManager>();
    }


    public void LoadAd()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Tried to load rewarded ad before Unity ads was initialized!");
            return;
        }
        Debug.Log("Loading rewarded ad");
        Advertisement.Load(_adUnitId, this);
    }
    // IUnityAdsLoadListener implementation
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log("Rewarded ad loaded: " + placementId);

        if (placementId.Equals(_adUnitId))
        {
            isReady = true;
            if (_rewardedAdButton != null)
                _rewardedAdButton.interactable = true;
        }
    }
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning("Failed to load rewarded ad!");
        StartCoroutine(WaitAndLoad(5f));
    }
    public IEnumerator WaitAndLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadAd();
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning("Failde to show rewarded ad!");
        isReady = false;
        // ensure game is not left paused if ad show failed
        Time.timeScale = 1f;
        StartCoroutine(WaitAndLoad(5f));
    }
    public void OnUnityAdsShowStart(string placementId)
    {
        // ad is being shown; mark not-ready so Show won't be attempted again
        isReady = false;
        // pause the game's time while ad is showing
        Time.timeScale = 0f;
    }
    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log("Rewarded ad clicked!");
    }
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
#if UNITY_ANDROID
        if (placementId.Equals(_adUnitId) &&
         showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Rewarded ad completed!");
            if (flyingObjectManager != null)
                flyingObjectManager.DestroyAllFlyingObjects();
            _rewardedAdButton.interactable = false;
            StartCoroutine(WaitAndLoad(10f));
            // notify listeners that reward should be granted
            try { OnRewardGranted?.Invoke(); } catch { }
        }
#else
        Debug.Log("Rewarded ad completed!");
        if (flyingObjectManager != null)
            flyingObjectManager.DestroyAllFlyingObjects();
        _rewardedAdButton.interactable = false;
        StartCoroutine(WaitAndLoad(10f));
#endif

        // Always resume time when the ad finishes (completed or skipped)
        Time.timeScale = 1f;
    }

    public void SetButton(Button btn)
    {
        if (btn == null)
            return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ShowAd);
        _rewardedAdButton = btn;
        // Allow the button to be clickable so ShowAd() can trigger a load if the ad isn't ready yet.
        // This makes testing easier: clicking will either show the ad (if ready) or start loading it.
        _rewardedAdButton.interactable = true;
        Debug.Log("RewardedAds: SetButton wired to button " + btn.name + ", interactable=" + _rewardedAdButton.interactable);

        // Start loading the rewarded ad immediately (LoadAd() has its own guard for Advertisement.isInitialized).
        LoadAd();
    }

    public void ShowAd()
    {
        if (!isReady)
        {
            Debug.Log("Rewarded ad not ready when ShowAd() called — loading now.");
            if (_rewardedAdButton != null) _rewardedAdButton.interactable = false;
            LoadAd();
            return;
        }

        if (_rewardedAdButton != null) _rewardedAdButton.interactable = false;
        Debug.Log("Showing rewarded ad now.");
        Advertisement.Show(_adUnitId, this);
    }
}


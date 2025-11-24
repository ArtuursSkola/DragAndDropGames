using System;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] string _androidGameId;
    [SerializeField] bool _testMode = true;
    private string _gameId;
    public event Action OnAdsInitialized;

    private void Awake()
    {
        InitializeAds();
    }

    public void InitializeAds()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        _gameId = _androidGameId;
#endif
        if (string.IsNullOrWhiteSpace(_gameId))
        {
            Debug.LogWarning("AdsInitializer: _androidGameId is empty. Skipping Advertisement.Initialize(). Set a valid Game ID in the Inspector to enable ads.");
            return;
        }

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Debug.Log("AdsInitializer: Initializing Unity Ads with gameId=" + _gameId + " testMode=" + _testMode);
            Advertisement.Initialize(_gameId, _testMode, this);
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity ads initialization complete!");
        OnAdsInitialized?.Invoke();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"Unity ads initialization failed: {error.ToString()} - {message}");
    }
}
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdManager : MonoBehaviour
{
    public AdsInitializer adsInitializer;
    public InterstitialAd interstitialAd;
    [SerializeField] bool turnOffInterstitialAd = false;
    private bool firstAdShown = false;

    // .......

    public static AdManager Instance { get; private set; }


    private void Awake()
    {
        if(adsInitializer == null)
            adsInitializer = FindFirstObjectByType<AdsInitializer>();

        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        adsInitializer.OnAdsInitialized += HandleAdsInitialized;
    }

    private void HandleAdsInitialized()
    {
        if(!turnOffInterstitialAd)
        {
            interstitialAd.OnInterstitialAdReady += HandleInterstitialReady;
            interstitialAd.LoadAd();
        }
    }

    private void HandleInterstitialReady()
    {
        if (!firstAdShown)
        {
            Debug.Log("Showing first time interstitial ad automatically!");
            interstitialAd.ShowAd();
            firstAdShown = true;
        }
        else
        {
            Debug.Log("Next interstitial ad is ready for manual show!");
        }
    }

    private void OnEnable()
    {
        // subscribe to sceneLoaded so we can wire interstitial button after scenes load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private bool firstSceneLoad = false;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (interstitialAd == null)
            interstitialAd = FindFirstObjectByType<InterstitialAd>();

        GameObject btnObj = GameObject.FindGameObjectWithTag("InterstitialButton");
        Button interstitialButton = null;
        if (btnObj != null)
            interstitialButton = btnObj.GetComponent<Button>();

        if (interstitialButton != null && interstitialAd != null)
        {
            interstitialAd.SetButton(interstitialButton);
        }

        if (!firstSceneLoad)
        {
            firstSceneLoad = true;
            Debug.Log("First time scene loaded!");
            return;
        }
        Debug.Log("Scene loaded, attempting to show interstitial ad...");

        // If the interstitial ad is ready, show it immediately.
        if (interstitialAd != null)
        {
            if (interstitialAd.isReady)
            {
                Debug.Log("Interstitial ad is ready; showing now.");
                interstitialAd.ShowAd();
            }
            else
            {
                Debug.Log("Interstitial ad not ready; loading and will show when ready.");
                // One-shot listener: when the ad becomes ready, show it and unsubscribe.
                Action onReady = null;
                onReady = () =>
                {
                    try { interstitialAd.OnInterstitialAdReady -= onReady; } catch { }
                    if (interstitialAd != null && interstitialAd.isReady)
                    {
                        interstitialAd.ShowAd();
                    }
                };
                interstitialAd.OnInterstitialAdReady += onReady;
                interstitialAd.LoadAd();
            }
        }
        else
        {
            Debug.LogWarning("InterstitialAd reference missing on AdManager when scene loaded.");
            HandleAdsInitialized();
        }
       
    
    }


}
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

    public RewardedAds rewardedAds;
    [SerializeField] bool turnOffRewardedAds = false;

    
    // .......
    public BannerAd bannerAd;
    [SerializeField] bool turnOffBannerAd = false;
    public static AdManager Instance { get; private set; }

    // Helper: safely find a GameObject by tag without throwing when the tag isn't defined.
    private GameObject SafeFindWithTag(string tag)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tag);
        }
        catch (UnityException)
        {
            // tag not defined in project; return null instead of throwing
            return null;
        }
    }


    private void Awake()
    {
        if(adsInitializer == null)
            adsInitializer = UnityEngine.Object.FindFirstObjectByType<AdsInitializer>();

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
        // Interstitial
        if (!turnOffInterstitialAd)
        {
            if (interstitialAd == null)
                interstitialAd = UnityEngine.Object.FindFirstObjectByType<InterstitialAd>();

            if (interstitialAd != null)
            {
                interstitialAd.OnInterstitialAdReady += HandleInterstitialReady;
                interstitialAd.LoadAd();
            }
            else
            {
                Debug.LogWarning("InterstitialAd not found during HandleAdsInitialized(). Will attempt to wire later on scene load.");
            }
        }

        // Rewarded
        if (!turnOffRewardedAds)
        {
            if (rewardedAds == null)
                rewardedAds = UnityEngine.Object.FindFirstObjectByType<RewardedAds>();

            if (rewardedAds != null)
            {
                rewardedAds.LoadAd();
            }
            else
            {
                Debug.LogWarning("RewardedAds not found during HandleAdsInitialized(). Will attempt to wire later on scene load.");
            }
        }

        // Banner: do not automatically load/show the banner on initialization.
        // We'll wire the Banner button (if any) on scene load so the user controls when banners appear.
        if (!turnOffBannerAd)
        {
            if (bannerAd == null)
                bannerAd = UnityEngine.Object.FindFirstObjectByType<BannerAd>();

            if (bannerAd != null)
            {
                Debug.Log("AdManager: BannerAd component present. Banner will be loaded/shown only when the Banner button is clicked or when BannerAd.SetButton triggers a load.");
            }
            else
            {
                Debug.LogWarning("BannerAd not found during HandleAdsInitialized(). Will attempt to wire later on scene load.");
            }
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
            interstitialAd = UnityEngine.Object.FindFirstObjectByType<InterstitialAd>();

        // Use SafeFindWithTag to avoid UnityException when a tag is not defined in the project.
        GameObject btnObj = SafeFindWithTag("InterstitialButton");
        Button interstitialButton = null;
        if (btnObj != null)
            interstitialButton = btnObj.GetComponent<Button>();

        if (interstitialButton != null && interstitialAd != null)
        {
            interstitialAd.SetButton(interstitialButton);
            Debug.Log("AdManager: wired interstitial button: " + interstitialButton.name);
        }

        // Ensure banner ad reference and wire the banner button (if present)
        if (bannerAd == null)
            bannerAd = UnityEngine.Object.FindFirstObjectByType<BannerAd>();

        // Try multiple ways to find the banner button: common tag "BannerButton", legacy tag "BannerAd", or by name.
        Button bannerAdButton = null;
        GameObject bannerObj = SafeFindWithTag("BannerButton");
        if (bannerObj == null)
            bannerObj = SafeFindWithTag("BannerAd");
        if (bannerObj == null)
            bannerObj = GameObject.Find("BannerAd");
        if (bannerObj != null)
            bannerAdButton = bannerObj.GetComponent<Button>();

        if (bannerAd != null && bannerAdButton != null)
        {
            bannerAd.SetButton(bannerAdButton);
            Debug.Log("AdManager: wired banner button: " + bannerAdButton.name);
        }
        else if (bannerAd != null && bannerAdButton == null)
        {
            Debug.LogWarning("AdManager: BannerAd component found but no Button GameObject with tag 'BannerButton' or 'BannerAd' or name 'BannerAd' was found.");
        }

        // Ensure rewarded ad reference and wire its button (always attempt wiring so UI works on first load)
        if (rewardedAds == null)
            rewardedAds = UnityEngine.Object.FindFirstObjectByType<RewardedAds>();

        GameObject rewardedObj = SafeFindWithTag("RewardedButton");
        Button rewardedAdButton = rewardedObj != null ? rewardedObj.GetComponent<Button>() : null;
        if (rewardedAds != null && rewardedAdButton != null)
        {
            rewardedAds.SetButton(rewardedAdButton);
            Debug.Log("AdManager: wired rewarded button: " + rewardedAdButton.name);
        }

        if (!firstSceneLoad)
        {
            firstSceneLoad = true;
            Debug.Log("First time scene loaded!");
            // don't auto-show interstitial on the very first scene load, but we've already wired buttons above
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

            // Ensure rewardedAds reference is set
            if (rewardedAds == null)
                rewardedAds = UnityEngine.Object.FindFirstObjectByType<RewardedAds>();

            rewardedObj = SafeFindWithTag("RewardedButton");
            rewardedAdButton = rewardedObj != null ? rewardedObj.GetComponent<Button>() : null;
            if (rewardedAds != null && rewardedAdButton != null)
            {
                rewardedAds.SetButton(rewardedAdButton);
                Debug.Log("AdManager: wired rewarded button in fallback block: " + rewardedAdButton.name);
            }
        }
       
    
    }


}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;

public class BannerAd : MonoBehaviour
{
    [SerializeField] string _androidAdUnitId = "Banner_Android";
    string _adUnitId;
    [SerializeField] Button _bannerAdButton;
    public bool isBannerVisible = false;
    [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;
    // track whether a banner load finished
    private bool _bannerLoaded = false;
    // if Show was requested before load completed
    private bool _pendingShowRequest = false;
    [SerializeField] bool _autoShowOnLoad = false;

    private void Awake()
    {
        _adUnitId = _androidAdUnitId;
        Advertisement.Banner.SetPosition(_bannerPosition);
    }

    public void LoadBanner()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Tried to load banner ad before Unity ads was initialized!");
            return;
        }

        Debug.Log("Loading banner ad");
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        Advertisement.Banner.Load(_adUnitId, options);
    }

    void OnBannerLoaded()
    {
        Debug.Log("Banner ad loaded");
        _bannerLoaded = true;
        if (_bannerAdButton != null)
            _bannerAdButton.interactable = true;

        if (_pendingShowRequest)
        {
            _pendingShowRequest = false;
            BannerOptions options = new BannerOptions
            {
                showCallback = OnBannerShown,
                hideCallback = OnBannerHidden,
                clickCallback = OnBannerClicked
            };
            Advertisement.Banner.Show(_adUnitId, options);
        }
        else if (_autoShowOnLoad)
        {
            Debug.Log("BannerAd: auto-showing banner because _autoShowOnLoad is true");
            ShowBannerAd();
        }
    }
    void OnBannerError(string message)
    {
        Debug.LogWarning("Banner ad failed to load: " + message);
        _bannerLoaded = false;
        _pendingShowRequest = false;
        StartCoroutine(RetryLoadAfterDelay());
    }

    private IEnumerator RetryLoadAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        LoadBanner();
    }
    public void ShowBannerAd()
    {
        if (isBannerVisible)
        {
            Debug.Log("BannerAd: ShowBannerAd called while visible -> hiding banner");
            HideBannerAd();
        }
        else
        {
            if (!_bannerLoaded)
            {
                _pendingShowRequest = true;
                LoadBanner();
                Debug.Log("BannerAd: requested load and will show when ready");
                return;
            }

            BannerOptions options = new BannerOptions
            {
                showCallback = OnBannerShown,
                hideCallback = OnBannerHidden,
                clickCallback = OnBannerClicked
            };
            Advertisement.Banner.Show(_adUnitId, options);
        }
    }

    public void HideBannerAd()
    {
        // Immediately update local state so UI reflects the hide action even if the SDK callback is delayed.
        isBannerVisible = false;
        _pendingShowRequest = false;
        Debug.Log("BannerAd: HideBannerAd called");
        Advertisement.Banner.Hide();
    }

    void OnBannerShown()
    {
        Debug.Log("Banner ad shown");
        isBannerVisible = true;
    }
    void OnBannerHidden()
    {
        Debug.Log("Banner ad hidden");
        isBannerVisible = false;
    }
    void OnBannerClicked()
    {
        Debug.Log("Banner ad clicked");
    }

    public void SetButton(Button button)
    {
        if (button == null)
            return;

        // Wire the provided button to toggle the banner ad.
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowBannerAd);
        _bannerAdButton = button;
        // allow clicks so user can request the banner even before load finishes
        _bannerAdButton.interactable = true;
        Debug.Log("BannerAd: SetButton wired to " + button.name + ", interactable=" + _bannerAdButton.interactable);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// CHANGES FOR ANDROID
public class CameraScript : MonoBehaviour
{
    // sensible defaults for orthographicSize (min = zoomed-in, max = zoomed-out)
    public float minZoom = 150f;
    private float maxZoom;

    // zoom speeds
    public float pinchZoomSpeed = 0.02f;
    public float mouseZoomSpeed = 10f;
    public float mouseFollowSpeed = 8f;
    public float touchPanSpeed = 1f;
    public ScreenBoundriesScript screenBoundries;
    public Camera cam;
    [Header("Behavior")]
    public bool allowZoom = true; // master toggle for zooming; can be disabled per-scene

    // runtime: whether zoom is currently enabled (may be turned off by scene presets)
    private bool zoomEnabled = true;

    float startZoom;
    Vector2 lastTouchPos;
    int panFingerId = -1;
    bool isTouchPanning = false;

    float lastTapTime = 0f;
    public float doubleTapMaxDelay = 0.4f;
    public float doubleTapMaxDistance = 100f;


    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
                cam = GetComponent<Camera>();
        }

        if (screenBoundries == null)
            screenBoundries = UnityEngine.Object.FindFirstObjectByType<ScreenBoundriesScript>();
    // listen for scene changes so we can apply scene-specific camera presets
    SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (cam == null)
        {
            Debug.LogError("Camera not found on CameraScript.");
            enabled = false;
            return;
        }

        startZoom = cam.orthographicSize;

        if (screenBoundries != null)
        {
            screenBoundries.RecalculateBounds();
            transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
        }
        // initialize max zoom limits based on world bounds
        UpdateMaxZoom();

        // initialize runtime zoom flag (can be overridden per-scene)
        zoomEnabled = allowZoom;
        // Apply any scene-specific camera configuration immediately (Start covers the initial scene)
        ConfigureForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureForScene(scene.name);
    }

    // Apply camera presets based on scene name. For the Hanoi scene we want landscape orientation
    // and a camera zoom that shows the whole map (not cropped).
    public void ConfigureForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        string s = sceneName.ToLowerInvariant();
        if (s.Contains("hanoi") || s.Contains("hannoi"))
        {
            Debug.Log("CameraScript: Applying Hanoi scene camera preset (landscape & fit map)");

            // Force a strict landscape orientation on mobile devices for the Hanoi scene.
            // Disable autorotation so the app stays in landscape even if the user holds the device portrait.
            #if !UNITY_EDITOR
            try {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            } catch { }
            #else
            // In the Editor this has no effect, but we still adjust the camera size below.
            #endif

            if (screenBoundries != null && cam != null)
            {
                screenBoundries.RecalculateBounds();
                Rect wb = screenBoundries.worldBounds;

                float sizeForHeight = wb.height * 0.5f;
                float sizeForWidth = (wb.width * 0.5f) / Mathf.Max(0.0001f, cam.aspect);
                float fitAllSize = Mathf.Max(sizeForHeight, sizeForWidth);

                cam.orthographicSize = fitAllSize;

                // ensure zoom limits are updated after forcing a full-fit size
                screenBoundries.RecalculateBounds();
                UpdateMaxZoom();

                // center camera instantly on map center
                Vector3 center = new Vector3(wb.x + wb.width * 0.5f, wb.y + wb.height * 0.5f, transform.position.z);
                transform.position = screenBoundries.GetClampedCameraPosition(center);
            }
            // disable zoom in the Hanoi scene (we want a fixed view)
            zoomEnabled = false;
            // ensure orientation is applied after a short delay (works around platform timing)
            #if !UNITY_EDITOR
            try { StartCoroutine(EnforceLandscapeLock()); } catch { }
            #endif
            return;
        }

        // for other scenes, ensure zoom is enabled according to the inspector toggle
        zoomEnabled = allowZoom;

        // restore autorotate behavior for non-Hanoi scenes (allow portrait + landscape)
        #if !UNITY_EDITOR
        try {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        } catch { }
        #endif
    }

    private System.Collections.IEnumerator EnforceLandscapeLock()
    {
        // Re-apply the landscape orientation repeatedly for a longer window.
        // Some devices or OS versions temporarily report portrait before honoring the lock,
        // so applying several times increases reliability. We'll also add diagnostics and
        // call the Android Activity API using SENSOR_LANDSCAPE which some devices respect better.
        float timeout = 3f;
        float elapsed = 0f;
        float interval = 0.15f;
        while (elapsed < timeout)
        {
            try
            {
                // disable autorotate entirely and force LandscapeLeft at Unity level
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.orientation = ScreenOrientation.LandscapeLeft;

                // Diagnostic: log current Unity-side orientation and autorotate flags
                Debug.Log($"CameraScript: EnforceLandscapeLock iteration elapsed={elapsed:F2}s; Screen.orientation={Screen.orientation}; autoP={Screen.autorotateToPortrait} autoPU={Screen.autorotateToPortraitUpsideDown} autoL={Screen.autorotateToLandscapeLeft} autoR={Screen.autorotateToLandscapeRight}");

                // Additionally request the Android activity to lock orientation at the OS level.
                // Use SENSOR_LANDSCAPE which allows both landscape-left and landscape-right and
                // is often more reliable across devices.
                #if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                        if (activity != null)
                        {
                            // ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE = 6
                            const int SCREEN_ORIENTATION_SENSOR_LANDSCAPE = 6;
                            activity.Call("setRequestedOrientation", SCREEN_ORIENTATION_SENSOR_LANDSCAPE);
                            Debug.Log("CameraScript: Called activity.setRequestedOrientation(SENSOR_LANDSCAPE)");
                        }
                        else
                        {
                            Debug.Log("CameraScript: activity was null when calling setRequestedOrientation");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("CameraScript: Exception while calling Android activity.setRequestedOrientation: " + ex);
                }
                #endif
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("CameraScript: Exception in EnforceLandscapeLock loop: " + ex);
            }

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (TransformationScript.isTransforming)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        DesktopFollowCursor();

        // mouse wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (zoomEnabled && Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            cam.orthographicSize -= scroll * mouseZoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            if (screenBoundries != null)
            {
                screenBoundries.RecalculateBounds();
                transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
            }
        }

        // removed double-click reset: clicking should not change zoom
#else
        HandleTouch();
#endif

        // pinch zoom (touch)
        if (Input.touchCount == 2)
        {
            if (zoomEnabled)
                HandlePinch();
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            if (screenBoundries != null)
            {
                screenBoundries.RecalculateBounds();
                UpdateMaxZoom();
                // ensure camera remains inside allowed area
                transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
            }
        }
    }

    void DesktopFollowCursor()
    {
        Vector3 mouse = Input.mousePosition;
        // ensure the mouse position is inside the screen and inside the camera viewport
        if (mouse.x < 0 || mouse.x > Screen.width || mouse.y < 0 || mouse.y > Screen.height)
            return;
        if (cam != null && !cam.pixelRect.Contains(new Vector2(mouse.x, mouse.y)))
            return;

        // EDGE PAN: move camera when cursor near screen edges
        float edgeThreshold = 40f; // px from edge
        Vector3 pan = Vector3.zero;
        if (mouse.x <= edgeThreshold) pan += Vector3.left;
        else if (mouse.x >= Screen.width - edgeThreshold) pan += Vector3.right;
        if (mouse.y <= edgeThreshold) pan += Vector3.down;
        else if (mouse.y >= Screen.height - edgeThreshold) pan += Vector3.up;

        if (pan != Vector3.zero)
        {
            float panSpeed = 800f * Time.deltaTime * (cam.orthographicSize / 10f); // scale with zoom
            transform.Translate(pan.normalized * panSpeed, Space.World);
            if (screenBoundries != null)
                transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
            return;
        }

        // If not edge panning, optionally follow cursor center (existing behavior)
        Vector3 screenPoint = new Vector3(mouse.x, mouse.y, cam.nearClipPlane);
        Vector3 targetWorld = cam.ScreenToWorldPoint(screenPoint);
        Vector3 desired = new Vector3(targetWorld.x, targetWorld.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, mouseFollowSpeed * Time.deltaTime);
        if (screenBoundries != null)
            transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
    }

    void HandleTouch()
    {
        if (Input.touchCount != 1)
            return;

        Touch t = Input.GetTouch(0);

        if (IsTouchingUIButton(t.position))
            return;

        if (t.phase == TouchPhase.Began)
        {
            lastTouchPos = t.position;
            panFingerId = t.fingerId;
            isTouchPanning = true;
        }
        else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && isTouchPanning && t.fingerId == panFingerId)
        {
            Vector2 delta = t.position - lastTouchPos;
            transform.Translate(ScreenDeltaToWorldDelta(delta) * touchPanSpeed, Space.World);
            lastTouchPos = t.position;

            if (screenBoundries != null)
                transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
        }
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
        {
            isTouchPanning = false;
            panFingerId = -1;
        }
    }

    bool IsTouchingUIButton(Vector2 touchPos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = touchPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                return true;
        }

        return false;
    }

    void HandlePinch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
        float currDist = (t0.position - t1.position).magnitude;
        cam.orthographicSize -= (currDist - prevDist) * pinchZoomSpeed;
    }

    Vector3 ScreenDeltaToWorldDelta(Vector2 delta)
    {
        float worldPerPixel = (cam.orthographicSize * 2f) / Screen.height;
        return new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0f);
    }

    IEnumerator ResetZoomSmooth()
    {
        if (!zoomEnabled)
            yield break;
        float duration = 0.25f;
        float elapsed = 0f;
        float initialZoom = cam.orthographicSize;


        float targetZoom = maxZoom;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cam.orthographicSize = Mathf.Lerp(initialZoom, targetZoom, elapsed / duration);
            if (screenBoundries != null)
            {
                screenBoundries.RecalculateBounds();
                UpdateMaxZoom();
                transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
            }
            yield return null;
        }

        cam.orthographicSize = targetZoom;
        if (screenBoundries != null)
        {
            screenBoundries.RecalculateBounds();
            UpdateMaxZoom();
            transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
        }
    }

    // Smooth-center camera to map center (uses unscaled time so it works even when Time.timeScale = 0)
    public void CenterToMapCenter(float duration = 0.6f)
    {
        Vector3 target = Vector3.zero;
        if (screenBoundries != null)
        {
            Rect wb = screenBoundries.worldBounds;
            target = new Vector3(wb.x + wb.width * 0.5f, wb.y + wb.height * 0.5f, transform.position.z);
        }
        StartCoroutine(SmoothCenter(target, duration));
    }

    private IEnumerator SmoothCenter(Vector3 targetWorld, float duration)
    {
        Vector3 start = transform.position;
        Vector3 target = new Vector3(targetWorld.x, targetWorld.y, start.z);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            Vector3 pos = Vector3.Lerp(start, target, k);
            if (screenBoundries != null)
                pos = screenBoundries.GetClampedCameraPosition(pos);
            transform.position = pos;
            yield return null;
        }
        Vector3 final = target;
        if (screenBoundries != null)
            final = screenBoundries.GetClampedCameraPosition(final);
        transform.position = final;
    }
void UpdateMaxZoom()
{
    if (screenBoundries == null || cam == null)
        return;

    Rect wb = screenBoundries.worldBounds;
        // size required to show the whole map vertically/horizontally
        float sizeForHeight = wb.height * 0.5f;
        float sizeForWidth = (wb.width * 0.5f) / Mathf.Max(0.0001f, cam.aspect);
        float fitAllSize = Mathf.Max(sizeForHeight, sizeForWidth); // shows entire map (may leave empty bands)
        float fillSize = Mathf.Min(sizeForHeight, sizeForWidth);   // fills screen without empty bands (may crop map)

        // To prevent showing empty background bands, restrict maxZoom to the "fill" size.
        // This ensures the camera cannot be zoomed out far enough to reveal empty space.
        maxZoom = Mathf.Max(minZoom, fillSize);

        // Safety: also ensure maxZoom is at least small positive number
        if (float.IsNaN(maxZoom) || maxZoom <= 0f)
            maxZoom = fitAllSize;
}

}
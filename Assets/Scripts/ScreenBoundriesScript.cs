using UnityEngine;

// CHANGES FOR ANDROID
public class ScreenBoundriesScript : MonoBehaviour
{
    [HideInInspector]
    public Vector3 screenPoint, offset;
    [HideInInspector]
    public float minX, maxX, minY, maxY;
   
    public Rect worldBounds = new Rect(-960, -540, 1920, 1080);
    [Range(0f, 0.5f)]
    public float padding = 0.02f;

    public Camera targetCamera;

    public enum CameraFitMode { FitAll, Fill }
    [Tooltip("FitAll shows the entire worldBounds (may show empty space). Fill crops to ensure no empty space is visible.")]
    public CameraFitMode fitMode = CameraFitMode.FitAll;

    public float minCamX { get; private set; }
    public float maxCamX { get; private set; }
    public float minCamY { get; private set; }
    public float maxCamY { get; private set; }

    float lastOrthoSize;
    float lastAspect;
    Vector3 lastCamPos;

    void Awake() {
        if(targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Fit the camera so the worldBounds are visible on start
        EnsureCameraFitsWorld(true);
    }

    void Update()
    {
        if(targetCamera == null)
        {
            return;
        }

        bool changed = false;

        if (targetCamera.orthographic)
        {
            if (!Mathf.Approximately(targetCamera.orthographicSize, lastOrthoSize))
                changed = true;
        }

        if (!Mathf.Approximately(targetCamera.aspect, lastAspect))
            changed = true;

        if (targetCamera.transform.position != lastCamPos)
            changed = true;

        if (changed) {
            // Re-evaluate camera size to avoid empty bands when aspect/size changed
            EnsureCameraFitsWorld(false);
        }
    }

    /// <summary>
    /// Adjust camera orthographic size to either show the entire worldBounds (FitAll)
    /// or to fill the screen without empty sypace (Fill). Optionally center the camera.
    /// </summary>
    public void EnsureCameraFitsWorld(bool centerCamera = true)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;
        float halfH = worldBounds.height * 0.5f;
        float halfW = worldBounds.width * 0.5f;

        float sizeForHeight = halfH;
        float sizeForWidth = halfW / Mathf.Max(0.0001f, targetCamera.aspect);

        float desiredSize;
        if (fitMode == CameraFitMode.FitAll)
            desiredSize = Mathf.Max(sizeForHeight, sizeForWidth);
        else // Fill
            desiredSize = Mathf.Min(sizeForHeight, sizeForWidth);

        // apply desired size
        targetCamera.orthographic = true;
        targetCamera.orthographicSize = desiredSize;

        // optionally center camera on bounds center
        if (centerCamera)
        {
            Vector3 center = new Vector3(worldBounds.center.x, worldBounds.center.y, targetCamera.transform.position.z);
            targetCamera.transform.position = GetClampedCameraPosition(center);
        }

        // refresh derived bounds
        RecalculateBounds();
    }

public void RecalculateBounds()
{
    if (targetCamera == null)
        return;

    float wbMinX = worldBounds.xMin;
    float wbMaxX = worldBounds.xMax;
    float wbMinY = worldBounds.yMin;
    float wbMaxY = worldBounds.yMax;

    if(targetCamera.orthographic)
    {
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;

        if(halfW * 2f >= (wbMaxX - wbMinX)) {
            minCamX = maxCamX = (wbMinX + wbMaxX) * 0.5f;
        } else {
            minCamX = wbMinX + halfW;
            maxCamX = wbMaxX - halfW;
        }

        if(halfH * 2f >= (wbMaxY - wbMinY)) {
            minCamY = maxCamY = (wbMinY + wbMaxY) * 0.5f;
        } else {
            minCamY = wbMinY + halfH;
            maxCamY = wbMaxY - halfH;
        }

        minY = wbMinY;
        maxY = wbMaxY;
    }

    lastOrthoSize = targetCamera.orthographicSize;
    lastAspect = targetCamera.aspect;
    lastCamPos = targetCamera.transform.position;
}


    // For draggable objects
    public Vector2 GetClampedPosition(Vector3 curPosition)
    {
        float shrinkW = worldBounds.width * padding;
        float shrinkH = worldBounds.height * padding;
        float wbMinX = worldBounds.xMin + shrinkW;
        float wbMaxX = worldBounds.xMax - shrinkW;
        float wbMinY = worldBounds.yMin + shrinkH;
        float wbMaxY = worldBounds.yMax - shrinkH;  

        float cx = Mathf.Clamp(curPosition.x, wbMinX, wbMaxX);
        float cy = Mathf.Clamp(curPosition.y, wbMinY, wbMaxY);
        return new Vector2(cx, cy);
    }

    // For camera movement
    public Vector3 GetClampedCameraPosition(Vector3 desiredCamCenter) {
        float cx = Mathf.Clamp(desiredCamCenter.x, minCamX, maxCamX);
        float cy = Mathf.Clamp(desiredCamCenter.y, minCamY, maxCamY);
        return new Vector3(cx, cy, desiredCamCenter.z);
    }
}
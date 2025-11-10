using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyingObjectsControllerScript : MonoBehaviour
{
    [HideInInspector]
    public float speed = 1f;
    public float fadeDuration = 1.5f;
    public float waveAmplitude = 25f;
    public float waveFrequency = 1f;

    private ObjectScript objectScript;
    private ScreenBoundriesScript screenBoundriesScript;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isFadingOut = false;
    private bool isExploding = false;
    private Image image;
    private Color originalColor;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalColor = image != null ? image.color : Color.white;

        objectScript = Object.FindFirstObjectByType<ObjectScript>();
        screenBoundriesScript = Object.FindFirstObjectByType<ScreenBoundriesScript>();

        StartCoroutine(FadeIn());
    }

    void Update()
    {
        // protect against missing components
        if (rectTransform == null)
            return;

        float waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;

        // Move in world-space so transform.position reflects the object's true location
        transform.position += new Vector3(-speed * Time.deltaTime, waveOffset * Time.deltaTime, 0f);

        // Destroy when leaving left or right world bounds (use worldBounds to avoid
        // relying on uninitialized minX/maxX fields)
        if (screenBoundriesScript != null)
        {
            float leftLimit = screenBoundriesScript.worldBounds.xMin + 80f;
            float rightLimit = screenBoundriesScript.worldBounds.xMax - 80f;

            if (speed > 0 && transform.position.x < leftLimit && !isFadingOut)
            {
                isFadingOut = true;
                StartCoroutine(FadeOutAndDestroy());
            }

            if (speed < 0 && transform.position.x > rightLimit && !isFadingOut)
            {
                isFadingOut = true;
                StartCoroutine(FadeOutAndDestroy());
            }
        }

        // Handle touch or mouse input
        Vector2 inputPosition;
        bool hasInput = TryGetInputPosition(out inputPosition);

        Camera cam = Camera.main;
        // only proceed if we have a valid camera
        if (cam == null)
            return;

        // Click/tap directly on Bomb
        if (hasInput && CompareTag("Bomb") && !isExploding &&
            RectTransformUtility.RectangleContainsScreenPoint(rectTransform, inputPosition, cam))
        {
            Debug.Log("The cursor collided with a bomb!");
            TriggerExplosion();
        }

        // Drag collision with flying objects
        if (hasInput && ObjectScript.drag && !isFadingOut &&
            RectTransformUtility.RectangleContainsScreenPoint(rectTransform, inputPosition, cam))
        {
            Debug.Log("Cursor collided with a flying object while dragging!");

            if (ObjectScript.lastDragged != null)
            {
                StartCoroutine(ShrinkAndDestroy(ObjectScript.lastDragged, 0.5f));
                ObjectScript.lastDragged = null;
                ObjectScript.drag = false;
            }

            // If bomb hit, use red; otherwise cyan
            if (CompareTag("Bomb"))
                StartToDestroy(Color.red);
            else
                StartToDestroy(Color.cyan);
        }
    }

    // robust input getter: returns false if no valid input or outside screen bounds
    bool TryGetInputPosition(out Vector2 position)
    {
        position = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        Vector3 mp = Input.mousePosition;
        // guard against NaN/Infinity
        if (!IsFinite(mp.x) || !IsFinite(mp.y))
            return false;

        // ignore if mouse is outside the game window
        if (mp.x < 0f || mp.x > Screen.width || mp.y < 0f || mp.y > Screen.height)
            return false;

        position = new Vector2(mp.x, mp.y);
        return true;
#else
        if (Input.touchCount > 0)
        {
            Vector2 tp = Input.GetTouch(0).position;
            if (!IsFinite(tp.x) || !IsFinite(tp.y))
                return false;
            position = tp;
            return true;
        }
        return false;
#endif
    }

    bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));

    public void TriggerExplosion()
    {
        if (isExploding) return;
        isExploding = true;

        if (objectScript != null && objectScript.effects != null && objectScript.audioCli != null && objectScript.audioCli.Length > 15)
            objectScript.effects.PlayOneShot(objectScript.audioCli[15], 5f);

        if (TryGetComponent<Animator>(out Animator animator))
            animator.SetBool("explode", true);

        if (image != null)
        {
            image.color = Color.red;
            StartCoroutine(RecoverColor(0.4f));
        }
        StartCoroutine(Vibrate());
        StartCoroutine(WaitBeforeExplode());
    }

    IEnumerator WaitBeforeExplode()
    {
        float radius = 0f;
        if (TryGetComponent<CircleCollider2D>(out CircleCollider2D circleCollider))
            radius = circleCollider.radius * transform.lossyScale.x;

        ExplodeAndDestroyNearby(radius);
        yield return new WaitForSeconds(1f);
        ExplodeAndDestroyNearby(radius);
        Destroy(gameObject);
    }

    void ExplodeAndDestroyNearby(float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var hit in hits)
        {
            if (hit != null && hit.gameObject != gameObject)
            {
                var obj = hit.GetComponent<FlyingObjectsControllerScript>();
                if (obj != null && !obj.isExploding)
                {
                    obj.StartToDestroy(Color.cyan);
                }
            }
        }
    }

    public void StartToDestroy(Color c)
    {
        if (!isFadingOut)
        {
            StartCoroutine(FadeOutAndDestroy());
            isFadingOut = true;

            if (image != null)
            {
                image.color = c;
                StartCoroutine(RecoverColor(0.5f));
            }
            StartCoroutine(Vibrate());

            if (objectScript != null && objectScript.effects != null && objectScript.audioCli != null && objectScript.audioCli.Length > 14)
                objectScript.effects.PlayOneShot(objectScript.audioCli[14]);
        }
    }

    IEnumerator Vibrate()
    {
#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
        Vector2 originalPosition = rectTransform.anchoredPosition;
        float duration = 0.3f;
        float elapsed = 0f;
        float intensity = 5f;

        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = originalPosition + Random.insideUnitCircle * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutAndDestroy()
    {
        float t = 0f;
        float startAlpha = canvasGroup.alpha;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    IEnumerator ShrinkAndDestroy(GameObject target, float duration)
    {
        Vector3 originalScale = target.transform.localScale;
        Quaternion originalRotation = target.transform.rotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t / duration);
            float angle = Mathf.Lerp(0f, 360f, t / duration);
            target.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        if (objectScript != null)
            objectScript.CarDestroyed(); // Keep your original destruction logic

        Destroy(target);
    }

    IEnumerator RecoverColor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (image != null)
            image.color = originalColor;
    }
}

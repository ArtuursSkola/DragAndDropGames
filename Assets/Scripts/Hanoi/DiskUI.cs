using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

// Disk UI with drag & drop behavior. Supports snapping to PegUI.dropZone on drop
// and reverting to original position if not dropped on a valid peg.
public class DiskUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int size = 1; // 1 = smallest, larger = bigger
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public HanoiManager manager;

    // runtime
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        // find root canvas for reparenting while dragging
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = FindObjectOfType<Canvas>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnDiskClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // store original parent and anchored position so we can revert if needed
        originalParent = rect.parent;
        originalAnchoredPos = rect.anchoredPosition;

        // only allow dragging the top disk of a peg: check parent peg if present
        var parentPeg = originalParent != null ? originalParent.GetComponentInParent<PegUI>() : null;
        if (parentPeg != null && parentPeg.Peek() != this)
        {
            // not top disk -> cancel drag
            eventData.pointerDrag = null;
            return;
        }

        // set parent to root canvas so the disk appears above other UI while dragging
        if (rootCanvas != null)
            rect.SetParent(rootCanvas.transform, true);

        // allow raycasts to pass through this disk while dragging so we can detect dropping targets
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.95f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rect == null || rootCanvas == null)
            return;

        Vector2 localPoint;
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rect.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // restore raycast blocking so the disk receives events normally
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // perform a UI raycast at the release position to find a PegUI
        PointerEventData pd = new PointerEventData(EventSystem.current);
        pd.position = eventData.position;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pd, results);

        PegUI targetPeg = null;
        foreach (var r in results)
        {
            var peg = r.gameObject.GetComponentInParent<PegUI>();
            if (peg != null)
            {
                targetPeg = peg;
                break;
            }
        }

        // find source peg from original parent
        PegUI sourcePeg = originalParent != null ? originalParent.GetComponentInParent<PegUI>() : null;

        // if a valid target was found and placement rules allow it, move the disk
        if (targetPeg != null && targetPeg.CanPlace(this))
        {
            // remove from source peg (only if it really is the top)
            if (sourcePeg != null && sourcePeg.Peek() == this)
            {
                sourcePeg.Pop();
            }

            targetPeg.Push(this);
        }
        else
        {
            // revert to original parent/position
            if (originalParent != null)
            {
                rect.SetParent(originalParent, false);
                rect.anchoredPosition = originalAnchoredPos;
            }
        }
    }
}

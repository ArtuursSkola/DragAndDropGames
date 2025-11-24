using UnityEngine;
using UnityEngine.EventSystems;

// Simple Disk component for Tower of Hanoi (UI Image based).
// Requires the GameObject to have a RectTransform and Image component.
public class DiskUI : MonoBehaviour, IPointerClickHandler
{
    public int size = 1; // 1 = smallest, larger = bigger
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public HanoiManager manager;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnDiskClicked(this);
    }
}

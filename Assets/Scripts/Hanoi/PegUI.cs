using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Peg UI holds disks in a stack (bottom-up). Attach to an empty UI GameObject that represents a peg.
public class PegUI : MonoBehaviour, IPointerClickHandler
{
    // the RectTransform under which disks will be parented and positioned
    public RectTransform dropZone;
    [HideInInspector]
    public List<DiskUI> disks = new List<DiskUI>();
    [HideInInspector]
    public HanoiManager manager;
    public int pegIndex = 0;

    public bool CanPlace(DiskUI disk)
    {
        if (disks.Count == 0) return true;
        return disk.size < disks[disks.Count - 1].size;
    }

    public void Push(DiskUI disk)
    {
        if (disk == null) return;
        disks.Add(disk);
        disk.transform.SetParent(dropZone, false);
        UpdateDiskPositions();
    }

    public DiskUI Pop()
    {
        if (disks.Count == 0) return null;
        var d = disks[disks.Count - 1];
        disks.RemoveAt(disks.Count - 1);
        UpdateDiskPositions();
        return d;
    }

    public DiskUI Peek()
    {
        if (disks.Count == 0) return null;
        return disks[disks.Count - 1];
    }

    public void UpdateDiskPositions()
    {
        float spacing = 8f;
        float y = 0f;
        for (int i = 0; i < disks.Count; i++)
        {
            var d = disks[i];
            d.rect.anchoredPosition = new Vector2(0f, y);
            // ensure top disk is last sibling to receive clicks
            d.rect.SetSiblingIndex(i);
            y += d.rect.rect.height + spacing;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnPegClicked(this);
    }
}

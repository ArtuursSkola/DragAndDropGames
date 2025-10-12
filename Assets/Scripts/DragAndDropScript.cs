using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropScript : MonoBehaviour, IPointerDownHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGro;
    private RectTransform rectTra;
    public ObjectScript objectScr;
    public ScreenBoundriesScript screenBou;

    // Start is called before the first frame update
    void Start()
    {
        canvasGro = GetComponent<CanvasGroup>();
        rectTra = GetComponent<RectTransform>();
        if (objectScr == null)
            objectScr = FindObjectOfType<ObjectScript>();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            Debug.Log("OnPointerDown");
            objectScr.effects.PlayOneShot(objectScr.audioCli[0]);
        }
    }

public void OnBeginDrag(PointerEventData eventData)
{
    if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
    {
        ObjectScript.drag = true;
        ObjectScript.lastDragged = eventData.pointerDrag;
        canvasGro.blocksRaycasts = false;
        canvasGro.alpha = 0.6f;

        // Calculate offset between mouse and rect center
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTra.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localMousePos
        );
        screenBou.offset = rectTra.anchoredPosition - localMousePos;
    }
}

public void OnDrag(PointerEventData eventData)
{
    if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTra.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localMousePos
        );
        rectTra.anchoredPosition = screenBou.GetClampedPosition(localMousePos + screenBou.offset);
    }
}

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Input.GetMouseButtonUp(0))
        {
            ObjectScript.drag = false;
            canvasGro.blocksRaycasts = true;
            canvasGro.alpha = 1.0f;

            if (objectScr.rightPlace)
            {
                canvasGro.blocksRaycasts = false;
                ObjectScript.lastDragged = null;
            }

            objectScr.rightPlace = false;
        }
    }
}
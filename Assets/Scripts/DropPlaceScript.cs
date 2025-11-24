using UnityEngine;
using UnityEngine.EventSystems;

public class DropPlaceScript : MonoBehaviour, IDropHandler
{
    private float placeZRot, vehicleZRot, rotDiff;
    private Vector3 placeSiz, vehicleSiz;
    private float xSizeDiff, ySizeDiff;
    public ObjectScript objScript;

    void Start()
    {
        if (objScript == null)
            objScript = UnityEngine.Object.FindFirstObjectByType<ObjectScript>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        if (eventData.pointerDrag.tag.Equals(tag))
        {
            var dragRect = eventData.pointerDrag.GetComponent<RectTransform>();
            var placeRect = GetComponent<RectTransform>();

            if (dragRect == null || placeRect == null || objScript == null)
            {
                Debug.LogWarning("RectTransform or objScript missing on dragged object or drop place.");
                return;
            }

            placeZRot = dragRect.transform.eulerAngles.z;
            vehicleZRot = placeRect.transform.eulerAngles.z;
            rotDiff = Mathf.Abs(placeZRot - vehicleZRot);
            Debug.Log("Rotation difference: " + rotDiff);

            placeSiz = dragRect.localScale;
            vehicleSiz = placeRect.localScale;
            xSizeDiff = Mathf.Abs(placeSiz.x - vehicleSiz.x);
            ySizeDiff = Mathf.Abs(placeSiz.y - vehicleSiz.y);
            Debug.Log("X size difference: " + xSizeDiff);
            Debug.Log("Y size difference: " + ySizeDiff);

            // Adjusted threshold: tighter like teacher's (0.05)
            if ((rotDiff <= 5 || (rotDiff >= 355 && rotDiff <= 360)) &&
                (xSizeDiff <= 0.05 && ySizeDiff <= 0.05))
            {
                Debug.Log("Correct place");
                objScript.rightPlace = true;
                objScript.CarPlaced();

                dragRect.SetParent(placeRect.parent);
                dragRect.anchoredPosition = placeRect.anchoredPosition;
                dragRect.localRotation = placeRect.localRotation;
                dragRect.localScale = placeRect.localScale;

                if (objScript.effects != null && objScript.audioCli != null)
                {
                    int audioIndex = -1;
                    switch (eventData.pointerDrag.tag)
                    {
                        case "Garbage": audioIndex = 2; break;
                        case "Medicine": audioIndex = 3; break;
                        case "Fire": audioIndex = 4; break;
                        case "Bus": audioIndex = 5; break;
                        case "B2": audioIndex = 6; break;
                        case "Concrete": audioIndex = 7; break;
                        case "E46": audioIndex = 8; break;
                        case "Tractor": audioIndex = 9; break;
                        case "Excavator": audioIndex = 10; break;
                        case "E61": audioIndex = 11; break;
                        case "Police": audioIndex = 12; break;
                        case "Tractor2": audioIndex = 13; break;
                        default:
                            Debug.Log("Unknown tag detected");
                            break;
                    }
                    if (audioIndex >= 0 && audioIndex < objScript.audioCli.Length && objScript.audioCli[audioIndex] != null)
                        objScript.effects.PlayOneShot(objScript.audioCli[audioIndex]);
                }
            }
        }
        else
        {
            objScript.rightPlace = false;
            if (objScript != null && objScript.effects != null && objScript.audioCli != null && objScript.audioCli.Length > 1 && objScript.audioCli[1] != null)
            {
                objScript.effects.PlayOneShot(objScript.audioCli[1]);
            }
            else
            {
                Debug.LogWarning("Missing reference in DropPlaceScript: objScript or audioCli[1] is null.");
            }

            if (objScript != null && objScript.vehicles != null && objScript.startCoordinates != null)
            {
                for (int i = 0; i < objScript.vehicles.Length; i++)
                {
                    if (objScript.vehicles[i] != null && objScript.vehicles[i].tag == eventData.pointerDrag.tag)
                    {
                        var vRect = objScript.vehicles[i].GetComponent<RectTransform>();
                        if (vRect != null)
                            vRect.localPosition = objScript.startCoordinates[i];
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("objScript.vehicles or startCoordinates is null!");
            }
        }
    }
}

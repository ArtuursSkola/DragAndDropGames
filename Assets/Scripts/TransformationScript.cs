using UnityEngine;
using UnityEngine.EventSystems;

public class TransformationScript : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float scaleSpeed = 0.5f;
    public static bool isTransforming = false;

    private bool rotateCW, rotateCCW, scaleUpY, scaleDownY, scaleUpX, scaleDownX;

    void Update()
    {
        if (ObjectScript.lastDragged == null)
        {
            isTransforming = false;
            return;
        }

        RectTransform rt = ObjectScript.lastDragged.GetComponent<RectTransform>();
        if (rt == null)
        {
            isTransforming = false;
            return;
        }

        if (rotateCW)
        {
            rt.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }

        if (rotateCCW)
        {
            rt.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        if (scaleUpY && rt.localScale.y < 0.9f)
        {
            rt.localScale += new Vector3(0f, scaleSpeed * Time.deltaTime, 0f);
        }

        if (scaleDownY && rt.localScale.y > 0.35f)
        {
            rt.localScale -= new Vector3(0f, scaleSpeed * Time.deltaTime, 0f);
        }

        if (scaleUpX && rt.localScale.x < 0.9f)
        {
            rt.localScale += new Vector3(scaleSpeed * Time.deltaTime, 0f, 0f);
        }

        if (scaleDownX && rt.localScale.x > 0.35f)
        {
            rt.localScale -= new Vector3(scaleSpeed * Time.deltaTime, 0f, 0f);
        }

        isTransforming = rotateCW || rotateCCW || scaleUpY || scaleDownY || scaleUpX || scaleDownX;
    }

    public void StartRotateCW(BaseEventData data) { rotateCW = true; }
    public void StopRotateCW(BaseEventData data) { rotateCW = false; }

    public void StartRotateCCW(BaseEventData data) { rotateCCW = true; }
    public void StopRotateCCW(BaseEventData data) { rotateCCW = false; }

    public void StartScaleUpY(BaseEventData data) { scaleUpY = true; }
    public void StopScaleUpY(BaseEventData data) { scaleUpY = false; }

    public void StartScaleDownY(BaseEventData data) { scaleDownY = true; }
    public void StopScaleDownY(BaseEventData data) { scaleDownY = false; }

    public void StartScaleUpX(BaseEventData data) { scaleUpX = true; }
    public void StopScaleUpX(BaseEventData data) { scaleUpX = false; }

    public void StartScaleDownX(BaseEventData data) { scaleDownX = true; }
    public void StopScaleDownX(BaseEventData data) { scaleDownX = false; }
}


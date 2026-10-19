using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class C4DrawingAreaAutoFit : MonoBehaviour
{
    private RectTransform rectTransform;
    private RawImage rawImage;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();

        FitToParent();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }

        FitToParent();
    }

    private void Update()
    {
        // Re-fit automatically if the phone/game-view resolution changes.
        if (Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            FitToParent();
        }
    }

    private void FitToParent()
    {
        if (rectTransform == null)
        {
            return;
        }

        // Fill the entire C4_DrawingMask parent.
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        Vector3 anchoredPosition = rectTransform.anchoredPosition3D;
        anchoredPosition.x = 0f;
        anchoredPosition.y = 0f;
        anchoredPosition.z = 0f;

        rectTransform.anchoredPosition3D = anchoredPosition;

        if (rawImage != null)
        {
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            rawImage.raycastTarget = true;
        }
    }
}

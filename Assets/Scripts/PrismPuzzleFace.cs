using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PrismPuzzleFaceType
{
    Rectangle,
    Triangle
}

public class PrismPuzzleFace :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Face Identity")]
    public PrismPuzzleFaceType faceType;

    public int faceId;

    [Header("References")]
    [SerializeField]
    private Canvas rootCanvas;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private RectTransform rectTransform;

    private Transform originalParent;
    private Vector2 originalAnchoredPosition;

    private PrismPuzzleSlot currentSlot;
    private bool lockedInSlot = false;

    public bool IsPlaced =>
        lockedInSlot;

    public PrismPuzzleSlot CurrentSlot =>
        currentSlot;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        if (rootCanvas == null)
        {
            rootCanvas =
                GetComponentInParent<Canvas>();
        }

        originalParent =
            transform.parent;

        originalAnchoredPosition =
            rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (lockedInSlot)
            return;

        canvasGroup.alpha = 0.75f;
        canvasGroup.blocksRaycasts = false;

        transform.SetParent(
            rootCanvas.transform,
            true
        );

        transform.SetAsLastSibling();
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (lockedInSlot)
            return;

        if (rootCanvas == null)
            return;

        rectTransform.anchoredPosition +=
            eventData.delta /
            rootCanvas.scaleFactor;
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (lockedInSlot)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (currentSlot == null)
        {
            ReturnToTray();
        }
    }

    public void SetCurrentSlot(
        PrismPuzzleSlot slot)
    {
        currentSlot = slot;
    }

    public void ClearCurrentSlot()
    {
        currentSlot = null;
    }

    public void SnapIntoSlot(
        PrismPuzzleSlot slot)
    {
        if (slot == null)
            return;

        currentSlot = slot;
        lockedInSlot = true;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        transform.SetParent(
            slot.transform,
            false
        );

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            Vector2.zero;

        rectTransform.localRotation =
            Quaternion.identity;

        rectTransform.localScale =
            Vector3.one;

        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        rectTransform.sizeDelta =
            slotRect.rect.size;

        Debug.Log(
            $"Puzzle face {faceId} snapped into {slot.name}."
        );
    }

    public void ReturnToTray()
    {
        lockedInSlot = false;
        currentSlot = null;

        transform.SetParent(
            originalParent,
            false
        );

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            originalAnchoredPosition;

        rectTransform.localRotation =
            Quaternion.identity;

        rectTransform.localScale =
            Vector3.one;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void UnlockAndReturn()
    {
        lockedInSlot = false;
        currentSlot = null;

        ReturnToTray();
    }
}
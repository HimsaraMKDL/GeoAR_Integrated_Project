using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrismPuzzleSlot :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Accepted Face")]
    public PrismPuzzleFaceType acceptedFaceType;

    public int acceptedFaceId;

    [Header("References")]
    [SerializeField]
    private PrismPuzzleManager puzzleManager;

    [SerializeField]
    private Image slotImage;

    [Header("Visual Feedback")]
    [SerializeField]
    private Color normalColor =
        new Color(1f, 1f, 1f, 0.18f);

    [SerializeField]
    private Color hoverColor =
        new Color(0.5f, 0.8f, 1f, 0.35f);

    [SerializeField]
    private Color correctColor =
        new Color(0.4f, 1f, 0.5f, 0.30f);

    [SerializeField]
    private Color wrongColor =
        new Color(1f, 0.3f, 0.3f, 0.35f);

    private bool occupied;
    private PrismPuzzleFace placedFace;

    public bool IsOccupied =>
        occupied;

    public PrismPuzzleFace PlacedFace =>
        placedFace;

    private void Awake()
    {
        if (slotImage == null)
        {
            slotImage =
                GetComponent<Image>();
        }

        if (slotImage != null)
        {
            slotImage.color =
                normalColor;

            slotImage.raycastTarget =
                true;
        }

        if (puzzleManager == null)
        {
            puzzleManager =
                GetComponentInParent<
                    PrismPuzzleManager>();
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (occupied)
            return;

        if (slotImage != null)
            slotImage.color = hoverColor;
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        if (occupied)
            return;

        if (slotImage != null)
            slotImage.color = normalColor;
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        if (occupied)
            return;

        PrismPuzzleFace draggedFace =
            eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<
                    PrismPuzzleFace>()
                : null;

        if (draggedFace == null)
            return;

        bool correctType =
            draggedFace.faceType ==
            acceptedFaceType;

        bool correctId =
            acceptedFaceId <= 0 ||
            draggedFace.faceId ==
            acceptedFaceId;

        if (correctType && correctId)
        {
            occupied = true;
            placedFace = draggedFace;

            draggedFace.SetCurrentSlot(this);
            draggedFace.SnapIntoSlot(this);

            if (slotImage != null)
                slotImage.color = correctColor;

            if (puzzleManager != null)
            {
                puzzleManager.NotifyCorrectPlacement(
                    draggedFace,
                    this
                );
            }
        }
        else
        {
            if (slotImage != null)
                slotImage.color = wrongColor;

            draggedFace.ClearCurrentSlot();
            draggedFace.ReturnToTray();

            if (puzzleManager != null)
            {
                puzzleManager.NotifyWrongPlacement(
                    draggedFace,
                    this
                );
            }

            Invoke(
                nameof(ResetSlotColor),
                0.35f
            );
        }
    }

    public void ResetSlot()
    {
        occupied = false;
        placedFace = null;

        CancelInvoke();

        if (slotImage != null)
            slotImage.color = normalColor;
    }

    private void ResetSlotColor()
    {
        if (!occupied &&
            slotImage != null)
        {
            slotImage.color = normalColor;
        }
    }
}
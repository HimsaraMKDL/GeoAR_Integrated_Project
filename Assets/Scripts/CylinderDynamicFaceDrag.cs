using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CylinderDynamicFaceDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Runtime References")]
    [SerializeField]
    private RectTransform puzzleBoard;

    [SerializeField]
    private Canvas rootCanvas;

    [SerializeField]
    private CylinderEdgeSnapManager snapManager;

    [SerializeField]
    private CylinderGuidedInteractionManager interactionManager;

    [Header("Settings")]
    [SerializeField]
    private float dragAlpha = 0.75f;

    [SerializeField]
    private bool keepInsidePuzzleBoard = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Transform originalParent;

    private Vector2 originalAnchoredPosition;

    private bool initialized = false;
    private bool isOnPuzzleBoard = false;

    public bool IsOnPuzzleBoard =>
        isOnPuzzleBoard;

    public CylinderGeneratedFace FaceData
    {
        get;
        private set;
    }

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        FaceData =
            GetComponent<CylinderGeneratedFace>();

        originalParent =
            transform.parent;

        if (rectTransform != null)
        {
            originalAnchoredPosition =
                rectTransform.anchoredPosition;
        }
    }

    // ==================================================
    // INITIALIZE
    // ==================================================

    public void Initialize(
        RectTransform board,
        Canvas canvas,
        CylinderEdgeSnapManager edgeSnapManager,
        CylinderGuidedInteractionManager newInteractionManager)
    {
        puzzleBoard = board;
        rootCanvas = canvas;
        snapManager = edgeSnapManager;
        interactionManager = newInteractionManager;

        originalParent = transform.parent;

        if (rectTransform == null)
            rectTransform =
                GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup =
                GetComponent<CanvasGroup>();

        if (rectTransform != null)
        {
            originalAnchoredPosition =
                rectTransform.anchoredPosition;
        }

        initialized =
            puzzleBoard != null &&
            rootCanvas != null;

        if (!initialized)
        {
            Debug.LogError(
                $"{name}: CylinderDynamicFaceDrag " +
                "initialization failed."
            );
        }
    }

    // ==================================================
    // BEGIN DRAG
    // ==================================================

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        // Added snapManager check to clear old snap relationship
        if (snapManager != null &&
            FaceData != null)
        {
            snapManager.ReleaseFace(
                FaceData
            );
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                dragAlpha;

            canvasGroup.blocksRaycasts =
                false;
        }

        /*
         * Bring dragged piece in front of
         * other UI elements.
         */
        transform.SetAsLastSibling();

        Debug.Log(
            $"{name} drag started."
        );
    }

    // ==================================================
    // DRAG
    // ==================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!initialized ||
            rectTransform == null)
        {
            return;
        }

        float scaleFactor =
            rootCanvas != null &&
            rootCanvas.scaleFactor > 0f
                ? rootCanvas.scaleFactor
                : 1f;

        rectTransform.anchoredPosition +=
            eventData.delta /
            scaleFactor;
    }

    // ==================================================
    // END DRAG
    // ==================================================

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                1f;

            canvasGroup.blocksRaycasts =
                true;
        }

        bool droppedInside =
            RectTransformUtility
                .RectangleContainsScreenPoint(
                    puzzleBoard,
                    eventData.position,
                    eventData.pressEventCamera
                );

        if (droppedInside)
        {
            PlaceOnPuzzleBoard(
                eventData
            );
        }
        else
        {
            ReturnToTray();
        }
    }

    // ==================================================
    // PLACE ON BOARD
    // ==================================================

    private void PlaceOnPuzzleBoard(
        PointerEventData eventData)
    {
        if (puzzleBoard == null ||
            rectTransform == null)
        {
            return;
        }

        /*
         * Preserve the exact visual position where
         * the student released the piece.
         */
        Vector3 currentWorldPosition =
            rectTransform.position;

        Quaternion currentWorldRotation =
            rectTransform.rotation;

        Vector3 currentWorldScale =
            rectTransform.lossyScale;

        transform.SetParent(
            puzzleBoard,
            true
        );

        rectTransform.position =
            currentWorldPosition;

        rectTransform.rotation =
            currentWorldRotation;

        /*
         * Do NOT force anchoredPosition to the
         * pointer position here.
         *
         * The dragged object already followed
         * the pointer, so preserving its world
         * transform gives the exact drop point.
         */

        if (keepInsidePuzzleBoard)
        {
            ClampInsidePuzzleBoard();
        }

        isOnPuzzleBoard =
            true;

        if (snapManager != null)
        {
            snapManager.TrySnap(
                this
            );
        }

        Debug.Log(
            $"{name} placed on CylinderPuzzleBoard " +
            $"at {rectTransform.anchoredPosition}."
        );
    }

    // ==================================================
    // RETURN TO TRAY
    // ==================================================

    public void ReturnToTray()
    {
        if (rectTransform == null ||
            originalParent == null)
        {
            return;
        }

        transform.SetParent(
            originalParent,
            false
        );

        rectTransform.anchoredPosition =
            originalAnchoredPosition;

        rectTransform.localRotation =
            Quaternion.identity;

        rectTransform.localScale =
            Vector3.one;

        isOnPuzzleBoard =
            false;

        Debug.Log(
            $"{name} returned to CylinderFaceTray."
        );
    }

    // ==================================================
    // KEEP INSIDE BOARD
    // ==================================================

    private void ClampInsidePuzzleBoard()
    {
        if (puzzleBoard == null ||
            rectTransform == null)
        {
            return;
        }

        Rect boardRect =
            puzzleBoard.rect;

        Rect faceRect =
            rectTransform.rect;

        float halfWidth =
            faceRect.width *
            Mathf.Abs(
                rectTransform.localScale.x
            ) *
            0.5f;

        float halfHeight =
            faceRect.height *
            Mathf.Abs(
                rectTransform.localScale.y
            ) *
            0.5f;

        Vector2 position =
            rectTransform.anchoredPosition;

        float minimumX =
            boardRect.xMin +
            halfWidth;

        float maximumX =
            boardRect.xMax -
            halfWidth;

        float minimumY =
            boardRect.yMin +
            halfHeight;

        float maximumY =
            boardRect.yMax -
            halfHeight;

        position.x =
            Mathf.Clamp(
                position.x,
                minimumX,
                maximumX
            );

        position.y =
            Mathf.Clamp(
                position.y,
                minimumY,
                maximumY
            );

        rectTransform.anchoredPosition =
            position;
    }

    // ==================================================
    // CLICK / SELECTION TEST
    // ==================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (FaceData == null)
            return;

        if (interactionManager != null)
        {
            interactionManager.SelectFace(
                this
            );
        }

        Debug.Log(
            $"Cylinder Face {FaceData.faceId} selected. " +
            $"Type = {FaceData.faceType}"
        );
    }
}
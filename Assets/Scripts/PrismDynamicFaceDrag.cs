using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PrismDynamicFaceDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private RectTransform puzzleBoard;
    private Canvas rootCanvas;
    private RectTransform rootCanvasRect;

    private Transform previousParent;
    private Vector2 previousAnchoredPosition;
    private int previousSiblingIndex;

    private Vector2 pointerOffset;

    private bool initialized;
    private bool isOnPuzzleBoard;

    private PrismGuidedInteractionManager interactionManager;
    private PrismEdgeSnapManager snapManager;
    private PrismGuidedSetupManager setupManager;

    private float currentRotation = 0f;
    private bool isFlipped = false;

    public float CurrentRotation => currentRotation;
    public bool IsOnPuzzleBoard => isOnPuzzleBoard;
    public RectTransform RectTransform => rectTransform;
    public bool IsFlipped => isFlipped;

    // --------------------------------------------------
    // INITIALIZE
    // --------------------------------------------------

    public void Initialize(
        RectTransform board,
        Canvas canvas,
        PrismGuidedInteractionManager manager = null,
        PrismEdgeSnapManager edgeSnapManager = null,
        PrismGuidedSetupManager guidedSetupManager = null)
    {
        puzzleBoard = board;
        rootCanvas = canvas;
        interactionManager = manager;
        snapManager = edgeSnapManager;
        setupManager = guidedSetupManager;

        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        if (rootCanvas == null)
        {
            rootCanvas =
                GetComponentInParent<Canvas>();
        }

        if (rootCanvas != null)
        {
            rootCanvasRect =
                rootCanvas.GetComponent<RectTransform>();
        }

        initialized =
            puzzleBoard != null &&
            rootCanvas != null &&
            rootCanvasRect != null;

        if (!initialized)
        {
            Debug.LogError(
                $"Dynamic drag initialization failed for {name}."
            );
        }
    }

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();
    }

    // --------------------------------------------------
    // POINTER CLICK
    // --------------------------------------------------

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        if (!isOnPuzzleBoard)
            return;

        if (interactionManager != null)
        {
            interactionManager.SelectFace(this);
        }
    }

    // --------------------------------------------------
    // ROTATION & FLIPPING
    // --------------------------------------------------

    public void RotateClockwise()
    {
        if (!isOnPuzzleBoard)
            return;

        currentRotation += 90f;

        if (currentRotation >= 360f)
            currentRotation = 0f;

        rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -currentRotation
            );

        Debug.Log(
            $"{name} rotated to {currentRotation} degrees."
        );
    }

    public void FlipHorizontal()
    {
        if (!isOnPuzzleBoard)
        {
            Debug.LogWarning(
                $"{name}: Cannot flip because face is not on PuzzleBoard."
            );
            return;
        }

        PrismGeneratedFace faceData =
            GetComponent<PrismGeneratedFace>();

        if (faceData == null ||
            !faceData.IsTriangle)
        {
            Debug.LogWarning(
                $"{name}: Only triangle faces can be flipped."
            );
            return;
        }

        isFlipped = !isFlipped;

        Vector3 currentScale =
            rectTransform.localScale;

        currentScale.x =
            isFlipped
                ? -Mathf.Abs(currentScale.x)
                : Mathf.Abs(currentScale.x);

        rectTransform.localScale =
            currentScale;

        Debug.Log(
            $"{name} flipped horizontally. " +
            $"Flipped = {isFlipped}"
        );
    }

    // --------------------------------------------------
    // BEGIN DRAG
    // --------------------------------------------------

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        previousParent =
            transform.parent;

        previousAnchoredPosition =
            rectTransform.anchoredPosition;

        previousSiblingIndex =
            transform.GetSiblingIndex();

        canvasGroup.alpha = 0.80f;
        canvasGroup.blocksRaycasts = false;

        Camera eventCamera =
            GetEventCamera();

        /*
         * Find pointer position inside root Canvas
         * BEFORE re-parenting.
         */
        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                rootCanvasRect,
                eventData.position,
                eventCamera,
                out Vector2 pointerCanvasPosition
            );

        /*
         * Find current face center position
         * relative to root Canvas.
         */
        Vector3 worldCenter =
            rectTransform.TransformPoint(
                rectTransform.rect.center
            );

        Vector2 faceCanvasPosition =
            rootCanvasRect.InverseTransformPoint(
                worldCenter
            );

        /*
         * Keeps the exact place where the child
         * touched the object.
         *
         * Therefore the face does NOT jump to its
         * center when dragging starts.
         */
        pointerOffset =
            faceCanvasPosition -
            pointerCanvasPosition;

        /*
         * Move temporarily to the Canvas root.
         */
        transform.SetParent(
            rootCanvasRect,
            true
        );

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        transform.SetAsLastSibling();

        MoveInCanvas(eventData);
    }

    // --------------------------------------------------
    // DRAG
    // --------------------------------------------------

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        MoveInCanvas(eventData);
    }

    private void MoveInCanvas(
        PointerEventData eventData)
    {
        Camera eventCamera =
            GetEventCamera();

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                rootCanvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPointerPosition))
        {
            rectTransform.anchoredPosition =
                localPointerPosition +
                pointerOffset;
        }
    }

    // --------------------------------------------------
    // END DRAG
    // --------------------------------------------------

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        Camera eventCamera =
            GetEventCamera();

        bool insideBoard =
            RectTransformUtility
                .RectangleContainsScreenPoint(
                    puzzleBoard,
                    eventData.position,
                    eventCamera
                );

        if (insideBoard)
        {
            PlaceOnPuzzleBoard(
                eventData
            );
        }
        else
        {
            RestorePreviousPosition();
        }
    }

    // --------------------------------------------------
    // PLACE ON BOARD
    // --------------------------------------------------

    private void PlaceOnPuzzleBoard(
        PointerEventData eventData)
    {
        Camera eventCamera =
            GetEventCamera();

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                puzzleBoard,
                eventData.position,
                eventCamera,
                out Vector2 boardPointerPosition))
        {
            RestorePreviousPosition();
            return;
        }

        /*
         * Convert the drag offset from Canvas space
         * into PuzzleBoard local scale.
         */
        Vector2 boardOffset =
            ConvertCanvasOffsetToBoardOffset(
                pointerOffset
            );

        Vector2 desiredPosition =
            boardPointerPosition +
            boardOffset;

        transform.SetParent(
            puzzleBoard,
            false
        );

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        // Update: Keep flip state when placing on board
        rectTransform.localScale =
            new Vector3(
                isFlipped ? -1f : 1f,
                1f,
                1f
            );

        rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -currentRotation
            );

        rectTransform.anchoredPosition =
            ClampInsideBoard(
                desiredPosition
            );

        isOnPuzzleBoard = true;

        if (snapManager != null)
        {
            snapManager.TrySnapFace(this);
        }

        /*
         * Successful board placement ??
         * Undo history ??? register ?????.
         */
        if (setupManager != null)
        {
            setupManager.RegisterPlacedFace(
                this
            );
        }

        Debug.Log(
            $"{name} placed correctly on PuzzleBoard at " +
            $"{rectTransform.anchoredPosition}."
        );
    }

    private Vector2 ConvertCanvasOffsetToBoardOffset(
        Vector2 canvasOffset)
    {
        Vector3 canvasVector =
            rootCanvasRect.TransformVector(
                new Vector3(
                    canvasOffset.x,
                    canvasOffset.y,
                    0f
                )
            );

        Vector3 boardVector =
            puzzleBoard.InverseTransformVector(
                canvasVector
            );

        return new Vector2(
            boardVector.x,
            boardVector.y
        );
    }

    // --------------------------------------------------
    // KEEP FACE INSIDE BOARD
    // --------------------------------------------------

    private Vector2 ClampInsideBoard(
        Vector2 desiredPosition)
    {
        Rect boardRect =
            puzzleBoard.rect;

        float halfWidth =
            rectTransform.rect.width *
            0.5f;

        float halfHeight =
            rectTransform.rect.height *
            0.5f;

        float minX =
            boardRect.xMin +
            halfWidth;

        float maxX =
            boardRect.xMax -
            halfWidth;

        float minY =
            boardRect.yMin +
            halfHeight;

        float maxY =
            boardRect.yMax -
            halfHeight;

        /*
         * Safety for unusually large generated faces.
         */
        if (minX > maxX)
        {
            minX = 0f;
            maxX = 0f;
        }

        if (minY > maxY)
        {
            minY = 0f;
            maxY = 0f;
        }

        return new Vector2(
            Mathf.Clamp(
                desiredPosition.x,
                minX,
                maxX
            ),
            Mathf.Clamp(
                desiredPosition.y,
                minY,
                maxY
            )
        );
    }

    // --------------------------------------------------
    // RESTORE
    // --------------------------------------------------

    private void RestorePreviousPosition()
    {
        if (previousParent == null)
            return;

        transform.SetParent(
            previousParent,
            false
        );

        int maximumIndex =
            Mathf.Max(
                0,
                previousParent.childCount - 1
            );

        transform.SetSiblingIndex(
            Mathf.Clamp(
                previousSiblingIndex,
                0,
                maximumIndex
            )
        );

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            previousAnchoredPosition;

        // Update: Keep flip state when restoring position
        rectTransform.localScale =
            new Vector3(
                isFlipped ? -1f : 1f,
                1f,
                1f
            );

        rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -currentRotation
            );

        isOnPuzzleBoard =
            previousParent ==
            puzzleBoard;
    }

    // --------------------------------------------------
    // RESET SUPPORT
    // --------------------------------------------------

    public void ReturnToTray(
        Transform tray)
    {
        if (tray == null)
            return;

        transform.SetParent(
            tray,
            false
        );

        rectTransform.localScale =
            Vector3.one;

        // Reset rotation and flip state when returning to tray
        rectTransform.localRotation =
            Quaternion.identity;

        currentRotation = 0f;
        isFlipped = false; // Reset flip state when put back to tray

        isOnPuzzleBoard = false;
    }

    // --------------------------------------------------
    // CAMERA
    // --------------------------------------------------

    private Camera GetEventCamera()
    {
        if (rootCanvas == null)
            return null;

        if (rootCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return rootCanvas.worldCamera;
    }
}
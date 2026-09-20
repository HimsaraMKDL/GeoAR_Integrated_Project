using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismGuidedSetupManager : MonoBehaviour
{
    [Header("Drag System")]
    [SerializeField]
    private Canvas rootCanvas;

    [SerializeField]
    private PrismGuidedInteractionManager interactionManager;

    // Snap Manager reference
    [SerializeField]
    private PrismEdgeSnapManager edgeSnapManager;

    [Header("Selection Panels")]
    [SerializeField]
    private GameObject triangleChoicePanel;

    [SerializeField]
    private GameObject depthChoicePanel;

    [Header("Puzzle")]
    [SerializeField]
    private GameObject puzzleBoard;

    [SerializeField]
    private RectTransform faceTray;

    [Header("UI")]
    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text progressText;

    [SerializeField]
    private Button checkPuzzleButton;

    [SerializeField]
    private Button create3DButton;

    [Header("Puzzle UI Visibility")]
    [SerializeField]
    private GameObject rotateButtonObject;

    [SerializeField]
    private GameObject flipButtonObject;

    [SerializeField]
    private GameObject undoButtonObject;

    [SerializeField]
    private GameObject validateButtonObject;

    [SerializeField]
    private GameObject create3DButtonObject;

    [SerializeField]
    private GameObject guidedStatusObject;

    [SerializeField]
    private GameObject guidedProgressObject;

    [SerializeField]
    private GameObject guidedTitleObject;

    [Header("Generated Face Colors")]
    [SerializeField]
    private Color rectangleColor = new Color(0.25f, 0.72f, 0.38f, 1f);

    [SerializeField]
    private Color triangleColor = new Color(0.12f, 0.38f, 1f, 1f);

    [Header("Display Scaling")]
    [SerializeField]
    private float pixelsPerUnit = 32f;

    [SerializeField]
    private float minimumFaceWidth = 80f;

    [SerializeField]
    private float minimumFaceHeight = 70f;

    private PrismGuidedConfig config = new PrismGuidedConfig();

    private const string GeneratedPrefix = "GeneratedPrismFace_";

    private readonly List<PrismDynamicFaceDrag> placementHistory = new List<PrismDynamicFaceDrag>();

    private void Start()
    {
        ResetGuidedActivity();
    }

    // --------------------------------------------------
    // UI VISIBILITY HELPER
    // --------------------------------------------------

    private void SetPrismPuzzleUIVisible(bool visible)
    {
        if (puzzleBoard != null)
            puzzleBoard.SetActive(visible);

        if (faceTray != null)
            faceTray.gameObject.SetActive(visible);

        if (rotateButtonObject != null)
            rotateButtonObject.SetActive(visible);

        if (flipButtonObject != null)
            flipButtonObject.SetActive(visible);

        if (undoButtonObject != null)
            undoButtonObject.SetActive(visible);

        if (validateButtonObject != null)
            validateButtonObject.SetActive(visible);

        if (create3DButtonObject != null)
            create3DButtonObject.SetActive(visible);

        if (guidedStatusObject != null)
            guidedStatusObject.SetActive(visible);

        if (guidedProgressObject != null)
            guidedProgressObject.SetActive(visible);

        if (guidedTitleObject != null)
            guidedTitleObject.SetActive(visible);
    }

    // --------------------------------------------------
    // TRIANGLE SELECTION
    // --------------------------------------------------

    public void SelectRightTriangle()
    {
        config.SetTriangle(GuidedTriangleType.RightTriangle);

        ShowDepthSelection("Right triangle selected.");
    }

    public void SelectIsoscelesTriangle()
    {
        config.SetTriangle(GuidedTriangleType.IsoscelesTriangle);

        ShowDepthSelection("Isosceles triangle selected.");
    }

    private void ShowDepthSelection(string message)
    {
        if (triangleChoicePanel != null)
        {
            triangleChoicePanel.SetActive(false);
        }

        if (depthChoicePanel != null)
        {
            depthChoicePanel.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = message + " Now choose the prism depth.";
        }
    }

    // --------------------------------------------------
    // DEPTH SELECTION
    // --------------------------------------------------

    public void SelectShortDepth()
    {
        SelectDepth(GuidedPrismDepth.Short);
    }

    public void SelectMediumDepth()
    {
        SelectDepth(GuidedPrismDepth.Medium);
    }

    public void SelectLongDepth()
    {
        SelectDepth(GuidedPrismDepth.Long);
    }

    private void SelectDepth(GuidedPrismDepth depth)
    {
        config.SetDepth(depth);

        if (!config.IsReady)
        {
            Debug.LogError("Guided Prism configuration is incomplete.");
            return;
        }

        StartPuzzle();
    }

    // --------------------------------------------------
    // PUZZLE START
    // --------------------------------------------------

    private void StartPuzzle()
    {
        if (triangleChoicePanel != null)
        {
            triangleChoicePanel.SetActive(false);
        }

        if (depthChoicePanel != null)
        {
            depthChoicePanel.SetActive(false);
        }

        SetPrismPuzzleUIVisible(true);

        ClearGeneratedFaces();

        GenerateAllFaces();

        if (statusText != null)
        {
            statusText.text =
                "Build the prism net by connecting compatible edges.";
        }

        if (progressText != null)
        {
            progressText.text =
                "Place and connect all 5 faces.";
        }

        // IMPORTANT:
        // Student can validate at any time after puzzle starts.
        if (checkPuzzleButton != null)
        {
            checkPuzzleButton.interactable = true;

            Debug.Log(
                "Validate Prism button ENABLED."
            );
        }
        else
        {
            Debug.LogError(
                "Check Puzzle Button reference is missing."
            );
        }

        // Create 3D stays disabled until validation passes.
        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }

        Debug.Log(
            "Guided Prism puzzle started."
        );
    }

    // --------------------------------------------------
    // FACE GENERATION
    // --------------------------------------------------

    private void GenerateAllFaces()
    {
        /*
         * Triangle faces.
         */

        CreateTriangleFace(
            1,
            config.trianglePointA,
            config.trianglePointB,
            config.trianglePointC
        );

        CreateTriangleFace(
            2,
            config.trianglePointA,
            config.trianglePointB,
            config.trianglePointC
        );

        /*
         * Rectangle faces.
         *
         * Triangle sides:
         * a, b, c
         *
         * Rectangles:
         * a × depth
         * b × depth
         * c × depth
         */

        CreateRectangleFace(
            3,
            config.sideA,
            config.depth
        );

        CreateRectangleFace(
            4,
            config.sideB,
            config.depth
        );

        CreateRectangleFace(
            5,
            config.sideC,
            config.depth
        );

        Debug.Log(
            $"Generated guided prism faces. " +
            $"Triangle: {config.triangleType}, " +
            $"Sides: {config.sideA}, " +
            $"{config.sideB}, {config.sideC}, " +
            $"Depth: {config.depth}"
        );
    }

    private void CreateRectangleFace(
        int faceId,
        float edgeLength,
        float prismDepth)
    {
        if (faceTray == null)
            return;

        GameObject faceObject =
            new GameObject(
                GeneratedPrefix +
                faceId +
                "_Rectangle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(PrismGeneratedFace),
                typeof(PrismDynamicFaceDrag)
            );

        faceObject.transform.SetParent(
            faceTray,
            false
        );

        RectTransform rect =
            faceObject.GetComponent<RectTransform>();

        float width =
            Mathf.Max(
                minimumFaceWidth,
                edgeLength * pixelsPerUnit
            );

        float height =
            Mathf.Max(
                minimumFaceHeight,
                prismDepth * pixelsPerUnit
            );

        rect.sizeDelta =
            new Vector2(width, height);

        rect.localScale = Vector3.one;

        Image image = faceObject.GetComponent<Image>();
        image.color = rectangleColor;
        image.raycastTarget = true;

        PrismGeneratedFace data =
            faceObject.GetComponent<PrismGeneratedFace>();

        data.faceId = faceId;
        data.faceType = GeneratedPrismFaceType.Rectangle;
        data.edgeLength = edgeLength;
        data.prismDepth = prismDepth;

        AddFaceLabel(
            faceObject.transform,
            faceId.ToString()
        );

        Outline outline = faceObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2f, -2f);

        // Add the initialization for Drag System
        PrismDynamicFaceDrag drag = faceObject.GetComponent<PrismDynamicFaceDrag>();
        if (drag != null && puzzleBoard != null)
        {
            drag.Initialize(
                puzzleBoard.GetComponent<RectTransform>(),
                rootCanvas,
                interactionManager,
                edgeSnapManager,
                this
            );
        }
    }

    private void CreateTriangleFace(
        int faceId,
        Vector2 pointA,
        Vector2 pointB,
        Vector2 pointC)
    {
        if (faceTray == null)
            return;

        float minX =
            Mathf.Min(
                pointA.x,
                Mathf.Min(pointB.x, pointC.x)
            );

        float maxX =
            Mathf.Max(
                pointA.x,
                Mathf.Max(pointB.x, pointC.x)
            );

        float minY =
            Mathf.Min(
                pointA.y,
                Mathf.Min(pointB.y, pointC.y)
            );

        float maxY =
            Mathf.Max(
                pointA.y,
                Mathf.Max(pointB.y, pointC.y)
            );

        float triangleWidth =
            (maxX - minX) * pixelsPerUnit;

        float triangleHeight =
            (maxY - minY) * pixelsPerUnit;

        triangleWidth =
            Mathf.Max(
                minimumFaceWidth,
                triangleWidth
            );

        triangleHeight =
            Mathf.Max(
                minimumFaceHeight,
                triangleHeight
            );

        /*
         * Parent:
         * invisible drag/event container.
         */

        GameObject faceObject =
            new GameObject(
                GeneratedPrefix +
                faceId +
                "_Triangle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(PrismGeneratedFace),
                typeof(PrismDynamicFaceDrag)
            );

        faceObject.transform.SetParent(
            faceTray,
            false
        );

        RectTransform rect =
            faceObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(triangleWidth, triangleHeight);

        rect.localScale = Vector3.one;

        Image hitImage = faceObject.GetComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0f);
        hitImage.raycastTarget = true;

        PrismGeneratedFace data =
            faceObject.GetComponent<PrismGeneratedFace>();

        data.faceId = faceId;
        data.faceType = GeneratedPrismFaceType.Triangle;
        data.trianglePointA = pointA;
        data.trianglePointB = pointB;
        data.trianglePointC = pointC;

        /*
         * Triangle visual child.
         */

        GameObject visual =
            new GameObject(
                "TriangleVisual",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(PrismDynamicTriangleGraphic)
            );

        visual.transform.SetParent(
            faceObject.transform,
            false
        );

        RectTransform visualRect =
            visual.GetComponent<RectTransform>();

        visualRect.anchorMin = Vector2.zero;
        visualRect.anchorMax = Vector2.one;
        visualRect.offsetMin = Vector2.zero;
        visualRect.offsetMax = Vector2.zero;

        PrismDynamicTriangleGraphic graphic =
            visual.GetComponent<PrismDynamicTriangleGraphic>();

        graphic.color = triangleColor;
        graphic.raycastTarget = false;

        graphic.SetTriangle(
            pointA,
            pointB,
            pointC
        );

        AddFaceLabel(
            faceObject.transform,
            faceId.ToString()
        );

        // Add the initialization for Drag System
        PrismDynamicFaceDrag drag = faceObject.GetComponent<PrismDynamicFaceDrag>();
        if (drag != null && puzzleBoard != null)
        {
            drag.Initialize(
                puzzleBoard.GetComponent<RectTransform>(),
                rootCanvas,
                interactionManager,
                edgeSnapManager,
                this
            );
        }
    }

    private void AddFaceLabel(
        Transform parent,
        string number)
    {
        GameObject labelObject =
            new GameObject(
                "FaceNumber",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

        labelObject.transform.SetParent(
            parent,
            false
        );

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = labelObject.GetComponent<Text>();
        text.text = number;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        labelObject.transform.SetAsLastSibling();
    }

    // --------------------------------------------------
    // PLACEMENT HISTORY / UNDO
    // --------------------------------------------------

    public void RegisterPlacedFace(PrismDynamicFaceDrag face)
    {
        if (face == null)
            return;

        /*
         * Same face ?? ???? move ?????
         * history ??? duplicate ????
         * last placed face ?????? move ?????.
         */
        placementHistory.Remove(face);

        placementHistory.Add(face);

        Debug.Log(
            $"{face.name} added to Prism undo history. " +
            $"History count = {placementHistory.Count}"
        );
    }

    public void UndoLastPlacedFace()
    {
        /*
         * Destroy ???? ?????/null references
         * history ??? ???????? ???? ?????.
         */
        while (placementHistory.Count > 0 &&
               placementHistory[
                   placementHistory.Count - 1
               ] == null)
        {
            placementHistory.RemoveAt(
                placementHistory.Count - 1
            );
        }

        if (placementHistory.Count == 0)
        {
            if (statusText != null)
            {
                statusText.text =
                    "There is nothing to undo.";
            }

            Debug.Log(
                "Prism Undo: no placed faces."
            );

            return;
        }

        int lastIndex =
            placementHistory.Count - 1;

        PrismDynamicFaceDrag lastFace =
            placementHistory[lastIndex];

        placementHistory.RemoveAt(
            lastIndex
        );

        /*
         * Last dragged/placed face ??
         * FaceTray ??? ?????.
         */
        lastFace.ReturnToTray(
            faceTray
        );

        /*
         * Previous validation result ?? ????
         * ???????? valid ??.
         */
        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }

        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }

        if (statusText != null)
        {
            PrismGeneratedFace data =
                lastFace.GetComponent<
                    PrismGeneratedFace>();

            if (data != null)
            {
                statusText.text =
                    $"Face {data.faceId} returned to the tray.";
            }
            else
            {
                statusText.text =
                    "Last face returned to the tray.";
            }
        }

        if (progressText != null)
        {
            int remainingOnBoard =
                0;

            if (puzzleBoard != null)
            {
                PrismGeneratedFace[] boardFaces =
                    puzzleBoard
                        .GetComponentsInChildren<
                            PrismGeneratedFace>(
                                false
                            );

                remainingOnBoard =
                    boardFaces.Length;
            }

            progressText.text =
                $"{remainingOnBoard}/5 faces on the board.";
        }

        Debug.Log(
            $"{lastFace.name} undone and returned to FaceTray."
        );
    }

    private void ClearPlacementHistory()
    {
        placementHistory.Clear();

        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }

        Debug.Log(
            "Prism placement history cleared."
        );
    }

    // --------------------------------------------------
    // RESET
    // --------------------------------------------------

    public void ResetPrismForHome()
    {
        Debug.Log(
            "PRISM HOME RESET STARTED."
        );

        // ----------------------------------------------
        // 1. CLEAR HISTORY / SELECTION
        // ----------------------------------------------

        ClearPlacementHistory();

        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }

        // ----------------------------------------------
        // 2. DESTROY ALL GENERATED FACES
        // ----------------------------------------------

        ClearGeneratedFaces();

        // ----------------------------------------------
        // 3. RESET GEOMETRY CONFIG
        // ----------------------------------------------

        config.Reset();

        config.SetTriangle(
            GuidedTriangleType.IsoscelesTriangle
        );

        // ----------------------------------------------
        // 4. RESTORE INITIAL GUIDED SCREEN
        // ----------------------------------------------

        if (triangleChoicePanel != null)
        {
            triangleChoicePanel.SetActive(
                false
            );
        }

        if (depthChoicePanel != null)
        {
            depthChoicePanel.SetActive(
                true
            );
        }

        // ----------------------------------------------
        // 5. HIDE PUZZLE
        // ----------------------------------------------

        SetPrismPuzzleUIVisible(false);

        // ----------------------------------------------
        // 6. RESET BUTTON STATES
        // ----------------------------------------------

        if (checkPuzzleButton != null)
        {
            checkPuzzleButton.interactable =
                false;
        }

        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }

        // ----------------------------------------------
        // 7. TEXT RESET
        // ----------------------------------------------

        if (statusText != null)
        {
            statusText.text =
                "Choose the prism depth to begin.";
        }

        if (progressText != null)
        {
            progressText.text = "";
        }

        Debug.Log(
            "PRISM HOME RESET COMPLETED."
        );
    }

    public void ResetGuidedActivity()
    {
        ClearPlacementHistory();

        ClearGeneratedFaces();

        config.Reset();

        config.SetTriangle(
            GuidedTriangleType.IsoscelesTriangle
        );

        if (triangleChoicePanel != null)
        {
            triangleChoicePanel.SetActive(false);
        }

        if (depthChoicePanel != null)
        {
            depthChoicePanel.SetActive(true);
        }

        SetPrismPuzzleUIVisible(false);

        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }

        if (statusText != null)
        {
            statusText.text =
                "Choose the prism depth to begin.";
        }

        if (progressText != null)
        {
            progressText.text = "";
        }

        if (checkPuzzleButton != null)
        {
            checkPuzzleButton.interactable =
                false;
        }

        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }

        Debug.Log(
            "Prism Guided Activity fully reset."
        );
    }

    private void ClearGeneratedFaces()
    {
        /*
         * Find ALL runtime PrismGeneratedFace objects,
         * regardless of whether they are currently
         * inside FaceTray, PuzzleBoard, Canvas root,
         * or another generated face.
         */
        PrismGeneratedFace[] allFaces =
            FindObjectsOfType<PrismGeneratedFace>(
                true
            );

        foreach (PrismGeneratedFace face in allFaces)
        {
            if (face == null)
                continue;

            if (!face.name.StartsWith(
                    GeneratedPrefix))
            {
                continue;
            }

            face.gameObject.SetActive(false);

            Destroy(
                face.gameObject
            );
        }

        Debug.Log(
            $"FULL PRISM CLEANUP: " +
            $"{allFaces.Length} Prism face objects checked."
        );
    }

    // --------------------------------------------------
    // GETTERS FOR NEXT STAGES
    // --------------------------------------------------

    public PrismGuidedConfig GetConfig()
    {
        return config;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CuboidDrawManager : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private Text statusText;

    [Header("Validation")]
    [SerializeField] private CuboidNetValidator netValidator;

    [Header("Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button create3DButton;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 12;
    [SerializeField] private int gridHeight = 12;

    [Header("Line Settings")]
    [SerializeField] private float gridLineThickness = 2f;
    [SerializeField] private float drawnLineThickness = 3f;

    [Header("Colors")]
    [SerializeField]
    private Color gridColor =
        new Color(0f, 0f, 0f, 0.15f);

    [SerializeField]
    private Color drawnLineColor =
        new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("Face Visual Settings")]
    [SerializeField]
    private Color faceColor =
        new Color(1f, 1f, 1f, 0.85f);

    [SerializeField]
    private Color invalidFaceColor =
        new Color(1f, 0.35f, 0.35f, 0.75f);

    [SerializeField] private Font faceNumberFont;

    [Header("3D Cuboid")]
    [SerializeField] private CuboidModelSpawner cuboidModelSpawner;
    [SerializeField] private GameObject drawingCanvas;
    [SerializeField] private ARModeManager arModeManager;

    private RectTransform gridLayer;
    private RectTransform faceLayer;
    private RectTransform drawLayer;

    private readonly HashSet<CuboidGridEdge> drawnEdges =
        new HashSet<CuboidGridEdge>();

    private readonly List<CuboidGridEdge> edgeHistory =
        new List<CuboidGridEdge>();

    private readonly List<CuboidFace> detectedFaces =
        new List<CuboidFace>();

    private readonly List<GameObject> faceVisualObjects =
        new List<GameObject>();

    private Vector2Int lastGridPoint;
    private bool isDrawing;
    private bool initialized;

    private float CellWidth =>
        drawingArea != null && gridWidth > 0
            ? drawingArea.rect.width / gridWidth
            : 0f;

    private float CellHeight =>
        drawingArea != null && gridHeight > 0
            ? drawingArea.rect.height / gridHeight
            : 0f;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (initialized)
            UpdateStatusText();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        if (drawingArea == null)
            drawingArea = GetComponent<RectTransform>();

        if (drawingArea == null)
        {
            Debug.LogError(
                "CuboidDrawManager: Drawing Area is not assigned."
            );

            return;
        }

        CreateLayers();
        BuildGrid();

        if (undoButton != null)
        {
            undoButton.onClick.RemoveListener(
                UndoLastEdge
            );

            undoButton.onClick.AddListener(
                UndoLastEdge
            );
        }

        if (validateButton != null)
            validateButton.interactable = false;

        if (create3DButton != null)
            create3DButton.interactable = false;

        initialized = true;

        UpdateUndoButtonState();
        UpdateStatusText();
    }

    private void CreateLayers()
    {
        gridLayer =
            CreateLayer("CuboidGridLayer");

        faceLayer =
            CreateLayer("CuboidFaceLayer");

        drawLayer =
            CreateLayer("CuboidDrawLayer");

        gridLayer.SetAsFirstSibling();
        faceLayer.SetAsLastSibling();
        drawLayer.SetAsLastSibling();
    }

    private RectTransform CreateLayer(
        string layerName)
    {
        Transform existing =
            drawingArea.Find(layerName);

        if (existing != null)
        {
            RectTransform existingRect =
                existing.GetComponent<RectTransform>();

            if (existingRect != null)
                return existingRect;
        }

        GameObject layerObject =
            new GameObject(
                layerName,
                typeof(RectTransform)
            );

        layerObject.transform.SetParent(
            drawingArea,
            false
        );

        RectTransform rect =
            layerObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        return rect;
    }

    private void BuildGrid()
    {
        ClearLayer(gridLayer);

        for (int x = 0; x <= gridWidth; x++)
        {
            CreateLine(
                gridLayer,
                GridToLocal(new Vector2Int(x, 0)),
                GridToLocal(
                    new Vector2Int(x, gridHeight)
                ),
                gridLineThickness,
                gridColor,
                "CuboidGridVertical"
            );
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            CreateLine(
                gridLayer,
                GridToLocal(new Vector2Int(0, y)),
                GridToLocal(
                    new Vector2Int(gridWidth, y)
                ),
                gridLineThickness,
                gridColor,
                "CuboidGridHorizontal"
            );
        }
    }

    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (!CanDraw(eventData))
            return;

        isDrawing = true;

        lastGridPoint =
            ScreenToGridPoint(eventData);
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        Vector2Int currentGridPoint =
            ScreenToGridPoint(eventData);

        if (currentGridPoint == lastGridPoint)
            return;

        AddSnappedPath(
            lastGridPoint,
            currentGridPoint
        );

        lastGridPoint = currentGridPoint;
    }

    public void OnPointerUp(
        PointerEventData eventData)
    {
        isDrawing = false;

        DetectRectangleFaces();
        UpdateStatusText();
    }

    private bool CanDraw(
        PointerEventData eventData)
    {
        if (!initialized || drawingArea == null)
            return false;

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                drawingArea,
                eventData.position,
                eventData.pressEventCamera
            );
    }

    private Vector2Int ScreenToGridPoint(
        PointerEventData eventData)
    {
        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                drawingArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

        float x =
            localPoint.x +
            drawingArea.rect.width * 0.5f;

        float y =
            localPoint.y +
            drawingArea.rect.height * 0.5f;

        int gridX =
            Mathf.RoundToInt(x / CellWidth);

        int gridY =
            Mathf.RoundToInt(y / CellHeight);

        gridX =
            Mathf.Clamp(gridX, 0, gridWidth);

        gridY =
            Mathf.Clamp(gridY, 0, gridHeight);

        return new Vector2Int(gridX, gridY);
    }

    private Vector2 GridToLocal(
        Vector2Int gridPoint)
    {
        float x =
            gridPoint.x * CellWidth -
            drawingArea.rect.width * 0.5f;

        float y =
            gridPoint.y * CellHeight -
            drawingArea.rect.height * 0.5f;

        return new Vector2(x, y);
    }

    private void AddSnappedPath(
        Vector2Int from,
        Vector2Int to)
    {
        if (from == to)
            return;

        if (from.x == to.x)
        {
            int step =
                to.y > from.y ? 1 : -1;

            for (int y = from.y;
                 y != to.y;
                 y += step)
            {
                AddEdge(
                    new Vector2Int(from.x, y),
                    new Vector2Int(
                        from.x,
                        y + step
                    )
                );
            }

            return;
        }

        if (from.y == to.y)
        {
            int step =
                to.x > from.x ? 1 : -1;

            for (int x = from.x;
                 x != to.x;
                 x += step)
            {
                AddEdge(
                    new Vector2Int(x, from.y),
                    new Vector2Int(
                        x + step,
                        from.y
                    )
                );
            }
        }
    }

    private void AddEdge(
        Vector2Int pointA,
        Vector2Int pointB)
    {
        CuboidGridEdge edge =
            new CuboidGridEdge(
                pointA,
                pointB
            );

        if (drawnEdges.Contains(edge))
            return;

        // =====================================================
        // START RESEARCH ACTIVITY / ATTEMPT
        // =====================================================
        if (SessionDataLogger.Instance != null &&
            edgeHistory.Count == 0)
        {
            SessionDataLogger.Instance.StartActivitySession(
                "Cuboid"
            );

            SessionDataLogger.Instance.StartAttempt();
        }

        drawnEdges.Add(edge);
        edgeHistory.Add(edge);

        if (netValidator != null)
            netValidator.ResetValidation();

        CreateLine(
            drawLayer,
            GridToLocal(edge.PointA),
            GridToLocal(edge.PointB),
            drawnLineThickness,
            drawnLineColor,
            "CuboidDrawnEdge"
        );

        DetectRectangleFaces();
        UpdateUndoButtonState();
        UpdateStatusText();
    }

    public void UndoLastEdge()
    {
        if (edgeHistory.Count == 0)
            return;

        int lastIndex =
            edgeHistory.Count - 1;

        CuboidGridEdge lastEdge =
            edgeHistory[lastIndex];

        edgeHistory.RemoveAt(lastIndex);
        drawnEdges.Remove(lastEdge);

        if (netValidator != null)
            netValidator.ResetValidation();

        RebuildDrawnLines();
        DetectRectangleFaces();

        UpdateUndoButtonState();
        UpdateStatusText();
    }

    private void RebuildDrawnLines()
    {
        ClearLayer(drawLayer);

        foreach (CuboidGridEdge edge
                 in edgeHistory)
        {
            CreateLine(
                drawLayer,
                GridToLocal(edge.PointA),
                GridToLocal(edge.PointB),
                drawnLineThickness,
                drawnLineColor,
                "CuboidDrawnEdge"
            );
        }
    }

    private void DetectRectangleFaces()
    {
        detectedFaces.Clear();

        List<CuboidFace> newFaces =
            CuboidRectangleDetector.DetectFaces(
                drawnEdges,
                gridWidth,
                gridHeight
            );

        detectedFaces.AddRange(newFaces);

        RefreshFaceVisuals();

        if (validateButton != null)
        {
            validateButton.interactable =
                detectedFaces.Count == 6;
        }

        if (create3DButton != null)
            create3DButton.interactable = false;
    }

    private void RefreshFaceVisuals()
    {
        ClearFaceVisualObjects();

        for (int index = 0;
             index < detectedFaces.Count;
             index++)
        {
            CuboidFace face =
                detectedFaces[index];

            GameObject faceObject =
                new GameObject(
                    $"CuboidFace_{index + 1}",
                    typeof(Image)
                );

            faceObject.transform.SetParent(
                faceLayer,
                false
            );

            Image image =
                faceObject.GetComponent<Image>();

            image.color = faceColor;
            image.raycastTarget = false;

            RectTransform faceRect =
                faceObject.GetComponent<RectTransform>();

            faceRect.sizeDelta =
                new Vector2(
                    face.gridWidth * CellWidth,
                    face.gridHeight * CellHeight
                );

            Vector2 bottomLeft =
                GridToLocal(face.bottomLeft);

            faceRect.anchoredPosition =
                bottomLeft +
                new Vector2(
                    face.gridWidth *
                    CellWidth *
                    0.5f,

                    face.gridHeight *
                    CellHeight *
                    0.5f
                );

            CreateFaceNumber(
                faceObject.transform,
                index + 1
            );

            faceVisualObjects.Add(faceObject);
        }

        drawLayer.SetAsLastSibling();
    }

    private void CreateFaceNumber(
        Transform parent,
        int number)
    {
        GameObject numberObject =
            new GameObject(
                $"FaceNumber_{number}",
                typeof(Text)
            );

        numberObject.transform.SetParent(
            parent,
            false
        );

        Text numberText =
            numberObject.GetComponent<Text>();

        numberText.text = number.ToString();
        numberText.alignment =
            TextAnchor.MiddleCenter;

        numberText.color = Color.black;
        numberText.fontSize = 26;
        numberText.raycastTarget = false;

        numberText.font =
            faceNumberFont != null
                ? faceNumberFont
                : Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"
                );

        RectTransform rect =
            numberObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void ShowValidationResult(
        bool isValid)
    {
        Color color =
            isValid
                ? faceColor
                : invalidFaceColor;

        foreach (GameObject faceObject
                 in faceVisualObjects)
        {
            if (faceObject == null)
                continue;

            Image image =
                faceObject.GetComponent<Image>();

            if (image != null)
                image.color = color;
        }

        if (drawLayer != null)
            drawLayer.SetAsLastSibling();
    }

    public void ClearDrawing()
    {
        isDrawing = false;

        drawnEdges.Clear();
        edgeHistory.Clear();
        detectedFaces.Clear();

        if (netValidator != null)
            netValidator.ResetValidation();

        ClearLayer(drawLayer);
        ClearFaceVisualObjects();

        if (validateButton != null)
            validateButton.interactable = false;

        if (create3DButton != null)
            create3DButton.interactable = false;

        UpdateUndoButtonState();
        UpdateStatusText();
    }

    private void ClearFaceVisualObjects()
    {
        foreach (GameObject faceObject
                 in faceVisualObjects)
        {
            if (faceObject != null)
                Destroy(faceObject);
        }

        faceVisualObjects.Clear();
    }

    private void UpdateUndoButtonState()
    {
        if (undoButton != null)
        {
            undoButton.interactable =
                edgeHistory.Count > 0;
        }
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        if (edgeHistory.Count == 0)
        {
            statusText.text =
                "Draw 6 connected rectangular faces.";

            return;
        }

        if (detectedFaces.Count < 6)
        {
            statusText.text =
                $"Great! {detectedFaces.Count} of 6 faces completed.";

            return;
        }

        if (detectedFaces.Count == 6)
        {
            statusText.text =
                "All 6 faces are drawn. Tap Validate.";

            return;
        }

        statusText.text =
            $"Too many faces detected: {detectedFaces.Count}.";
    }

    private void CreateLine(
        RectTransform parent,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        string lineName)
    {
        GameObject lineObject =
            new GameObject(
                lineName,
                typeof(Image)
            );

        lineObject.transform.SetParent(
            parent,
            false
        );

        Image image =
            lineObject.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = false;

        RectTransform rect =
            lineObject.GetComponent<RectTransform>();

        Vector2 direction = end - start;

        rect.sizeDelta =
            new Vector2(
                direction.magnitude,
                thickness
            );

        rect.anchoredPosition =
            (start + end) * 0.5f;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    private void ClearLayer(
        RectTransform layer)
    {
        if (layer == null)
            return;

        for (int i = layer.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                layer.GetChild(i).gameObject
            );
        }
    }

    public IReadOnlyList<CuboidFace>
        GetDetectedFaces()
    {
        return detectedFaces;
    }

    public IReadOnlyCollection<CuboidGridEdge>
        GetDrawnEdges()
    {
        return drawnEdges;
    }

    public int GetDetectedFaceCount()
    {
        return detectedFaces.Count;
    }

    // ==================================================
    // PUBLIC GETTERS FOR CORRECTION SYSTEM
    // ==================================================
    public IReadOnlyList<CuboidFace> DetectedFaces
    {
        get
        {
            return detectedFaces;
        }
    }

    public int GetGridWidth()
    {
        return gridWidth;
    }

    public int GetGridHeight()
    {
        return gridHeight;
    }

    public RectTransform GetDrawingArea()
    {
        return drawingArea;
    }

    public void Create3DCuboidFromValidNet()
    {
        if (netValidator == null ||
            !netValidator.CurrentNetIsValid)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Please validate a correct cuboid net first.";
            }

            return;
        }

        if (cuboidModelSpawner == null)
        {
            Debug.LogError(
                "CuboidModelSpawner is not assigned."
            );

            return;
        }

        Vector3Int inferredDimensions =
            netValidator.GetInferredDimensions();

        List<CuboidFace> faceCopy =
            new List<CuboidFace>(detectedFaces);

        cuboidModelSpawner.CreateCuboidFromCanvasNet(
            faceCopy,
            drawingArea,
            gridWidth,
            gridHeight,
            inferredDimensions,
            2.5f
        );

        Transform modelRoot =
            cuboidModelSpawner.GetCurrentModelRoot();

        if (modelRoot == null)
        {
            Debug.LogError(
                "Cuboid model was not created. Transition cancelled."
            );

            if (statusText != null)
            {
                statusText.text =
                    "Could not create the 3D cuboid.";
            }

            return;
        }

        if (statusText != null)
        {
            statusText.text =
                "Your cuboid net is converting into 3D.";
        }

        /*
         * The Canvas uses Screen Space Overlay.
         * It must be hidden so the generated world-space
         * cuboid can be seen by the normal camera.
         */
        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }

        /*
         * The cuboid remains visible using the normal camera
         * while its faces move and rotate for 3.2 seconds.
         * After that, AR mode starts.
         */
        if (arModeManager != null)
        {
            arModeManager.StartARAfterDelay(
                3.5f,
                modelRoot
            );
        }
    }
}
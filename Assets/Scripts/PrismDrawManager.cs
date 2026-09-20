using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrismDrawManager : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button create3DButton;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 12;
    [SerializeField] private int gridHeight = 12;

    [Header("Line Settings")]
    [SerializeField] private float gridLineThickness = 2f;
    [SerializeField] private float drawnLineThickness = 4f;
    [SerializeField] private float previewLineThickness = 4f;

    [Header("Colors")]
    [SerializeField]
    private Color gridColor =
        new Color(0f, 0f, 0f, 0.15f);

    [SerializeField]
    private Color drawnLineColor =
        new Color(0.2f, 0.2f, 0.2f, 1f);

    [SerializeField]
    private Color previewLineColor =
        new Color(0.2f, 0.45f, 0.9f, 0.75f);

    private RectTransform gridLayer;
    private RectTransform drawLayer;
    private RectTransform previewLayer;

    private readonly HashSet<PrismGridEdge> drawnEdges =
        new HashSet<PrismGridEdge>();

    private readonly List<PrismGridEdge> edgeHistory =
        new List<PrismGridEdge>();

    private Vector2Int dragStartPoint;
    private Vector2Int currentDragPoint;

    private bool isDrawing;
    private bool initialized;

    private GameObject previewLineObject;

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

    private void Initialize()
    {
        if (initialized)
            return;

        if (drawingArea == null)
            drawingArea = GetComponent<RectTransform>();

        if (drawingArea == null)
        {
            Debug.LogError(
                "PrismDrawManager: Drawing Area is not assigned."
            );

            return;
        }

        CreateLayers();
        BuildGrid();

        if (undoButton != null)
        {
            undoButton.onClick.RemoveListener(UndoLastAction);
            undoButton.onClick.AddListener(UndoLastAction);
        }

        // Changed to true for testing
        if (validateButton != null)
            validateButton.interactable = true;

        if (create3DButton != null)
            create3DButton.interactable = false;

        initialized = true;

        UpdateUndoButton();
        UpdateStatusText();
    }

    private void CreateLayers()
    {
        gridLayer = CreateLayer("PrismGridLayer");
        drawLayer = CreateLayer("PrismDrawLayer");
        previewLayer = CreateLayer("PrismPreviewLayer");

        gridLayer.SetAsFirstSibling();
        drawLayer.SetAsLastSibling();
        previewLayer.SetAsLastSibling();
    }

    private RectTransform CreateLayer(string layerName)
    {
        Transform existing = drawingArea.Find(layerName);

        if (existing != null)
        {
            RectTransform existingRect =
                existing.GetComponent<RectTransform>();

            if (existingRect != null)
                return existingRect;
        }

        GameObject layerObject = new GameObject(
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
                GridToLocal(new Vector2Int(x, gridHeight)),
                gridLineThickness,
                gridColor,
                "PrismGridVertical"
            );
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            CreateLine(
                gridLayer,
                GridToLocal(new Vector2Int(0, y)),
                GridToLocal(new Vector2Int(gridWidth, y)),
                gridLineThickness,
                gridColor,
                "PrismGridHorizontal"
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanDraw(eventData))
            return;

        isDrawing = true;

        dragStartPoint =
            ScreenToGridPoint(eventData);

        currentDragPoint =
            dragStartPoint;

        ShowPreview(
            dragStartPoint,
            currentDragPoint
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        currentDragPoint =
            ScreenToGridPoint(eventData);

        ShowPreview(
            dragStartPoint,
            currentDragPoint
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        isDrawing = false;

        currentDragPoint =
            ScreenToGridPoint(eventData);

        RemovePreview();

        AddSnappedEdge(
            dragStartPoint,
            currentDragPoint
        );

        UpdateUndoButton();
        UpdateStatusText();
    }

    private bool CanDraw(PointerEventData eventData)
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

        float localX =
            localPoint.x +
            drawingArea.rect.width * 0.5f;

        float localY =
            localPoint.y +
            drawingArea.rect.height * 0.5f;

        int gridX =
            Mathf.RoundToInt(localX / CellWidth);

        int gridY =
            Mathf.RoundToInt(localY / CellHeight);

        gridX = Mathf.Clamp(
            gridX,
            0,
            gridWidth
        );

        gridY = Mathf.Clamp(
            gridY,
            0,
            gridHeight
        );

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

    private void AddSnappedEdge(
        Vector2Int start,
        Vector2Int end)
    {
        if (start == end)
            return;

        if (start.x == end.x)
        {
            AddVerticalPath(start, end);
            return;
        }

        if (start.y == end.y)
        {
            AddHorizontalPath(start, end);
            return;
        }

        // For the prism, an arbitrary diagonal grid edge is allowed.
        AddSingleEdge(start, end);
    }

    private void AddVerticalPath(
        Vector2Int start,
        Vector2Int end)
    {
        int step =
            end.y > start.y ? 1 : -1;

        for (int y = start.y;
             y != end.y;
             y += step)
        {
            AddSingleEdge(
                new Vector2Int(start.x, y),
                new Vector2Int(
                    start.x,
                    y + step
                )
            );
        }
    }

    private void AddHorizontalPath(
        Vector2Int start,
        Vector2Int end)
    {
        int step =
            end.x > start.x ? 1 : -1;

        for (int x = start.x;
             x != end.x;
             x += step)
        {
            AddSingleEdge(
                new Vector2Int(x, start.y),
                new Vector2Int(
                    x + step,
                    start.y
                )
            );
        }
    }

    private void AddSingleEdge(
        Vector2Int pointA,
        Vector2Int pointB)
    {
        PrismGridEdge edge =
            new PrismGridEdge(
                pointA,
                pointB
            );

        if (drawnEdges.Contains(edge))
            return;

        drawnEdges.Add(edge);
        edgeHistory.Add(edge);

        CreateLine(
            drawLayer,
            GridToLocal(edge.PointA),
            GridToLocal(edge.PointB),
            drawnLineThickness,
            drawnLineColor,
            edge.IsDiagonal
                ? "PrismDiagonalEdge"
                : "PrismStraightEdge"
        );

        // Changed to true for testing
        if (validateButton != null)
            validateButton.interactable = true;

        if (create3DButton != null)
            create3DButton.interactable = false;

        // Added code block
        PrismShapeVisualizer visualizer =
            GetComponent<PrismShapeVisualizer>();

        if (visualizer != null)
        {
            visualizer.ClearVisualization();
        }

        TriangularPrismValidator validator =
            GetComponent<TriangularPrismValidator>();

        if (validator != null)
        {
            validator.InvalidateCurrentResult();
        }
    }

    private void ShowPreview(
        Vector2Int start,
        Vector2Int end)
    {
        RemovePreview();

        if (start == end)
            return;

        previewLineObject = CreateLine(
            previewLayer,
            GridToLocal(start),
            GridToLocal(end),
            previewLineThickness,
            previewLineColor,
            "PrismEdgePreview"
        );

        previewLayer.SetAsLastSibling();
    }

    private void RemovePreview()
    {
        if (previewLineObject == null)
            return;

        Destroy(previewLineObject);
        previewLineObject = null;
    }

    public void UndoLastAction()
    {
        if (edgeHistory.Count == 0)
            return;

        PrismGridEdge latestEdge =
            edgeHistory[edgeHistory.Count - 1];

        edgeHistory.RemoveAt(
            edgeHistory.Count - 1
        );

        drawnEdges.Remove(latestEdge);

        RebuildDrawnLines();

        // Changed to true for testing
        if (validateButton != null)
            validateButton.interactable = true;

        if (create3DButton != null)
            create3DButton.interactable = false;

        UpdateUndoButton();
        UpdateStatusText();

        // Added code block
        PrismShapeVisualizer visualizer =
            GetComponent<PrismShapeVisualizer>();

        if (visualizer != null)
        {
            visualizer.ClearVisualization();
        }

        TriangularPrismValidator validator =
            GetComponent<TriangularPrismValidator>();

        if (validator != null)
        {
            validator.InvalidateCurrentResult();
        }
    }

    private void RebuildDrawnLines()
    {
        ClearLayer(drawLayer);

        foreach (PrismGridEdge edge in edgeHistory)
        {
            CreateLine(
                drawLayer,
                GridToLocal(edge.PointA),
                GridToLocal(edge.PointB),
                drawnLineThickness,
                drawnLineColor,
                edge.IsDiagonal
                    ? "PrismDiagonalEdge"
                    : "PrismStraightEdge"
            );
        }
    }

    public void ClearDrawing()
    {
        isDrawing = false;

        drawnEdges.Clear();
        edgeHistory.Clear();

        RemovePreview();
        ClearLayer(drawLayer);

        // Changed to true for testing (just in case you clear and still want to test)
        if (validateButton != null)
            validateButton.interactable = true;

        if (create3DButton != null)
            create3DButton.interactable = false;

        UpdateUndoButton();
        UpdateStatusText();

        // Added code block
        PrismShapeVisualizer visualizer =
            GetComponent<PrismShapeVisualizer>();

        if (visualizer != null)
        {
            visualizer.ClearVisualization();
        }

        TriangularPrismValidator validator =
            GetComponent<TriangularPrismValidator>();

        if (validator != null)
        {
            validator.InvalidateCurrentResult();
        }
    }

    private void UpdateUndoButton()
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
                "Draw 2 matching triangles and 3 connected rectangles.";

            return;
        }

        int diagonalCount = 0;
        int straightCount = 0;

        foreach (PrismGridEdge edge in drawnEdges)
        {
            if (edge.IsDiagonal)
                diagonalCount++;
            else
                straightCount++;
        }

        statusText.text =
            $"Straight edges: {straightCount} | " +
            $"Diagonal edges: {diagonalCount}";
    }

    private GameObject CreateLine(
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

        Image lineImage =
            lineObject.GetComponent<Image>();

        lineImage.color = color;
        lineImage.raycastTarget = false;

        RectTransform lineRect =
            lineObject.GetComponent<RectTransform>();

        Vector2 direction = end - start;

        lineRect.sizeDelta =
            new Vector2(
                direction.magnitude,
                thickness
            );

        lineRect.anchoredPosition =
            (start + end) * 0.5f;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        return lineObject;
    }

    private void ClearLayer(
        RectTransform layer)
    {
        if (layer == null)
            return;

        for (int index = layer.childCount - 1;
             index >= 0;
             index--)
        {
            Destroy(
                layer.GetChild(index).gameObject
            );
        }
    }

    public IReadOnlyCollection<PrismGridEdge>
        GetDrawnEdges()
    {
        return drawnEdges;
    }

    public int GetEdgeCount()
    {
        return drawnEdges.Count;
    }
}
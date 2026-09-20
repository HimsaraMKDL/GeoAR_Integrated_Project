using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrismGuidedTriangleDrawer : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField]
    private RectTransform drawingArea;

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Button undoButton;

    [SerializeField]
    private Button clearButton;

    [SerializeField]
    private GameObject depthPanel;

    [Header("Grid Settings")]
    [SerializeField]
    private int gridWidth = 12;

    [SerializeField]
    private int gridHeight = 12;

    [Header("Line Settings")]
    [SerializeField]
    private float gridLineThickness = 2f;

    [SerializeField]
    private float drawnLineThickness = 4f;

    [Header("Colors")]
    [SerializeField]
    private Color gridColor =
        new Color(0f, 0f, 0f, 0.14f);

    [SerializeField]
    private Color drawnLineColor =
        new Color(0.15f, 0.15f, 0.15f, 1f);

    private RectTransform gridLayer;
    private RectTransform drawLayer;

    private readonly HashSet<PrismGuidedGridEdge>
        drawnEdges =
            new HashSet<PrismGuidedGridEdge>();

    private readonly List<PrismGuidedGridEdge>
        edgeHistory =
            new List<PrismGuidedGridEdge>();

    private Vector2Int strokeStartPoint;
    private bool isDrawing;
    private bool initialized;
    private bool drawingLocked;

    private float CellWidth =>
        drawingArea != null && gridWidth > 0
            ? drawingArea.rect.width / gridWidth
            : 0f;

    private float CellHeight =>
        drawingArea != null && gridHeight > 0
            ? drawingArea.rect.height / gridHeight
            : 0f;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public bool DrawingLocked => drawingLocked;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        if (drawingArea == null)
            drawingArea =
                GetComponent<RectTransform>();

        if (drawingArea == null)
        {
            Debug.LogError(
                "PrismGuidedTriangleDrawer: " +
                "Drawing Area is not assigned."
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

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(
                ClearDrawing
            );

            clearButton.onClick.AddListener(
                ClearDrawing
            );
        }

        if (depthPanel != null)
            depthPanel.SetActive(false);

        initialized = true;
        drawingLocked = false;

        UpdateButtons();

        if (statusText != null)
        {
            statusText.text =
                "Draw one closed triangle on the grid.";
        }
    }

    private void CreateLayers()
    {
        gridLayer =
            CreateLayer("GuidedPrismGridLayer");

        drawLayer =
            CreateLayer("GuidedPrismDrawLayer");

        gridLayer.SetAsFirstSibling();
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

        RectTransform layerRect =
            layerObject.GetComponent<RectTransform>();

        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layerRect.localScale = Vector3.one;

        return layerRect;
    }

    private void BuildGrid()
    {
        ClearLayer(gridLayer);

        for (int x = 0;
             x <= gridWidth;
             x++)
        {
            CreateLine(
                gridLayer,
                GridToLocal(
                    new Vector2Int(x, 0)
                ),
                GridToLocal(
                    new Vector2Int(
                        x,
                        gridHeight
                    )
                ),
                gridLineThickness,
                gridColor,
                "GuidedGridVertical"
            );
        }

        for (int y = 0;
             y <= gridHeight;
             y++)
        {
            CreateLine(
                gridLayer,
                GridToLocal(
                    new Vector2Int(0, y)
                ),
                GridToLocal(
                    new Vector2Int(
                        gridWidth,
                        y
                    )
                ),
                gridLineThickness,
                gridColor,
                "GuidedGridHorizontal"
            );
        }
    }

    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (!initialized ||
            drawingLocked ||
            !IsInsideDrawingArea(eventData))
        {
            return;
        }

        isDrawing = true;

        strokeStartPoint =
            ScreenToGridPoint(eventData);
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        /*
         * Guided mode keeps one edge per stroke.
         * The child can see the finger movement,
         * but the final snapped edge is added
         * when the finger is released.
         */
    }

    public void OnPointerUp(
        PointerEventData eventData)
    {
        if (!isDrawing ||
            drawingLocked)
        {
            return;
        }

        isDrawing = false;

        Vector2Int endPoint =
            ScreenToGridPoint(eventData);

        AddEdge(
            strokeStartPoint,
            endPoint
        );
    }

    private bool IsInsideDrawingArea(
        PointerEventData eventData)
    {
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

        float adjustedX =
            localPoint.x +
            drawingArea.rect.width * 0.5f;

        float adjustedY =
            localPoint.y +
            drawingArea.rect.height * 0.5f;

        int gridX =
            Mathf.RoundToInt(
                adjustedX / CellWidth
            );

        int gridY =
            Mathf.RoundToInt(
                adjustedY / CellHeight
            );

        gridX =
            Mathf.Clamp(
                gridX,
                0,
                gridWidth
            );

        gridY =
            Mathf.Clamp(
                gridY,
                0,
                gridHeight
            );

        return new Vector2Int(
            gridX,
            gridY
        );
    }

    private Vector2 GridToLocal(
        Vector2Int gridPoint)
    {
        float localX =
            gridPoint.x * CellWidth -
            drawingArea.rect.width * 0.5f;

        float localY =
            gridPoint.y * CellHeight -
            drawingArea.rect.height * 0.5f;

        return new Vector2(
            localX,
            localY
        );
    }

    private void AddEdge(
        Vector2Int startPoint,
        Vector2Int endPoint)
    {
        if (startPoint == endPoint)
            return;

        PrismGuidedGridEdge edge =
            new PrismGuidedGridEdge(
                startPoint,
                endPoint
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
                ? "GuidedTriangleDiagonal"
                : "GuidedTriangleStraight"
        );

        UpdateButtons();

        PrismGuidedTriangleDetector detector =
            GetComponent<
                PrismGuidedTriangleDetector>();

        if (detector != null)
            detector.CheckForTriangle();
    }

    public void UndoLastEdge()
    {
        if (drawingLocked ||
            edgeHistory.Count == 0)
        {
            return;
        }

        int lastIndex =
            edgeHistory.Count - 1;

        PrismGuidedGridEdge edge =
            edgeHistory[lastIndex];

        edgeHistory.RemoveAt(lastIndex);
        drawnEdges.Remove(edge);

        RebuildDrawnLines();
        UpdateButtons();

        if (statusText != null)
        {
            statusText.text =
                "Continue drawing one closed triangle.";
        }
    }

    private void RebuildDrawnLines()
    {
        ClearLayer(drawLayer);

        foreach (PrismGuidedGridEdge edge
                 in edgeHistory)
        {
            CreateLine(
                drawLayer,
                GridToLocal(edge.PointA),
                GridToLocal(edge.PointB),
                drawnLineThickness,
                drawnLineColor,
                edge.IsDiagonal
                    ? "GuidedTriangleDiagonal"
                    : "GuidedTriangleStraight"
            );
        }
    }

    public void ClearDrawing()
    {
        isDrawing = false;
        drawingLocked = false;

        drawnEdges.Clear();
        edgeHistory.Clear();

        ClearLayer(drawLayer);

        PrismGuidedTriangleDetector detector =
            GetComponent<
                PrismGuidedTriangleDetector>();

        if (detector != null)
            detector.ResetDetectedTriangle();

        if (depthPanel != null)
            depthPanel.SetActive(false);

        UpdateButtons();

        if (statusText != null)
        {
            statusText.text =
                "Draw one closed triangle on the grid.";
        }
    }

    public void LockDrawing()
    {
        drawingLocked = true;
        isDrawing = false;

        UpdateButtons();
    }

    public void UnlockDrawing()
    {
        drawingLocked = false;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (undoButton != null)
        {
            undoButton.interactable =
                !drawingLocked &&
                edgeHistory.Count > 0;
        }

        if (clearButton != null)
            clearButton.interactable = true;
    }

    public void ShowDepthSelection()
    {
        if (depthPanel != null)
            depthPanel.SetActive(true);
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

        Vector2 direction =
            end - start;

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

        for (int index =
                 layer.childCount - 1;
             index >= 0;
             index--)
        {
            GameObject child =
                layer.GetChild(index).gameObject;

            child.SetActive(false);
            Destroy(child);
        }
    }

    public IReadOnlyCollection<
        PrismGuidedGridEdge>
        GetDrawnEdges()
    {
        return drawnEdges;
    }

    public RectTransform GetDrawingArea()
    {
        return drawingArea;
    }

    public Vector2 GridPointToLocal(
        Vector2Int gridPoint)
    {
        return GridToLocal(gridPoint);
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TetrahedronGridDrawManager :
    MonoBehaviour,
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
    private Button validateButton;

    [SerializeField]
    private Button create3DButton;

    [SerializeField]
    private Button clearButton;


    // ==================================================
    // GRID SETTINGS
    // ==================================================

    [Header("Triangular Grid Settings")]

    [SerializeField]
    private int columns = 8;

    [SerializeField]
    private int rows = 8;

    [SerializeField]
    private float triangleSide = 70f;


    // ==================================================
    // LINE SETTINGS
    // ==================================================

    [Header("Line Settings")]

    [SerializeField]
    private float gridLineThickness = 2f;

    [SerializeField]
    private float drawnLineThickness = 5f;

    [SerializeField]
    private float previewLineThickness = 4f;


    // ==================================================
    // COLORS
    // ==================================================

    [Header("Colors")]

    [SerializeField]
    private Color gridColor =
        new Color(
            0f,
            0f,
            0f,
            0.16f
        );

    [SerializeField]
    private Color drawnLineColor =
        new Color(
            0.10f,
            0.20f,
            0.45f,
            1f
        );

    [SerializeField]
    private Color previewLineColor =
        new Color(
            0.25f,
            0.55f,
            1f,
            0.75f
        );


    // ==================================================
    // RUNTIME
    // ==================================================

    private RectTransform gridLayer;
    private RectTransform drawLayer;
    private RectTransform previewLayer;

    private readonly HashSet<TetrahedronGridEdge>
        drawnEdges =
        new HashSet<TetrahedronGridEdge>();

    private readonly List<TetrahedronGridEdge>
        edgeHistory =
        new List<TetrahedronGridEdge>();

    private Vector2Int dragStartPoint;
    private Vector2Int currentDragPoint;

    private bool isDrawing = false;
    private bool initialized = false;

    private GameObject previewLineObject;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        Initialize();
    }


    private void Initialize()
    {
        if (initialized)
            return;

        if (drawingArea == null)
        {
            drawingArea =
                GetComponent<RectTransform>();
        }

        if (drawingArea == null)
        {
            Debug.LogError(
                "TetrahedronGridDrawManager: " +
                "Drawing Area is not assigned."
            );

            return;
        }

        CreateLayers();

        BuildTriangularGrid();

        if (undoButton != null)
        {
            undoButton.onClick.RemoveListener(
                UndoLastAction
            );

            undoButton.onClick.AddListener(
                UndoLastAction
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

        if (validateButton != null)
        {
            validateButton.interactable = false;
        }

        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }

        UpdateUndoButton();

        UpdateStatusText();

        initialized = true;
    }


    // ==================================================
    // CREATE LAYERS
    // ==================================================

    private void CreateLayers()
    {
        gridLayer =
            CreateLayer(
                "TetrahedronGridLayer"
            );

        drawLayer =
            CreateLayer(
                "TetrahedronDrawLayer"
            );

        previewLayer =
            CreateLayer(
                "TetrahedronPreviewLayer"
            );

        gridLayer.SetAsFirstSibling();

        drawLayer.SetAsLastSibling();

        previewLayer.SetAsLastSibling();
    }


    private RectTransform CreateLayer(
        string layerName)
    {
        Transform existing =
            drawingArea.Find(
                layerName
            );

        if (existing != null)
        {
            RectTransform existingRect =
                existing.GetComponent<
                    RectTransform>();

            if (existingRect != null)
            {
                return existingRect;
            }
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
            layerObject.GetComponent<
                RectTransform>();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        rect.localScale =
            Vector3.one;

        return rect;
    }


    // ==================================================
    // TRIANGULAR GRID
    // ==================================================

    private void BuildTriangularGrid()
    {
        ClearLayer(
            gridLayer
        );

        float triangleHeight =
            triangleSide *
            Mathf.Sqrt(3f) *
            0.5f;

        List<Vector2Int> validPoints =
            new List<Vector2Int>();

        for (int row = 0;
             row <= rows;
             row++)
        {
            for (int column = 0;
                 column <= columns;
                 column++)
            {
                validPoints.Add(
                    new Vector2Int(
                        column,
                        row
                    )
                );
            }
        }

        // ----------------------------------------------
        // HORIZONTAL GRID LINES
        // ----------------------------------------------

        for (int row = 0;
             row <= rows;
             row++)
        {
            for (int column = 0;
                 column < columns;
                 column++)
            {
                CreateLine(
                    gridLayer,
                    GridPointToLocal(
                        new Vector2Int(
                            column,
                            row
                        )
                    ),
                    GridPointToLocal(
                        new Vector2Int(
                            column + 1,
                            row
                        )
                    ),
                    gridLineThickness,
                    gridColor,
                    "TetraGridHorizontal"
                );
            }
        }


        // ----------------------------------------------
        // DIAGONAL CONNECTIONS
        // ----------------------------------------------

        for (int row = 0;
             row < rows;
             row++)
        {
            for (int column = 0;
                 column <= columns;
                 column++)
            {
                Vector2Int lower =
                    new Vector2Int(
                        column,
                        row
                    );

                if (row % 2 == 0)
                {
                    if (column > 0)
                    {
                        CreateLine(
                            gridLayer,
                            GridPointToLocal(
                                lower
                            ),
                            GridPointToLocal(
                                new Vector2Int(
                                    column - 1,
                                    row + 1
                                )
                            ),
                            gridLineThickness,
                            gridColor,
                            "TetraGridDiagonal"
                        );
                    }

                    CreateLine(
                        gridLayer,
                        GridPointToLocal(
                            lower
                        ),
                        GridPointToLocal(
                            new Vector2Int(
                                column,
                                row + 1
                            )
                        ),
                        gridLineThickness,
                        gridColor,
                        "TetraGridDiagonal"
                    );
                }
                else
                {
                    CreateLine(
                        gridLayer,
                        GridPointToLocal(
                            lower
                        ),
                        GridPointToLocal(
                            new Vector2Int(
                                column,
                                row + 1
                            )
                        ),
                        gridLineThickness,
                        gridColor,
                        "TetraGridDiagonal"
                    );

                    if (column < columns)
                    {
                        CreateLine(
                            gridLayer,
                            GridPointToLocal(
                                lower
                            ),
                            GridPointToLocal(
                                new Vector2Int(
                                    column + 1,
                                    row + 1
                                )
                            ),
                            gridLineThickness,
                            gridColor,
                            "TetraGridDiagonal"
                        );
                    }
                }
            }
        }

        Debug.Log(
            "Triangular grid created."
        );
    }


    // ==================================================
    // POINTER INPUT
    // ==================================================

    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (!initialized)
            return;

        if (!IsInsideDrawingArea(
                eventData))
        {
            return;
        }

        isDrawing =
            true;

        dragStartPoint =
            ScreenToNearestGridPoint(
                eventData
            );

        currentDragPoint =
            dragStartPoint;

        ShowPreview(
            dragStartPoint,
            currentDragPoint
        );
    }


    public void OnDrag(
        PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        currentDragPoint =
            ScreenToNearestGridPoint(
                eventData
            );

        ShowPreview(
            dragStartPoint,
            currentDragPoint
        );
    }


    public void OnPointerUp(
        PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        isDrawing =
            false;

        currentDragPoint =
            ScreenToNearestGridPoint(
                eventData
            );

        RemovePreview();

        AddSnappedEdge(
            dragStartPoint,
            currentDragPoint
        );

        UpdateUndoButton();

        UpdateStatusText();
    }


    // ==================================================
    // SCREEN -> GRID
    // ==================================================

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


    private Vector2Int
        ScreenToNearestGridPoint(
        PointerEventData eventData)
    {
        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                drawingArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

        float bestDistance =
            float.MaxValue;

        Vector2Int bestPoint =
            Vector2Int.zero;

        for (int row = 0;
             row <= rows;
             row++)
        {
            for (int column = 0;
                 column <= columns;
                 column++)
            {
                Vector2Int point =
                    new Vector2Int(
                        column,
                        row
                    );

                Vector2 gridLocal =
                    GridPointToLocal(
                        point
                    );

                float distance =
                    Vector2.SqrMagnitude(
                        localPoint -
                        gridLocal
                    );

                if (distance <
                    bestDistance)
                {
                    bestDistance =
                        distance;

                    bestPoint =
                        point;
                }
            }
        }

        return bestPoint;
    }


    // ==================================================
    // GRID POINT TO LOCAL
    // ==================================================

    public Vector2 GridPointToLocal(
        Vector2Int gridPoint)
    {
        float triangleHeight =
            triangleSide *
            Mathf.Sqrt(3f) *
            0.5f;

        float totalWidth =
            columns *
            triangleSide +
            triangleSide * 0.5f;

        float totalHeight =
            rows *
            triangleHeight;

        float startX =
            -totalWidth *
            0.5f;

        float startY =
            -totalHeight *
            0.5f;


        float rowOffset =
            gridPoint.y % 2 == 0
                ? 0f
                : triangleSide * 0.5f;


        float localX =
            startX +
            rowOffset +
            gridPoint.x *
            triangleSide;


        float localY =
            startY +
            gridPoint.y *
            triangleHeight;


        return new Vector2(
            localX,
            localY
        );
    }


    // ==================================================
    // DRAW EDGE
    // ==================================================

    private void AddSnappedEdge(
        Vector2Int start,
        Vector2Int end)
    {
        if (start == end)
            return;


        /*
         * Only allow actual neighbouring
         * triangular-grid points.
         */

        if (!AreNeighbourGridPoints(
                start,
                end))
        {
            if (statusText != null)
            {
                statusText.text =
                    "Draw along one triangle edge at a time.";
            }

            return;
        }


        TetrahedronGridEdge edge =
            new TetrahedronGridEdge(
                start,
                end
            );


        if (drawnEdges.Contains(
                edge))
        {
            return;
        }


        // ==================================================
        // START RESEARCH ACTIVITY / ATTEMPT
        // ==================================================

        /*
         * Start the Tetrahedron activity only
         * when the first real edge is drawn.
         *
         * This creates Attempt 1.
         *
         * Further edges in the same drawing
         * do NOT restart the attempt.
         */

        if (SessionDataLogger.Instance != null)
        {
            if (edgeHistory.Count == 0)
            {
                SessionDataLogger.Instance
                    .StartActivitySession(
                        "Tetrahedron"
                    );

                SessionDataLogger.Instance
                    .StartAttempt();

                Debug.Log(
                    "Tetrahedron research session started. " +
                    "Attempt 1."
                );
            }
        }


        drawnEdges.Add(
            edge
        );


        edgeHistory.Add(
            edge
        );


        CreateLine(
            drawLayer,
            GridPointToLocal(
                edge.PointA
            ),
            GridPointToLocal(
                edge.PointB
            ),
            drawnLineThickness,
            drawnLineColor,
            "TetrahedronDrawnEdge"
        );


        if (validateButton != null)
        {
            validateButton.interactable =
                drawnEdges.Count >= 3;
        }


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }


        // ----------------------------------------------
        // TRIANGLE DETECTOR
        // ----------------------------------------------

        TetrahedronTriangleDetector detector =
            GetComponent<
                TetrahedronTriangleDetector>();

        if (detector != null)
        {
            detector.DetectTriangles();
        }


        // ----------------------------------------------
        // VALIDATOR RESET
        // ----------------------------------------------

        TetrahedronNetValidator validator =
            GetComponent<
                TetrahedronNetValidator>();

        if (validator != null)
        {
            validator.InvalidateCurrentNet();
        }
    }


    // ==================================================
    // NEIGHBOUR CHECK
    // ==================================================

    private bool AreNeighbourGridPoints(
        Vector2Int first,
        Vector2Int second)
    {
        Vector2 firstLocal =
            GridPointToLocal(
                first
            );

        Vector2 secondLocal =
            GridPointToLocal(
                second
            );

        float distance =
            Vector2.Distance(
                firstLocal,
                secondLocal
            );


        return Mathf.Abs(
                   distance -
                   triangleSide
               )
               <= 1f;
    }


    // ==================================================
    // PREVIEW
    // ==================================================

    private void ShowPreview(
        Vector2Int start,
        Vector2Int end)
    {
        RemovePreview();

        if (start == end)
            return;


        previewLineObject =
            CreateLine(
                previewLayer,
                GridPointToLocal(start),
                GridPointToLocal(end),
                previewLineThickness,
                previewLineColor,
                "TetrahedronEdgePreview"
            );


        previewLayer.SetAsLastSibling();
    }


    private void RemovePreview()
    {
        if (previewLineObject == null)
            return;


        Destroy(
            previewLineObject
        );


        previewLineObject =
            null;
    }


    // ==================================================
    // UNDO
    // ==================================================

    public void UndoLastAction()
    {
        if (edgeHistory.Count == 0)
            return;


        int lastIndex =
            edgeHistory.Count - 1;


        TetrahedronGridEdge latestEdge =
            edgeHistory[
                lastIndex
            ];


        edgeHistory.RemoveAt(
            lastIndex
        );


        drawnEdges.Remove(
            latestEdge
        );


        RebuildDrawnLines();


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }


        UpdateUndoButton();

        UpdateStatusText();


        // ----------------------------------------------
        // TRIANGLE DETECTOR
        // ----------------------------------------------

        TetrahedronTriangleDetector detector =
            GetComponent<
                TetrahedronTriangleDetector>();

        if (detector != null)
        {
            detector.DetectTriangles();
        }


        // ----------------------------------------------
        // VALIDATOR RESET
        // ----------------------------------------------

        TetrahedronNetValidator validator =
            GetComponent<
                TetrahedronNetValidator>();

        if (validator != null)
        {
            validator.InvalidateCurrentNet();
        }
    }


    // ==================================================
    // CLEAR
    // ==================================================

    public void ClearDrawing()
    {
        isDrawing =
            false;


        drawnEdges.Clear();

        edgeHistory.Clear();


        RemovePreview();

        ClearLayer(
            drawLayer
        );


        if (validateButton != null)
        {
            validateButton.interactable =
                false;
        }


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }


        UpdateUndoButton();

        UpdateStatusText();


        // ----------------------------------------------
        // TRIANGLE DETECTOR
        // ----------------------------------------------

        TetrahedronTriangleDetector detector =
            GetComponent<
                TetrahedronTriangleDetector>();

        if (detector != null)
        {
            detector.ClearDetectedTriangles();
        }


        // ----------------------------------------------
        // VALIDATOR RESET
        // ----------------------------------------------

        TetrahedronNetValidator validator =
            GetComponent<
                TetrahedronNetValidator>();

        if (validator != null)
        {
            validator.InvalidateCurrentNet();
        }
    }


    // ==================================================
    // REBUILD
    // ==================================================

    private void RebuildDrawnLines()
    {
        ClearLayer(
            drawLayer
        );


        foreach (TetrahedronGridEdge edge
                 in edgeHistory)
        {
            CreateLine(
                drawLayer,
                GridPointToLocal(
                    edge.PointA
                ),
                GridPointToLocal(
                    edge.PointB
                ),
                drawnLineThickness,
                drawnLineColor,
                "TetrahedronDrawnEdge"
            );
        }
    }


    // ==================================================
    // UI
    // ==================================================

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
                "Draw a tetrahedron net using 4 connected triangles.";

            return;
        }


        statusText.text =
            $"Edges drawn: {drawnEdges.Count}";
    }


    // ==================================================
    // CREATE LINE
    // ==================================================

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
            lineObject.GetComponent<
                Image>();


        lineImage.color =
            color;


        lineImage.raycastTarget =
            false;


        RectTransform lineRect =
            lineObject.GetComponent<
                RectTransform>();


        Vector2 direction =
            end - start;


        lineRect.sizeDelta =
            new Vector2(
                direction.magnitude,
                thickness
            );


        lineRect.anchoredPosition =
            (start + end) *
            0.5f;


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;


        lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        return lineObject;
    }


    // ==================================================
    // CLEAR LAYER
    // ==================================================

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
            Destroy(
                layer.GetChild(
                    index
                ).gameObject
            );
        }
    }


    // ==================================================
    // PUBLIC DATA
    // ==================================================

    public IReadOnlyCollection<
        TetrahedronGridEdge>
        GetDrawnEdges()
    {
        return drawnEdges;
    }


    public RectTransform
        GetDrawingArea()
    {
        return drawingArea;
    }


    public float
        GetTriangleSide()
    {
        return triangleSide;
    }
}
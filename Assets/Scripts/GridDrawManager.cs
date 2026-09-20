using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridDrawManager : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform drawingArea;
    public Text statusText;
    public GameObject drawingCanvas;

    [Header("3D Model")]
    public CubeModelSpawner cubeModelSpawner;
    public Button create3DButton;

    [Header("AR Mode")]
    public ARModeManager arModeManager;

    [Header("Buttons")]
    public Button undoButton;
    public Button validateButton;
    public Button clearButton;

    [Header("Grid Settings")]
    public int gridWidth = 12;
    public int gridHeight = 12;

    [Header("Line Settings")]
    public float gridLineThickness = 3f;
    public float drawnLineThickness = 3f;

    [Header("Colors")]
    public Color gridColor = new Color(0f, 0f, 0f, 0.18f);
    public Color drawnLineColor = new Color(0.72f, 0.72f, 0.72f, 1f);

    [Header("Face Numbering")]
    public Font numberFont;
    public Color numberColor = Color.black;

    [Header("Validation Colors")]
    public Color validFaceColor = new Color(1f, 1f, 1f, 1f);
    public Color invalidFaceColor = new Color(1f, 0.35f, 0.35f, 0.65f);

    [Header("2D Net Correction")]
    public InvalidNetPopupManager invalidPopupManager;

    [Header("Invalid Feedback Delay")]
    [SerializeField] private float invalidPopupDelay = 5f;
    private Coroutine invalidPopupRoutine;

    private RectTransform gridLayer;
    private RectTransform faceLayer;
    private RectTransform drawLayer;

    private Vector2Int lastGridPoint;
    private bool isDrawing = false;
    private bool currentNetIsValid = false;

    private readonly HashSet<GridEdge> drawnEdges = new HashSet<GridEdge>();
    private readonly List<GridEdge> edgeHistory = new List<GridEdge>();
    private readonly HashSet<Vector2Int> detectedFaces = new HashSet<Vector2Int>();
    private readonly List<GameObject> faceObjects = new List<GameObject>();

    private float CellWidth => drawingArea.rect.width / gridWidth;
    private float CellHeight => drawingArea.rect.height / gridHeight;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (drawingArea == null)
            drawingArea = GetComponent<RectTransform>();

        CreateLayers();
        BuildGrid();

        if (statusText != null)
            statusText.text = "Draw cube net edges on the grid.";

        if (create3DButton != null)
            create3DButton.interactable = false;

        if (validateButton != null)
            validateButton.interactable = false;

        if (clearButton != null)
        {
            clearButton.interactable = false;

            clearButton.onClick.RemoveListener(ClearDrawing);
            clearButton.onClick.AddListener(ClearDrawing);
        }

        if (undoButton != null)
        {
            undoButton.interactable = false;

            undoButton.onClick.RemoveListener(UndoLastEdge);
            undoButton.onClick.AddListener(UndoLastEdge);
        }
    }


    // =========================================================
    // CREATE LAYERS
    // =========================================================

    private void CreateLayers()
    {
        gridLayer = CreateLayer("GridLayer");
        faceLayer = CreateLayer("FaceLayer");
        drawLayer = CreateLayer("DrawLayer");
    }


    private RectTransform CreateLayer(string layerName)
    {
        GameObject layerObj =
            new GameObject(
                layerName,
                typeof(RectTransform)
            );

        layerObj.transform.SetParent(
            drawingArea,
            false
        );

        RectTransform rt =
            layerObj.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return rt;
    }


    // =========================================================
    // BUILD GRID
    // =========================================================

    private void BuildGrid()
    {
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector2 start =
                GridToLocal(
                    new Vector2Int(x, 0)
                );

            Vector2 end =
                GridToLocal(
                    new Vector2Int(x, gridHeight)
                );

            CreateLine(
                gridLayer,
                start,
                end,
                gridLineThickness,
                gridColor,
                "GridVertical"
            );
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector2 start =
                GridToLocal(
                    new Vector2Int(0, y)
                );

            Vector2 actualEnd =
                GridToLocal(
                    new Vector2Int(gridWidth, y)
                );

            CreateLine(
                gridLayer,
                start,
                actualEnd,
                gridLineThickness,
                gridColor,
                "GridHorizontal"
            );
        }
    }


    // =========================================================
    // POINTER DOWN
    // =========================================================

    public void OnPointerDown(
        PointerEventData eventData)
    {
        isDrawing = true;

        lastGridPoint =
            ScreenToGridPoint(eventData);
    }


    // =========================================================
    // DRAG
    // =========================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        Vector2Int currentGridPoint =
            ScreenToGridPoint(eventData);

        if (currentGridPoint != lastGridPoint)
        {
            AddSnappedPath(
                lastGridPoint,
                currentGridPoint
            );

            lastGridPoint =
                currentGridPoint;
        }
    }


    // =========================================================
    // POINTER UP
    // =========================================================

    public void OnPointerUp(
        PointerEventData eventData)
    {
        isDrawing = false;

        DetectFaces();

        currentNetIsValid = false;

        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }

        if (validateButton != null)
        {
            validateButton.interactable =
                detectedFaces.Count == 6;
        }

        UpdateUndoButtonState();

        if (statusText != null)
        {
            if (detectedFaces.Count == 6)
            {
                statusText.text =
                    "6 faces completed. Tap Validate to check your cube net.";
            }
            else
            {
                statusText.text =
                    $"Detected faces: {detectedFaces.Count} / 6";
            }
        }
    }


    // =========================================================
    // SCREEN TO GRID
    // =========================================================

    private Vector2Int ScreenToGridPoint(
        PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        float x =
            localPoint.x +
            drawingArea.rect.width / 2f;

        float y =
            localPoint.y +
            drawingArea.rect.height / 2f;

        int gx =
            Mathf.RoundToInt(
                x / CellWidth
            );

        int gy =
            Mathf.RoundToInt(
                y / CellHeight
            );

        gx =
            Mathf.Clamp(
                gx,
                0,
                gridWidth
            );

        gy =
            Mathf.Clamp(
                gy,
                0,
                gridHeight
            );

        return new Vector2Int(
            gx,
            gy
        );
    }


    // =========================================================
    // GRID TO LOCAL
    // =========================================================

    private Vector2 GridToLocal(
        Vector2Int gridPoint)
    {
        float x =
            gridPoint.x * CellWidth -
            drawingArea.rect.width / 2f;

        float y =
            gridPoint.y * CellHeight -
            drawingArea.rect.height / 2f;

        return new Vector2(
            x,
            y
        );
    }


    // =========================================================
    // ADD SNAPPED PATH
    // =========================================================

    private void AddSnappedPath(
        Vector2Int from,
        Vector2Int to)
    {
        if (from == to)
            return;

        if (from.x == to.x)
        {
            int step =
                to.y > from.y
                    ? 1
                    : -1;

            for (
                int y = from.y;
                y != to.y;
                y += step)
            {
                AddEdge(
                    new Vector2Int(
                        from.x,
                        y
                    ),
                    new Vector2Int(
                        from.x,
                        y + step
                    )
                );
            }
        }
        else if (from.y == to.y)
        {
            int step =
                to.x > from.x
                    ? 1
                    : -1;

            for (
                int x = from.x;
                x != to.x;
                x += step)
            {
                AddEdge(
                    new Vector2Int(
                        x,
                        from.y
                    ),
                    new Vector2Int(
                        x + step,
                        from.y
                    )
                );
            }
        }
    }


    // =========================================================
    // ADD EDGE
    // =========================================================

    private void AddEdge(
        Vector2Int a,
        Vector2Int b)
    {
        if (invalidPopupRoutine != null)
        {
            StopCoroutine(
                invalidPopupRoutine
            );

            invalidPopupRoutine = null;
        }

        GridEdge edge =
            new GridEdge(
                a,
                b
            );

        if (drawnEdges.Contains(edge))
            return;


        // =====================================================
        // START RESEARCH ATTEMPT
        // =====================================================

        if (SessionDataLogger.Instance != null)
        {
            /*
             * Start the Cube activity automatically
             * when the first drawing begins.
             *
             * StartAttempt() is only called when
             * there is currently no active attempt.
             */

            if (edgeHistory.Count == 0)
            {
                SessionDataLogger.Instance
                    .StartActivitySession("Cube");

                SessionDataLogger.Instance
                    .StartAttempt();
            }
        }


        drawnEdges.Add(edge);

        edgeHistory.Add(edge);


        if (clearButton != null)
        {
            clearButton.interactable = true;
        }


        Vector2 localA =
            GridToLocal(a);

        Vector2 localB =
            GridToLocal(b);


        CreateLine(
            drawLayer,
            localA,
            localB,
            drawnLineThickness,
            drawnLineColor,
            "DrawnEdge"
        );


        currentNetIsValid = false;


        if (create3DButton != null)
            create3DButton.interactable = false;


        DetectFaces();


        if (validateButton != null)
        {
            validateButton.interactable =
                detectedFaces.Count == 6;
        }


        UpdateUndoButtonState();


        if (statusText != null)
        {
            if (detectedFaces.Count >= 6)
            {
                statusText.text =
                    "6 faces completed. Please validate your cube net.";
            }
            else
            {
                statusText.text =
                    "Detected faces: " +
                    detectedFaces.Count +
                    " / 6";
            }
        }
    }


    // =========================================================
    // UNDO BUTTON
    // =========================================================

    private void UpdateUndoButtonState()
    {
        if (undoButton == null)
            return;

        undoButton.interactable =
            edgeHistory.Count > 0 &&
            detectedFaces.Count < 6;
    }


    public void UndoLastEdge()
    {
        if (edgeHistory.Count == 0)
            return;

        GridEdge lastEdge =
            edgeHistory[
                edgeHistory.Count - 1
            ];

        edgeHistory.RemoveAt(
            edgeHistory.Count - 1
        );

        drawnEdges.Remove(
            lastEdge
        );


        RebuildDrawnLines();

        currentNetIsValid = false;


        if (create3DButton != null)
            create3DButton.interactable = false;


        DetectFaces();


        if (validateButton != null)
        {
            validateButton.interactable =
                detectedFaces.Count == 6;
        }


        UpdateUndoButtonState();


        if (clearButton != null)
        {
            clearButton.interactable =
                edgeHistory.Count > 0;
        }


        if (statusText != null)
        {
            statusText.text =
                "Undo complete. Detected faces: " +
                detectedFaces.Count +
                " / 6";
        }
    }


    // =========================================================
    // REBUILD DRAWN LINES
    // =========================================================

    private void RebuildDrawnLines()
    {
        foreach (Transform child
                 in drawLayer)
        {
            Destroy(
                child.gameObject
            );
        }


        foreach (GridEdge edge
                 in drawnEdges)
        {
            Vector2 localA =
                GridToLocal(edge.a);

            Vector2 localB =
                GridToLocal(edge.b);


            CreateLine(
                drawLayer,
                localA,
                localB,
                drawnLineThickness,
                drawnLineColor,
                "DrawnEdge"
            );
        }
    }


    // =========================================================
    // DETECT FACES
    // =========================================================

    private void DetectFaces()
    {
        detectedFaces.Clear();


        for (
            int x = 0;
            x < gridWidth;
            x++)
        {
            for (
                int y = 0;
                y < gridHeight;
                y++)
            {
                Vector2Int bottomLeft =
                    new Vector2Int(
                        x,
                        y
                    );

                Vector2Int bottomRight =
                    new Vector2Int(
                        x + 1,
                        y
                    );

                Vector2Int topLeft =
                    new Vector2Int(
                        x,
                        y + 1
                    );

                Vector2Int topRight =
                    new Vector2Int(
                        x + 1,
                        y + 1
                    );


                bool bottom =
                    drawnEdges.Contains(
                        new GridEdge(
                            bottomLeft,
                            bottomRight
                        )
                    );

                bool top =
                    drawnEdges.Contains(
                        new GridEdge(
                            topLeft,
                            topRight
                        )
                    );

                bool left =
                    drawnEdges.Contains(
                        new GridEdge(
                            bottomLeft,
                            topLeft
                        )
                    );

                bool right =
                    drawnEdges.Contains(
                        new GridEdge(
                            bottomRight,
                            topRight
                        )
                    );


                if (
                    bottom &&
                    top &&
                    left &&
                    right)
                {
                    detectedFaces.Add(
                        new Vector2Int(
                            x,
                            y
                        )
                    );
                }
            }
        }


        RefreshFaceVisuals();
    }


    // =========================================================
    // REFRESH FACE VISUALS
    // =========================================================

    private void RefreshFaceVisuals()
    {
        foreach (GameObject obj
                 in faceObjects)
        {
            Destroy(obj);
        }


        faceObjects.Clear();


        List<Vector2Int> sortedFaces =
            new List<Vector2Int>(
                detectedFaces
            );


        sortedFaces.Sort(
            (a, b) =>
            {
                if (a.y == b.y)
                    return a.x.CompareTo(b.x);

                return b.y.CompareTo(a.y);
            }
        );


        for (
            int i = 0;
            i < sortedFaces.Count;
            i++)
        {
            Vector2Int cell =
                sortedFaces[i];


            GameObject faceObj =
                new GameObject(
                    "DetectedFace_" +
                    (i + 1),
                    typeof(Image)
                );


            faceObj.transform.SetParent(
                faceLayer,
                false
            );


            Image img =
                faceObj.GetComponent<Image>();


            img.color =
                validFaceColor;

            img.raycastTarget = false;


            RectTransform rt =
                faceObj.GetComponent<
                    RectTransform
                >();


            rt.sizeDelta =
                new Vector2(
                    CellWidth,
                    CellHeight
                );


            Vector2 center =
                GridToLocal(cell) +
                new Vector2(
                    CellWidth / 2f,
                    CellHeight / 2f
                );


            rt.anchoredPosition =
                center;


            GameObject numberObj =
                new GameObject(
                    "FaceNumber_" +
                    (i + 1),
                    typeof(Text)
                );


            numberObj.transform.SetParent(
                faceObj.transform,
                false
            );


            Text numberText =
                numberObj.GetComponent<Text>();


            numberText.text =
                (i + 1).ToString();

            numberText.alignment =
                TextAnchor.MiddleCenter;

            numberText.color =
                numberColor;

            numberText.fontSize = 28;

            numberText.raycastTarget = false;


            if (numberFont != null)
            {
                numberText.font =
                    numberFont;
            }
            else
            {
                numberText.font =
                    Resources.GetBuiltinResource<Font>(
                        "LegacyRuntime.ttf"
                    );
            }


            RectTransform numberRT =
                numberObj.GetComponent<
                    RectTransform
                >();


            numberRT.anchorMin =
                Vector2.zero;

            numberRT.anchorMax =
                Vector2.one;

            numberRT.offsetMin =
                Vector2.zero;

            numberRT.offsetMax =
                Vector2.zero;


            faceObjects.Add(
                faceObj
            );
        }
    }


    // =========================================================
    // HIGHLIGHT INVALID FACES
    // =========================================================

    private void HighlightInvalidFaces()
    {
        foreach (GameObject obj
                 in faceObjects)
        {
            Image img =
                obj.GetComponent<Image>();


            if (img != null)
            {
                img.color =
                    invalidFaceColor;
            }
        }
    }


    // =========================================================
    // CREATE LINE
    // =========================================================

    private void CreateLine(
        RectTransform parent,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        string lineName)
    {
        GameObject lineObj =
            new GameObject(
                lineName,
                typeof(Image)
            );


        lineObj.transform.SetParent(
            parent,
            false
        );


        Image img =
            lineObj.GetComponent<Image>();


        img.color =
            color;

        img.raycastTarget = false;


        RectTransform rt =
            lineObj.GetComponent<
                RectTransform
            >();


        Vector2 direction =
            end - start;


        float length =
            direction.magnitude;


        rt.sizeDelta =
            new Vector2(
                length,
                thickness
            );


        rt.anchoredPosition =
            (start + end) / 2f;


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;


        rt.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }


    // =========================================================
    // VALIDATE CURRENT NET
    // =========================================================

    public void ValidateCurrentNet()
    {
        DetectFaces();


        currentNetIsValid =
            CubeNetValidator.ValidateCubeNet(
                detectedFaces,
                out string message
            );


        // =====================================================
        // SAVE RESEARCH VALIDATION DATA
        // =====================================================

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance.LogValidation(
                "Cube",
                currentNetIsValid,
                message,
                detectedFaces.Count
            );
        }


        // =====================================================
        // VALID
        // =====================================================

        if (currentNetIsValid)
        {
            if (statusText != null)
            {
                statusText.text =
                    message;
            }


            if (create3DButton != null)
            {
                create3DButton.interactable =
                    true;
            }


            if (invalidPopupManager != null)
            {
                invalidPopupManager.HideInvalidPopup();
            }


            Debug.Log(
                "Cube net VALID."
            );
        }


        // =====================================================
        // INVALID
        // =====================================================

        else
        {
            if (statusText != null)
            {
                statusText.text =
                    message;
            }


            if (create3DButton != null)
            {
                create3DButton.interactable =
                    false;
            }


            HighlightInvalidFaces();


            if (invalidPopupRoutine != null)
            {
                StopCoroutine(
                    invalidPopupRoutine
                );
            }


            invalidPopupRoutine =
                StartCoroutine(
                    ShowInvalidPopupAfterDelay(
                        message
                    )
                );


            Debug.LogWarning(
                "Cube net INVALID: " +
                message
            );
        }


        UpdateUndoButtonState();
    }


    // =========================================================
    // INVALID POPUP DELAY
    // =========================================================

    private IEnumerator ShowInvalidPopupAfterDelay(
        string message)
    {
        yield return new WaitForSeconds(
            invalidPopupDelay
        );


        if (invalidPopupManager != null)
        {
            invalidPopupManager.ShowInvalidCubePopup(
                message
            );
        }
        else
        {
            Debug.LogError(
                "InvalidNetPopupManager is not assigned."
            );
        }


        invalidPopupRoutine = null;
    }


    // =========================================================
    // CREATE 3D MODEL
    // =========================================================

    public void Create3DModelFromValidNet()
    {
        if (!currentNetIsValid)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Please validate a correct cube net first.";
            }

            return;
        }


        if (cubeModelSpawner == null)
        {
            Debug.LogError(
                "CubeModelSpawner is not assigned."
            );

            return;
        }


        HashSet<Vector2Int> netFacesCopy =
            new HashSet<Vector2Int>(
                detectedFaces
            );


        cubeModelSpawner.CreateCubeFromCanvasNet(
            netFacesCopy,
            drawingArea,
            gridWidth,
            gridHeight,
            2.5f
        );


        // =====================================================
        // SAVE 3D GENERATION
        // =====================================================

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance
                .Log3DGeneration();
        }


        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }


        if (
            arModeManager != null &&
            cubeModelSpawner != null)
        {
            Transform modelRoot =
                cubeModelSpawner
                    .GetCurrentModelRoot();


            arModeManager.StartARAfterDelay(
                3.5f,
                modelRoot
            );
        }


        if (statusText != null)
        {
            statusText.text =
                "Your drawn net is converting into 3D.";
        }
    }


    // =========================================================
    // CLEAR DRAWING
    // =========================================================

    public void ClearDrawing()
    {
        if (invalidPopupRoutine != null)
        {
            StopCoroutine(
                invalidPopupRoutine
            );

            invalidPopupRoutine = null;
        }


        foreach (Transform child
                 in drawLayer)
        {
            Destroy(
                child.gameObject
            );
        }


        drawnEdges.Clear();

        edgeHistory.Clear();

        detectedFaces.Clear();


        RefreshFaceVisuals();


        currentNetIsValid = false;


        if (create3DButton != null)
            create3DButton.interactable = false;


        if (validateButton != null)
            validateButton.interactable = false;


        if (undoButton != null)
            undoButton.interactable = false;


        if (clearButton != null)
            clearButton.interactable = false;


        if (cubeModelSpawner != null)
            cubeModelSpawner.ClearCube();


        if (statusText != null)
        {
            statusText.text =
                "Draw cube net edges on the grid.";
        }
    }


    // =========================================================
    // PUBLIC GETTERS
    // =========================================================

    public HashSet<Vector2Int> GetDetectedFacesCopy()
    {
        return new HashSet<Vector2Int>(
            detectedFaces
        );
    }


    public RectTransform GetDrawingArea()
    {
        return drawingArea;
    }


    public int GetGridWidth()
    {
        return gridWidth;
    }


    public int GetGridHeight()
    {
        return gridHeight;
    }


    // =========================================================
    // INNER STRUCT
    // =========================================================

    private struct GridEdge :
        IEquatable<GridEdge>
    {
        public Vector2Int a;
        public Vector2Int b;


        public GridEdge(
            Vector2Int p1,
            Vector2Int p2)
        {
            if (
                p1.x < p2.x ||
                (
                    p1.x == p2.x &&
                    p1.y <= p2.y
                ))
            {
                a = p1;
                b = p2;
            }
            else
            {
                a = p2;
                b = p1;
            }
        }


        public bool Equals(
            GridEdge other)
        {
            return
                a == other.a &&
                b == other.b;
        }


        public override bool Equals(
            object obj)
        {
            return
                obj is GridEdge other &&
                Equals(other);
        }


        public override int GetHashCode()
        {
            return
                a.GetHashCode() ^
                b.GetHashCode();
        }
    }
}
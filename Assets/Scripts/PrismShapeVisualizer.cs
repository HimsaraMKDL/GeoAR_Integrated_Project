using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismShapeVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PrismShapeDetector shapeDetector;

    [SerializeField]
    private RectTransform drawingArea;

    [Header("Grid")]
    [SerializeField]
    private int gridWidth = 12;

    [SerializeField]
    private int gridHeight = 12;

    [Header("Face Colors")]
    [SerializeField]
    private Color rectangleFillColor =
        new Color(0.25f, 0.75f, 0.45f, 1f);

    [SerializeField]
    private Color triangleFillColor =
        new Color(0.15f, 0.45f, 1f, 1f);

    [Header("Label Settings")]
    [SerializeField]
    private Font labelFont;

    [SerializeField]
    private int labelFontSize = 26;

    [SerializeField]
    private Color labelColor = Color.white;

    [SerializeField]
    private Vector2 labelSize =
        new Vector2(60f, 60f);

    private RectTransform faceLayer;
    private RectTransform labelLayer;

    private float CellWidth
    {
        get
        {
            if (drawingArea == null ||
                gridWidth <= 0)
            {
                return 0f;
            }

            return
                drawingArea.rect.width /
                gridWidth;
        }
    }

    private float CellHeight
    {
        get
        {
            if (drawingArea == null ||
                gridHeight <= 0)
            {
                return 0f;
            }

            return
                drawingArea.rect.height /
                gridHeight;
        }
    }

    private void Awake()
    {
        InitializeReferences();
        CreateLayers();
    }

    private void InitializeReferences()
    {
        if (drawingArea == null)
        {
            drawingArea =
                GetComponent<RectTransform>();
        }

        if (shapeDetector == null)
        {
            shapeDetector =
                GetComponent<PrismShapeDetector>();
        }
    }

    private void CreateLayers()
    {
        if (drawingArea == null)
        {
            Debug.LogError(
                "PrismShapeVisualizer: " +
                "Drawing Area is missing."
            );

            return;
        }

        faceLayer =
            CreateLayer("PrismFaceLayer");

        labelLayer =
            CreateLayer("PrismFaceLabelLayer");

        UpdateLayerOrder();
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

        layerRect.pivot =
            new Vector2(0.5f, 0.5f);

        layerRect.localScale =
            Vector3.one;

        return layerRect;
    }

    private void UpdateLayerOrder()
    {
        Transform gridLayer =
            drawingArea.Find("PrismGridLayer");

        Transform drawLayer =
            drawingArea.Find("PrismDrawLayer");

        Transform previewLayer =
            drawingArea.Find("PrismPreviewLayer");

        if (gridLayer != null)
            gridLayer.SetSiblingIndex(0);

        if (faceLayer != null)
            faceLayer.SetSiblingIndex(1);

        if (drawLayer != null)
            drawLayer.SetAsLastSibling();

        if (previewLayer != null)
            previewLayer.SetAsLastSibling();

        if (labelLayer != null)
            labelLayer.SetAsLastSibling();
    }

    public void DetectAndVisualize()
    {
        if (shapeDetector == null)
        {
            Debug.LogError(
                "PrismShapeVisualizer: " +
                "Shape Detector is missing."
            );

            return;
        }

        shapeDetector.DetectShapes();
        VisualizeDetectedShapes();
    }

    public void VisualizeDetectedShapes()
    {
        ClearVisualization();

        if (shapeDetector == null)
            return;

        List<PrismFace> faces =
            new List<PrismFace>(
                shapeDetector.DetectedFaces
            );

        /*
         * Draw larger faces first and smaller faces last.
         * This prevents a larger accidental face from
         * covering smaller individual faces.
         */
        faces.Sort(
            (first, second) =>
            {
                int firstArea =
                    first.Width * first.Height;

                int secondArea =
                    second.Width * second.Height;

                return secondArea.CompareTo(
                    firstArea
                );
            }
        );

        for (int index = 0;
             index < faces.Count;
             index++)
        {
            PrismFace face = faces[index];

            if (face == null)
                continue;

            int faceNumber = index + 1;

            CreateFaceFill(
                face,
                faceNumber
            );

            CreateFaceLabel(
                face,
                faceNumber
            );
        }

        UpdateLayerOrder();
    }

    private void CreateFaceFill(
        PrismFace face,
        int faceNumber)
    {
        if (face == null ||
            face.Vertices == null ||
            face.Vertices.Count < 3)
        {
            return;
        }

        List<Vector2> worldLocalPoints =
            new List<Vector2>();

        foreach (Vector2Int vertex in face.Vertices)
        {
            worldLocalPoints.Add(
                GridToLocal(vertex)
            );
        }

        // Arrange polygon vertices consistently.
        List<Vector2> orderedPoints =
            OrderPointsClockwise(worldLocalPoints);

        float minimumX = float.MaxValue;
        float maximumX = float.MinValue;

        float minimumY = float.MaxValue;
        float maximumY = float.MinValue;

        foreach (Vector2 point in orderedPoints)
        {
            minimumX = Mathf.Min(minimumX, point.x);
            maximumX = Mathf.Max(maximumX, point.x);

            minimumY = Mathf.Min(minimumY, point.y);
            maximumY = Mathf.Max(maximumY, point.y);
        }

        Vector2 faceCenter =
            new Vector2(
                (minimumX + maximumX) * 0.5f,
                (minimumY + maximumY) * 0.5f
            );

        Vector2 faceSize =
            new Vector2(
                maximumX - minimumX,
                maximumY - minimumY
            );

        if (faceSize.x <= 0f ||
            faceSize.y <= 0f)
        {
            return;
        }

        GameObject faceObject =
            new GameObject(
                $"PrismFace_{faceNumber}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(PrismPolygonGraphic)
            );

        faceObject.transform.SetParent(
            faceLayer,
            false
        );

        RectTransform faceRect =
            faceObject.GetComponent<RectTransform>();

        faceRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        faceRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        faceRect.pivot =
            new Vector2(0.5f, 0.5f);

        faceRect.anchoredPosition =
            faceCenter;

        faceRect.sizeDelta =
            faceSize;

        faceRect.localScale =
            Vector3.one;

        List<Vector2> faceLocalPoints =
            new List<Vector2>();

        foreach (Vector2 point in orderedPoints)
        {
            faceLocalPoints.Add(
                point - faceCenter
            );
        }

        PrismPolygonGraphic polygon =
            faceObject.GetComponent<PrismPolygonGraphic>();

        polygon.raycastTarget = false;

        polygon.color =
            face.IsTriangle()
                ? triangleFillColor
                : rectangleFillColor;

        polygon.SetPoints(faceLocalPoints);
    }

    private List<Vector2> OrderPointsClockwise(
        List<Vector2> points)
    {
        List<Vector2> orderedPoints =
            new List<Vector2>(points);

        if (orderedPoints.Count < 3)
            return orderedPoints;

        Vector2 center = Vector2.zero;

        foreach (Vector2 point in orderedPoints)
        {
            center += point;
        }

        center /= orderedPoints.Count;

        orderedPoints.Sort(
            (first, second) =>
            {
                float firstAngle =
                    Mathf.Atan2(
                        first.y - center.y,
                        first.x - center.x
                    );

                float secondAngle =
                    Mathf.Atan2(
                        second.y - center.y,
                        second.x - center.x
                    );

                return secondAngle.CompareTo(
                    firstAngle
                );
            }
        );

        return orderedPoints;
    }

    private void CreateFaceLabel(
        PrismFace face,
        int faceNumber)
    {
        GameObject labelObject =
            new GameObject(
                $"PrismFaceLabel_{faceNumber}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

        labelObject.transform.SetParent(
            labelLayer,
            false
        );

        Text label =
            labelObject.GetComponent<Text>();

        label.text =
            faceNumber.ToString();

        label.font =
            labelFont != null
                ? labelFont
                : Resources.GetBuiltinResource<Font>(
                    "Arial.ttf"
                );

        label.fontSize =
            labelFontSize;

        label.fontStyle =
            FontStyle.Bold;

        label.alignment =
            TextAnchor.MiddleCenter;

        label.color =
            labelColor;

        label.raycastTarget =
            false;

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        labelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        labelRect.pivot =
            new Vector2(0.5f, 0.5f);

        labelRect.sizeDelta =
            labelSize;

        labelRect.anchoredPosition =
            GridToLocal(face.Center);

        labelRect.localScale =
            Vector3.one;

        labelObject.transform.SetAsLastSibling();
    }

    private Vector2 GridToLocal(
        Vector2Int gridPoint)
    {
        return GridToLocal(
            new Vector2(
                gridPoint.x,
                gridPoint.y
            )
        );
    }

    private Vector2 GridToLocal(
        Vector2 gridPoint)
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

    public void ClearVisualization()
    {
        ClearLayer(faceLayer);
        ClearLayer(labelLayer);
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
            GameObject childObject =
                layer.GetChild(index).gameObject;

            childObject.SetActive(false);
            Destroy(childObject);
        }
    }
}
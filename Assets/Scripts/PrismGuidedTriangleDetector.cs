using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismGuidedTriangleDetector :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PrismGuidedTriangleDrawer drawer;

    [SerializeField]
    private Text statusText;

    [Header("Visual Settings")]
    [SerializeField]
    private Color triangleFillColor =
        new Color(0.15f, 0.4f, 1f, 1f);

    [SerializeField]
    private Color labelColor =
        Color.white;

    [SerializeField]
    private int labelFontSize = 28;

    private RectTransform triangleLayer;
    private RectTransform labelLayer;

    private Vector2Int[] detectedVertices;
    private bool triangleDetected;

    public bool TriangleDetected =>
        triangleDetected;

    public IReadOnlyList<Vector2Int>
        DetectedVertices =>
            detectedVertices;

    private void Awake()
    {
        if (drawer == null)
        {
            drawer =
                GetComponent<
                    PrismGuidedTriangleDrawer>();
        }

        CreateLayers();
    }

    private void CreateLayers()
    {
        if (drawer == null)
            return;

        RectTransform drawingArea =
            drawer.GetDrawingArea();

        triangleLayer =
            CreateLayer(
                drawingArea,
                "GuidedTriangleFillLayer"
            );

        labelLayer =
            CreateLayer(
                drawingArea,
                "GuidedTriangleLabelLayer"
            );

        triangleLayer.SetAsLastSibling();
        labelLayer.SetAsLastSibling();
    }

    private RectTransform CreateLayer(
        RectTransform parent,
        string layerName)
    {
        Transform existing =
            parent.Find(layerName);

        if (existing != null)
        {
            RectTransform existingRect =
                existing.GetComponent<
                    RectTransform>();

            if (existingRect != null)
                return existingRect;
        }

        GameObject layerObject =
            new GameObject(
                layerName,
                typeof(RectTransform)
            );

        layerObject.transform.SetParent(
            parent,
            false
        );

        RectTransform layerRect =
            layerObject.GetComponent<
                RectTransform>();

        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layerRect.localScale = Vector3.one;

        return layerRect;
    }

    public void CheckForTriangle()
    {
        if (triangleDetected ||
            drawer == null)
        {
            return;
        }

        List<PrismGuidedGridEdge> edges =
            new List<PrismGuidedGridEdge>(
                drawer.GetDrawnEdges()
            );

        if (edges.Count < 3)
        {
            if (statusText != null)
            {
                statusText.text =
                    $"Triangle edges: {edges.Count}/3";
            }

            return;
        }

        if (edges.Count > 3)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Use only 3 lines to draw one triangle.";
            }

            return;
        }

        HashSet<Vector2Int> uniquePoints =
            new HashSet<Vector2Int>();

        foreach (PrismGuidedGridEdge edge
                 in edges)
        {
            uniquePoints.Add(edge.PointA);
            uniquePoints.Add(edge.PointB);
        }

        if (uniquePoints.Count != 3)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Connect all 3 lines to close the triangle.";
            }

            return;
        }

        List<Vector2Int> vertices =
            new List<Vector2Int>(
                uniquePoints
            );

        if (AreCollinear(
                vertices[0],
                vertices[1],
                vertices[2]))
        {
            if (statusText != null)
            {
                statusText.text =
                    "The three points do not form a triangle.";
            }

            return;
        }

        if (!EveryPointHasTwoConnections(
                vertices,
                edges))
        {
            if (statusText != null)
            {
                statusText.text =
                    "Connect all triangle corners.";
            }

            return;
        }

        triangleDetected = true;
        detectedVertices =
            vertices.ToArray();

        drawer.LockDrawing();
        DrawTriangleFill();
        drawer.ShowDepthSelection();

        if (statusText != null)
        {
            statusText.text =
                "Great! Now choose the prism depth.";
        }

        Debug.Log(
            "Guided prism triangle detected."
        );
    }

    private bool EveryPointHasTwoConnections(
        List<Vector2Int> vertices,
        List<PrismGuidedGridEdge> edges)
    {
        foreach (Vector2Int vertex
                 in vertices)
        {
            int connectionCount = 0;

            foreach (PrismGuidedGridEdge edge
                     in edges)
            {
                if (edge.PointA == vertex ||
                    edge.PointB == vertex)
                {
                    connectionCount++;
                }
            }

            if (connectionCount != 2)
                return false;
        }

        return true;
    }

    private bool AreCollinear(
        Vector2Int pointA,
        Vector2Int pointB,
        Vector2Int pointC)
    {
        int crossProduct =
            (pointB.x - pointA.x) *
            (pointC.y - pointA.y) -
            (pointB.y - pointA.y) *
            (pointC.x - pointA.x);

        return crossProduct == 0;
    }

    private void DrawTriangleFill()
    {
        ClearLayer(triangleLayer);
        ClearLayer(labelLayer);

        if (detectedVertices == null ||
            detectedVertices.Length != 3)
        {
            return;
        }

        List<Vector2> points =
            new List<Vector2>();

        foreach (Vector2Int vertex
                 in detectedVertices)
        {
            points.Add(
                drawer.GridPointToLocal(
                    vertex
                )
            );
        }

        points =
            OrderClockwise(points);

        float minimumX = float.MaxValue;
        float maximumX = float.MinValue;
        float minimumY = float.MaxValue;
        float maximumY = float.MinValue;

        foreach (Vector2 point in points)
        {
            minimumX =
                Mathf.Min(minimumX, point.x);

            maximumX =
                Mathf.Max(maximumX, point.x);

            minimumY =
                Mathf.Min(minimumY, point.y);

            maximumY =
                Mathf.Max(maximumY, point.y);
        }

        Vector2 center =
            new Vector2(
                (minimumX + maximumX) * 0.5f,
                (minimumY + maximumY) * 0.5f
            );

        Vector2 size =
            new Vector2(
                maximumX - minimumX,
                maximumY - minimumY
            );

        GameObject fillObject =
            new GameObject(
                "GuidedTriangleFace",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(PrismPolygonGraphic)
            );

        fillObject.transform.SetParent(
            triangleLayer,
            false
        );

        RectTransform fillRect =
            fillObject.GetComponent<
                RectTransform>();

        fillRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        fillRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        fillRect.pivot =
            new Vector2(0.5f, 0.5f);

        fillRect.anchoredPosition =
            center;

        fillRect.sizeDelta =
            size;

        PrismPolygonGraphic polygon =
            fillObject.GetComponent<
                PrismPolygonGraphic>();

        polygon.color =
            triangleFillColor;

        polygon.raycastTarget = false;

        List<Vector2> localPoints =
            new List<Vector2>();

        foreach (Vector2 point in points)
        {
            localPoints.Add(
                point - center
            );
        }

        polygon.SetPoints(localPoints);

        CreateLabel(center);

        triangleLayer.SetAsLastSibling();
        labelLayer.SetAsLastSibling();
    }

    private void CreateLabel(
        Vector2 position)
    {
        GameObject labelObject =
            new GameObject(
                "GuidedTriangleLabel",
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

        label.text = "1";

        label.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
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
            labelObject.GetComponent<
                RectTransform>();

        labelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        labelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        labelRect.sizeDelta =
            new Vector2(60f, 60f);

        labelRect.anchoredPosition =
            position;
    }

    private List<Vector2> OrderClockwise(
        List<Vector2> points)
    {
        Vector2 center = Vector2.zero;

        foreach (Vector2 point in points)
            center += point;

        center /= points.Count;

        points.Sort(
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

        return points;
    }

    public void ResetDetectedTriangle()
    {
        triangleDetected = false;
        detectedVertices = null;

        ClearLayer(triangleLayer);
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
            GameObject child =
                layer.GetChild(index).gameObject;

            child.SetActive(false);
            Destroy(child);
        }
    }
}
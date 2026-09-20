using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TetrahedronNetVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform drawingArea;

    [Header("Valid Face Appearance")]
    [SerializeField]
    private Color validFaceColor =
        new Color(0.25f, 0.75f, 0.45f, 0.35f);

    [SerializeField]
    private Color validEdgeColor =
        new Color(0.05f, 0.35f, 0.15f, 1f);

    [SerializeField] private float edgeThickness = 5f;

    private RectTransform visualizationLayer;

    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void Awake()
    {
        if (drawingArea == null)
        {
            drawingArea = GetComponent<RectTransform>();
        }

        CreateVisualizationLayer();
    }

    private void CreateVisualizationLayer()
    {
        if (drawingArea == null)
        {
            Debug.LogError(
                "TetrahedronNetVisualizer: Drawing Area is not assigned."
            );

            return;
        }

        Transform existing =
            drawingArea.Find("TetrahedronValidFaceLayer");

        if (existing != null)
        {
            visualizationLayer =
                existing.GetComponent<RectTransform>();

            return;
        }

        GameObject layerObject =
            new GameObject(
                "TetrahedronValidFaceLayer",
                typeof(RectTransform)
            );

        layerObject.transform.SetParent(
            drawingArea,
            false
        );

        visualizationLayer =
            layerObject.GetComponent<RectTransform>();

        visualizationLayer.anchorMin = Vector2.zero;
        visualizationLayer.anchorMax = Vector2.one;

        visualizationLayer.offsetMin = Vector2.zero;
        visualizationLayer.offsetMax = Vector2.zero;

        visualizationLayer.localScale = Vector3.one;

        // Keep fill below drawn lines if possible.
        visualizationLayer.SetAsFirstSibling();
    }

    // ==================================================
    // SHOW VALID NET
    // ==================================================

    public void ShowValidNet(
        List<Vector2[]> triangles)
    {
        ClearVisualization();

        if (drawingArea == null)
            return;

        if (triangles == null ||
            triangles.Count != 4)
        {
            Debug.LogWarning(
                "TetrahedronNetVisualizer expected exactly 4 triangles."
            );

            return;
        }

        if (visualizationLayer == null)
        {
            CreateVisualizationLayer();
        }

        foreach (Vector2[] triangle in triangles)
        {
            if (triangle == null ||
                triangle.Length != 3)
            {
                continue;
            }

            CreateTriangleFill(
                triangle[0],
                triangle[1],
                triangle[2]
            );

            CreateTriangleEdge(
                triangle[0],
                triangle[1]
            );

            CreateTriangleEdge(
                triangle[1],
                triangle[2]
            );

            CreateTriangleEdge(
                triangle[2],
                triangle[0]
            );
        }

        Debug.Log(
            "Tetrahedron valid-net visualization created."
        );
    }

    // ==================================================
    // TRIANGLE FILL
    // ==================================================

    private void CreateTriangleFill(
        Vector2 pointA,
        Vector2 pointB,
        Vector2 pointC)
    {
        GameObject triangleObject =
            new GameObject(
                "ValidTetrahedronFace",
                typeof(CanvasRenderer),
                typeof(TetrahedronTriangleGraphic)
            );

        triangleObject.transform.SetParent(
            visualizationLayer,
            false
        );

        RectTransform rect =
            triangleObject.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = Vector2.zero;

        rect.sizeDelta =
            drawingArea.rect.size;

        TetrahedronTriangleGraphic graphic =
            triangleObject.GetComponent<
                TetrahedronTriangleGraphic>();

        graphic.raycastTarget = false;

        graphic.color =
            validFaceColor;

        graphic.SetTriangle(
            pointA,
            pointB,
            pointC,
            drawingArea.rect.size
        );
    }

    // ==================================================
    // TRIANGLE EDGE
    // ==================================================

    private void CreateTriangleEdge(
        Vector2 start,
        Vector2 end)
    {
        GameObject edgeObject =
            new GameObject(
                "ValidTetrahedronEdge",
                typeof(Image)
            );

        edgeObject.transform.SetParent(
            visualizationLayer,
            false
        );

        Image image =
            edgeObject.GetComponent<Image>();

        image.color =
            validEdgeColor;

        image.raycastTarget = false;

        RectTransform rect =
            edgeObject.GetComponent<RectTransform>();

        Vector2 direction =
            end - start;

        rect.sizeDelta =
            new Vector2(
                direction.magnitude,
                edgeThickness
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

    // ==================================================
    // CLEAR
    // ==================================================

    public void ClearVisualization()
    {
        if (visualizationLayer == null)
            return;

        for (int i =
                 visualizationLayer.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                visualizationLayer
                    .GetChild(i)
                    .gameObject
            );
        }
    }
}
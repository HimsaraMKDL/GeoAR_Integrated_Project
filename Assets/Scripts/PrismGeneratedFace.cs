using System.Collections.Generic;
using UnityEngine;

public enum GeneratedPrismFaceType
{
    Rectangle,
    Triangle
}

public class PrismGeneratedFace : MonoBehaviour
{
    public GeneratedPrismFaceType faceType;

    public int faceId;

    [Header("Rectangle Geometry")]
    public float edgeLength;
    public float prismDepth;

    [Header("Triangle Geometry")]
    public Vector2 trianglePointA;
    public Vector2 trianglePointB;
    public Vector2 trianglePointC;

    public bool IsTriangle =>
        faceType ==
        GeneratedPrismFaceType.Triangle;

    public bool IsRectangle =>
        faceType ==
        GeneratedPrismFaceType.Rectangle;

    public List<PrismCompatibleEdge>
        GetCompatibleEdges()
    {
        if (IsTriangle)
            return GetTriangleEdges();

        return GetRectangleEdges();
    }

    // --------------------------------------------------
    // RECTANGLE EDGES
    // --------------------------------------------------

    private List<PrismCompatibleEdge>
        GetRectangleEdges()
    {
        List<PrismCompatibleEdge> edges =
            new List<PrismCompatibleEdge>();

        RectTransform rectTransform =
            GetComponent<RectTransform>();

        if (rectTransform == null)
            return edges;

        Rect rect =
            rectTransform.rect;

        Vector2 bottomLeft =
            new Vector2(
                rect.xMin,
                rect.yMin
            );

        Vector2 bottomRight =
            new Vector2(
                rect.xMax,
                rect.yMin
            );

        Vector2 topRight =
            new Vector2(
                rect.xMax,
                rect.yMax
            );

        Vector2 topLeft =
            new Vector2(
                rect.xMin,
                rect.yMax
            );

        /*
         * Horizontal sides correspond to
         * triangle-side length.
         */
        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.RectangleLength,
                bottomLeft,
                bottomRight,
                edgeLength
            )
        );

        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.RectangleLength,
                topLeft,
                topRight,
                edgeLength
            )
        );

        /*
         * Vertical sides correspond to
         * prism depth.
         */
        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.RectangleDepth,
                bottomLeft,
                topLeft,
                prismDepth
            )
        );

        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.RectangleDepth,
                bottomRight,
                topRight,
                prismDepth
            )
        );

        return edges;
    }

    // --------------------------------------------------
    // TRIANGLE EDGES
    // --------------------------------------------------

    private List<PrismCompatibleEdge>
        GetTriangleEdges()
    {
        List<PrismCompatibleEdge> edges =
            new List<PrismCompatibleEdge>();

        RectTransform rectTransform =
            GetComponent<RectTransform>();

        if (rectTransform == null)
            return edges;

        Vector2 localA =
            ConvertTrianglePointToRect(
                trianglePointA
            );

        Vector2 localB =
            ConvertTrianglePointToRect(
                trianglePointB
            );

        Vector2 localC =
            ConvertTrianglePointToRect(
                trianglePointC
            );

        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.TriangleSide,
                localA,
                localB,
                Vector2.Distance(
                    trianglePointA,
                    trianglePointB
                )
            )
        );

        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.TriangleSide,
                localB,
                localC,
                Vector2.Distance(
                    trianglePointB,
                    trianglePointC
                )
            )
        );

        edges.Add(
            new PrismCompatibleEdge(
                this,
                PrismEdgeType.TriangleSide,
                localC,
                localA,
                Vector2.Distance(
                    trianglePointC,
                    trianglePointA
                )
            )
        );

        return edges;
    }

    private Vector2 ConvertTrianglePointToRect(
        Vector2 sourcePoint)
    {
        RectTransform rectTransform =
            GetComponent<RectTransform>();

        Rect rect =
            rectTransform.rect;

        float minimumX =
            Mathf.Min(
                trianglePointA.x,
                Mathf.Min(
                    trianglePointB.x,
                    trianglePointC.x
                )
            );

        float maximumX =
            Mathf.Max(
                trianglePointA.x,
                Mathf.Max(
                    trianglePointB.x,
                    trianglePointC.x
                )
            );

        float minimumY =
            Mathf.Min(
                trianglePointA.y,
                Mathf.Min(
                    trianglePointB.y,
                    trianglePointC.y
                )
            );

        float maximumY =
            Mathf.Max(
                trianglePointA.y,
                Mathf.Max(
                    trianglePointB.y,
                    trianglePointC.y
                )
            );

        float width =
            Mathf.Max(
                0.001f,
                maximumX - minimumX
            );

        float height =
            Mathf.Max(
                0.001f,
                maximumY - minimumY
            );

        float normalizedX =
            (sourcePoint.x - minimumX) /
            width;

        float normalizedY =
            (sourcePoint.y - minimumY) /
            height;

        float localX =
            Mathf.Lerp(
                rect.xMin,
                rect.xMax,
                normalizedX
            );

        float localY =
            Mathf.Lerp(
                rect.yMin,
                rect.yMax,
                normalizedY
            );

        return new Vector2(
            localX,
            localY
        );
    }
}
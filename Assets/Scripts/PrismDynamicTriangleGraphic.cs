using UnityEngine;
using UnityEngine.UI;

public class PrismDynamicTriangleGraphic :
    Graphic
{
    private Vector2 pointA;
    private Vector2 pointB;
    private Vector2 pointC;

    private bool hasGeometry = false;

    public void SetTriangle(
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        pointA = a;
        pointB = b;
        pointC = c;

        hasGeometry = true;

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(
        VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (!hasGeometry)
            return;

        Rect rect =
            rectTransform.rect;

        float minX =
            Mathf.Min(
                pointA.x,
                Mathf.Min(
                    pointB.x,
                    pointC.x
                )
            );

        float maxX =
            Mathf.Max(
                pointA.x,
                Mathf.Max(
                    pointB.x,
                    pointC.x
                )
            );

        float minY =
            Mathf.Min(
                pointA.y,
                Mathf.Min(
                    pointB.y,
                    pointC.y
                )
            );

        float maxY =
            Mathf.Max(
                pointA.y,
                Mathf.Max(
                    pointB.y,
                    pointC.y
                )
            );

        float sourceWidth =
            Mathf.Max(
                0.001f,
                maxX - minX
            );

        float sourceHeight =
            Mathf.Max(
                0.001f,
                maxY - minY
            );

        Vector2 convertedA =
            ConvertPoint(
                pointA,
                minX,
                minY,
                sourceWidth,
                sourceHeight,
                rect
            );

        Vector2 convertedB =
            ConvertPoint(
                pointB,
                minX,
                minY,
                sourceWidth,
                sourceHeight,
                rect
            );

        Vector2 convertedC =
            ConvertPoint(
                pointC,
                minX,
                minY,
                sourceWidth,
                sourceHeight,
                rect
            );

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.color = color;

        vertex.position = convertedA;
        vertexHelper.AddVert(vertex);

        vertex.position = convertedB;
        vertexHelper.AddVert(vertex);

        vertex.position = convertedC;
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(
            0,
            1,
            2
        );
    }

    private Vector2 ConvertPoint(
        Vector2 source,
        float minX,
        float minY,
        float sourceWidth,
        float sourceHeight,
        Rect targetRect)
    {
        float normalizedX =
            (source.x - minX) /
            sourceWidth;

        float normalizedY =
            (source.y - minY) /
            sourceHeight;

        float x =
            Mathf.Lerp(
                targetRect.xMin,
                targetRect.xMax,
                normalizedX
            );

        float y =
            Mathf.Lerp(
                targetRect.yMin,
                targetRect.yMax,
                normalizedY
            );

        return new Vector2(x, y);
    }
}
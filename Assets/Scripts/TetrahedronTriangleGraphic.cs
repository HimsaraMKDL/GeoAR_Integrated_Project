using UnityEngine;
using UnityEngine.UI;

public class TetrahedronTriangleGraphic : MaskableGraphic
{
    private Vector2 pointA;
    private Vector2 pointB;
    private Vector2 pointC;

    private Vector2 referenceSize;

    public void SetTriangle(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 drawingAreaSize)
    {
        pointA = a;
        pointB = b;
        pointC = c;

        referenceSize =
            drawingAreaSize;

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(
        VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (referenceSize.x <= 0f ||
            referenceSize.y <= 0f)
        {
            return;
        }

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.color =
            color;

        vertex.position =
            pointA;

        vertexHelper.AddVert(vertex);

        vertex.position =
            pointB;

        vertexHelper.AddVert(vertex);

        vertex.position =
            pointC;

        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(
            0,
            1,
            2
        );
    }
}
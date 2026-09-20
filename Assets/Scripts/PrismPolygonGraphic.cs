using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismPolygonGraphic : Graphic
{
    private readonly List<Vector2> points =
        new List<Vector2>();

    public void SetPoints(
        IEnumerable<Vector2> newPoints)
    {
        points.Clear();

        if (newPoints != null)
        {
            points.AddRange(newPoints);
        }

        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(
        VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (points.Count < 3)
            return;

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.color = color;

        for (int index = 0;
             index < points.Count;
             index++)
        {
            vertex.position =
                points[index];

            vertex.uv0 =
                Vector2.zero;

            vertexHelper.AddVert(vertex);
        }

        for (int index = 1;
             index < points.Count - 1;
             index++)
        {
            // Front-facing triangle
            vertexHelper.AddTriangle(
                0,
                index,
                index + 1
            );

            // Reverse-facing triangle
            vertexHelper.AddTriangle(
                0,
                index + 1,
                index
            );
        }
    }
}
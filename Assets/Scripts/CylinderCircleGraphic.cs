using UnityEngine;
using UnityEngine.UI;

public class CylinderCircleGraphic : Graphic
{
    [Header("Circle Settings")]
    [SerializeField]
    private int segments = 64;

    protected override void OnPopulateMesh(
        VertexHelper vh)
    {
        vh.Clear();

        if (segments < 12)
            segments = 12;

        Rect rect =
            rectTransform.rect;

        float radius =
            Mathf.Min(
                rect.width,
                rect.height
            ) * 0.5f;

        Vector2 center =
            rect.center;

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.color =
            color;

        // -----------------------------------------
        // CENTER VERTEX
        // -----------------------------------------

        vertex.position =
            center;

        vh.AddVert(
            vertex
        );

        // -----------------------------------------
        // OUTER CIRCLE VERTICES
        // -----------------------------------------

        for (int i = 0;
             i <= segments;
             i++)
        {
            float angle =
                Mathf.PI * 2f *
                i / segments;

            float x =
                Mathf.Cos(angle) *
                radius;

            float y =
                Mathf.Sin(angle) *
                radius;

            vertex.position =
                center +
                new Vector2(
                    x,
                    y
                );

            vh.AddVert(
                vertex
            );
        }

        // -----------------------------------------
        // TRIANGLE FAN
        // -----------------------------------------

        for (int i = 1;
             i <= segments;
             i++)
        {
            vh.AddTriangle(
                0,
                i,
                i + 1
            );
        }
    }
}
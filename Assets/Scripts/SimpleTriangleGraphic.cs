using UnityEngine;
using UnityEngine.UI;

public class SimpleTriangleGraphic : Graphic
{
    public enum TriangleDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [SerializeField]
    private TriangleDirection direction = TriangleDirection.Up;

    public TriangleDirection Direction
    {
        get => direction;
        set
        {
            direction = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        Vector2 p1;
        Vector2 p2;
        Vector2 p3;

        switch (direction)
        {
            case TriangleDirection.Down:
                p1 = new Vector2(rect.center.x, rect.yMin);
                p2 = new Vector2(rect.xMin, rect.yMax);
                p3 = new Vector2(rect.xMax, rect.yMax);
                break;

            case TriangleDirection.Left:
                p1 = new Vector2(rect.xMin, rect.center.y);
                p2 = new Vector2(rect.xMax, rect.yMax);
                p3 = new Vector2(rect.xMax, rect.yMin);
                break;

            case TriangleDirection.Right:
                p1 = new Vector2(rect.xMax, rect.center.y);
                p2 = new Vector2(rect.xMin, rect.yMin);
                p3 = new Vector2(rect.xMin, rect.yMax);
                break;

            default:
                p1 = new Vector2(rect.center.x, rect.yMax);
                p2 = new Vector2(rect.xMin, rect.yMin);
                p3 = new Vector2(rect.xMax, rect.yMin);
                break;
        }

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = p1;
        vh.AddVert(vertex);

        vertex.position = p2;
        vh.AddVert(vertex);

        vertex.position = p3;
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
    }
}
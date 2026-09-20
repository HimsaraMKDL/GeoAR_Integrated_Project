using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(PrismPolygonGraphic))]
public class PrismTriangleVisual : MonoBehaviour
{
    public enum TriangleDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [Header("Triangle Settings")]
    [SerializeField]
    private TriangleDirection direction =
        TriangleDirection.Up;

    [SerializeField]
    private Color triangleColor =
        new Color(0.15f, 0.4f, 1f, 1f);

    private PrismPolygonGraphic polygonGraphic;

    private void Awake()
    {
        polygonGraphic =
            GetComponent<PrismPolygonGraphic>();

        DrawTriangle();
    }

    private void OnEnable()
    {
        DrawTriangle();
    }

    public void DrawTriangle()
    {
        if (polygonGraphic == null)
        {
            polygonGraphic =
                GetComponent<PrismPolygonGraphic>();
        }

        RectTransform rect =
            GetComponent<RectTransform>();

        float halfWidth =
            rect.rect.width * 0.5f;

        float halfHeight =
            rect.rect.height * 0.5f;

        List<Vector2> points =
            new List<Vector2>();

        switch (direction)
        {
            case TriangleDirection.Left:

                points.Add(
                    new Vector2(-halfWidth, 0f)
                );

                points.Add(
                    new Vector2(halfWidth, halfHeight)
                );

                points.Add(
                    new Vector2(halfWidth, -halfHeight)
                );

                break;

            case TriangleDirection.Right:

                points.Add(
                    new Vector2(halfWidth, 0f)
                );

                points.Add(
                    new Vector2(-halfWidth, halfHeight)
                );

                points.Add(
                    new Vector2(-halfWidth, -halfHeight)
                );

                break;

            case TriangleDirection.Down:

                points.Add(
                    new Vector2(0f, -halfHeight)
                );

                points.Add(
                    new Vector2(-halfWidth, halfHeight)
                );

                points.Add(
                    new Vector2(halfWidth, halfHeight)
                );

                break;

            default:

                points.Add(
                    new Vector2(0f, halfHeight)
                );

                points.Add(
                    new Vector2(-halfWidth, -halfHeight)
                );

                points.Add(
                    new Vector2(halfWidth, -halfHeight)
                );

                break;
        }

        polygonGraphic.color =
            triangleColor;

        polygonGraphic.raycastTarget =
            false;

        polygonGraphic.SetPoints(points);
    }
}
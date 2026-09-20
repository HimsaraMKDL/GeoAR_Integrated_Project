using System;
using System.Collections.Generic;
using UnityEngine;

public enum PrismFaceType
{
    Rectangle,
    Triangle
}

[Serializable]
public class PrismFace
{
    [SerializeField]
    private PrismFaceType faceType;

    [SerializeField]
    private List<Vector2Int> vertices =
        new List<Vector2Int>();

    [SerializeField]
    private int width;

    [SerializeField]
    private int height;

    [SerializeField]
    private Vector2 center;

    public PrismFaceType FaceType => faceType;

    public IReadOnlyList<Vector2Int> Vertices =>
        vertices;

    public int Width => width;

    public int Height => height;

    public Vector2 Center => center;

    public PrismFace(
        PrismFaceType type,
        IEnumerable<Vector2Int> faceVertices)
    {
        faceType = type;

        vertices = new List<Vector2Int>(
            faceVertices
        );

        CalculateDimensions();
        CalculateCenter();
    }

    private void CalculateDimensions()
    {
        if (vertices == null ||
            vertices.Count == 0)
        {
            width = 0;
            height = 0;
            return;
        }

        int minimumX = vertices[0].x;
        int maximumX = vertices[0].x;

        int minimumY = vertices[0].y;
        int maximumY = vertices[0].y;

        foreach (Vector2Int vertex in vertices)
        {
            minimumX = Mathf.Min(
                minimumX,
                vertex.x
            );

            maximumX = Mathf.Max(
                maximumX,
                vertex.x
            );

            minimumY = Mathf.Min(
                minimumY,
                vertex.y
            );

            maximumY = Mathf.Max(
                maximumY,
                vertex.y
            );
        }

        width = maximumX - minimumX;
        height = maximumY - minimumY;
    }

    private void CalculateCenter()
    {
        if (vertices == null ||
            vertices.Count == 0)
        {
            center = Vector2.zero;
            return;
        }

        Vector2 total = Vector2.zero;

        foreach (Vector2Int vertex in vertices)
        {
            total += new Vector2(
                vertex.x,
                vertex.y
            );
        }

        center = total / vertices.Count;
    }

    public bool IsRectangle()
    {
        return faceType ==
               PrismFaceType.Rectangle;
    }

    public bool IsTriangle()
    {
        return faceType ==
               PrismFaceType.Triangle;
    }

    public string GetShapeKey()
    {
        List<Vector2Int> sortedVertices =
            new List<Vector2Int>(vertices);

        sortedVertices.Sort(
            (first, second) =>
            {
                int xComparison =
                    first.x.CompareTo(second.x);

                if (xComparison != 0)
                    return xComparison;

                return first.y.CompareTo(
                    second.y
                );
            }
        );

        string key = faceType.ToString();

        foreach (Vector2Int vertex
                 in sortedVertices)
        {
            key +=
                $"_{vertex.x}_{vertex.y}";
        }

        return key;
    }
}
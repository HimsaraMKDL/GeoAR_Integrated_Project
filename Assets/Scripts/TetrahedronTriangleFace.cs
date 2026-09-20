using System;
using UnityEngine;

[Serializable]
public class TetrahedronTriangleFace
{
    public Vector2Int PointA;
    public Vector2Int PointB;
    public Vector2Int PointC;


    public TetrahedronTriangleFace(
        Vector2Int pointA,
        Vector2Int pointB,
        Vector2Int pointC)
    {
        PointA = pointA;
        PointB = pointB;
        PointC = pointC;
    }


    public string GetKey()
    {
        Vector2Int[] points =
        {
            PointA,
            PointB,
            PointC
        };

        Array.Sort(
            points,
            ComparePoints
        );

        return
            $"{points[0].x},{points[0].y}|" +
            $"{points[1].x},{points[1].y}|" +
            $"{points[2].x},{points[2].y}";
    }


    private static int ComparePoints(
        Vector2Int first,
        Vector2Int second)
    {
        if (first.x != second.x)
        {
            return
                first.x.CompareTo(
                    second.x
                );
        }

        return
            first.y.CompareTo(
                second.y
            );
    }


    public Vector2Int[] GetPoints()
    {
        return new[]
        {
            PointA,
            PointB,
            PointC
        };
    }
}
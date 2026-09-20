using System;
using UnityEngine;

[Serializable]
public readonly struct PrismGuidedGridEdge :
    IEquatable<PrismGuidedGridEdge>
{
    public Vector2Int PointA { get; }
    public Vector2Int PointB { get; }

    public bool IsHorizontal =>
        PointA.y == PointB.y;

    public bool IsVertical =>
        PointA.x == PointB.x;

    public bool IsDiagonal =>
        !IsHorizontal && !IsVertical;

    public PrismGuidedGridEdge(
        Vector2Int firstPoint,
        Vector2Int secondPoint)
    {
        bool firstComesBefore =
            firstPoint.x < secondPoint.x ||
            (
                firstPoint.x == secondPoint.x &&
                firstPoint.y <= secondPoint.y
            );

        if (firstComesBefore)
        {
            PointA = firstPoint;
            PointB = secondPoint;
        }
        else
        {
            PointA = secondPoint;
            PointB = firstPoint;
        }
    }

    public bool Equals(
        PrismGuidedGridEdge other)
    {
        return
            PointA == other.PointA &&
            PointB == other.PointB;
    }

    public override bool Equals(object obj)
    {
        return
            obj is PrismGuidedGridEdge other &&
            Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return
                (PointA.GetHashCode() * 397) ^
                PointB.GetHashCode();
        }
    }
}
using System;
using UnityEngine;

[Serializable]
public readonly struct PrismGridEdge : IEquatable<PrismGridEdge>
{
    public Vector2Int PointA { get; }
    public Vector2Int PointB { get; }

    public bool IsHorizontal => PointA.y == PointB.y;
    public bool IsVertical => PointA.x == PointB.x;
    public bool IsDiagonal => !IsHorizontal && !IsVertical;

    public PrismGridEdge(
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

    public float Length =>
        Vector2.Distance(PointA, PointB);

    public bool Equals(PrismGridEdge other)
    {
        return PointA == other.PointA &&
               PointB == other.PointB;
    }

    public override bool Equals(object obj)
    {
        return obj is PrismGridEdge other &&
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
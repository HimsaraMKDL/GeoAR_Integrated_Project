using System;
using UnityEngine;

[Serializable]
public readonly struct CuboidGridEdge : IEquatable<CuboidGridEdge>
{
    public Vector2Int PointA { get; }
    public Vector2Int PointB { get; }

    public CuboidGridEdge(Vector2Int firstPoint, Vector2Int secondPoint)
    {
        bool firstComesBefore =
            firstPoint.x < secondPoint.x ||
            (firstPoint.x == secondPoint.x &&
             firstPoint.y <= secondPoint.y);

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

    public bool Equals(CuboidGridEdge other)
    {
        return PointA == other.PointA &&
               PointB == other.PointB;
    }

    public override bool Equals(object obj)
    {
        return obj is CuboidGridEdge other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (PointA.GetHashCode() * 397) ^
                   PointB.GetHashCode();
        }
    }
}
using UnityEngine;

public struct TetrahedronGridEdge
{
    public Vector2Int PointA;
    public Vector2Int PointB;


    public TetrahedronGridEdge(
        Vector2Int pointA,
        Vector2Int pointB)
    {
        /*
         * Canonical ordering.
         * Same physical edge should compare equal
         * regardless of drawing direction.
         */

        if (ComparePoints(
                pointA,
                pointB)
            <= 0)
        {
            PointA = pointA;
            PointB = pointB;
        }
        else
        {
            PointA = pointB;
            PointB = pointA;
        }
    }


    private static int ComparePoints(
        Vector2Int first,
        Vector2Int second)
    {
        if (first.x !=
            second.x)
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


    public override bool Equals(
        object obj)
    {
        if (!(obj is
              TetrahedronGridEdge))
        {
            return false;
        }

        TetrahedronGridEdge other =
            (TetrahedronGridEdge)obj;

        return
            PointA == other.PointA &&
            PointB == other.PointB;
    }


    public override int GetHashCode()
    {
        unchecked
        {
            return
                PointA.GetHashCode() *
                397 ^
                PointB.GetHashCode();
        }
    }


    public static bool operator ==(
        TetrahedronGridEdge first,
        TetrahedronGridEdge second)
    {
        return first.Equals(
            second
        );
    }


    public static bool operator !=(
        TetrahedronGridEdge first,
        TetrahedronGridEdge second)
    {
        return !first.Equals(
            second
        );
    }
}
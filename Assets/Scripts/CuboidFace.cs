using System;
using UnityEngine;

public enum CuboidFaceType
{
    Unknown,
    DimensionAB,
    DimensionAC,
    DimensionBC
}

[Serializable]
public class CuboidFace
{
    public Vector2Int bottomLeft;
    public int gridWidth;
    public int gridHeight;
    public CuboidFaceType faceType;

    public CuboidFace(
        Vector2Int bottomLeft,
        int gridWidth,
        int gridHeight,
        CuboidFaceType faceType = CuboidFaceType.Unknown)
    {
        this.bottomLeft = bottomLeft;
        this.gridWidth = gridWidth;
        this.gridHeight = gridHeight;
        this.faceType = faceType;
    }

    public int Area => gridWidth * gridHeight;

    public int MinDimension =>
        Mathf.Min(gridWidth, gridHeight);

    public int MaxDimension =>
        Mathf.Max(gridWidth, gridHeight);

    public Vector2Int BottomRight =>
        new Vector2Int(
            bottomLeft.x + gridWidth,
            bottomLeft.y
        );

    public Vector2Int TopLeft =>
        new Vector2Int(
            bottomLeft.x,
            bottomLeft.y + gridHeight
        );

    public Vector2Int TopRight =>
        new Vector2Int(
            bottomLeft.x + gridWidth,
            bottomLeft.y + gridHeight
        );

    public Vector2 Center =>
        new Vector2(
            bottomLeft.x + gridWidth * 0.5f,
            bottomLeft.y + gridHeight * 0.5f
        );

    public bool HasSameBounds(CuboidFace other)
    {
        if (other == null)
            return false;

        return bottomLeft == other.bottomLeft &&
               gridWidth == other.gridWidth &&
               gridHeight == other.gridHeight;
    }

    public bool HasSameSize(CuboidFace other)
    {
        if (other == null)
            return false;

        return MinDimension == other.MinDimension &&
               MaxDimension == other.MaxDimension;
    }

    public bool ContainsGridPoint(Vector2Int point)
    {
        return point.x >= bottomLeft.x &&
               point.x <= bottomLeft.x + gridWidth &&
               point.y >= bottomLeft.y &&
               point.y <= bottomLeft.y + gridHeight;
    }
}
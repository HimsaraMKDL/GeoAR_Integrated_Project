using UnityEngine;

public enum PrismEdgeType
{
    TriangleSide,
    RectangleLength,
    RectangleDepth
}

public class PrismCompatibleEdge
{
    public PrismGeneratedFace owner;

    public PrismEdgeType edgeType;

    public Vector2 localPointA;
    public Vector2 localPointB;

    public float geometricLength;

    public PrismCompatibleEdge(
        PrismGeneratedFace owner,
        PrismEdgeType edgeType,
        Vector2 localPointA,
        Vector2 localPointB,
        float geometricLength)
    {
        this.owner = owner;
        this.edgeType = edgeType;
        this.localPointA = localPointA;
        this.localPointB = localPointB;
        this.geometricLength = geometricLength;
    }

    public Vector2 GetBoardPointA(
        RectTransform puzzleBoard)
    {
        Vector3 worldPoint =
            owner.transform.TransformPoint(
                localPointA
            );

        Vector3 boardPoint =
            puzzleBoard.InverseTransformPoint(
                worldPoint
            );

        return new Vector2(
            boardPoint.x,
            boardPoint.y
        );
    }

    public Vector2 GetBoardPointB(
        RectTransform puzzleBoard)
    {
        Vector3 worldPoint =
            owner.transform.TransformPoint(
                localPointB
            );

        Vector3 boardPoint =
            puzzleBoard.InverseTransformPoint(
                worldPoint
            );

        return new Vector2(
            boardPoint.x,
            boardPoint.y
        );
    }

    public Vector2 GetBoardMidPoint(
        RectTransform puzzleBoard)
    {
        return
            (
                GetBoardPointA(puzzleBoard) +
                GetBoardPointB(puzzleBoard)
            ) * 0.5f;
    }

    public Vector2 GetBoardDirection(
        RectTransform puzzleBoard)
    {
        Vector2 pointA =
            GetBoardPointA(puzzleBoard);

        Vector2 pointB =
            GetBoardPointB(puzzleBoard);

        return
            (pointB - pointA).normalized;
    }
}
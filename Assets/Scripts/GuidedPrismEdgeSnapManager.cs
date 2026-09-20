using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismEdgeSnapManager :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RectTransform puzzleBoard;

    [SerializeField]
    private Text statusText;

    [Header("Snap Settings")]
    [SerializeField]
    private float snapDistance = 45f;

    [SerializeField]
    private float angleTolerance = 12f;

    [SerializeField]
    private float lengthTolerance = 0.05f;

    public bool TrySnapFace(
        PrismDynamicFaceDrag movingDrag)
    {
        if (movingDrag == null ||
            puzzleBoard == null)
        {
            return false;
        }

        PrismGeneratedFace movingFace =
            movingDrag.GetComponent<
                PrismGeneratedFace>();

        if (movingFace == null)
            return false;

        List<PrismCompatibleEdge>
            movingEdges =
                movingFace
                    .GetCompatibleEdges();

        PrismDynamicFaceDrag[] allFaces =
            puzzleBoard.GetComponentsInChildren<
                PrismDynamicFaceDrag>(
                    false
                );

        PrismCompatibleEdge bestMovingEdge =
            null;

        PrismCompatibleEdge bestTargetEdge =
            null;

        float bestDistance =
            float.MaxValue;

        foreach (
            PrismDynamicFaceDrag otherDrag
            in allFaces)
        {
            if (otherDrag == null ||
                otherDrag == movingDrag)
            {
                continue;
            }

            if (!otherDrag.IsOnPuzzleBoard)
                continue;

            PrismGeneratedFace otherFace =
                otherDrag.GetComponent<
                    PrismGeneratedFace>();

            if (otherFace == null)
                continue;

            List<PrismCompatibleEdge>
                targetEdges =
                    otherFace
                        .GetCompatibleEdges();

            foreach (
                PrismCompatibleEdge movingEdge
                in movingEdges)
            {
                foreach (
                    PrismCompatibleEdge targetEdge
                    in targetEdges)
                {
                    if (!AreEdgeTypesCompatible(
                            movingEdge,
                            targetEdge))
                    {
                        continue;
                    }

                    if (!AreLengthsCompatible(
                            movingEdge,
                            targetEdge))
                    {
                        continue;
                    }

                    if (!AreDirectionsCompatible(
                            movingEdge,
                            targetEdge))
                    {
                        continue;
                    }

                    float distance =
                        Vector2.Distance(
                            movingEdge
                                .GetBoardMidPoint(
                                    puzzleBoard
                                ),
                            targetEdge
                                .GetBoardMidPoint(
                                    puzzleBoard
                                )
                        );

                    if (distance >
                        snapDistance)
                    {
                        continue;
                    }

                    if (distance <
                        bestDistance)
                    {
                        bestDistance =
                            distance;

                        bestMovingEdge =
                            movingEdge;

                        bestTargetEdge =
                            targetEdge;
                    }
                }
            }
        }

        if (bestMovingEdge == null ||
            bestTargetEdge == null)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Face placed. Move it near a compatible edge to connect.";
            }

            return false;
        }

        SnapEdgesTogether(
            movingDrag,
            bestMovingEdge,
            bestTargetEdge
        );

        return true;
    }

    // --------------------------------------------------
    // COMPATIBILITY RULES
    // --------------------------------------------------

    private bool AreEdgeTypesCompatible(
        PrismCompatibleEdge first,
        PrismCompatibleEdge second)
    {
        /*
         * Triangle side
         * ? Rectangle long edge.
         */
        bool triangleRectangle =
            (
                first.edgeType ==
                PrismEdgeType.TriangleSide
                &&
                second.edgeType ==
                PrismEdgeType.RectangleLength
            )
            ||
            (
                second.edgeType ==
                PrismEdgeType.TriangleSide
                &&
                first.edgeType ==
                PrismEdgeType.RectangleLength
            );

        if (triangleRectangle)
            return true;

        /*
         * Rectangles join together through
         * prism-depth edges.
         */
        bool rectangleDepthConnection =
            first.edgeType ==
                PrismEdgeType.RectangleDepth
            &&
            second.edgeType ==
                PrismEdgeType.RectangleDepth;

        return rectangleDepthConnection;
    }

    private bool AreLengthsCompatible(
        PrismCompatibleEdge first,
        PrismCompatibleEdge second)
    {
        return
            Mathf.Abs(
                first.geometricLength -
                second.geometricLength
            )
            <= lengthTolerance;
    }

    private bool AreDirectionsCompatible(
        PrismCompatibleEdge first,
        PrismCompatibleEdge second)
    {
        Vector2 firstDirection =
            first.GetBoardDirection(
                puzzleBoard
            );

        Vector2 secondDirection =
            second.GetBoardDirection(
                puzzleBoard
            );

        float angle =
            Vector2.Angle(
                firstDirection,
                secondDirection
            );

        /*
         * An edge can run in either direction,
         * so 0° and 180° both mean parallel.
         */
        float parallelAngle =
            Mathf.Min(
                angle,
                Mathf.Abs(
                    180f - angle
                )
            );

        return
            parallelAngle <=
            angleTolerance;
    }

    // --------------------------------------------------
    // SNAP
    // --------------------------------------------------

    private void SnapEdgesTogether(
        PrismDynamicFaceDrag movingDrag,
        PrismCompatibleEdge movingEdge,
        PrismCompatibleEdge targetEdge)
    {
        Vector2 movingMidPoint =
            movingEdge.GetBoardMidPoint(
                puzzleBoard
            );

        Vector2 targetMidPoint =
            targetEdge.GetBoardMidPoint(
                puzzleBoard
            );

        Vector2 movement =
            targetMidPoint -
            movingMidPoint;

        RectTransform movingRect =
            movingDrag.RectTransform;

        movingRect.anchoredPosition +=
            movement;

        if (statusText != null)
        {
            PrismGeneratedFace movingFace =
                movingDrag.GetComponent<
                    PrismGeneratedFace>();

            PrismGeneratedFace targetFace =
                targetEdge.owner;

            statusText.text =
                $"Connected Face " +
                $"{movingFace.faceId} to " +
                $"Face {targetFace.faceId}.";
        }

        Debug.Log(
            $"SNAP: Face " +
            $"{movingEdge.owner.faceId} " +
            $"edge {movingEdge.geometricLength:0.##} " +
            $"? Face " +
            $"{targetEdge.owner.faceId} " +
            $"edge {targetEdge.geometricLength:0.##}"
        );
    }
}
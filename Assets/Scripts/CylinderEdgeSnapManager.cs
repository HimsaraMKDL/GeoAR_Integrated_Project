using UnityEngine;

public class CylinderEdgeSnapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RectTransform puzzleBoard;

    [Header("Snap Settings")]
    [SerializeField]
    private float snapDistance = 90f;

    [SerializeField]
    private float horizontalTolerance = 140f;

    private CylinderGeneratedFace rectangleFace;

    private CylinderGeneratedFace topCircle;
    private CylinderGeneratedFace bottomCircle;

    // ==================================================
    // TRY SNAP
    // ==================================================

    public bool TrySnap(
        CylinderDynamicFaceDrag draggedFace)
    {
        if (draggedFace == null ||
            draggedFace.FaceData == null)
        {
            return false;
        }

        RefreshRectangleReference();

        /*
         * Rectangle itself does not snap yet.
         * It acts as the main/base face.
         */
        if (draggedFace.FaceData.IsRectangle)
        {
            Debug.Log(
                "Cylinder rectangle placed. " +
                "Waiting for circles."
            );

            return false;
        }

        if (!draggedFace.FaceData.IsCircle)
            return false;

        if (rectangleFace == null)
        {
            Debug.Log(
                "Circle cannot snap yet: " +
                "rectangle is not on the board."
            );

            return false;
        }

        return TrySnapCircle(
            draggedFace
        );
    }

    // ==================================================
    // FIND RECTANGLE
    // ==================================================

    private void RefreshRectangleReference()
    {
        rectangleFace = null;

        if (puzzleBoard == null)
            return;

        CylinderGeneratedFace[] faces =
            puzzleBoard.GetComponentsInChildren<
                CylinderGeneratedFace>(
                false
            );

        foreach (CylinderGeneratedFace face
                 in faces)
        {
            if (face != null &&
                face.IsRectangle)
            {
                rectangleFace = face;
                return;
            }
        }
    }

    // ==================================================
    // CIRCLE SNAP
    // ==================================================

    private bool TrySnapCircle(
        CylinderDynamicFaceDrag circleDrag)
    {
        RectTransform circleRect =
            circleDrag.GetComponent<
                RectTransform>();

        RectTransform rectangleRect =
            rectangleFace.GetComponent<
                RectTransform>();

        if (circleRect == null ||
            rectangleRect == null)
        {
            return false;
        }

        /*
         * All calculations are converted into
         * PuzzleBoard-local coordinates.
         */

        Vector2 circleCenter =
            GetBoardLocalCenter(
                circleRect
            );

        Vector2 rectangleCenter =
            GetBoardLocalCenter(
                rectangleRect
            );

        // ----------------------------------------------
        // TARGET POSITIONS (ORIENTATION AWARE)
        // ----------------------------------------------

        float rectangleHalfHeight =
            rectangleRect.rect.height *
            0.5f;

        float circleRadius =
            circleRect.rect.height *
            0.5f;

        // Rectangle's LOCAL up direction
        Vector3 worldUpPoint =
            rectangleRect.TransformPoint(
                Vector3.up
            );

        Vector3 worldCenterPoint =
            rectangleRect.TransformPoint(
                Vector3.zero
            );

        Vector3 boardUpPoint =
            puzzleBoard.InverseTransformPoint(
                worldUpPoint
            );

        Vector3 boardCenterPoint =
            puzzleBoard.InverseTransformPoint(
                worldCenterPoint
            );

        Vector2 rectangleUp =
            (
                new Vector2(
                    boardUpPoint.x,
                    boardUpPoint.y
                )
                -
                new Vector2(
                    boardCenterPoint.x,
                    boardCenterPoint.y
                )
            ).normalized;

        Vector2 topTarget =
            rectangleCenter +
            rectangleUp *
            (
                rectangleHalfHeight +
                circleRadius
            );

        Vector2 bottomTarget =
            rectangleCenter -
            rectangleUp *
            (
                rectangleHalfHeight +
                circleRadius
            );

        float topDistance =
            Vector2.Distance(
                circleCenter,
                topTarget
            );

        float bottomDistance =
            Vector2.Distance(
                circleCenter,
                bottomTarget
            );

        // ----------------------------------------------
        // SIDEWAYS TOLERANCE CHECK
        // ----------------------------------------------

        Vector2 rectangleRight =
            new Vector2(
                rectangleUp.y,
                -rectangleUp.x
            );

        Vector2 fromRectangleToCircle =
            circleCenter -
            rectangleCenter;

        float sidewaysDifference =
            Mathf.Abs(
                Vector2.Dot(
                    fromRectangleToCircle,
                    rectangleRight
                )
            );

        if (sidewaysDifference >
            horizontalTolerance)
        {
            return false;
        }

        // ----------------------------------------------
        // TRY TOP
        // ----------------------------------------------

        if (topDistance <= snapDistance &&
            CanUseTop(circleDrag.FaceData))
        {
            SnapCircleToTarget(
                circleRect,
                topTarget
            );

            topCircle =
                circleDrag.FaceData;

            if (bottomCircle ==
                circleDrag.FaceData)
            {
                bottomCircle = null;
            }

            Debug.Log(
                $"Cylinder Circle " +
                $"{circleDrag.FaceData.faceId} " +
                "snapped to TOP edge."
            );

            return true;
        }

        // ----------------------------------------------
        // TRY BOTTOM
        // ----------------------------------------------

        if (bottomDistance <= snapDistance &&
            CanUseBottom(circleDrag.FaceData))
        {
            SnapCircleToTarget(
                circleRect,
                bottomTarget
            );

            bottomCircle =
                circleDrag.FaceData;

            if (topCircle ==
                circleDrag.FaceData)
            {
                topCircle = null;
            }

            Debug.Log(
                $"Cylinder Circle " +
                $"{circleDrag.FaceData.faceId} " +
                "snapped to BOTTOM edge."
            );

            return true;
        }

        return false;
    }

    // ==================================================
    // EDGE OCCUPANCY
    // ==================================================

    private bool CanUseTop(
        CylinderGeneratedFace circle)
    {
        return
            topCircle == null ||
            topCircle == circle;
    }

    private bool CanUseBottom(
        CylinderGeneratedFace circle)
    {
        return
            bottomCircle == null ||
            bottomCircle == circle;
    }

    // ==================================================
    // SNAP POSITION
    // ==================================================

    private void SnapCircleToTarget(
        RectTransform circleRect,
        Vector2 boardLocalTarget)
    {
        if (circleRect.parent !=
            puzzleBoard)
        {
            circleRect.SetParent(
                puzzleBoard,
                true
            );
        }

        circleRect.anchoredPosition =
            boardLocalTarget;
    }

    // ==================================================
    // BOARD SPACE HELPERS
    // ==================================================

    private Vector2 GetBoardLocalCenter(
        RectTransform rect)
    {
        Vector3 worldCenter =
            rect.TransformPoint(
                rect.rect.center
            );

        Vector3 boardLocal =
            puzzleBoard.InverseTransformPoint(
                worldCenter
            );

        return new Vector2(
            boardLocal.x,
            boardLocal.y
        );
    }

    private float GetBoardHeight(
        RectTransform rect)
    {
        Vector3 worldBottom =
            rect.TransformPoint(
                new Vector3(
                    0f,
                    rect.rect.yMin,
                    0f
                )
            );

        Vector3 worldTop =
            rect.TransformPoint(
                new Vector3(
                    0f,
                    rect.rect.yMax,
                    0f
                )
            );

        Vector3 boardBottom =
            puzzleBoard.InverseTransformPoint(
                worldBottom
            );

        Vector3 boardTop =
            puzzleBoard.InverseTransformPoint(
                worldTop
            );

        return Vector3.Distance(
            boardBottom,
            boardTop
        );
    }

    // ==================================================
    // RELEASE FACE
    // ==================================================

    public void ReleaseFace(
        CylinderGeneratedFace face)
    {
        if (face == null)
            return;

        if (topCircle == face)
        {
            topCircle = null;

            Debug.Log(
                $"Cylinder Circle {face.faceId} " +
                "released from TOP edge."
            );
        }

        if (bottomCircle == face)
        {
            bottomCircle = null;

            Debug.Log(
                $"Cylinder Circle {face.faceId} " +
                "released from BOTTOM edge."
            );
        }
    }

    // ==================================================
    // RESET
    // ==================================================

    public void ResetSnapping()
    {
        rectangleFace = null;
        topCircle = null;
        bottomCircle = null;

        Debug.Log(
            "Cylinder snapping reset."
        );
    }

    // ==================================================
    // VALIDATION HELPERS FOR NEXT STEP
    // ==================================================

    public bool HasTopCircle =>
        topCircle != null;

    public bool HasBottomCircle =>
        bottomCircle != null;

    public bool HasBothCirclesSnapped =>
        topCircle != null &&
        bottomCircle != null;

    public CylinderGeneratedFace GetTopCircle()
    {
        return topCircle;
    }

    public CylinderGeneratedFace GetBottomCircle()
    {
        return bottomCircle;
    }

    public CylinderGeneratedFace GetRectangleFace()
    {
        RefreshRectangleReference();

        return rectangleFace;
    }
}
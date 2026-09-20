using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismGuidedNetValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RectTransform puzzleBoard;

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text progressText;

    [SerializeField]
    private Button checkPuzzleButton;

    [SerializeField]
    private Button create3DButton;

    [Header("Validation Tolerance")]
    [SerializeField]
    private float edgePositionTolerance = 6f;

    [SerializeField]
    private float lengthTolerance = 0.05f;

    [SerializeField]
    private float sideTolerance = 0.05f;

    private bool currentNetValid = false;

    public bool CurrentNetValid => currentNetValid;

    // ==================================================
    // MAIN VALIDATION
    // ==================================================

    public void ValidateCurrentNet()
    {
        Debug.Log(
            "GUIDED PRISM: Starting orientation-aware validation."
        );

        currentNetValid = false;

        if (create3DButton != null)
            create3DButton.interactable = false;

        if (puzzleBoard == null)
        {
            ShowInvalid(
                "Puzzle Board is not assigned."
            );
            return;
        }

        PrismDynamicFaceDrag[] dragFaces =
            puzzleBoard.GetComponentsInChildren<
                PrismDynamicFaceDrag>(
                false
            );

        // --------------------------------------------------
        // 1. ALL 5 FACES MUST BE ON BOARD
        // --------------------------------------------------

        if (dragFaces.Length != 5)
        {
            ShowInvalid(
                $"Place all 5 faces on the board first. " +
                $"Currently placed: {dragFaces.Length}/5."
            );

            UpdateProgress(
                dragFaces.Length
            );

            return;
        }

        List<PrismGeneratedFace> allFaces =
            new List<PrismGeneratedFace>();

        List<PrismGeneratedFace> triangles =
            new List<PrismGeneratedFace>();

        List<PrismGeneratedFace> rectangles =
            new List<PrismGeneratedFace>();

        foreach (PrismDynamicFaceDrag drag
                 in dragFaces)
        {
            if (drag == null ||
                !drag.IsOnPuzzleBoard)
            {
                ShowInvalid(
                    "All 5 faces must be placed on the board."
                );
                return;
            }

            PrismGeneratedFace face =
                drag.GetComponent<
                    PrismGeneratedFace>();

            if (face == null)
            {
                ShowInvalid(
                    "A generated face is missing geometry data."
                );
                return;
            }

            allFaces.Add(face);

            if (face.IsTriangle)
                triangles.Add(face);

            if (face.IsRectangle)
                rectangles.Add(face);
        }

        // --------------------------------------------------
        // 2. FACE COUNT
        // --------------------------------------------------

        if (triangles.Count != 2)
        {
            ShowInvalid(
                $"A triangular prism requires exactly " +
                $"2 triangles. Found: {triangles.Count}."
            );
            return;
        }

        if (rectangles.Count != 3)
        {
            ShowInvalid(
                $"A triangular prism requires exactly " +
                $"3 rectangles. Found: {rectangles.Count}."
            );
            return;
        }

        // --------------------------------------------------
        // 3. BUILD CONNECTION GRAPH
        // --------------------------------------------------

        Dictionary<int, HashSet<int>> graph =
            new Dictionary<int, HashSet<int>>();

        foreach (PrismGeneratedFace face
                 in allFaces)
        {
            graph[face.faceId] =
                new HashSet<int>();
        }

        int totalConnections = 0;

        int rectangleRectangleConnections = 0;
        int triangleRectangleConnections = 0;
        int triangleTriangleConnections = 0;

        for (int firstIndex = 0;
             firstIndex < allFaces.Count - 1;
             firstIndex++)
        {
            for (int secondIndex =
                     firstIndex + 1;
                 secondIndex < allFaces.Count;
                 secondIndex++)
            {
                PrismGeneratedFace first =
                    allFaces[firstIndex];

                PrismGeneratedFace second =
                    allFaces[secondIndex];

                int sharedEdgeCount =
                    CountMatchingSharedEdges(
                        first,
                        second
                    );

                if (sharedEdgeCount > 1)
                {
                    ShowInvalid(
                        $"Faces {first.faceId} and " +
                        $"{second.faceId} overlap or share " +
                        $"more than one complete edge."
                    );
                    return;
                }

                if (sharedEdgeCount == 0)
                    continue;

                graph[first.faceId]
                    .Add(second.faceId);

                graph[second.faceId]
                    .Add(first.faceId);

                totalConnections++;

                if (first.IsRectangle &&
                    second.IsRectangle)
                {
                    rectangleRectangleConnections++;
                }
                else if (
                    first.IsTriangle &&
                    second.IsTriangle)
                {
                    triangleTriangleConnections++;
                }
                else
                {
                    triangleRectangleConnections++;
                }
            }
        }

        // --------------------------------------------------
        // 4. BASIC TRIANGULAR PRISM TOPOLOGY
        // --------------------------------------------------

        if (totalConnections != 4)
        {
            ShowInvalid(
                $"A valid prism net requires exactly " +
                $"4 hinge connections. Found: " +
                $"{totalConnections}/4."
            );
            return;
        }

        if (rectangleRectangleConnections != 2)
        {
            ShowInvalid(
                "The three rectangular faces must form " +
                "one connected strip."
            );
            return;
        }

        if (triangleRectangleConnections != 2)
        {
            ShowInvalid(
                "Each triangular end face must connect " +
                "to one rectangular face."
            );
            return;
        }

        if (triangleTriangleConnections != 0)
        {
            ShowInvalid(
                "The two triangular end faces cannot " +
                "connect directly."
            );
            return;
        }

        if (!IsGraphConnected(
                graph,
                allFaces[0].faceId,
                allFaces.Count))
        {
            ShowInvalid(
                "All 5 faces must form one connected net."
            );
            return;
        }

        // --------------------------------------------------
        // 5. EACH TRIANGLE MUST HAVE EXACTLY ONE HINGE
        // --------------------------------------------------

        foreach (PrismGeneratedFace triangle
                 in triangles)
        {
            if (graph[triangle.faceId].Count != 1)
            {
                ShowInvalid(
                    $"Triangle Face {triangle.faceId} must " +
                    $"connect along exactly one edge."
                );
                return;
            }
        }

        // --------------------------------------------------
        // 6. FIND TRIANGLE ATTACHMENTS
        // --------------------------------------------------

        TriangleAttachment attachment1 =
            FindTriangleAttachment(
                triangles[0],
                rectangles
            );

        TriangleAttachment attachment2 =
            FindTriangleAttachment(
                triangles[1],
                rectangles
            );

        if (attachment1 == null ||
            attachment2 == null)
        {
            ShowInvalid(
                "Could not determine the triangular " +
                "end-face connections."
            );
            return;
        }

        // --------------------------------------------------
        // 7. TRIANGLE MUST POINT OUTWARD
        // --------------------------------------------------

        if (!TrianglePointsOutsideRectangle(
                attachment1))
        {
            ShowInvalid(
                $"Triangle Face {attachment1.triangle.faceId} " +
                $"has the wrong orientation. Rotate or flip " +
                $"the triangle so it points outside the prism net."
            );
            return;
        }

        if (!TrianglePointsOutsideRectangle(
                attachment2))
        {
            ShowInvalid(
                $"Triangle Face {attachment2.triangle.faceId} " +
                $"has the wrong orientation. Rotate or flip " +
                $"the triangle so it points outside the prism net."
            );
            return;
        }

        // --------------------------------------------------
        // 8. DETERMINE RECTANGLE STRIP AXIS
        // --------------------------------------------------

        Vector2 stripAxis =
            GetRectangleStripAxis(
                rectangles
            );

        if (stripAxis.sqrMagnitude <
            0.001f)
        {
            ShowInvalid(
                "Could not determine the rectangle strip direction."
            );
            return;
        }

        Vector2 stripNormal =
            new Vector2(
                -stripAxis.y,
                stripAxis.x
            ).normalized;

        Vector2 stripCenter =
            GetRectangleStripCenter(
                rectangles
            );

        // --------------------------------------------------
        // 9. TRIANGLES MUST BE ON OPPOSITE PRISM ENDS
        // --------------------------------------------------

        Vector2 triangleCenter1 =
            GetFaceCenterOnBoard(
                attachment1.triangle
            );

        Vector2 triangleCenter2 =
            GetFaceCenterOnBoard(
                attachment2.triangle
            );

        float side1 =
            Vector2.Dot(
                triangleCenter1 -
                stripCenter,
                stripNormal
            );

        float side2 =
            Vector2.Dot(
                triangleCenter2 -
                stripCenter,
                stripNormal
            );

        if (Mathf.Abs(side1) <
                edgePositionTolerance ||
            Mathf.Abs(side2) <
                edgePositionTolerance)
        {
            ShowInvalid(
                "A triangular end face is not positioned " +
                "clearly outside the rectangle strip."
            );
            return;
        }

        if (Mathf.Sign(side1) ==
            Mathf.Sign(side2))
        {
            ShowInvalid(
                "The two triangular end faces are on " +
                "the same side of the prism strip. " +
                "Move one triangle to the opposite side."
            );
            return;
        }

        // --------------------------------------------------
        // 10. TRIANGLE HANDEDNESS / END-CAP CORRESPONDENCE
        // --------------------------------------------------

        if (!DoTriangleEndsCorrespond(
                attachment1,
                attachment2,
                stripAxis))
        {
            ShowInvalid(
                "The triangular end faces have mismatched " +
                "orientation. Rotate or flip one triangle " +
                "so the corresponding corners match."
            );
            return;
        }

        // --------------------------------------------------
        // SUCCESS
        // --------------------------------------------------

        currentNetValid = true;

        if (statusText != null)
        {
            statusText.text =
                "Excellent! This is a valid triangular prism net.";
        }

        if (progressText != null)
        {
            progressText.text =
                "Valid net ?";
        }

        if (create3DButton != null)
        {
            create3DButton.interactable =
                true;
        }

        Debug.Log(
            "GUIDED PRISM VALIDATION SUCCESSFUL: " +
            "topology, orientation and end-cap geometry passed."
        );
    }

    // ==================================================
    // TRIANGLE ATTACHMENT
    // ==================================================

    private TriangleAttachment FindTriangleAttachment(
        PrismGeneratedFace triangle,
        List<PrismGeneratedFace> rectangles)
    {
        foreach (PrismGeneratedFace rectangle
                 in rectangles)
        {
            PrismCompatibleEdge triangleEdge;
            PrismCompatibleEdge rectangleEdge;

            if (TryGetSharedEdge(
                    triangle,
                    rectangle,
                    out triangleEdge,
                    out rectangleEdge))
            {
                return new TriangleAttachment(
                    triangle,
                    rectangle,
                    triangleEdge,
                    rectangleEdge
                );
            }
        }

        return null;
    }

    private bool TryGetSharedEdge(
        PrismGeneratedFace first,
        PrismGeneratedFace second,
        out PrismCompatibleEdge firstSharedEdge,
        out PrismCompatibleEdge secondSharedEdge)
    {
        firstSharedEdge = null;
        secondSharedEdge = null;

        List<PrismCompatibleEdge> firstEdges =
            first.GetCompatibleEdges();

        List<PrismCompatibleEdge> secondEdges =
            second.GetCompatibleEdges();

        foreach (PrismCompatibleEdge firstEdge
                 in firstEdges)
        {
            foreach (PrismCompatibleEdge secondEdge
                     in secondEdges)
            {
                if (!AreEdgeTypesCompatible(
                        firstEdge,
                        secondEdge))
                {
                    continue;
                }

                if (!AreLengthsCompatible(
                        firstEdge,
                        secondEdge))
                {
                    continue;
                }

                if (!AreEdgesCoincident(
                        firstEdge,
                        secondEdge))
                {
                    continue;
                }

                firstSharedEdge =
                    firstEdge;

                secondSharedEdge =
                    secondEdge;

                return true;
            }
        }

        return false;
    }

    // ==================================================
    // OUTWARD ORIENTATION
    // ==================================================

    private bool TrianglePointsOutsideRectangle(
        TriangleAttachment attachment)
    {
        Vector2 edgeA =
            attachment.rectangleEdge
                .GetBoardPointA(
                    puzzleBoard
                );

        Vector2 edgeB =
            attachment.rectangleEdge
                .GetBoardPointB(
                    puzzleBoard
                );

        Vector2 triangleCenter =
            GetFaceCenterOnBoard(
                attachment.triangle
            );

        Vector2 rectangleCenter =
            GetFaceCenterOnBoard(
                attachment.rectangle
            );

        float triangleSide =
            SignedSideOfLine(
                edgeA,
                edgeB,
                triangleCenter
            );

        float rectangleSide =
            SignedSideOfLine(
                edgeA,
                edgeB,
                rectangleCenter
            );

        /*
         * Triangle and rectangle must exist
         * on opposite sides of their shared hinge.
         */
        if (Mathf.Abs(triangleSide) <
                0.001f ||
            Mathf.Abs(rectangleSide) <
                0.001f)
        {
            return false;
        }

        return
            Mathf.Sign(triangleSide) !=
            Mathf.Sign(rectangleSide);
    }

    private float SignedSideOfLine(
        Vector2 lineA,
        Vector2 lineB,
        Vector2 point)
    {
        Vector2 edge =
            lineB - lineA;

        Vector2 toPoint =
            point - lineA;

        return
            edge.x * toPoint.y -
            edge.y * toPoint.x;
    }

    // ==================================================
    // TRIANGLE CORRESPONDENCE
    // ==================================================

    private bool DoTriangleEndsCorrespond(
        TriangleAttachment first,
        TriangleAttachment second,
        Vector2 stripAxis)
    {
        Vector2 firstEdgeA =
            first.triangleEdge
                .GetBoardPointA(
                    puzzleBoard
                );

        Vector2 firstEdgeB =
            first.triangleEdge
                .GetBoardPointB(
                    puzzleBoard
                );

        Vector2 secondEdgeA =
            second.triangleEdge
                .GetBoardPointA(
                    puzzleBoard
                );

        Vector2 secondEdgeB =
            second.triangleEdge
                .GetBoardPointB(
                    puzzleBoard
                );

        Vector2 firstThird =
            GetTriangleThirdPoint(
                first.triangle,
                firstEdgeA,
                firstEdgeB
            );

        Vector2 secondThird =
            GetTriangleThirdPoint(
                second.triangle,
                secondEdgeA,
                secondEdgeB
            );

        /*
         * Order each shared edge consistently along
         * the rectangle-strip axis.
         */
        OrderEdgeAlongAxis(
            ref firstEdgeA,
            ref firstEdgeB,
            stripAxis
        );

        OrderEdgeAlongAxis(
            ref secondEdgeA,
            ref secondEdgeB,
            stripAxis
        );

        float firstLeftLength =
            Vector2.Distance(
                firstEdgeA,
                firstThird
            );

        float firstRightLength =
            Vector2.Distance(
                firstEdgeB,
                firstThird
            );

        float secondLeftLength =
            Vector2.Distance(
                secondEdgeA,
                secondThird
            );

        float secondRightLength =
            Vector2.Distance(
                secondEdgeB,
                secondThird
            );

        /*
         * Compare ratios rather than UI pixel
         * lengths because each triangle is scaled
         * from mathematical geometry.
         */
        float firstSum =
            Mathf.Max(
                0.001f,
                firstLeftLength +
                firstRightLength
            );

        float secondSum =
            Mathf.Max(
                0.001f,
                secondLeftLength +
                secondRightLength
            );

        float firstLeftRatio =
            firstLeftLength /
            firstSum;

        float firstRightRatio =
            firstRightLength /
            firstSum;

        float secondLeftRatio =
            secondLeftLength /
            secondSum;

        float secondRightRatio =
            secondRightLength /
            secondSum;

        bool sameCorrespondence =
            Mathf.Abs(
                firstLeftRatio -
                secondLeftRatio
            ) <= sideTolerance
            &&
            Mathf.Abs(
                firstRightRatio -
                secondRightRatio
            ) <= sideTolerance;

        return sameCorrespondence;
    }

    private Vector2 GetTriangleThirdPoint(
        PrismGeneratedFace triangle,
        Vector2 sharedA,
        Vector2 sharedB)
    {
        List<PrismCompatibleEdge> edges =
            triangle.GetCompatibleEdges();

        foreach (PrismCompatibleEdge edge
                 in edges)
        {
            Vector2 pointA =
                edge.GetBoardPointA(
                    puzzleBoard
                );

            Vector2 pointB =
                edge.GetBoardPointB(
                    puzzleBoard
                );

            if (!IsNearEitherSharedEndpoint(
                    pointA,
                    sharedA,
                    sharedB))
            {
                return pointA;
            }

            if (!IsNearEitherSharedEndpoint(
                    pointB,
                    sharedA,
                    sharedB))
            {
                return pointB;
            }
        }

        return
            GetFaceCenterOnBoard(
                triangle
            );
    }

    private bool IsNearEitherSharedEndpoint(
        Vector2 point,
        Vector2 sharedA,
        Vector2 sharedB)
    {
        return
            Vector2.Distance(
                point,
                sharedA
            ) <= edgePositionTolerance
            ||
            Vector2.Distance(
                point,
                sharedB
            ) <= edgePositionTolerance;
    }

    private void OrderEdgeAlongAxis(
        ref Vector2 pointA,
        ref Vector2 pointB,
        Vector2 axis)
    {
        float projectionA =
            Vector2.Dot(
                pointA,
                axis
            );

        float projectionB =
            Vector2.Dot(
                pointB,
                axis
            );

        if (projectionA >
            projectionB)
        {
            Vector2 temporary =
                pointA;

            pointA =
                pointB;

            pointB =
                temporary;
        }
    }

    // ==================================================
    // RECTANGLE STRIP
    // ==================================================

    private Vector2 GetRectangleStripAxis(
        List<PrismGeneratedFace> rectangles)
    {
        if (rectangles == null ||
            rectangles.Count == 0)
        {
            return Vector2.zero;
        }

        List<PrismCompatibleEdge> edges =
            rectangles[0]
                .GetCompatibleEdges();

        foreach (PrismCompatibleEdge edge
                 in edges)
        {
            if (edge.edgeType !=
                PrismEdgeType.RectangleLength)
            {
                continue;
            }

            Vector2 direction =
                edge.GetBoardDirection(
                    puzzleBoard
                ).normalized;

            /*
             * Canonical direction so that
             * opposite 180-degree rectangle rotations
             * still produce the same strip axis.
             */
            if (direction.x < -0.001f ||
                (
                    Mathf.Abs(direction.x) <
                    0.001f &&
                    direction.y < 0f
                ))
            {
                direction =
                    -direction;
            }

            return direction;
        }

        return Vector2.zero;
    }

    private Vector2 GetRectangleStripCenter(
        List<PrismGeneratedFace> rectangles)
    {
        Vector2 center =
            Vector2.zero;

        foreach (PrismGeneratedFace rectangle
                 in rectangles)
        {
            center +=
                GetFaceCenterOnBoard(
                    rectangle
                );
        }

        return
            center /
            rectangles.Count;
    }

    // ==================================================
    // FACE CENTER
    // ==================================================

    private Vector2 GetFaceCenterOnBoard(
        PrismGeneratedFace face)
    {
        RectTransform rect =
            face.GetComponent<
                RectTransform>();

        if (rect == null)
            return Vector2.zero;

        Vector3 worldCenter =
            rect.TransformPoint(
                rect.rect.center
            );

        Vector3 boardCenter =
            puzzleBoard
                .InverseTransformPoint(
                    worldCenter
                );

        return new Vector2(
            boardCenter.x,
            boardCenter.y
        );
    }

    // ==================================================
    // SHARED EDGE DETECTION
    // ==================================================

    private int CountMatchingSharedEdges(
        PrismGeneratedFace first,
        PrismGeneratedFace second)
    {
        List<PrismCompatibleEdge> firstEdges =
            first.GetCompatibleEdges();

        List<PrismCompatibleEdge> secondEdges =
            second.GetCompatibleEdges();

        int sharedEdgeCount = 0;

        foreach (PrismCompatibleEdge firstEdge
                 in firstEdges)
        {
            foreach (PrismCompatibleEdge secondEdge
                     in secondEdges)
            {
                if (!AreEdgeTypesCompatible(
                        firstEdge,
                        secondEdge))
                {
                    continue;
                }

                if (!AreLengthsCompatible(
                        firstEdge,
                        secondEdge))
                {
                    continue;
                }

                if (AreEdgesCoincident(
                        firstEdge,
                        secondEdge))
                {
                    sharedEdgeCount++;
                }
            }
        }

        return sharedEdgeCount;
    }

    private bool AreEdgeTypesCompatible(
        PrismCompatibleEdge first,
        PrismCompatibleEdge second)
    {
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

        bool rectangleRectangle =
            first.edgeType ==
                PrismEdgeType.RectangleDepth
            &&
            second.edgeType ==
                PrismEdgeType.RectangleDepth;

        return rectangleRectangle;
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

    private bool AreEdgesCoincident(
        PrismCompatibleEdge first,
        PrismCompatibleEdge second)
    {
        Vector2 firstA =
            first.GetBoardPointA(
                puzzleBoard
            );

        Vector2 firstB =
            first.GetBoardPointB(
                puzzleBoard
            );

        Vector2 secondA =
            second.GetBoardPointA(
                puzzleBoard
            );

        Vector2 secondB =
            second.GetBoardPointB(
                puzzleBoard
            );

        bool sameDirection =
            Vector2.Distance(
                firstA,
                secondA
            ) <= edgePositionTolerance
            &&
            Vector2.Distance(
                firstB,
                secondB
            ) <= edgePositionTolerance;

        bool reversedDirection =
            Vector2.Distance(
                firstA,
                secondB
            ) <= edgePositionTolerance
            &&
            Vector2.Distance(
                firstB,
                secondA
            ) <= edgePositionTolerance;

        return
            sameDirection ||
            reversedDirection;
    }

    // ==================================================
    // CONNECTIVITY
    // ==================================================

    private bool IsGraphConnected(
        Dictionary<int, HashSet<int>> graph,
        int startingFace,
        int expectedCount)
    {
        HashSet<int> visited =
            new HashSet<int>();

        Queue<int> queue =
            new Queue<int>();

        queue.Enqueue(
            startingFace
        );

        visited.Add(
            startingFace
        );

        while (queue.Count > 0)
        {
            int current =
                queue.Dequeue();

            foreach (int neighbour
                     in graph[current])
            {
                if (visited.Contains(
                        neighbour))
                {
                    continue;
                }

                visited.Add(
                    neighbour
                );

                queue.Enqueue(
                    neighbour
                );
            }
        }

        return
            visited.Count ==
            expectedCount;
    }

    // ==================================================
    // FEEDBACK
    // ==================================================

    private void ShowInvalid(
        string reason)
    {
        currentNetValid = false;

        if (statusText != null)
        {
            statusText.text =
                "Not valid yet: " +
                reason;
        }

        if (progressText != null)
        {
            progressText.text =
                "Adjust the net and try again.";
        }

        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }

        Debug.LogWarning(
            "Guided prism validation: " +
            reason
        );
    }

    private void UpdateProgress(
        int placedFaces)
    {
        if (progressText != null)
        {
            progressText.text =
                $"Faces on board: " +
                $"{placedFaces}/5";
        }
    }

    public void InvalidateCurrentNet()
    {
        currentNetValid =
            false;

        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }
    }

    // ==================================================
    // HELPER CLASS
    // ==================================================

    private class TriangleAttachment
    {
        public PrismGeneratedFace triangle;

        public PrismGeneratedFace rectangle;

        public PrismCompatibleEdge triangleEdge;

        public PrismCompatibleEdge rectangleEdge;

        public TriangleAttachment(
            PrismGeneratedFace triangle,
            PrismGeneratedFace rectangle,
            PrismCompatibleEdge triangleEdge,
            PrismCompatibleEdge rectangleEdge)
        {
            this.triangle =
                triangle;

            this.rectangle =
                rectangle;

            this.triangleEdge =
                triangleEdge;

            this.rectangleEdge =
                rectangleEdge;
        }
    }
}
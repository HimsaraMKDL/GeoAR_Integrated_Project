using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CuboidNetValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CuboidDrawManager drawManager;
    [SerializeField] private Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button validateButton;
    [SerializeField] private Button create3DButton;

    [Header("2D Net Correction")]
    [SerializeField] private CuboidInvalidPopupManager invalidPopupManager;
    [SerializeField] private float invalidPopupDelay = 5f;
    private Coroutine invalidPopupRoutine;

    private bool currentNetIsValid;

    private int inferredDimensionA;
    private int inferredDimensionB;
    private int inferredDimensionC;

    public bool CurrentNetIsValid => currentNetIsValid;

    public int InferredDimensionA => inferredDimensionA;
    public int InferredDimensionB => inferredDimensionB;
    public int InferredDimensionC => inferredDimensionC;

    private void Start()
    {
        ResetValidation();
    }

    public void ValidateCurrentNet()
    {
        ResetValidation();

        if (drawManager == null)
        {
            ShowInvalid("Drawing system is not connected.");
            return;
        }

        List<CuboidFace> faces = new List<CuboidFace>(drawManager.GetDetectedFaces());

        if (faces.Count != 6)
        {
            ShowInvalid($"Draw exactly 6 faces. Current faces: {faces.Count}.");
            return;
        }

        // STEP 1:
        // Automatically determine the cuboid dimensions
        // using the six rectangular face sizes.
        CuboidDimensionInferencer.InferenceResult inference =
            CuboidDimensionInferencer.InferDimensions(faces);

        if (!inference.IsValid)
        {
            ShowInvalid(inference.Message);
            return;
        }

        inferredDimensionA = inference.DimensionA;
        inferredDimensionB = inference.DimensionB;
        inferredDimensionC = inference.DimensionC;

        // STEP 2:
        // Reject overlapping faces in the 2D drawing.
        if (HasInteriorFaceOverlap(faces))
        {
            ShowInvalid("Some faces overlap. Draw each face in a separate grid area.");
            return;
        }

        // STEP 3:
        // Build face connections using complete shared edges.
        if (!TryBuildAdjacencyGraph(
                faces,
                out List<List<FaceConnection>> graph,
                out int connectionCount,
                out string adjacencyMessage))
        {
            ShowInvalid(adjacencyMessage);
            return;
        }

        // A six-face net must form a fold tree.
        // Six nodes require five fold connections.
        if (connectionCount != 5)
        {
            ShowInvalid(
                "The faces are not arranged as one cuboid net. " +
                $"Detected fold connections: {connectionCount}."
            );
            return;
        }

        // STEP 4:
        // All faces must belong to one connected net.
        if (!IsGraphConnected(graph))
        {
            ShowInvalid("All 6 faces must be connected.");
            return;
        }

        // STEP 5:
        // Simulate 90-degree folding into 3D.
        if (!ValidateFolding(
                faces,
                graph,
                inferredDimensionA,
                inferredDimensionB,
                inferredDimensionC,
                out string foldingMessage))
        {
            ShowInvalid(foldingMessage);
            return;
        }

        // Cancel previous delay and hide stale popup if net is now valid
        if (invalidPopupRoutine != null)
        {
            StopCoroutine(invalidPopupRoutine);
            invalidPopupRoutine = null;
        }

        if (invalidPopupManager != null)
        {
            invalidPopupManager.HideInvalidPopup();
        }

        currentNetIsValid = true;

        // Preserve and use the valid message for both status text and logging
        string validMessage = $"Excellent! This net folds into a " +
                              $"{inferredDimensionA} × " +
                              $"{inferredDimensionB} × " +
                              $"{inferredDimensionC} cuboid.";

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance.LogValidation(
                "Cuboid",
                true,
                validMessage,
                faces.Count
            );
        }

        if (statusText != null)
        {
            statusText.text = validMessage;
        }

        drawManager.ShowValidationResult(true);

        if (create3DButton != null)
            create3DButton.interactable = true;

        Debug.Log(
            $"Valid cuboid net. Dimensions: " +
            $"{inferredDimensionA} × " +
            $"{inferredDimensionB} × " +
            $"{inferredDimensionC}"
        );
    }

    public void ResetValidation()
    {
        if (invalidPopupRoutine != null)
        {
            StopCoroutine(invalidPopupRoutine);
            invalidPopupRoutine = null;
        }

        currentNetIsValid = false;

        inferredDimensionA = 0;
        inferredDimensionB = 0;
        inferredDimensionC = 0;

        if (create3DButton != null)
            create3DButton.interactable = false;
    }

    private void ShowInvalid(string message)
    {
        currentNetIsValid = false;

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance.LogValidation(
                "Cuboid",
                false,
                message,
                drawManager != null
                    ? drawManager.GetDetectedFaceCount()
                    : 0
            );
        }

        inferredDimensionA = 0;
        inferredDimensionB = 0;
        inferredDimensionC = 0;

        // ----------------------------------------------
        // SHOW ACTUAL ERROR FIRST
        // ----------------------------------------------

        if (statusText != null)
        {
            statusText.text = message;
        }

        // ----------------------------------------------
        // INVALID FACE COLORING
        // ----------------------------------------------

        if (drawManager != null)
        {
            drawManager.ShowValidationResult(false);
        }

        // ----------------------------------------------
        // CREATE 3D DISABLED
        // ----------------------------------------------

        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }

        // ----------------------------------------------
        // CANCEL PREVIOUS DELAY
        // ----------------------------------------------

        if (invalidPopupRoutine != null)
        {
            StopCoroutine(invalidPopupRoutine);
        }

        // ----------------------------------------------
        // OPEN POPUP AFTER DELAY
        // ----------------------------------------------

        invalidPopupRoutine = StartCoroutine(ShowInvalidPopupAfterDelay(message));

        Debug.LogWarning("Cuboid net INVALID: " + message);
    }

    private IEnumerator ShowInvalidPopupAfterDelay(string message)
    {
        yield return new WaitForSeconds(invalidPopupDelay);

        if (invalidPopupManager != null)
        {
            invalidPopupManager.ShowInvalidCuboidPopup(message);
        }
        else
        {
            Debug.LogError("CuboidInvalidPopupManager is not assigned.");
        }

        invalidPopupRoutine = null;
    }

    private static bool HasInteriorFaceOverlap(List<CuboidFace> faces)
    {
        for (int firstIndex = 0; firstIndex < faces.Count; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1; secondIndex < faces.Count; secondIndex++)
            {
                if (FacesHaveInteriorOverlap(faces[firstIndex], faces[secondIndex]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool FacesHaveInteriorOverlap(CuboidFace first, CuboidFace second)
    {
        int firstLeft = first.bottomLeft.x;
        int firstRight = first.bottomLeft.x + first.gridWidth;

        int firstBottom = first.bottomLeft.y;
        int firstTop = first.bottomLeft.y + first.gridHeight;

        int secondLeft = second.bottomLeft.x;
        int secondRight = second.bottomLeft.x + second.gridWidth;

        int secondBottom = second.bottomLeft.y;
        int secondTop = second.bottomLeft.y + second.gridHeight;

        bool overlapX = firstLeft < secondRight && firstRight > secondLeft;
        bool overlapY = firstBottom < secondTop && firstTop > secondBottom;

        return overlapX && overlapY;
    }

    private static bool TryBuildAdjacencyGraph(
        List<CuboidFace> faces,
        out List<List<FaceConnection>> graph,
        out int connectionCount,
        out string message)
    {
        graph = new List<List<FaceConnection>>();
        connectionCount = 0;

        for (int index = 0; index < faces.Count; index++)
        {
            graph.Add(new List<FaceConnection>());
        }

        for (int firstIndex = 0; firstIndex < faces.Count; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1; secondIndex < faces.Count; secondIndex++)
            {
                ConnectionCheckResult result = CheckConnection(
                    faces[firstIndex],
                    faces[secondIndex],
                    out FaceSide sideFromFirst,
                    out FaceSide sideFromSecond
                );

                if (result == ConnectionCheckResult.PartialEdgeContact)
                {
                    message = "Faces must join using a complete matching edge.";
                    return false;
                }

                if (result != ConnectionCheckResult.FullEdgeConnection)
                {
                    continue;
                }

                graph[firstIndex].Add(new FaceConnection(secondIndex, sideFromFirst));
                graph[secondIndex].Add(new FaceConnection(firstIndex, sideFromSecond));

                connectionCount++;
            }
        }

        message = string.Empty;
        return true;
    }

    private static ConnectionCheckResult CheckConnection(
        CuboidFace first,
        CuboidFace second,
        out FaceSide sideFromFirst,
        out FaceSide sideFromSecond)
    {
        sideFromFirst = FaceSide.None;
        sideFromSecond = FaceSide.None;

        int firstLeft = first.bottomLeft.x;
        int firstRight = first.bottomLeft.x + first.gridWidth;

        int firstBottom = first.bottomLeft.y;
        int firstTop = first.bottomLeft.y + first.gridHeight;

        int secondLeft = second.bottomLeft.x;
        int secondRight = second.bottomLeft.x + second.gridWidth;

        int secondBottom = second.bottomLeft.y;
        int secondTop = second.bottomLeft.y + second.gridHeight;

        // Second face is on the right side.
        if (firstRight == secondLeft)
        {
            int overlapLength = IntervalOverlapLength(firstBottom, firstTop, secondBottom, secondTop);

            if (overlapLength > 0)
            {
                int firstEdgeLength = firstTop - firstBottom;
                int secondEdgeLength = secondTop - secondBottom;

                if (overlapLength == firstEdgeLength && overlapLength == secondEdgeLength)
                {
                    sideFromFirst = FaceSide.Right;
                    sideFromSecond = FaceSide.Left;
                    return ConnectionCheckResult.FullEdgeConnection;
                }

                return ConnectionCheckResult.PartialEdgeContact;
            }
        }

        // Second face is on the left side.
        if (firstLeft == secondRight)
        {
            int overlapLength = IntervalOverlapLength(firstBottom, firstTop, secondBottom, secondTop);

            if (overlapLength > 0)
            {
                int firstEdgeLength = firstTop - firstBottom;
                int secondEdgeLength = secondTop - secondBottom;

                if (overlapLength == firstEdgeLength && overlapLength == secondEdgeLength)
                {
                    sideFromFirst = FaceSide.Left;
                    sideFromSecond = FaceSide.Right;
                    return ConnectionCheckResult.FullEdgeConnection;
                }

                return ConnectionCheckResult.PartialEdgeContact;
            }
        }

        // Second face is above.
        if (firstTop == secondBottom)
        {
            int overlapLength = IntervalOverlapLength(firstLeft, firstRight, secondLeft, secondRight);

            if (overlapLength > 0)
            {
                int firstEdgeLength = firstRight - firstLeft;
                int secondEdgeLength = secondRight - secondLeft;

                if (overlapLength == firstEdgeLength && overlapLength == secondEdgeLength)
                {
                    sideFromFirst = FaceSide.Top;
                    sideFromSecond = FaceSide.Bottom;
                    return ConnectionCheckResult.FullEdgeConnection;
                }

                return ConnectionCheckResult.PartialEdgeContact;
            }
        }

        // Second face is below.
        if (firstBottom == secondTop)
        {
            int overlapLength = IntervalOverlapLength(firstLeft, firstRight, secondLeft, secondRight);

            if (overlapLength > 0)
            {
                int firstEdgeLength = firstRight - firstLeft;
                int secondEdgeLength = secondRight - secondLeft;

                if (overlapLength == firstEdgeLength && overlapLength == secondEdgeLength)
                {
                    sideFromFirst = FaceSide.Bottom;
                    sideFromSecond = FaceSide.Top;
                    return ConnectionCheckResult.FullEdgeConnection;
                }

                return ConnectionCheckResult.PartialEdgeContact;
            }
        }

        return ConnectionCheckResult.NoConnection;
    }

    private static int IntervalOverlapLength(int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        return Mathf.Max(
            0,
            Mathf.Min(firstEnd, secondEnd) - Mathf.Max(firstStart, secondStart)
        );
    }

    private static bool IsGraphConnected(List<List<FaceConnection>> graph)
    {
        if (graph == null || graph.Count != 6)
            return false;

        bool[] visited = new bool[graph.Count];
        Queue<int> queue = new Queue<int>();

        visited[0] = true;
        queue.Enqueue(0);

        int visitedCount = 0;

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();
            visitedCount++;

            foreach (FaceConnection connection in graph[currentIndex])
            {
                int neighbourIndex = connection.neighbourIndex;

                if (visited[neighbourIndex])
                    continue;

                visited[neighbourIndex] = true;
                queue.Enqueue(neighbourIndex);
            }
        }

        return visitedCount == graph.Count;
    }

    private static bool ValidateFolding(
        List<CuboidFace> faces,
        List<List<FaceConnection>> graph,
        int dimensionA,
        int dimensionB,
        int dimensionC,
        out string message)
    {
        FoldFrame?[] foldFrames = new FoldFrame?[faces.Count];
        Queue<int> queue = new Queue<int>();

        // Root face lies in the XY plane.
        foldFrames[0] = new FoldFrame(Vector3Int.right, Vector3Int.up, Vector3Int.forward);
        queue.Enqueue(0);

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();
            FoldFrame currentFrame = foldFrames[currentIndex].Value;

            foreach (FaceConnection connection in graph[currentIndex])
            {
                FoldFrame neighbourFrame = FoldFrameAcrossSide(currentFrame, connection.side);
                int neighbourIndex = connection.neighbourIndex;

                if (!foldFrames[neighbourIndex].HasValue)
                {
                    foldFrames[neighbourIndex] = neighbourFrame;
                    queue.Enqueue(neighbourIndex);
                    continue;
                }

                if (!foldFrames[neighbourIndex].Value.Equals(neighbourFrame))
                {
                    message = "This arrangement cannot fold consistently.";
                    return false;
                }
            }
        }

        HashSet<Vector3Int> usedNormals = new HashSet<Vector3Int>();

        for (int index = 0; index < foldFrames.Length; index++)
        {
            if (!foldFrames[index].HasValue)
            {
                message = "One or more faces are disconnected.";
                return false;
            }

            Vector3Int normal = foldFrames[index].Value.normal;

            if (!usedNormals.Add(normal))
            {
                message = "Two faces overlap when the net is folded.";
                return false;
            }
        }

        Vector3Int[] requiredNormals =
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

        foreach (Vector3Int requiredNormal in requiredNormals)
        {
            if (!usedNormals.Contains(requiredNormal))
            {
                message = "The net does not close into a cuboid.";
                return false;
            }
        }

        // Check that each folded face has a valid pair of cuboid dimensions.
        List<FaceSize> allowedSizes = BuildAllowedFaceSizes(dimensionA, dimensionB, dimensionC);

        foreach (CuboidFace face in faces)
        {
            FaceSize faceSize = new FaceSize(face.gridWidth, face.gridHeight);
            bool sizeAllowed = false;

            foreach (FaceSize allowedSize in allowedSizes)
            {
                if (faceSize.Equals(allowedSize))
                {
                    sizeAllowed = true;
                    break;
                }
            }

            if (!sizeAllowed)
            {
                message = "A face has dimensions that do not belong to the detected cuboid.";
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private static List<FaceSize> BuildAllowedFaceSizes(int dimensionA, int dimensionB, int dimensionC)
    {
        List<FaceSize> allowedSizes = new List<FaceSize>();

        AddUniqueSize(allowedSizes, new FaceSize(dimensionA, dimensionB));
        AddUniqueSize(allowedSizes, new FaceSize(dimensionA, dimensionC));
        AddUniqueSize(allowedSizes, new FaceSize(dimensionB, dimensionC));

        return allowedSizes;
    }

    private static void AddUniqueSize(List<FaceSize> sizes, FaceSize candidate)
    {
        foreach (FaceSize existing in sizes)
        {
            if (existing.Equals(candidate))
                return;
        }

        sizes.Add(candidate);
    }

    private static FoldFrame FoldFrameAcrossSide(FoldFrame current, FaceSide side)
    {
        switch (side)
        {
            case FaceSide.Right:
                return new FoldFrame(-current.normal, current.up, current.right);
            case FaceSide.Left:
                return new FoldFrame(current.normal, current.up, -current.right);
            case FaceSide.Top:
                return new FoldFrame(current.right, -current.normal, current.up);
            case FaceSide.Bottom:
                return new FoldFrame(current.right, current.normal, -current.up);
            default:
                return current;
        }
    }

    // ==================================================
    // VALIDATE A CANDIDATE NET FOR CORRECTION SYSTEM
    // ==================================================
    public static bool ValidateCandidateFaces(
        IReadOnlyList<CuboidFace> sourceFaces,
        out string message)
    {
        if (sourceFaces == null ||
            sourceFaces.Count != 6)
        {
            message =
                "Exactly 6 faces are required.";

            return false;
        }

        List<CuboidFace> faces =
            new List<CuboidFace>();

        foreach (CuboidFace face
                 in sourceFaces)
        {
            if (face == null)
            {
                message =
                    "A face is missing.";

                return false;
            }

            faces.Add(
                new CuboidFace(
                    face.bottomLeft,
                    face.gridWidth,
                    face.gridHeight,
                    face.faceType
                )
            );
        }


        // ----------------------------------------------
        // 1. DIMENSIONS
        // ----------------------------------------------

        CuboidDimensionInferencer.InferenceResult
            inference =
                CuboidDimensionInferencer
                    .InferDimensions(
                        faces
                    );

        if (!inference.IsValid)
        {
            message =
                inference.Message;

            return false;
        }


        // ----------------------------------------------
        // 2. OVERLAP
        // ----------------------------------------------

        if (HasInteriorFaceOverlap(
                faces))
        {
            message =
                "Some faces overlap.";

            return false;
        }


        // ----------------------------------------------
        // 3. CONNECTION GRAPH
        // ----------------------------------------------

        if (!TryBuildAdjacencyGraph(
                faces,
                out List<List<FaceConnection>> graph,
                out int connectionCount,
                out string adjacencyMessage))
        {
            message =
                adjacencyMessage;

            return false;
        }


        if (connectionCount != 5)
        {
            message =
                "The net does not have exactly 5 folding connections.";

            return false;
        }


        // ----------------------------------------------
        // 4. CONNECTIVITY
        // ----------------------------------------------

        if (!IsGraphConnected(
                graph))
        {
            message =
                "All 6 faces are not connected.";

            return false;
        }


        // ----------------------------------------------
        // 5. ACTUAL 3D FOLD TEST
        // ----------------------------------------------

        if (!ValidateFolding(
                faces,
                graph,
                inference.DimensionA,
                inference.DimensionB,
                inference.DimensionC,
                out string foldingMessage))
        {
            message =
                foldingMessage;

            return false;
        }


        message =
            $"Valid correction: " +
            $"{inference.DimensionA} × " +
            $"{inference.DimensionB} × " +
            $"{inference.DimensionC} cuboid.";

        return true;
    }

    public Vector3Int GetInferredDimensions()
    {
        return new Vector3Int(inferredDimensionA, inferredDimensionB, inferredDimensionC);
    }

    private readonly struct FaceConnection
    {
        public readonly int neighbourIndex;
        public readonly FaceSide side;

        public FaceConnection(int neighbourIndex, FaceSide side)
        {
            this.neighbourIndex = neighbourIndex;
            this.side = side;
        }
    }

    private readonly struct FoldFrame
    {
        public readonly Vector3Int right;
        public readonly Vector3Int up;
        public readonly Vector3Int normal;

        public FoldFrame(Vector3Int right, Vector3Int up, Vector3Int normal)
        {
            this.right = right;
            this.up = up;
            this.normal = normal;
        }

        public bool Equals(FoldFrame other)
        {
            return right == other.right && up == other.up && normal == other.normal;
        }
    }

    private readonly struct FaceSize
    {
        private readonly int smaller;
        private readonly int larger;

        public FaceSize(int first, int second)
        {
            smaller = Mathf.Min(first, second);
            larger = Mathf.Max(first, second);
        }

        public bool Equals(FaceSize other)
        {
            return smaller == other.smaller && larger == other.larger;
        }
    }

    private enum FaceSide
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    private enum ConnectionCheckResult
    {
        NoConnection,
        FullEdgeConnection,
        PartialEdgeContact
    }
}
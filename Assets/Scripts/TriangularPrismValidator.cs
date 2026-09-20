using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TriangularPrismValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PrismShapeDetector shapeDetector;

    [SerializeField]
    private PrismShapeVisualizer shapeVisualizer;

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Button create3DButton;

    [Header("Validation")]
    [SerializeField]
    private bool requireConnectedNet = true;

    [SerializeField]
    private bool requireTreeStructure = true;

    private bool isValidPrismNet;

    public bool IsValidPrismNet =>
        isValidPrismNet;

    private void Awake()
    {
        if (shapeDetector == null)
        {
            shapeDetector =
                GetComponent<PrismShapeDetector>();
        }

        if (shapeVisualizer == null)
        {
            shapeVisualizer =
                GetComponent<PrismShapeVisualizer>();
        }

        SetCreate3DButton(false);
    }

    public void ValidatePrism()
    {
        isValidPrismNet = false;
        SetCreate3DButton(false);

        if (shapeDetector == null)
        {
            ShowInvalid(
                "Shape detector is not assigned."
            );

            return;
        }

        /*
         * Detect again so the validation always uses
         * the latest drawing.
         */
        shapeDetector.DetectShapes();

        if (shapeVisualizer != null)
        {
            shapeVisualizer
                .VisualizeDetectedShapes();
        }

        IReadOnlyList<PrismFace> detectedFaces =
            shapeDetector.DetectedFaces;

        List<PrismFace> rectangles =
            GetFacesByType(
                detectedFaces,
                PrismFaceType.Rectangle
            );

        List<PrismFace> triangles =
            GetFacesByType(
                detectedFaces,
                PrismFaceType.Triangle
            );

        /*
         * 1. Exact face count.
         */
        if (rectangles.Count != 3)
        {
            ShowInvalid(
                $"A triangular prism net requires " +
                $"exactly 3 rectangles. " +
                $"Detected: {rectangles.Count}."
            );

            return;
        }

        if (triangles.Count != 2)
        {
            ShowInvalid(
                $"A triangular prism net requires " +
                $"exactly 2 triangles. " +
                $"Detected: {triangles.Count}."
            );

            return;
        }

        /*
         * 2. Triangle congruence.
         */
        if (!AreTrianglesCongruent(
                triangles[0],
                triangles[1]))
        {
            ShowInvalid(
                "The two triangular faces must " +
                "have the same size and shape."
            );

            return;
        }

        /*
         * 3. Rectangle dimensions.
         */
        if (!DoRectanglesMatchTriangleSides(
                rectangles,
                triangles[0],
                out int prismDepth))
        {
            ShowInvalid(
                "The 3 rectangles must match the " +
                "3 triangle side lengths and share " +
                "one common prism depth."
            );

            return;
        }

        /*
         * 4. Face connectivity and net structure.
         */
        FaceConnectionResult connectionResult =
            AnalyzeFaceConnections(
                detectedFaces
            );

        if (connectionResult.HasInvalidTriangleConnection)
        {
            ShowInvalid(
                "Each triangle must connect to a " +
                "rectangle, not directly to the " +
                "other triangle."
            );

            return;
        }

        if (connectionResult.TriangleRectangleConnections
            != 2)
        {
            ShowInvalid(
                "Each triangle must share exactly " +
                "one complete edge with a rectangle."
            );

            return;
        }

        if (connectionResult.RectangleRectangleConnections
            != 2)
        {
            ShowInvalid(
                "The 3 rectangles must form one " +
                "connected strip."
            );

            return;
        }

        if (requireConnectedNet &&
            !connectionResult.AllFacesConnected)
        {
            ShowInvalid(
                "All 5 faces must be connected " +
                "as one prism net."
            );

            return;
        }

        if (requireTreeStructure &&
            connectionResult.TotalConnections != 4)
        {
            ShowInvalid(
                "The faces create an invalid loop " +
                "or an additional connection."
            );

            return;
        }

        /*
         * All checks passed.
         */
        isValidPrismNet = true;
        SetCreate3DButton(true);

        if (statusText != null)
        {
            statusText.text =
                $"Valid triangular prism net! " +
                $"Prism depth: {prismDepth} grid units.";
        }

        Debug.Log(
            "Triangular prism validation successful."
        );
    }

    private List<PrismFace> GetFacesByType(
        IReadOnlyList<PrismFace> faces,
        PrismFaceType requiredType)
    {
        List<PrismFace> results =
            new List<PrismFace>();

        if (faces == null)
            return results;

        foreach (PrismFace face in faces)
        {
            if (face == null)
                continue;

            if (face.FaceType == requiredType)
            {
                results.Add(face);
            }
        }

        return results;
    }

    private bool AreTrianglesCongruent(
        PrismFace firstTriangle,
        PrismFace secondTriangle)
    {
        if (firstTriangle == null ||
            secondTriangle == null)
        {
            return false;
        }

        if (firstTriangle.Vertices.Count != 3 ||
            secondTriangle.Vertices.Count != 3)
        {
            return false;
        }

        List<int> firstSideLengths =
            GetTriangleSquaredSideLengths(
                firstTriangle
            );

        List<int> secondSideLengths =
            GetTriangleSquaredSideLengths(
                secondTriangle
            );

        if (firstSideLengths.Count != 3 ||
            secondSideLengths.Count != 3)
        {
            return false;
        }

        firstSideLengths.Sort();
        secondSideLengths.Sort();

        for (int index = 0;
             index < 3;
             index++)
        {
            if (firstSideLengths[index] !=
                secondSideLengths[index])
            {
                return false;
            }
        }

        return true;
    }

    private List<int> GetTriangleSquaredSideLengths(
        PrismFace triangle)
    {
        List<int> lengths =
            new List<int>();

        if (triangle == null ||
            triangle.Vertices.Count != 3)
        {
            return lengths;
        }

        Vector2Int pointA =
            triangle.Vertices[0];

        Vector2Int pointB =
            triangle.Vertices[1];

        Vector2Int pointC =
            triangle.Vertices[2];

        lengths.Add(
            GetSquaredDistance(
                pointA,
                pointB
            )
        );

        lengths.Add(
            GetSquaredDistance(
                pointB,
                pointC
            )
        );

        lengths.Add(
            GetSquaredDistance(
                pointC,
                pointA
            )
        );

        return lengths;
    }

    private int GetSquaredDistance(
        Vector2Int pointA,
        Vector2Int pointB)
    {
        int deltaX =
            pointB.x - pointA.x;

        int deltaY =
            pointB.y - pointA.y;

        return
            deltaX * deltaX +
            deltaY * deltaY;
    }

    private bool DoRectanglesMatchTriangleSides(
        List<PrismFace> rectangles,
        PrismFace triangle,
        out int prismDepth)
    {
        prismDepth = 0;

        if (rectangles == null ||
            rectangles.Count != 3 ||
            triangle == null)
        {
            return false;
        }

        List<int> triangleSideSquares =
            GetTriangleSquaredSideLengths(
                triangle
            );

        triangleSideSquares.Sort();

        /*
         * Each rectangle has two dimensions.
         *
         * One dimension must equal the common
         * prism depth.
         *
         * The other dimensions must match the
         * triangle's three side lengths.
         *
         * Try all 2^3 rectangle orientations.
         */
        for (int orientationMask = 0;
             orientationMask < 8;
             orientationMask++)
        {
            List<int> selectedDepths =
                new List<int>();

            List<int> selectedTriangleSideSquares =
                new List<int>();

            bool validDimensions = true;

            for (int index = 0;
                 index < rectangles.Count;
                 index++)
            {
                PrismFace rectangle =
                    rectangles[index];

                int width =
                    rectangle.Width;

                int height =
                    rectangle.Height;

                if (width <= 0 ||
                    height <= 0)
                {
                    validDimensions = false;
                    break;
                }

                bool swapDimensions =
                    (orientationMask &
                     (1 << index)) != 0;

                int selectedDepth =
                    swapDimensions
                        ? height
                        : width;

                int selectedTriangleSide =
                    swapDimensions
                        ? width
                        : height;

                selectedDepths.Add(
                    selectedDepth
                );

                selectedTriangleSideSquares.Add(
                    selectedTriangleSide *
                    selectedTriangleSide
                );
            }

            if (!validDimensions)
                continue;

            if (selectedDepths[0] !=
                    selectedDepths[1] ||
                selectedDepths[1] !=
                    selectedDepths[2])
            {
                continue;
            }

            selectedTriangleSideSquares.Sort();

            bool sidesMatch = true;

            for (int sideIndex = 0;
                 sideIndex < 3;
                 sideIndex++)
            {
                if (selectedTriangleSideSquares[
                        sideIndex] !=
                    triangleSideSquares[sideIndex])
                {
                    sidesMatch = false;
                    break;
                }
            }

            if (!sidesMatch)
                continue;

            prismDepth = selectedDepths[0];
            return true;
        }

        return false;
    }

    private FaceConnectionResult
        AnalyzeFaceConnections(
            IReadOnlyList<PrismFace> faces)
    {
        FaceConnectionResult result =
            new FaceConnectionResult();

        if (faces == null ||
            faces.Count == 0)
        {
            return result;
        }

        Dictionary<int, List<int>>
            adjacency =
                new Dictionary<int, List<int>>();

        for (int index = 0;
             index < faces.Count;
             index++)
        {
            adjacency[index] =
                new List<int>();
        }

        for (int firstIndex = 0;
             firstIndex < faces.Count - 1;
             firstIndex++)
        {
            for (int secondIndex =
                     firstIndex + 1;
                 secondIndex < faces.Count;
                 secondIndex++)
            {
                PrismFace firstFace =
                    faces[firstIndex];

                PrismFace secondFace =
                    faces[secondIndex];

                if (!DoFacesShareCompleteEdge(
                        firstFace,
                        secondFace))
                {
                    continue;
                }

                result.TotalConnections++;

                adjacency[firstIndex].Add(
                    secondIndex
                );

                adjacency[secondIndex].Add(
                    firstIndex
                );

                bool firstTriangle =
                    firstFace.IsTriangle();

                bool secondTriangle =
                    secondFace.IsTriangle();

                if (firstTriangle &&
                    secondTriangle)
                {
                    result
                        .HasInvalidTriangleConnection =
                        true;
                }
                else if (firstTriangle ||
                         secondTriangle)
                {
                    result
                        .TriangleRectangleConnections++;
                }
                else
                {
                    result
                        .RectangleRectangleConnections++;
                }
            }
        }

        result.AllFacesConnected =
            IsGraphConnected(
                adjacency,
                faces.Count
            );

        /*
         * Each triangular end should attach by one
         * edge only.
         */
        for (int index = 0;
             index < faces.Count;
             index++)
        {
            if (!faces[index].IsTriangle())
                continue;

            if (adjacency[index].Count != 1)
            {
                result
                    .HasInvalidTriangleConnection =
                    true;
            }
        }

        return result;
    }

    private bool DoFacesShareCompleteEdge(
        PrismFace firstFace,
        PrismFace secondFace)
    {
        if (firstFace == null ||
            secondFace == null)
        {
            return false;
        }

        List<FaceEdge> firstEdges =
            GetFaceEdges(firstFace);

        List<FaceEdge> secondEdges =
            GetFaceEdges(secondFace);

        foreach (FaceEdge firstEdge
                 in firstEdges)
        {
            foreach (FaceEdge secondEdge
                     in secondEdges)
            {
                if (firstEdge.Equals(secondEdge))
                    return true;
            }
        }

        return false;
    }

    private List<FaceEdge> GetFaceEdges(
        PrismFace face)
    {
        List<FaceEdge> edges =
            new List<FaceEdge>();

        if (face == null ||
            face.Vertices.Count < 3)
        {
            return edges;
        }

        IReadOnlyList<Vector2Int> vertices =
            face.Vertices;

        for (int index = 0;
             index < vertices.Count;
             index++)
        {
            Vector2Int start =
                vertices[index];

            Vector2Int end =
                vertices[
                    (index + 1) %
                    vertices.Count
                ];

            edges.Add(
                new FaceEdge(
                    start,
                    end
                )
            );
        }

        return edges;
    }

    private bool IsGraphConnected(
        Dictionary<int, List<int>> adjacency,
        int faceCount)
    {
        if (faceCount == 0)
            return false;

        HashSet<int> visited =
            new HashSet<int>();

        Queue<int> queue =
            new Queue<int>();

        queue.Enqueue(0);
        visited.Add(0);

        while (queue.Count > 0)
        {
            int current =
                queue.Dequeue();

            foreach (int neighbour
                     in adjacency[current])
            {
                if (visited.Contains(neighbour))
                    continue;

                visited.Add(neighbour);
                queue.Enqueue(neighbour);
            }
        }

        return visited.Count == faceCount;
    }

    private void ShowInvalid(
        string message)
    {
        isValidPrismNet = false;
        SetCreate3DButton(false);

        if (statusText != null)
        {
            statusText.text =
                $"Invalid prism net: {message}";
        }

        Debug.LogWarning(
            $"Invalid prism net: {message}"
        );
    }

    private void SetCreate3DButton(
        bool interactable)
    {
        if (create3DButton != null)
        {
            create3DButton.interactable =
                interactable;
        }
    }

    public void InvalidateCurrentResult()
    {
        isValidPrismNet = false;
        SetCreate3DButton(false);
    }

    [Serializable]
    private struct FaceEdge :
        IEquatable<FaceEdge>
    {
        public Vector2Int PointA;
        public Vector2Int PointB;

        public FaceEdge(
            Vector2Int firstPoint,
            Vector2Int secondPoint)
        {
            if (ComparePoints(
                    firstPoint,
                    secondPoint) <= 0)
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
            FaceEdge other)
        {
            return PointA == other.PointA &&
                   PointB == other.PointB;
        }

        public override bool Equals(
            object obj)
        {
            return obj is FaceEdge other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    PointA.GetHashCode() * 397 ^
                    PointB.GetHashCode();
            }
        }

        private static int ComparePoints(
            Vector2Int first,
            Vector2Int second)
        {
            int xComparison =
                first.x.CompareTo(second.x);

            if (xComparison != 0)
                return xComparison;

            return first.y.CompareTo(
                second.y
            );
        }
    }

    private class FaceConnectionResult
    {
        public int TotalConnections;

        public int TriangleRectangleConnections;

        public int RectangleRectangleConnections;

        public bool AllFacesConnected;

        public bool HasInvalidTriangleConnection;
    }
}
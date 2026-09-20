using System.Collections.Generic;
using UnityEngine;

public static class CuboidDimensionInferencer
{
    public readonly struct InferenceResult
    {
        public bool IsValid { get; }
        public int DimensionA { get; }
        public int DimensionB { get; }
        public int DimensionC { get; }
        public string Message { get; }

        public InferenceResult(
            bool isValid,
            int dimensionA,
            int dimensionB,
            int dimensionC,
            string message)
        {
            IsValid = isValid;
            DimensionA = dimensionA;
            DimensionB = dimensionB;
            DimensionC = dimensionC;
            Message = message;
        }

        public Vector3Int GetDimensions()
        {
            return new Vector3Int(
                DimensionA,
                DimensionB,
                DimensionC
            );
        }
    }

    public static InferenceResult InferDimensions(
        IReadOnlyList<CuboidFace> faces)
    {
        if (faces == null || faces.Count != 6)
        {
            int faceCount = faces == null ? 0 : faces.Count;

            return Invalid(
                $"Exactly 6 faces are required. Current faces: {faceCount}."
            );
        }

        Dictionary<FaceSize, int> sizeCounts =
            new Dictionary<FaceSize, int>();

        HashSet<int> uniqueEdgeLengths =
            new HashSet<int>();

        foreach (CuboidFace face in faces)
        {
            if (face == null ||
                face.gridWidth <= 0 ||
                face.gridHeight <= 0)
            {
                return Invalid(
                    "One or more detected faces have invalid dimensions."
                );
            }

            FaceSize size = new FaceSize(
                face.gridWidth,
                face.gridHeight
            );

            if (!sizeCounts.ContainsKey(size))
                sizeCounts[size] = 0;

            sizeCounts[size]++;

            uniqueEdgeLengths.Add(face.gridWidth);
            uniqueEdgeLengths.Add(face.gridHeight);
        }

        /*
         * A cuboid may have:
         *
         * 3 distinct dimensions:
         * A×B, A×C, B×C — each appears twice.
         *
         * 2 distinct dimensions:
         * Example A×A×B.
         * A×A appears twice and A×B appears four times.
         *
         * 1 distinct dimension:
         * Cube. This must be rejected in the Cuboid activity.
         */

        if (uniqueEdgeLengths.Count == 1)
        {
            return Invalid(
                "This pattern forms a cube. Please use the Cube activity."
            );
        }

        if (uniqueEdgeLengths.Count > 3)
        {
            return Invalid(
                "The face sizes do not belong to one cuboid."
            );
        }

        List<int> lengths =
            new List<int>(uniqueEdgeLengths);

        lengths.Sort();

        if (lengths.Count == 2)
        {
            return InferTwoDimensionCuboid(
                sizeCounts,
                lengths[0],
                lengths[1]
            );
        }

        if (lengths.Count == 3)
        {
            return InferThreeDimensionCuboid(
                sizeCounts,
                lengths[0],
                lengths[1],
                lengths[2]
            );
        }

        return Invalid(
            "Unable to determine the cuboid dimensions."
        );
    }

    private static InferenceResult InferTwoDimensionCuboid(
        Dictionary<FaceSize, int> sizeCounts,
        int smaller,
        int larger)
    {
        FaceSize squareSmall =
            new FaceSize(smaller, smaller);

        FaceSize rectangle =
            new FaceSize(smaller, larger);

        FaceSize squareLarge =
            new FaceSize(larger, larger);

        /*
         * Possible cuboids:
         *
         * smaller × smaller × larger
         * Required:
         * 2 small squares
         * 4 smaller/larger rectangles
         *
         * OR
         *
         * smaller × larger × larger
         * Required:
         * 2 large squares
         * 4 smaller/larger rectangles
         */

        bool firstPattern =
            GetCount(sizeCounts, squareSmall) == 2 &&
            GetCount(sizeCounts, rectangle) == 4 &&
            GetCount(sizeCounts, squareLarge) == 0 &&
            sizeCounts.Count == 2;

        if (firstPattern)
        {
            return Valid(
                smaller,
                smaller,
                larger
            );
        }

        bool secondPattern =
            GetCount(sizeCounts, squareLarge) == 2 &&
            GetCount(sizeCounts, rectangle) == 4 &&
            GetCount(sizeCounts, squareSmall) == 0 &&
            sizeCounts.Count == 2;

        if (secondPattern)
        {
            return Valid(
                smaller,
                larger,
                larger
            );
        }

        return Invalid(
            "The six face sizes do not form the three matching face pairs of a cuboid."
        );
    }

    private static InferenceResult InferThreeDimensionCuboid(
        Dictionary<FaceSize, int> sizeCounts,
        int a,
        int b,
        int c)
    {
        FaceSize ab = new FaceSize(a, b);
        FaceSize ac = new FaceSize(a, c);
        FaceSize bc = new FaceSize(b, c);

        bool valid =
            sizeCounts.Count == 3 &&
            GetCount(sizeCounts, ab) == 2 &&
            GetCount(sizeCounts, ac) == 2 &&
            GetCount(sizeCounts, bc) == 2;

        if (!valid)
        {
            return Invalid(
                "The cuboid needs two matching faces of each rectangle size."
            );
        }

        return Valid(a, b, c);
    }

    private static int GetCount(
        Dictionary<FaceSize, int> sizeCounts,
        FaceSize size)
    {
        return sizeCounts.TryGetValue(
            size,
            out int count
        )
            ? count
            : 0;
    }

    private static InferenceResult Valid(
        int a,
        int b,
        int c)
    {
        return new InferenceResult(
            true,
            a,
            b,
            c,
            $"Cuboid dimensions detected: {a} × {b} × {c}."
        );
    }

    private static InferenceResult Invalid(
        string message)
    {
        return new InferenceResult(
            false,
            0,
            0,
            0,
            message
        );
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

        public override bool Equals(object obj)
        {
            return obj is FaceSize other &&
                   smaller == other.smaller &&
                   larger == other.larger;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (smaller * 397) ^ larger;
            }
        }
    }
}
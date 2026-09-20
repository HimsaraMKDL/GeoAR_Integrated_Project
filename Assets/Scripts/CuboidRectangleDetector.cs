using System.Collections.Generic;
using UnityEngine;

public static class CuboidRectangleDetector
{
    public static List<CuboidFace> DetectFaces(
        HashSet<CuboidGridEdge> drawnEdges,
        int gridWidth,
        int gridHeight)
    {
        List<CuboidFace> detectedFaces =
            new List<CuboidFace>();

        if (drawnEdges == null ||
            drawnEdges.Count == 0 ||
            gridWidth <= 0 ||
            gridHeight <= 0)
        {
            return detectedFaces;
        }

        for (int left = 0; left < gridWidth; left++)
        {
            for (int bottom = 0;
                 bottom < gridHeight;
                 bottom++)
            {
                for (int right = left + 1;
                     right <= gridWidth;
                     right++)
                {
                    for (int top = bottom + 1;
                         top <= gridHeight;
                         top++)
                    {
                        Vector2Int bottomLeft =
                            new Vector2Int(left, bottom);

                        int rectangleWidth = right - left;
                        int rectangleHeight = top - bottom;

                        if (!HasCompletePerimeter(
                                drawnEdges,
                                bottomLeft,
                                rectangleWidth,
                                rectangleHeight))
                        {
                            continue;
                        }

                        /*
                         * Reject composite rectangles.
                         *
                         * Example:
                         * Two adjacent faces may create the outer perimeter
                         * of one larger rectangle. Their shared boundary lies
                         * inside that larger rectangle, so the large candidate
                         * must not be treated as a separate face.
                         */
                        if (HasAnyInternalEdge(
                                drawnEdges,
                                bottomLeft,
                                rectangleWidth,
                                rectangleHeight))
                        {
                            continue;
                        }

                        detectedFaces.Add(
                            new CuboidFace(
                                bottomLeft,
                                rectangleWidth,
                                rectangleHeight,
                                CuboidFaceType.Unknown
                            )
                        );
                    }
                }
            }
        }

        RemoveDuplicateFaces(detectedFaces);
        SortFaces(detectedFaces);

        return detectedFaces;
    }

    private static bool HasCompletePerimeter(
        HashSet<CuboidGridEdge> drawnEdges,
        Vector2Int bottomLeft,
        int width,
        int height)
    {
        int left = bottomLeft.x;
        int right = bottomLeft.x + width;
        int bottom = bottomLeft.y;
        int top = bottomLeft.y + height;

        for (int x = left; x < right; x++)
        {
            CuboidGridEdge bottomEdge =
                new CuboidGridEdge(
                    new Vector2Int(x, bottom),
                    new Vector2Int(x + 1, bottom)
                );

            CuboidGridEdge topEdge =
                new CuboidGridEdge(
                    new Vector2Int(x, top),
                    new Vector2Int(x + 1, top)
                );

            if (!drawnEdges.Contains(bottomEdge) ||
                !drawnEdges.Contains(topEdge))
            {
                return false;
            }
        }

        for (int y = bottom; y < top; y++)
        {
            CuboidGridEdge leftEdge =
                new CuboidGridEdge(
                    new Vector2Int(left, y),
                    new Vector2Int(left, y + 1)
                );

            CuboidGridEdge rightEdge =
                new CuboidGridEdge(
                    new Vector2Int(right, y),
                    new Vector2Int(right, y + 1)
                );

            if (!drawnEdges.Contains(leftEdge) ||
                !drawnEdges.Contains(rightEdge))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasAnyInternalEdge(
        HashSet<CuboidGridEdge> drawnEdges,
        Vector2Int bottomLeft,
        int width,
        int height)
    {
        int left = bottomLeft.x;
        int right = bottomLeft.x + width;
        int bottom = bottomLeft.y;
        int top = bottomLeft.y + height;

        // Internal vertical unit edges.
        for (int x = left + 1; x < right; x++)
        {
            for (int y = bottom; y < top; y++)
            {
                CuboidGridEdge internalEdge =
                    new CuboidGridEdge(
                        new Vector2Int(x, y),
                        new Vector2Int(x, y + 1)
                    );

                if (drawnEdges.Contains(internalEdge))
                    return true;
            }
        }

        // Internal horizontal unit edges.
        for (int y = bottom + 1; y < top; y++)
        {
            for (int x = left; x < right; x++)
            {
                CuboidGridEdge internalEdge =
                    new CuboidGridEdge(
                        new Vector2Int(x, y),
                        new Vector2Int(x + 1, y)
                    );

                if (drawnEdges.Contains(internalEdge))
                    return true;
            }
        }

        return false;
    }

    private static void RemoveDuplicateFaces(
        List<CuboidFace> faces)
    {
        for (int i = faces.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if (!faces[i].HasSameBounds(faces[j]))
                    continue;

                faces.RemoveAt(i);
                break;
            }
        }
    }

    private static void SortFaces(
        List<CuboidFace> faces)
    {
        faces.Sort((first, second) =>
        {
            int yComparison =
                second.bottomLeft.y.CompareTo(
                    first.bottomLeft.y
                );

            if (yComparison != 0)
                return yComparison;

            int xComparison =
                first.bottomLeft.x.CompareTo(
                    second.bottomLeft.x
                );

            if (xComparison != 0)
                return xComparison;

            int areaComparison =
                first.Area.CompareTo(second.Area);

            return areaComparison;
        });
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismShapeDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PrismDrawManager drawManager;

    [SerializeField]
    private Text statusText;

    [Header("Detection Settings")]
    [SerializeField]
    private bool detectRectangles = true;

    [SerializeField]
    private bool detectTriangles = true;

    [SerializeField]
    private int maximumShapeSize = 12;

    private readonly List<PrismFace>
        detectedFaces =
            new List<PrismFace>();

    private readonly HashSet<string>
        detectedShapeKeys =
            new HashSet<string>();

    public IReadOnlyList<PrismFace>
        DetectedFaces => detectedFaces;

    public int RectangleCount
    {
        get
        {
            int count = 0;

            foreach (PrismFace face
                     in detectedFaces)
            {
                if (face.IsRectangle())
                    count++;
            }

            return count;
        }
    }

    public int TriangleCount
    {
        get
        {
            int count = 0;

            foreach (PrismFace face
                     in detectedFaces)
            {
                if (face.IsTriangle())
                    count++;
            }

            return count;
        }
    }

    private void Awake()
    {
        if (drawManager == null)
        {
            drawManager =
                GetComponent<PrismDrawManager>();
        }
    }

    public void DetectShapes()
    {
        detectedFaces.Clear();
        detectedShapeKeys.Clear();

        if (drawManager == null)
        {
            Debug.LogError(
                "PrismShapeDetector: " +
                "PrismDrawManager is not assigned."
            );

            return;
        }

        HashSet<PrismGridEdge> edgeSet =
            new HashSet<PrismGridEdge>(
                drawManager.GetDrawnEdges()
            );

        if (edgeSet.Count == 0)
        {
            UpdateStatusText();
            return;
        }

        HashSet<Vector2Int> points =
            CollectGridPoints(edgeSet);

        if (detectRectangles)
        {
            DetectAllRectangles(
                points,
                edgeSet
            );
        }

        if (detectTriangles)
        {
            DetectAllTriangles(
                points,
                edgeSet
            );
        }

        UpdateStatusText();

        Debug.Log(
            $"Prism shape detection complete. " +
            $"Rectangles: {RectangleCount}, " +
            $"Triangles: {TriangleCount}"
        );
    }

    private HashSet<Vector2Int>
        CollectGridPoints(
            HashSet<PrismGridEdge> edgeSet)
    {
        HashSet<Vector2Int> points =
            new HashSet<Vector2Int>();

        foreach (PrismGridEdge edge
                 in edgeSet)
        {
            points.Add(edge.PointA);
            points.Add(edge.PointB);
        }

        return points;
    }

    private void DetectAllRectangles(
        HashSet<Vector2Int> points,
        HashSet<PrismGridEdge> edges)
    {
        List<Vector2Int> pointList =
            new List<Vector2Int>(points);

        foreach (Vector2Int bottomLeft
                 in pointList)
        {
            foreach (Vector2Int topRight
                     in pointList)
            {
                if (topRight.x <= bottomLeft.x ||
                    topRight.y <= bottomLeft.y)
                {
                    continue;
                }

                int width =
                    topRight.x - bottomLeft.x;

                int height =
                    topRight.y - bottomLeft.y;

                if (width > maximumShapeSize ||
                    height > maximumShapeSize)
                {
                    continue;
                }

                Vector2Int bottomRight =
                    new Vector2Int(
                        topRight.x,
                        bottomLeft.y
                    );

                Vector2Int topLeft =
                    new Vector2Int(
                        bottomLeft.x,
                        topRight.y
                    );

                if (!points.Contains(bottomRight) ||
                    !points.Contains(topLeft))
                {
                    continue;
                }

                bool bottomExists =
                    HasStraightPath(
                        bottomLeft,
                        bottomRight,
                        edges
                    );

                bool rightExists =
                    HasStraightPath(
                        bottomRight,
                        topRight,
                        edges
                    );

                bool topExists =
                    HasStraightPath(
                        topLeft,
                        topRight,
                        edges
                    );

                bool leftExists =
                    HasStraightPath(
                        bottomLeft,
                        topLeft,
                        edges
                    );

                if (!bottomExists ||
                    !rightExists ||
                    !topExists ||
                    !leftExists)
                {
                    continue;
                }

                /*
                 * Reject a large rectangle if a complete internal
                 * horizontal or vertical line divides it into
                 * smaller rectangles.
                 */
                if (HasInternalRectangleDivider(
                        bottomLeft,
                        topRight,
                        edges))
                {
                    continue;
                }

                PrismFace rectangle =
                    new PrismFace(
                        PrismFaceType.Rectangle,
                        new[]
                        {
                            bottomLeft,
                            bottomRight,
                            topRight,
                            topLeft
                        }
                    );

                AddDetectedFace(rectangle);
            }
        }
    }

    private void DetectAllTriangles(
        HashSet<Vector2Int> points,
        HashSet<PrismGridEdge> edges)
    {
        List<Vector2Int> pointList =
            new List<Vector2Int>(points);

        for (int firstIndex = 0;
             firstIndex < pointList.Count - 2;
             firstIndex++)
        {
            for (int secondIndex = firstIndex + 1;
                 secondIndex < pointList.Count - 1;
                 secondIndex++)
            {
                for (int thirdIndex = secondIndex + 1;
                     thirdIndex < pointList.Count;
                     thirdIndex++)
                {
                    Vector2Int pointA =
                        pointList[firstIndex];

                    Vector2Int pointB =
                        pointList[secondIndex];

                    Vector2Int pointC =
                        pointList[thirdIndex];

                    if (ArePointsCollinear(
                            pointA,
                            pointB,
                            pointC))
                    {
                        continue;
                    }

                    if (!IsTriangleWithinMaximumSize(
                            pointA,
                            pointB,
                            pointC))
                    {
                        continue;
                    }

                    bool edgeAB =
                        HasValidTriangleSide(
                            pointA,
                            pointB,
                            edges
                        );

                    bool edgeBC =
                        HasValidTriangleSide(
                            pointB,
                            pointC,
                            edges
                        );

                    bool edgeCA =
                        HasValidTriangleSide(
                            pointC,
                            pointA,
                            edges
                        );

                    if (!edgeAB ||
                        !edgeBC ||
                        !edgeCA)
                    {
                        continue;
                    }

                    /*
                     * At least one triangle side should be
                     * horizontal or vertical. This prevents
                     * unrelated crossing diagonals from being
                     * detected as prism faces.
                     */
                    if (!HasStraightTriangleBase(
                            pointA,
                            pointB,
                            pointC))
                    {
                        continue;
                    }

                    PrismFace triangle =
                        new PrismFace(
                            PrismFaceType.Triangle,
                            new[]
                            {
                                pointA,
                                pointB,
                                pointC
                            }
                        );

                    AddDetectedFace(triangle);
                }
            }
        }
    }

    private bool HasValidTriangleSide(
        Vector2Int start,
        Vector2Int end,
        HashSet<PrismGridEdge> edges)
    {
        if (start == end)
            return false;

        if (start.x == end.x ||
            start.y == end.y)
        {
            return HasStraightPath(
                start,
                end,
                edges
            );
        }

        return HasDiagonalPath(
            start,
            end,
            edges
        );
    }

    private bool ArePointsCollinear(
        Vector2Int pointA,
        Vector2Int pointB,
        Vector2Int pointC)
    {
        int crossProduct =
            (pointB.x - pointA.x) *
            (pointC.y - pointA.y) -
            (pointB.y - pointA.y) *
            (pointC.x - pointA.x);

        return crossProduct == 0;
    }

    private bool IsTriangleWithinMaximumSize(
        Vector2Int pointA,
        Vector2Int pointB,
        Vector2Int pointC)
    {
        int minimumX =
            Mathf.Min(
                pointA.x,
                Mathf.Min(pointB.x, pointC.x)
            );

        int maximumX =
            Mathf.Max(
                pointA.x,
                Mathf.Max(pointB.x, pointC.x)
            );

        int minimumY =
            Mathf.Min(
                pointA.y,
                Mathf.Min(pointB.y, pointC.y)
            );

        int maximumY =
            Mathf.Max(
                pointA.y,
                Mathf.Max(pointB.y, pointC.y)
            );

        int width =
            maximumX - minimumX;

        int height =
            maximumY - minimumY;

        return width <= maximumShapeSize &&
               height <= maximumShapeSize;
    }

    private bool HasStraightTriangleBase(
        Vector2Int pointA,
        Vector2Int pointB,
        Vector2Int pointC)
    {
        bool sideABStraight =
            pointA.x == pointB.x ||
            pointA.y == pointB.y;

        bool sideBCStraight =
            pointB.x == pointC.x ||
            pointB.y == pointC.y;

        bool sideCAStraight =
            pointC.x == pointA.x ||
            pointC.y == pointA.y;

        return sideABStraight ||
               sideBCStraight ||
               sideCAStraight;
    }

    private bool HasInternalRectangleDivider(
        Vector2Int bottomLeft,
        Vector2Int topRight,
        HashSet<PrismGridEdge> edges)
    {
        /*
         * Check complete horizontal divider lines
         * inside the rectangle.
         */
        for (int y = bottomLeft.y + 1;
             y < topRight.y;
             y++)
        {
            Vector2Int dividerStart =
                new Vector2Int(
                    bottomLeft.x,
                    y
                );

            Vector2Int dividerEnd =
                new Vector2Int(
                    topRight.x,
                    y
                );

            if (HasStraightPath(
                    dividerStart,
                    dividerEnd,
                    edges))
            {
                return true;
            }
        }

        /*
         * Check complete vertical divider lines
         * inside the rectangle.
         */
        for (int x = bottomLeft.x + 1;
             x < topRight.x;
             x++)
        {
            Vector2Int dividerStart =
                new Vector2Int(
                    x,
                    bottomLeft.y
                );

            Vector2Int dividerEnd =
                new Vector2Int(
                    x,
                    topRight.y
                );

            if (HasStraightPath(
                    dividerStart,
                    dividerEnd,
                    edges))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasStraightPath(
        Vector2Int start,
        Vector2Int end,
        HashSet<PrismGridEdge> edges)
    {
        if (start == end)
            return false;

        bool horizontal =
            start.y == end.y;

        bool vertical =
            start.x == end.x;

        if (!horizontal && !vertical)
            return false;

        Vector2Int step;

        if (horizontal)
        {
            step = new Vector2Int(
                end.x > start.x ? 1 : -1,
                0
            );
        }
        else
        {
            step = new Vector2Int(
                0,
                end.y > start.y ? 1 : -1
            );
        }

        Vector2Int current = start;

        while (current != end)
        {
            Vector2Int next =
                current + step;

            PrismGridEdge requiredEdge =
                new PrismGridEdge(
                    current,
                    next
                );

            if (!edges.Contains(requiredEdge))
                return false;

            current = next;
        }

        return true;
    }

    private bool HasDiagonalPath(
        Vector2Int start,
        Vector2Int end,
        HashSet<PrismGridEdge> edges)
    {
        if (start == end)
            return false;

        if (start.x == end.x ||
            start.y == end.y)
        {
            return false;
        }

        /*
         * Current PrismDrawManager stores a
         * diagonal drag as one complete edge.
         */
        PrismGridEdge directDiagonal =
            new PrismGridEdge(start, end);

        if (edges.Contains(directDiagonal))
            return true;

        /*
         * This also allows a diagonal created from
         * multiple connected diagonal segments.
         */
        int deltaX = end.x - start.x;
        int deltaY = end.y - start.y;

        int stepCount =
            GreatestCommonDivisor(
                Mathf.Abs(deltaX),
                Mathf.Abs(deltaY)
            );

        if (stepCount <= 1)
            return false;

        Vector2Int step =
            new Vector2Int(
                deltaX / stepCount,
                deltaY / stepCount
            );

        Vector2Int current = start;

        for (int index = 0;
             index < stepCount;
             index++)
        {
            Vector2Int next =
                current + step;

            PrismGridEdge requiredEdge =
                new PrismGridEdge(
                    current,
                    next
                );

            if (!edges.Contains(requiredEdge))
                return false;

            current = next;
        }

        return current == end;
    }

    private int GreatestCommonDivisor(
        int first,
        int second)
    {
        while (second != 0)
        {
            int remainder =
                first % second;

            first = second;
            second = remainder;
        }

        return Mathf.Abs(first);
    }

    private void AddDetectedFace(
        PrismFace face)
    {
        if (face == null)
            return;

        string shapeKey =
            face.GetShapeKey();

        if (detectedShapeKeys.Contains(shapeKey))
            return;

        detectedShapeKeys.Add(shapeKey);
        detectedFaces.Add(face);
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        if (detectedFaces.Count == 0)
        {
            statusText.text =
                "No closed prism faces detected.";

            return;
        }

        statusText.text =
            $"Triangles: {TriangleCount}/2 | " +
            $"Rectangles: {RectangleCount}/3";
    }

    public void ClearDetectedShapes()
    {
        detectedFaces.Clear();
        detectedShapeKeys.Clear();

        UpdateStatusText();
    }

    public PrismFace GetFace(int index)
    {
        if (index < 0 ||
            index >= detectedFaces.Count)
        {
            return null;
        }

        return detectedFaces[index];
    }
}
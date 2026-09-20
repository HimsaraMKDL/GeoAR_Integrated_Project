using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TetrahedronTriangleDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TetrahedronGridDrawManager drawManager;

    [SerializeField]
    private Text statusText;

    [Header("Detection")]
    [SerializeField]
    private float sideTolerance = 2f;

    private readonly List<TetrahedronTriangleFace>
        detectedTriangles =
            new List<TetrahedronTriangleFace>();

    private readonly HashSet<string>
        detectedKeys =
            new HashSet<string>();

    public IReadOnlyList<TetrahedronTriangleFace>
        DetectedTriangles => detectedTriangles;

    public int TriangleCount =>
        detectedTriangles.Count;


    // ==================================================
    // START
    // ==================================================

    private void Awake()
    {
        if (drawManager == null)
        {
            drawManager =
                GetComponent<
                    TetrahedronGridDrawManager>();
        }
    }


    // ==================================================
    // MAIN DETECTION
    // ==================================================

    public void DetectTriangles()
    {
        detectedTriangles.Clear();
        detectedKeys.Clear();

        if (drawManager == null)
        {
            Debug.LogError(
                "TetrahedronTriangleDetector: " +
                "TetrahedronGridDrawManager is not assigned."
            );

            return;
        }

        HashSet<TetrahedronGridEdge> edges =
            new HashSet<TetrahedronGridEdge>(
                drawManager.GetDrawnEdges()
            );

        if (edges.Count < 3)
        {
            UpdateStatus();
            return;
        }

        HashSet<Vector2Int> points =
            CollectPoints(
                edges
            );

        List<Vector2Int> pointList =
            new List<Vector2Int>(
                points
            );

        for (int firstIndex = 0;
             firstIndex < pointList.Count - 2;
             firstIndex++)
        {
            for (int secondIndex =
                     firstIndex + 1;
                 secondIndex < pointList.Count - 1;
                 secondIndex++)
            {
                for (int thirdIndex =
                         secondIndex + 1;
                     thirdIndex < pointList.Count;
                     thirdIndex++)
                {
                    Vector2Int pointA =
                        pointList[firstIndex];

                    Vector2Int pointB =
                        pointList[secondIndex];

                    Vector2Int pointC =
                        pointList[thirdIndex];


                    // ----------------------------------
                    // 1. ALL THREE EDGES MUST EXIST
                    // ----------------------------------

                    TetrahedronGridEdge edgeAB =
                        new TetrahedronGridEdge(
                            pointA,
                            pointB
                        );

                    TetrahedronGridEdge edgeBC =
                        new TetrahedronGridEdge(
                            pointB,
                            pointC
                        );

                    TetrahedronGridEdge edgeCA =
                        new TetrahedronGridEdge(
                            pointC,
                            pointA
                        );


                    if (!edges.Contains(edgeAB) ||
                        !edges.Contains(edgeBC) ||
                        !edges.Contains(edgeCA))
                    {
                        continue;
                    }


                    // ----------------------------------
                    // 2. MUST BE EQUILATERAL
                    // ----------------------------------

                    Vector2 localA =
                        drawManager
                            .GridPointToLocal(
                                pointA
                            );

                    Vector2 localB =
                        drawManager
                            .GridPointToLocal(
                                pointB
                            );

                    Vector2 localC =
                        drawManager
                            .GridPointToLocal(
                                pointC
                            );


                    float lengthAB =
                        Vector2.Distance(
                            localA,
                            localB
                        );

                    float lengthBC =
                        Vector2.Distance(
                            localB,
                            localC
                        );

                    float lengthCA =
                        Vector2.Distance(
                            localC,
                            localA
                        );


                    if (!ApproximatelyEqual(
                            lengthAB,
                            lengthBC) ||
                        !ApproximatelyEqual(
                            lengthBC,
                            lengthCA) ||
                        !ApproximatelyEqual(
                            lengthCA,
                            lengthAB))
                    {
                        continue;
                    }


                    // ----------------------------------
                    // 3. CREATE DETECTED FACE
                    // ----------------------------------

                    TetrahedronTriangleFace face =
                        new TetrahedronTriangleFace(
                            pointA,
                            pointB,
                            pointC
                        );


                    string key =
                        face.GetKey();


                    if (detectedKeys.Contains(
                            key))
                    {
                        continue;
                    }


                    detectedKeys.Add(
                        key
                    );

                    detectedTriangles.Add(
                        face
                    );
                }
            }
        }

        UpdateStatus();

        Debug.Log(
            $"Tetrahedron triangle detection complete. " +
            $"Detected triangles = {TriangleCount}"
        );
    }


    // ==================================================
    // COLLECT GRID POINTS
    // ==================================================

    private HashSet<Vector2Int> CollectPoints(
        HashSet<TetrahedronGridEdge> edges)
    {
        HashSet<Vector2Int> points =
            new HashSet<Vector2Int>();

        foreach (TetrahedronGridEdge edge
                 in edges)
        {
            points.Add(
                edge.PointA
            );

            points.Add(
                edge.PointB
            );
        }

        return points;
    }


    // ==================================================
    // LENGTH CHECK
    // ==================================================

    private bool ApproximatelyEqual(
        float first,
        float second)
    {
        return Mathf.Abs(
                   first -
                   second
               )
               <= sideTolerance;
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ClearDetectedTriangles()
    {
        detectedTriangles.Clear();
        detectedKeys.Clear();

        UpdateStatus();
    }


    // ==================================================
    // UI
    // ==================================================

    private void UpdateStatus()
    {
        if (statusText == null)
            return;

        if (TriangleCount == 0)
        {
            statusText.text =
                "Draw 4 connected equilateral triangles.";

            return;
        }

        statusText.text =
            $"Triangles detected: {TriangleCount}/4";
    }
}
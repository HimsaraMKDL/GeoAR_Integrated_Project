using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TetrahedronNetValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TetrahedronTriangleDetector triangleDetector;

    [SerializeField]
    private TetrahedronGridDrawManager drawManager;

    [SerializeField]
    private TetrahedronNetVisualizer netVisualizer;

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Button create3DButton;

    private bool currentNetValid = false;

    public bool CurrentNetValid => currentNetValid;


    private void Start()
    {
        if (triangleDetector == null)
        {
            triangleDetector =
                GetComponent<TetrahedronTriangleDetector>();
        }

        if (drawManager == null)
        {
            drawManager =
                GetComponent<TetrahedronGridDrawManager>();
        }

        if (netVisualizer == null)
        {
            netVisualizer =
                GetComponent<TetrahedronNetVisualizer>();
        }

        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }
    }


    // ==================================================
    // MAIN VALIDATION
    // ==================================================

    public void ValidateCurrentNet()
    {
        currentNetValid = false;

        if (create3DButton != null)
        {
            create3DButton.interactable = false;
        }

        if (netVisualizer != null)
        {
            netVisualizer.ClearVisualization();
        }

        if (triangleDetector == null)
        {
            ShowInvalid(
                "Triangle detector is not assigned."
            );

            return;
        }

        if (drawManager == null)
        {
            ShowInvalid(
                "Drawing manager is not assigned."
            );

            return;
        }


        // ----------------------------------------------
        // REFRESH TRIANGLE DETECTION
        // ----------------------------------------------

        triangleDetector.DetectTriangles();

        IReadOnlyList<TetrahedronTriangleFace> triangles =
            triangleDetector.DetectedTriangles;


        // ==================================================
        // 1. EXACTLY FOUR TRIANGLES
        // ==================================================

        if (triangles.Count != 4)
        {
            string message =
                $"A tetrahedron net needs exactly 4 triangles. " +
                $"Found: {triangles.Count}/4.";

            ShowInvalid(
                message
            );

            LogValidationResult(
                false,
                message,
                triangles.Count
            );

            return;
        }


        // ==================================================
        // 2. BUILD FACE CONNECTION GRAPH
        // ==================================================

        Dictionary<int, HashSet<int>> graph =
            new Dictionary<int, HashSet<int>>();

        for (int i = 0;
             i < triangles.Count;
             i++)
        {
            graph[i] =
                new HashSet<int>();
        }

        int sharedEdgeConnections = 0;

        for (int first = 0;
             first < triangles.Count - 1;
             first++)
        {
            for (int second = first + 1;
                 second < triangles.Count;
                 second++)
            {
                int sharedPoints =
                    CountSharedPoints(
                        triangles[first],
                        triangles[second]
                    );

                if (sharedPoints == 2)
                {
                    graph[first].Add(
                        second
                    );

                    graph[second].Add(
                        first
                    );

                    sharedEdgeConnections++;
                }
            }
        }


        // ==================================================
        // 3. ALL FOUR FACES MUST BE CONNECTED
        // ==================================================

        if (!IsGraphConnected(
                graph,
                triangles.Count))
        {
            string message =
                "The 4 triangles must be connected together.";

            ShowInvalid(
                message
            );

            LogValidationResult(
                false,
                message,
                triangles.Count
            );

            return;
        }


        // ==================================================
        // 4. EXACTLY THREE FOLDING EDGES
        // ==================================================

        if (sharedEdgeConnections != 3)
        {
            string message =
                $"The triangles are connected incorrectly. " +
                $"Expected 3 shared folding edges, found " +
                $"{sharedEdgeConnections}.";

            ShowInvalid(
                message
            );

            LogValidationResult(
                false,
                message,
                triangles.Count
            );

            return;
        }


        // ==================================================
        // 5. NO ISOLATED FACE
        // ==================================================

        for (int i = 0;
             i < triangles.Count;
             i++)
        {
            if (graph[i].Count == 0)
            {
                string message =
                    "One triangle is not connected to the net.";

                ShowInvalid(
                    message
                );

                LogValidationResult(
                    false,
                    message,
                    triangles.Count
                );

                return;
            }
        }


        // ==================================================
        // SUCCESS
        // ==================================================

        currentNetValid = true;


        if (statusText != null)
        {
            statusText.text =
                "Excellent! This is a valid tetrahedron net.";
        }


        if (create3DButton != null)
        {
            create3DButton.interactable = true;
        }


        // ----------------------------------------------
        // SHOW COLORED 2D FACES
        // ----------------------------------------------

        ShowValidFaceColors(
            triangles
        );


        // ==================================================
        // RESEARCH DATA LOGGING
        // ==================================================

        LogValidationResult(
            true,
            "Excellent! This is a valid tetrahedron net.",
            triangles.Count
        );


        Debug.Log(
            "TETRAHEDRON VALIDATION SUCCESS: " +
            "4 connected triangular faces with 3 folding edges."
        );
    }


    // ==================================================
    // RESEARCH VALIDATION LOGGER
    // ==================================================

    private void LogValidationResult(
        bool isValid,
        string message,
        int faceCount)
    {
        if (SessionDataLogger.Instance == null)
        {
            Debug.LogWarning(
                "Tetrahedron validation could not be logged: " +
                "SessionDataLogger instance is missing."
            );

            return;
        }


        SessionDataLogger.Instance.LogValidation(
            "Tetrahedron",
            isValid,
            message,
            faceCount
        );


        Debug.Log(
            "Tetrahedron validation logged: " +
            (isValid ? "VALID" : "INVALID") +
            " | Faces: " +
            faceCount
        );
    }


    // ==================================================
    // VISUALIZATION
    // ==================================================

    private void ShowValidFaceColors(
        IReadOnlyList<TetrahedronTriangleFace> triangles)
    {
        if (netVisualizer == null ||
            drawManager == null)
        {
            return;
        }

        List<Vector2[]> visualTriangles =
            new List<Vector2[]>();

        foreach (TetrahedronTriangleFace triangle
                 in triangles)
        {
            Vector2 pointA =
                drawManager.GridPointToLocal(
                    triangle.PointA
                );

            Vector2 pointB =
                drawManager.GridPointToLocal(
                    triangle.PointB
                );

            Vector2 pointC =
                drawManager.GridPointToLocal(
                    triangle.PointC
                );

            visualTriangles.Add(
                new[]
                {
                    pointA,
                    pointB,
                    pointC
                }
            );
        }

        netVisualizer.ShowValidNet(
            visualTriangles
        );
    }


    // ==================================================
    // SHARED VERTEX CHECK
    // ==================================================

    private int CountSharedPoints(
        TetrahedronTriangleFace first,
        TetrahedronTriangleFace second)
    {
        Vector2Int[] firstPoints =
            first.GetPoints();

        Vector2Int[] secondPoints =
            second.GetPoints();

        int count = 0;

        foreach (Vector2Int firstPoint
                 in firstPoints)
        {
            foreach (Vector2Int secondPoint
                     in secondPoints)
            {
                if (firstPoint ==
                    secondPoint)
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }


    // ==================================================
    // CONNECTIVITY
    // ==================================================

    private bool IsGraphConnected(
        Dictionary<int, HashSet<int>> graph,
        int expectedFaceCount)
    {
        if (expectedFaceCount == 0)
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
            expectedFaceCount;
    }


    // ==================================================
    // INVALID FEEDBACK
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


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }


        if (netVisualizer != null)
        {
            netVisualizer
                .ClearVisualization();
        }


        Debug.LogWarning(
            "Tetrahedron validation: " +
            reason
        );
    }


    // ==================================================
    // INVALIDATE AFTER DRAW / UNDO / CLEAR
    // ==================================================

    public void InvalidateCurrentNet()
    {
        currentNetValid = false;


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }


        if (netVisualizer != null)
        {
            netVisualizer
                .ClearVisualization();
        }
    }
}
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class C2_ConvexMeshSliceUtility
{
    // =========================================================
    // SPLIT RESULT
    // =========================================================

    public struct SplitResult
    {
        public Mesh negativeMesh;
        public Mesh positiveMesh;


        public SplitResult(
            Mesh negative,
            Mesh positive
        )
        {
            negativeMesh =
                negative;


            positiveMesh =
                positive;
        }
    }


    // =========================================================
    // FIND CUT OFFSET FOR REQUESTED VOLUME FRACTION
    // =========================================================

    public static float FindOffsetForFraction(
        Mesh sourceMesh,
        Vector3 planeNormal,
        float targetFraction
    )
    {
        if (sourceMesh == null)
        {
            Debug.LogError(
                "C2 Slice Utility: Source mesh is missing."
            );

            return 0f;
        }


        planeNormal.Normalize();


        Vector3[] sourceVertices =
            sourceMesh.vertices;


        if (
            sourceVertices == null ||
            sourceVertices.Length == 0
        )
        {
            Debug.LogError(
                "C2 Slice Utility: Source mesh has no vertices."
            );

            return 0f;
        }


        float minimumProjection =
            float.PositiveInfinity;


        float maximumProjection =
            float.NegativeInfinity;


        foreach (
            Vector3 vertex in
            sourceVertices
        )
        {
            float projection =
                Vector3.Dot(
                    planeNormal,
                    vertex
                );


            minimumProjection =
                Mathf.Min(
                    minimumProjection,
                    projection
                );


            maximumProjection =
                Mathf.Max(
                    maximumProjection,
                    projection
                );
        }


        float totalVolume =
            CalculateVolume(
                sourceMesh
            );


        if (
            totalVolume <=
            0.0000001f
        )
        {
            Debug.LogError(
                "C2 Slice Utility: Source mesh has invalid volume."
            );

            return 0f;
        }


        float low =
            minimumProjection;


        float high =
            maximumProjection;


        // 48 iterations gives much more precision
        // than we require for this educational AR model.

        for (
            int iteration = 0;
            iteration < 48;
            iteration++
        )
        {
            float middle =
                (low + high) *
                0.5f;


            SplitResult temporaryResult =
                SplitMesh(
                    sourceMesh,
                    planeNormal,
                    middle
                );


            float negativeVolume =
                CalculateVolume(
                    temporaryResult
                        .negativeMesh
                );


            float currentFraction =
                negativeVolume /
                totalVolume;


            if (
                temporaryResult
                    .negativeMesh != null
            )
            {
                Object.DestroyImmediate(
                    temporaryResult
                        .negativeMesh
                );
            }


            if (
                temporaryResult
                    .positiveMesh != null
            )
            {
                Object.DestroyImmediate(
                    temporaryResult
                        .positiveMesh
                );
            }


            if (
                currentFraction <
                targetFraction
            )
            {
                low =
                    middle;
            }
            else
            {
                high =
                    middle;
            }
        }


        return
            (low + high) *
            0.5f;
    }


    // =========================================================
    // GENERATE AND CREATE ONE COMPLETE RESULT
    // =========================================================

    public static float GenerateFractionResult(
        Transform parent,
        Mesh sourceMesh,
        string outputFolder,
        string resultName,
        string pieceAName,
        string pieceBName,
        Vector3 planeNormal,
        float targetFraction,
        Material materialA,
        Material materialB
    )
    {
        if (parent == null)
        {
            Debug.LogError(
                "C2 Slice Utility: Result parent is missing."
            );

            return 0f;
        }


        if (sourceMesh == null)
        {
            Debug.LogError(
                "C2 Slice Utility: Source mesh is missing."
            );

            return 0f;
        }


        EnsureFolder(
            outputFolder
        );


        // Remove an older hierarchy result if this
        // generator has already been used before.

        DeleteExistingChild(
            parent,
            resultName
        );


        float cutOffset =
            FindOffsetForFraction(
                sourceMesh,
                planeNormal,
                targetFraction
            );


        SplitResult result =
            SplitMesh(
                sourceMesh,
                planeNormal.normalized,
                cutOffset
            );


        result.negativeMesh.name =
            pieceAName +
            "_Mesh";


        result.positiveMesh.name =
            pieceBName +
            "_Mesh";


        string pieceAPath =
            outputFolder +
            "/" +
            pieceAName +
            "_Mesh.asset";


        string pieceBPath =
            outputFolder +
            "/" +
            pieceBName +
            "_Mesh.asset";


        // Remove previous generated assets so this
        // generator can safely be run again.

        AssetDatabase.DeleteAsset(
            pieceAPath
        );


        AssetDatabase.DeleteAsset(
            pieceBPath
        );


        AssetDatabase.CreateAsset(
            result.negativeMesh,
            pieceAPath
        );


        AssetDatabase.CreateAsset(
            result.positiveMesh,
            pieceBPath
        );


        // =====================================================
        // CREATE RESULT GROUP
        // =====================================================

        GameObject resultGroup =
            new GameObject(
                resultName
            );


        Undo.RegisterCreatedObjectUndo(
            resultGroup,
            "Create C2 Slice Result"
        );


        resultGroup.transform.SetParent(
            parent,
            false
        );


        resultGroup.transform.localPosition =
            Vector3.zero;


        resultGroup.transform.localRotation =
            Quaternion.identity;


        resultGroup.transform.localScale =
            Vector3.one;


        // =====================================================
        // CREATE PIECE A
        // =====================================================

        CreatePhysicsPiece(
            resultGroup.transform,
            pieceAName,
            result.negativeMesh,
            materialA
        );


        // =====================================================
        // CREATE PIECE B
        // =====================================================

        CreatePhysicsPiece(
            resultGroup.transform,
            pieceBName,
            result.positiveMesh,
            materialB
        );


        // Every generated result starts hidden.
        //
        // The runtime manager will activate only the
        // selected result after a successful trace.

        resultGroup.SetActive(
            false
        );


        EditorUtility.SetDirty(
            parent.gameObject
        );


        return
            cutOffset;
    }


    // =========================================================
    // SPLIT SOURCE MESH
    // =========================================================

    public static SplitResult SplitMesh(
        Mesh sourceMesh,
        Vector3 planeNormal,
        float planeOffset
    )
    {
        planeNormal.Normalize();


        List<Vector3>
            negativeVertices =
                new List<Vector3>();


        List<int>
            negativeTriangles =
                new List<int>();


        List<Vector3>
            positiveVertices =
                new List<Vector3>();


        List<int>
            positiveTriangles =
                new List<int>();


        List<Vector3>
            cutPoints =
                new List<Vector3>();


        Vector3[] sourceVertices =
            sourceMesh.vertices;


        int[] sourceTriangles =
            sourceMesh.triangles;


        // =====================================================
        // PROCESS EVERY TRIANGLE
        // =====================================================

        for (
            int triangleIndex = 0;
            triangleIndex <
                sourceTriangles.Length;
            triangleIndex += 3
        )
        {
            Vector3 a =
                sourceVertices[
                    sourceTriangles[
                        triangleIndex
                    ]
                ];


            Vector3 b =
                sourceVertices[
                    sourceTriangles[
                        triangleIndex + 1
                    ]
                ];


            Vector3 c =
                sourceVertices[
                    sourceTriangles[
                        triangleIndex + 2
                    ]
                ];


            List<Vector3>
                sourceTriangle =
                    new List<Vector3>
                    {
                        a,
                        b,
                        c
                    };


            // Geometry on the negative side.
            List<Vector3>
                negativePolygon =
                    ClipPolygon(
                        sourceTriangle,
                        planeNormal,
                        planeOffset,
                        true
                    );


            // Geometry on the positive side.
            List<Vector3>
                positivePolygon =
                    ClipPolygon(
                        sourceTriangle,
                        planeNormal,
                        planeOffset,
                        false
                    );


            AddPolygonTriangles(
                negativePolygon,
                negativeVertices,
                negativeTriangles
            );


            AddPolygonTriangles(
                positivePolygon,
                positiveVertices,
                positiveTriangles
            );


            CollectTriangleIntersections(
                a,
                b,
                c,
                planeNormal,
                planeOffset,
                cutPoints
            );
        }


        // =====================================================
        // CLOSE THE NEW CUT SURFACE
        // =====================================================

        if (cutPoints.Count >= 3)
        {
            List<Vector3>
                orderedCutPoints =
                    SortCutPoints(
                        cutPoints,
                        planeNormal
                    );


            // Negative piece outward cap
            // faces in +planeNormal direction.

            AddCap(
                orderedCutPoints,
                negativeVertices,
                negativeTriangles,
                false
            );


            // Positive piece outward cap
            // faces in -planeNormal direction.

            AddCap(
                orderedCutPoints,
                positiveVertices,
                positiveTriangles,
                true
            );
        }


        Mesh negativeMesh =
            BuildMesh(
                negativeVertices,
                negativeTriangles
            );


        Mesh positiveMesh =
            BuildMesh(
                positiveVertices,
                positiveTriangles
            );


        return new SplitResult(
            negativeMesh,
            positiveMesh
        );
    }


    // =========================================================
    // CLIP POLYGON AGAINST PLANE
    // =========================================================

    private static List<Vector3> ClipPolygon(
        List<Vector3> inputPolygon,
        Vector3 planeNormal,
        float planeOffset,
        bool keepNegativeSide
    )
    {
        List<Vector3> output =
            new List<Vector3>();


        if (
            inputPolygon == null ||
            inputPolygon.Count == 0
        )
        {
            return output;
        }


        const float epsilon =
            0.000001f;


        for (
            int index = 0;
            index < inputPolygon.Count;
            index++
        )
        {
            Vector3 current =
                inputPolygon[index];


            Vector3 next =
                inputPolygon[
                    (index + 1) %
                    inputPolygon.Count
                ];


            float currentValue =
                Vector3.Dot(
                    planeNormal,
                    current
                ) -
                planeOffset;


            float nextValue =
                Vector3.Dot(
                    planeNormal,
                    next
                ) -
                planeOffset;


            bool currentInside =
                keepNegativeSide
                    ? currentValue <=
                        epsilon
                    : currentValue >=
                        -epsilon;


            bool nextInside =
                keepNegativeSide
                    ? nextValue <=
                        epsilon
                    : nextValue >=
                        -epsilon;


            // Inside → Inside

            if (
                currentInside &&
                nextInside
            )
            {
                output.Add(
                    next
                );
            }

            // Inside → Outside

            else if (
                currentInside &&
                !nextInside
            )
            {
                output.Add(
                    CalculateIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    )
                );
            }

            // Outside → Inside

            else if (
                !currentInside &&
                nextInside
            )
            {
                output.Add(
                    CalculateIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    )
                );


                output.Add(
                    next
                );
            }
        }


        return output;
    }


    // =========================================================
    // FIND EDGE/PLANE INTERSECTION
    // =========================================================

    private static Vector3 CalculateIntersection(
        Vector3 start,
        Vector3 end,
        float startValue,
        float endValue
    )
    {
        float denominator =
            startValue -
            endValue;


        if (
            Mathf.Abs(
                denominator
            ) <
            0.000001f
        )
        {
            return
                (start + end) *
                0.5f;
        }


        float t =
            startValue /
            denominator;


        return Vector3.Lerp(
            start,
            end,
            t
        );
    }


    // =========================================================
    // COLLECT CUT POINTS FROM TRIANGLE
    // =========================================================

    private static void CollectTriangleIntersections(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 planeNormal,
        float planeOffset,
        List<Vector3> cutPoints
    )
    {
        CollectEdgeIntersection(
            a,
            b,
            planeNormal,
            planeOffset,
            cutPoints
        );


        CollectEdgeIntersection(
            b,
            c,
            planeNormal,
            planeOffset,
            cutPoints
        );


        CollectEdgeIntersection(
            c,
            a,
            planeNormal,
            planeOffset,
            cutPoints
        );
    }


    // =========================================================
    // COLLECT ONE EDGE INTERSECTION
    // =========================================================

    private static void CollectEdgeIntersection(
        Vector3 a,
        Vector3 b,
        Vector3 planeNormal,
        float planeOffset,
        List<Vector3> cutPoints
    )
    {
        const float epsilon =
            0.000001f;


        float valueA =
            Vector3.Dot(
                planeNormal,
                a
            ) -
            planeOffset;


        float valueB =
            Vector3.Dot(
                planeNormal,
                b
            ) -
            planeOffset;


        if (
            Mathf.Abs(
                valueA
            ) <=
            epsilon
        )
        {
            AddUniquePoint(
                cutPoints,
                a
            );
        }


        if (
            Mathf.Abs(
                valueB
            ) <=
            epsilon
        )
        {
            AddUniquePoint(
                cutPoints,
                b
            );
        }


        if (
            valueA *
            valueB <
            0f
        )
        {
            Vector3 intersection =
                CalculateIntersection(
                    a,
                    b,
                    valueA,
                    valueB
                );


            AddUniquePoint(
                cutPoints,
                intersection
            );
        }
    }


    // =========================================================
    // PREVENT DUPLICATE CUT POINTS
    // =========================================================

    private static void AddUniquePoint(
        List<Vector3> points,
        Vector3 point
    )
    {
        const float tolerance =
            0.00001f;


        foreach (
            Vector3 existingPoint in
            points
        )
        {
            if (
                Vector3.Distance(
                    existingPoint,
                    point
                ) <=
                tolerance
            )
            {
                return;
            }
        }


        points.Add(
            point
        );
    }


    // =========================================================
    // SORT CUT SURFACE VERTICES
    // =========================================================

    private static List<Vector3> SortCutPoints(
        List<Vector3> points,
        Vector3 planeNormal
    )
    {
        Vector3 center =
            Vector3.zero;


        foreach (
            Vector3 point in
            points
        )
        {
            center +=
                point;
        }


        center /=
            points.Count;


        Vector3 referenceAxis =
            Mathf.Abs(
                Vector3.Dot(
                    planeNormal,
                    Vector3.up
                )
            ) <
            0.95f
                ? Vector3.up
                : Vector3.right;


        Vector3 axisU =
            Vector3.Cross(
                referenceAxis,
                planeNormal
            ).normalized;


        Vector3 axisV =
            Vector3.Cross(
                planeNormal,
                axisU
            ).normalized;


        List<Vector3> orderedPoints =
            new List<Vector3>(
                points
            );


        orderedPoints.Sort(
            (first, second) =>
            {
                Vector3 firstDelta =
                    first -
                    center;


                Vector3 secondDelta =
                    second -
                    center;


                float firstAngle =
                    Mathf.Atan2(
                        Vector3.Dot(
                            firstDelta,
                            axisV
                        ),
                        Vector3.Dot(
                            firstDelta,
                            axisU
                        )
                    );


                float secondAngle =
                    Mathf.Atan2(
                        Vector3.Dot(
                            secondDelta,
                            axisV
                        ),
                        Vector3.Dot(
                            secondDelta,
                            axisU
                        )
                    );


                return
                    firstAngle.CompareTo(
                        secondAngle
                    );
            }
        );


        return
            orderedPoints;
    }


    // =========================================================
    // CREATE CUT-SURFACE CAP
    // =========================================================

    private static void AddCap(
        List<Vector3> orderedPoints,
        List<Vector3> vertices,
        List<int> triangles,
        bool reverse
    )
    {
        if (
            orderedPoints == null ||
            orderedPoints.Count < 3
        )
        {
            return;
        }


        Vector3 center =
            Vector3.zero;


        foreach (
            Vector3 point in
            orderedPoints
        )
        {
            center +=
                point;
        }


        center /=
            orderedPoints.Count;


        for (
            int index = 0;
            index <
                orderedPoints.Count;
            index++
        )
        {
            Vector3 current =
                orderedPoints[index];


            Vector3 next =
                orderedPoints[
                    (index + 1) %
                    orderedPoints.Count
                ];


            if (!reverse)
            {
                AddTriangle(
                    vertices,
                    triangles,
                    center,
                    current,
                    next
                );
            }
            else
            {
                AddTriangle(
                    vertices,
                    triangles,
                    center,
                    next,
                    current
                );
            }
        }
    }


    // =========================================================
    // TRIANGULATE POLYGON
    // =========================================================

    private static void AddPolygonTriangles(
        List<Vector3> polygon,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        if (
            polygon == null ||
            polygon.Count < 3
        )
        {
            return;
        }


        for (
            int index = 1;
            index <
                polygon.Count - 1;
            index++
        )
        {
            AddTriangle(
                vertices,
                triangles,
                polygon[0],
                polygon[index],
                polygon[index + 1]
            );
        }
    }


    // =========================================================
    // ADD TRIANGLE
    // =========================================================

    private static void AddTriangle(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c
    )
    {
        int startIndex =
            vertices.Count;


        vertices.Add(
            a
        );


        vertices.Add(
            b
        );


        vertices.Add(
            c
        );


        triangles.Add(
            startIndex
        );


        triangles.Add(
            startIndex + 1
        );


        triangles.Add(
            startIndex + 2
        );
    }


    // =========================================================
    // BUILD UNITY MESH
    // =========================================================

    private static Mesh BuildMesh(
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        Mesh mesh =
            new Mesh();


        mesh.indexFormat =
            IndexFormat.UInt32;


        mesh.SetVertices(
            vertices
        );


        mesh.SetTriangles(
            triangles,
            0
        );


        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        return
            mesh;
    }


    // =========================================================
    // CALCULATE CLOSED MESH VOLUME
    // =========================================================

    public static float CalculateVolume(
        Mesh mesh
    )
    {
        if (
            mesh == null ||
            mesh.vertexCount == 0
        )
        {
            return 0f;
        }


        Vector3[] vertices =
            mesh.vertices;


        int[] triangles =
            mesh.triangles;


        double volume =
            0.0;


        for (
            int triangleIndex = 0;
            triangleIndex <
                triangles.Length;
            triangleIndex += 3
        )
        {
            Vector3 a =
                vertices[
                    triangles[
                        triangleIndex
                    ]
                ];


            Vector3 b =
                vertices[
                    triangles[
                        triangleIndex + 1
                    ]
                ];


            Vector3 c =
                vertices[
                    triangles[
                        triangleIndex + 2
                    ]
                ];


            volume +=
                Vector3.Dot(
                    a,
                    Vector3.Cross(
                        b,
                        c
                    )
                ) /
                6.0;
        }


        return Mathf.Abs(
            (float)volume
        );
    }


    // =========================================================
    // CREATE ONE PHYSICS SLICE PIECE
    // =========================================================

    private static void CreatePhysicsPiece(
        Transform parent,
        string objectName,
        Mesh mesh,
        Material material
    )
    {
        GameObject piece =
            new GameObject(
                objectName
            );


        Undo.RegisterCreatedObjectUndo(
            piece,
            "Create C2 Slice Piece"
        );


        piece.transform.SetParent(
            parent,
            false
        );


        // =====================================================
        // TRANSFORM
        // =====================================================

        piece.transform.localPosition =
            Vector3.zero;


        piece.transform.localRotation =
            Quaternion.identity;


        piece.transform.localScale =
            Vector3.one;


        // =====================================================
        // COMPONENTS
        // =====================================================

        MeshFilter meshFilter =
            piece.AddComponent<MeshFilter>();


        MeshRenderer meshRenderer =
            piece.AddComponent<MeshRenderer>();


        MeshCollider meshCollider =
            piece.AddComponent<MeshCollider>();


        Rigidbody rigidbody =
            piece.AddComponent<Rigidbody>();


        // =====================================================
        // MESH FILTER
        // =====================================================

        meshFilter.sharedMesh =
            mesh;


        // =====================================================
        // MESH RENDERER
        // =====================================================

        meshRenderer.sharedMaterial =
            material;


        // =====================================================
        // MESH COLLIDER
        // =====================================================

        meshCollider.sharedMesh =
            mesh;


        meshCollider.convex =
            true;


        meshCollider.isTrigger =
            false;


        // =====================================================
        // RIGIDBODY
        // =====================================================

        rigidbody.mass =
            0.5f;


        rigidbody.drag =
            0f;


        rigidbody.angularDrag =
            0.05f;


        rigidbody.useGravity =
            false;


        rigidbody.isKinematic =
            true;


        rigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;


        rigidbody.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    }


    // =========================================================
    // REMOVE OLD RESULT GROUP
    // =========================================================

    public static void DeleteExistingChild(
        Transform parent,
        string childName
    )
    {
        if (parent == null)
        {
            return;
        }


        Transform existingChild =
            parent.Find(
                childName
            );


        if (existingChild != null)
        {
            Undo.DestroyObjectImmediate(
                existingChild.gameObject
            );
        }
    }


    // =========================================================
    // ENSURE ASSET FOLDER
    // =========================================================

    public static void EnsureFolder(
        string folderPath
    )
    {
        if (
            AssetDatabase.IsValidFolder(
                folderPath
            )
        )
        {
            return;
        }


        int slashIndex =
            folderPath.LastIndexOf(
                '/'
            );


        if (slashIndex <= 0)
        {
            return;
        }


        string parentPath =
            folderPath.Substring(
                0,
                slashIndex
            );


        string folderName =
            folderPath.Substring(
                slashIndex + 1
            );


        EnsureFolder(
            parentPath
        );


        if (
            !AssetDatabase.IsValidFolder(
                folderPath
            )
        )
        {
            AssetDatabase.CreateFolder(
                parentPath,
                folderName
            );
        }
    }
}
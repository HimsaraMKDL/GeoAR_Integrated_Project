using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_CylinderAngledFractionsMeshGenerator
{
    // =========================================================
    // PATHS
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedCylinderMeshes";


    private const string FullMeshPath =
        OutputFolder +
        "/C2_Cylinder_Full_Mesh.asset";


    private const string PieceAMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartA_Mat.mat";


    private const string PieceBMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartB_Mat.mat";


    // =========================================================
    // NUMERICAL SETTINGS
    // =========================================================

    private const float PlaneEpsilon =
        0.000001f;


    private const float PointMergeTolerance =
        0.00001f;


    private const int BinarySearchIterations =
        56;


    // =========================================================
    // INTERNAL VERTEX DATA
    // =========================================================

    private struct SliceVertex
    {
        public Vector3 position;

        public Vector3 normal;


        public SliceVertex(
            Vector3 positionValue,
            Vector3 normalValue
        )
        {
            position =
                positionValue;


            normal =
                normalValue;
        }
    }


    // =========================================================
    // INTERNAL MESH BUILDER
    // =========================================================

    private class MeshBuilder
    {
        public readonly List<Vector3> vertices =
            new List<Vector3>();


        public readonly List<Vector3> normals =
            new List<Vector3>();


        public readonly List<int> triangles =
            new List<int>();


        public void AddSurfacePolygon(
            List<SliceVertex> polygon
        )
        {
            if (
                polygon == null ||
                polygon.Count < 3
            )
            {
                return;
            }


            int startIndex =
                vertices.Count;


            for (
                int index = 0;
                index < polygon.Count;
                index++
            )
            {
                vertices.Add(
                    polygon[index].position
                );


                normals.Add(
                    polygon[index].normal
                );
            }


            for (
                int index = 1;
                index < polygon.Count - 1;
                index++
            )
            {
                triangles.Add(
                    startIndex
                );


                triangles.Add(
                    startIndex + index
                );


                triangles.Add(
                    startIndex + index + 1
                );
            }
        }


        public void AddCap(
            List<Vector3> capPoints,
            Vector3 capNormal,
            bool reverse
        )
        {
            if (
                capPoints == null ||
                capPoints.Count < 3
            )
            {
                return;
            }


            Vector3 centre =
                Vector3.zero;


            for (
                int index = 0;
                index < capPoints.Count;
                index++
            )
            {
                centre +=
                    capPoints[index];
            }


            centre /=
                capPoints.Count;


            int centreIndex =
                vertices.Count;


            vertices.Add(
                centre
            );


            normals.Add(
                capNormal
            );


            int ringStart =
                vertices.Count;


            for (
                int index = 0;
                index < capPoints.Count;
                index++
            )
            {
                vertices.Add(
                    capPoints[index]
                );


                normals.Add(
                    capNormal
                );
            }


            for (
                int index = 0;
                index < capPoints.Count;
                index++
            )
            {
                int nextIndex =
                    (
                        index + 1
                    ) %
                    capPoints.Count;


                int current =
                    ringStart +
                    index;


                int next =
                    ringStart +
                    nextIndex;


                if (!reverse)
                {
                    triangles.Add(
                        centreIndex
                    );


                    triangles.Add(
                        current
                    );


                    triangles.Add(
                        next
                    );
                }
                else
                {
                    triangles.Add(
                        centreIndex
                    );


                    triangles.Add(
                        next
                    );


                    triangles.Add(
                        current
                    );
                }
            }
        }


        public Mesh Build(
            string meshName
        )
        {
            Mesh mesh =
                new Mesh();


            mesh.name =
                meshName;


            mesh.SetVertices(
                vertices
            );


            mesh.SetNormals(
                normals
            );


            mesh.SetTriangles(
                triangles,
                0
            );


            mesh.RecalculateBounds();


            return mesh;
        }
    }


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Cylinder Angled Fractions"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        // =====================================================
        // CHECK SELECTED OBJECT
        // =====================================================

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Angled Fractions",
                "Open C2_CylinderExperience.prefab and " +
                "select C2_CylinderSliceResults first.",
                "OK"
            );


            return;
        }


        if (
            selected.name !=
            "C2_CylinderSliceResults"
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Angled Fractions",
                "Please select C2_CylinderSliceResults.",
                "OK"
            );


            return;
        }


        // =====================================================
        // CHECK OUTPUT FOLDER
        // =====================================================

        if (
            !AssetDatabase.IsValidFolder(
                OutputFolder
            )
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Angled Fractions",
                "Generated Cylinder mesh folder is missing:\n\n" +
                OutputFolder,
                "OK"
            );


            return;
        }


        // =====================================================
        // LOAD FULL CYLINDER
        // =====================================================

        Mesh sourceMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                FullMeshPath
            );


        if (sourceMesh == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Angled Fractions",
                "Could not find:\n\n" +
                FullMeshPath,
                "OK"
            );


            return;
        }


        // =====================================================
        // LOAD MATERIALS
        // =====================================================

        Material materialA =
            AssetDatabase.LoadAssetAtPath<Material>(
                PieceAMaterialPath
            );


        Material materialB =
            AssetDatabase.LoadAssetAtPath<Material>(
                PieceBMaterialPath
            );


        if (
            materialA == null ||
            materialB == null
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Angled Fractions",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );


            return;
        }


        // =====================================================
        // REMOVE ONLY OLD ANGLED GROUPS
        // =====================================================

        DeleteExistingChild(
            selected.transform,
            "C2_CylinderAngled_1_2"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderAngled_1_3"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderAngled_1_4"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderAngled_3_4"
        );


        // =====================================================
        // OUR ANGLED PLANE
        //
        // The line in front view runs diagonally.
        //
        // Normal:
        // (-1,+1,0)
        // =====================================================

        Vector3 planeNormal =
            new Vector3(
                -1f,
                1f,
                0f
            ).normalized;


        // =====================================================
        // CREATE ALL FOUR FRACTIONS
        // =====================================================

        CreateAngledResult(
            selected.transform,
            sourceMesh,
            planeNormal,
            "C2_CylinderAngled_1_2",
            "C2_CylinderAngledHalf",
            0.5f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            sourceMesh,
            planeNormal,
            "C2_CylinderAngled_1_3",
            "C2_CylinderAngledThird",
            1f / 3f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            sourceMesh,
            planeNormal,
            "C2_CylinderAngled_1_4",
            "C2_CylinderAngledQuarter",
            0.25f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            sourceMesh,
            planeNormal,
            "C2_CylinderAngled_3_4",
            "C2_CylinderAngledThreeQuarter",
            0.75f,
            materialA,
            materialB
        );


        EditorUtility.SetDirty(
            selected
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        Debug.Log(
            "C2 Cylinder: All Angled fractions generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Cylinder Angled Fractions",
            "Cylinder Angled 1/2, 1/3, " +
            "1/4 and 3/4 generated successfully.",
            "OK"
        );
    }


    // =========================================================
    // CREATE ONE ANGLED RESULT
    // =========================================================

    private static void CreateAngledResult(
        Transform parent,
        Mesh sourceMesh,
        Vector3 planeNormal,
        string resultName,
        string piecePrefix,
        float targetFraction,
        Material materialA,
        Material materialB
    )
    {
        // =====================================================
        // FIND MATHEMATICALLY CORRECT OFFSET
        // =====================================================

        float offset =
            FindOffsetForFraction(
                sourceMesh,
                planeNormal,
                targetFraction
            );


        // =====================================================
        // PERFORM FINAL SPLIT
        // =====================================================

        Mesh pieceAMesh;

        Mesh pieceBMesh;


        SplitMesh(
            sourceMesh,
            planeNormal,
            offset,
            out pieceAMesh,
            out pieceBMesh
        );


        string pieceAName =
            piecePrefix +
            "_PieceA";


        string pieceBName =
            piecePrefix +
            "_PieceB";


        pieceAMesh.name =
            pieceAName +
            "_Mesh";


        pieceBMesh.name =
            pieceBName +
            "_Mesh";


        string pieceAPath =
            OutputFolder +
            "/" +
            pieceAName +
            "_Mesh.asset";


        string pieceBPath =
            OutputFolder +
            "/" +
            pieceBName +
            "_Mesh.asset";


        // =====================================================
        // REPLACE OLD ASSETS IF GENERATOR RUN AGAIN
        // =====================================================

        AssetDatabase.DeleteAsset(
            pieceAPath
        );


        AssetDatabase.DeleteAsset(
            pieceBPath
        );


        AssetDatabase.CreateAsset(
            pieceAMesh,
            pieceAPath
        );


        AssetDatabase.CreateAsset(
            pieceBMesh,
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
            "Create Cylinder Angled Result"
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
        // PIECE A
        // =====================================================

        CreatePiece(
            resultGroup.transform,
            pieceAName,
            pieceAMesh,
            materialA
        );


        // =====================================================
        // PIECE B
        // =====================================================

        CreatePiece(
            resultGroup.transform,
            pieceBName,
            pieceBMesh,
            materialB
        );


        resultGroup.SetActive(
            false
        );


        // =====================================================
        // VALIDATE ACTUAL VOLUME
        // =====================================================

        float pieceAVolume =
            CalculateVolume(
                pieceAMesh
            );


        float pieceBVolume =
            CalculateVolume(
                pieceBMesh
            );


        float totalVolume =
            pieceAVolume +
            pieceBVolume;


        float actualFraction =
            totalVolume > 0f
                ? pieceAVolume /
                  totalVolume
                : 0f;


        Debug.Log(
            "C2 Cylinder: " +
            resultName +
            " generated. Plane Offset = " +
            offset.ToString("F6") +
            ", Actual Piece A Fraction = " +
            actualFraction.ToString("F4")
        );
    }


    // =========================================================
    // FIND OFFSET FOR REQUESTED VOLUME FRACTION
    // =========================================================

    private static float FindOffsetForFraction(
        Mesh sourceMesh,
        Vector3 planeNormal,
        float targetFraction
    )
    {
        Vector3[] vertices =
            sourceMesh.vertices;


        float minimumProjection =
            float.MaxValue;


        float maximumProjection =
            float.MinValue;


        // =====================================================
        // FIND THE FULL RANGE OF POSSIBLE PLANE POSITIONS
        // =====================================================

        for (
            int index = 0;
            index < vertices.Length;
            index++
        )
        {
            float projection =
                Vector3.Dot(
                    planeNormal,
                    vertices[index]
                );


            if (
                projection <
                minimumProjection
            )
            {
                minimumProjection =
                    projection;
            }


            if (
                projection >
                maximumProjection
            )
            {
                maximumProjection =
                    projection;
            }
        }


        float low =
            minimumProjection;


        float high =
            maximumProjection;


        // =====================================================
        // BINARY SEARCH
        // =====================================================

        for (
            int iteration = 0;
            iteration < BinarySearchIterations;
            iteration++
        )
        {
            float middle =
                (
                    low +
                    high
                ) *
                0.5f;


            Mesh negativeMesh;

            Mesh positiveMesh;


            SplitMesh(
                sourceMesh,
                planeNormal,
                middle,
                out negativeMesh,
                out positiveMesh
            );


            float negativeVolume =
                CalculateVolume(
                    negativeMesh
                );


            float positiveVolume =
                CalculateVolume(
                    positiveMesh
                );


            float totalVolume =
                negativeVolume +
                positiveVolume;


            float currentFraction =
                totalVolume > 0f
                    ? negativeVolume /
                      totalVolume
                    : 0f;


            Object.DestroyImmediate(
                negativeMesh
            );


            Object.DestroyImmediate(
                positiveMesh
            );


            // As the offset increases,
            // the negative-side volume increases.

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
            (
                low +
                high
            ) *
            0.5f;
    }


    // =========================================================
    // SPLIT SOURCE MESH BY PLANE
    // =========================================================

    private static void SplitMesh(
        Mesh sourceMesh,
        Vector3 planeNormal,
        float planeOffset,
        out Mesh negativeMesh,
        out Mesh positiveMesh
    )
    {
        MeshBuilder negativeBuilder =
            new MeshBuilder();


        MeshBuilder positiveBuilder =
            new MeshBuilder();


        Vector3[] sourceVertices =
            sourceMesh.vertices;


        Vector3[] sourceNormals =
            sourceMesh.normals;


        int[] sourceTriangles =
            sourceMesh.triangles;


        List<Vector3> capPoints =
            new List<Vector3>();


        // =====================================================
        // PROCESS EVERY SOURCE TRIANGLE
        // =====================================================

        for (
            int triangleIndex = 0;
            triangleIndex <
            sourceTriangles.Length;
            triangleIndex += 3
        )
        {
            int indexA =
                sourceTriangles[
                    triangleIndex
                ];


            int indexB =
                sourceTriangles[
                    triangleIndex + 1
                ];


            int indexC =
                sourceTriangles[
                    triangleIndex + 2
                ];


            Vector3 normalA =
                GetSafeSourceNormal(
                    sourceNormals,
                    indexA,
                    sourceVertices[indexA]
                );


            Vector3 normalB =
                GetSafeSourceNormal(
                    sourceNormals,
                    indexB,
                    sourceVertices[indexB]
                );


            Vector3 normalC =
                GetSafeSourceNormal(
                    sourceNormals,
                    indexC,
                    sourceVertices[indexC]
                );


            SliceVertex vertexA =
                new SliceVertex(
                    sourceVertices[indexA],
                    normalA
                );


            SliceVertex vertexB =
                new SliceVertex(
                    sourceVertices[indexB],
                    normalB
                );


            SliceVertex vertexC =
                new SliceVertex(
                    sourceVertices[indexC],
                    normalC
                );


            List<SliceVertex> triangle =
                new List<SliceVertex>
                {
                    vertexA,
                    vertexB,
                    vertexC
                };


            // =================================================
            // NEGATIVE SIDE
            // Piece A / selected fraction
            // =================================================

            List<SliceVertex> negativePolygon =
                ClipTriangleAgainstPlane(
                    triangle,
                    planeNormal,
                    planeOffset,
                    true
                );


            negativeBuilder.AddSurfacePolygon(
                negativePolygon
            );


            // =================================================
            // POSITIVE SIDE
            // Piece B / remaining fraction
            // =================================================

            List<SliceVertex> positivePolygon =
                ClipTriangleAgainstPlane(
                    triangle,
                    planeNormal,
                    planeOffset,
                    false
                );


            positiveBuilder.AddSurfacePolygon(
                positivePolygon
            );


            // =================================================
            // COLLECT THE PLANE INTERSECTION
            // =================================================

            CollectTrianglePlaneIntersections(
                vertexA.position,
                vertexB.position,
                vertexC.position,
                planeNormal,
                planeOffset,
                capPoints
            );
        }


        // =====================================================
        // BUILD THE NEW CLOSED CUT FACE
        // =====================================================

        List<Vector3> sortedCapPoints =
            SortCapPoints(
                capPoints,
                planeNormal
            );


        if (
            sortedCapPoints.Count >=
            3
        )
        {
            // Negative Piece A outward face points
            // toward the positive side (+normal).

            negativeBuilder.AddCap(
                sortedCapPoints,
                planeNormal,
                false
            );


            // Positive Piece B outward face points
            // toward negative side (-normal).

            positiveBuilder.AddCap(
                sortedCapPoints,
                -planeNormal,
                true
            );
        }


        negativeMesh =
            negativeBuilder.Build(
                "C2_Cylinder_Angled_Negative"
            );


        positiveMesh =
            positiveBuilder.Build(
                "C2_Cylinder_Angled_Positive"
            );
    }


    // =========================================================
    // SOURCE NORMAL SAFETY
    // =========================================================

    private static Vector3 GetSafeSourceNormal(
        Vector3[] normals,
        int index,
        Vector3 position
    )
    {
        if (
            normals != null &&
            normals.Length >
            index
        )
        {
            Vector3 existing =
                normals[index];


            if (
                existing.sqrMagnitude >
                0.000001f
            )
            {
                return
                    existing.normalized;
            }
        }


        // Fallback is only used if source normals
        // unexpectedly do not exist.

        Vector3 radial =
            new Vector3(
                position.x,
                0f,
                position.z
            );


        if (
            radial.sqrMagnitude >
            0.000001f
        )
        {
            return
                radial.normalized;
        }


        return
            Vector3.up;
    }


    // =========================================================
    // CLIP ONE TRIANGLE AGAINST PLANE
    // =========================================================

    private static List<SliceVertex>
        ClipTriangleAgainstPlane(
            List<SliceVertex> input,
            Vector3 planeNormal,
            float planeOffset,
            bool keepNegative
        )
    {
        List<SliceVertex> output =
            new List<SliceVertex>();


        if (
            input == null ||
            input.Count < 3
        )
        {
            return output;
        }


        for (
            int index = 0;
            index < input.Count;
            index++
        )
        {
            SliceVertex current =
                input[index];


            SliceVertex next =
                input[
                    (
                        index + 1
                    ) %
                    input.Count
                ];


            float currentDistance =
                SignedDistance(
                    current.position,
                    planeNormal,
                    planeOffset
                );


            float nextDistance =
                SignedDistance(
                    next.position,
                    planeNormal,
                    planeOffset
                );


            bool currentInside =
                keepNegative
                    ? currentDistance <=
                      PlaneEpsilon
                    : currentDistance >=
                      -PlaneEpsilon;


            bool nextInside =
                keepNegative
                    ? nextDistance <=
                      PlaneEpsilon
                    : nextDistance >=
                      -PlaneEpsilon;


            // =================================================
            // INSIDE → INSIDE
            // =================================================

            if (
                currentInside &&
                nextInside
            )
            {
                output.Add(
                    next
                );
            }

            // =================================================
            // INSIDE → OUTSIDE
            // =================================================

            else if (
                currentInside &&
                !nextInside
            )
            {
                output.Add(
                    CalculateIntersectionVertex(
                        current,
                        next,
                        currentDistance,
                        nextDistance
                    )
                );
            }

            // =================================================
            // OUTSIDE → INSIDE
            // =================================================

            else if (
                !currentInside &&
                nextInside
            )
            {
                output.Add(
                    CalculateIntersectionVertex(
                        current,
                        next,
                        currentDistance,
                        nextDistance
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
    // INTERPOLATE EDGE INTERSECTION
    // =========================================================

    private static SliceVertex
        CalculateIntersectionVertex(
            SliceVertex a,
            SliceVertex b,
            float distanceA,
            float distanceB
        )
    {
        float denominator =
            distanceA -
            distanceB;


        float t;


        if (
            Mathf.Abs(
                denominator
            ) <=
            PlaneEpsilon
        )
        {
            t =
                0.5f;
        }
        else
        {
            t =
                distanceA /
                denominator;
        }


        t =
            Mathf.Clamp01(
                t
            );


        Vector3 position =
            Vector3.Lerp(
                a.position,
                b.position,
                t
            );


        Vector3 interpolatedNormal =
            Vector3.Lerp(
                a.normal,
                b.normal,
                t
            );


        if (
            interpolatedNormal.sqrMagnitude >
            0.000001f
        )
        {
            interpolatedNormal.Normalize();
        }


        return
            new SliceVertex(
                position,
                interpolatedNormal
            );
    }


    // =========================================================
    // SIGNED PLANE DISTANCE
    // =========================================================

    private static float SignedDistance(
        Vector3 point,
        Vector3 planeNormal,
        float planeOffset
    )
    {
        return
            Vector3.Dot(
                planeNormal,
                point
            ) -
            planeOffset;
    }


    // =========================================================
    // COLLECT CUT POINTS FROM ONE SOURCE TRIANGLE
    // =========================================================

    private static void CollectTrianglePlaneIntersections(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 planeNormal,
        float planeOffset,
        List<Vector3> points
    )
    {
        CollectEdgePlaneIntersection(
            a,
            b,
            planeNormal,
            planeOffset,
            points
        );


        CollectEdgePlaneIntersection(
            b,
            c,
            planeNormal,
            planeOffset,
            points
        );


        CollectEdgePlaneIntersection(
            c,
            a,
            planeNormal,
            planeOffset,
            points
        );
    }


    // =========================================================
    // COLLECT CUT POINT FROM ONE EDGE
    // =========================================================

    private static void CollectEdgePlaneIntersection(
        Vector3 a,
        Vector3 b,
        Vector3 planeNormal,
        float planeOffset,
        List<Vector3> points
    )
    {
        float distanceA =
            SignedDistance(
                a,
                planeNormal,
                planeOffset
            );


        float distanceB =
            SignedDistance(
                b,
                planeNormal,
                planeOffset
            );


        bool aOnPlane =
            Mathf.Abs(
                distanceA
            ) <=
            PlaneEpsilon;


        bool bOnPlane =
            Mathf.Abs(
                distanceB
            ) <=
            PlaneEpsilon;


        if (aOnPlane)
        {
            AddUniquePoint(
                points,
                a
            );
        }


        if (bOnPlane)
        {
            AddUniquePoint(
                points,
                b
            );
        }


        bool crossesPlane =
            (
                distanceA <
                -PlaneEpsilon &&

                distanceB >
                PlaneEpsilon
            ) ||

            (
                distanceA >
                PlaneEpsilon &&

                distanceB <
                -PlaneEpsilon
            );


        if (!crossesPlane)
        {
            return;
        }


        float t =
            distanceA /
            (
                distanceA -
                distanceB
            );


        Vector3 intersection =
            Vector3.Lerp(
                a,
                b,
                t
            );


        AddUniquePoint(
            points,
            intersection
        );
    }


    // =========================================================
    // UNIQUE CAP POINTS
    // =========================================================

    private static void AddUniquePoint(
        List<Vector3> points,
        Vector3 newPoint
    )
    {
        for (
            int index = 0;
            index < points.Count;
            index++
        )
        {
            if (
                Vector3.Distance(
                    points[index],
                    newPoint
                ) <=
                PointMergeTolerance
            )
            {
                return;
            }
        }


        points.Add(
            newPoint
        );
    }


    // =========================================================
    // SORT CUT POLYGON AROUND ITS CENTRE
    // =========================================================

    private static List<Vector3> SortCapPoints(
        List<Vector3> points,
        Vector3 planeNormal
    )
    {
        List<Vector3> sorted =
            new List<Vector3>(
                points
            );


        if (
            sorted.Count <
            3
        )
        {
            return sorted;
        }


        Vector3 centre =
            Vector3.zero;


        for (
            int index = 0;
            index < sorted.Count;
            index++
        )
        {
            centre +=
                sorted[index];
        }


        centre /=
            sorted.Count;


        // =====================================================
        // CREATE TWO AXES LYING ON THE CUTTING PLANE
        // =====================================================

        Vector3 axisU =
            Vector3.Cross(
                planeNormal,
                Vector3.forward
            );


        if (
            axisU.sqrMagnitude <
            0.000001f
        )
        {
            axisU =
                Vector3.Cross(
                    planeNormal,
                    Vector3.right
                );
        }


        axisU.Normalize();


        Vector3 axisV =
            Vector3.Cross(
                planeNormal,
                axisU
            ).normalized;


        sorted.Sort(
            delegate (
                Vector3 first,
                Vector3 second
            )
            {
                Vector3 deltaFirst =
                    first -
                    centre;


                Vector3 deltaSecond =
                    second -
                    centre;


                float angleFirst =
                    Mathf.Atan2(
                        Vector3.Dot(
                            deltaFirst,
                            axisV
                        ),

                        Vector3.Dot(
                            deltaFirst,
                            axisU
                        )
                    );


                float angleSecond =
                    Mathf.Atan2(
                        Vector3.Dot(
                            deltaSecond,
                            axisV
                        ),

                        Vector3.Dot(
                            deltaSecond,
                            axisU
                        )
                    );


                return
                    angleFirst.CompareTo(
                        angleSecond
                    );
            }
        );


        return sorted;
    }


    // =========================================================
    // CALCULATE CLOSED MESH VOLUME
    // =========================================================

    private static float CalculateVolume(
        Mesh mesh
    )
    {
        if (mesh == null)
        {
            return 0f;
        }


        Vector3[] vertices =
            mesh.vertices;


        int[] triangles =
            mesh.triangles;


        double signedVolume =
            0.0;


        for (
            int index = 0;
            index <
            triangles.Length;
            index += 3
        )
        {
            Vector3 a =
                vertices[
                    triangles[index]
                ];


            Vector3 b =
                vertices[
                    triangles[index + 1]
                ];


            Vector3 c =
                vertices[
                    triangles[index + 2]
                ];


            signedVolume +=
                Vector3.Dot(
                    a,
                    Vector3.Cross(
                        b,
                        c
                    )
                ) /
                6.0;
        }


        return
            Mathf.Abs(
                (float)signedVolume
            );
    }


    // =========================================================
    // CREATE PHYSICS PIECE
    // =========================================================

    private static void CreatePiece(
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
            "Create Cylinder Angled Piece"
        );


        piece.transform.SetParent(
            parent,
            false
        );


        piece.transform.localPosition =
            Vector3.zero;


        piece.transform.localRotation =
            Quaternion.identity;


        piece.transform.localScale =
            Vector3.one;


        // =====================================================
        // MESH FILTER
        // =====================================================

        MeshFilter meshFilter =
            piece.AddComponent<MeshFilter>();


        meshFilter.sharedMesh =
            mesh;


        // =====================================================
        // MESH RENDERER
        // =====================================================

        MeshRenderer meshRenderer =
            piece.AddComponent<MeshRenderer>();


        meshRenderer.sharedMaterial =
            material;


        // =====================================================
        // MESH COLLIDER
        // =====================================================

        MeshCollider meshCollider =
            piece.AddComponent<MeshCollider>();


        meshCollider.sharedMesh =
            mesh;


        meshCollider.convex =
            true;


        meshCollider.isTrigger =
            false;


        // =====================================================
        // RIGIDBODY
        // =====================================================

        Rigidbody rigidbody =
            piece.AddComponent<Rigidbody>();


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
    // DELETE OLD RESULT GROUP
    // =========================================================

    private static void DeleteExistingChild(
        Transform parent,
        string childName
    )
    {
        Transform existing =
            parent.Find(
                childName
            );


        if (existing != null)
        {
            Undo.DestroyObjectImmediate(
                existing.gameObject
            );
        }
    }
}
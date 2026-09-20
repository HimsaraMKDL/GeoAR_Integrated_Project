using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_CylinderVerticalFractionsMeshGenerator
{
    // =========================================================
    // PATHS
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedCylinderMeshes";


    private const string PieceAMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartA_Mat.mat";


    private const string PieceBMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartB_Mat.mat";


    // =========================================================
    // CYLINDER VALUES
    // =========================================================

    private const float Radius =
        0.14f;


    private const float Height =
        0.20f;


    private const int Segments =
        32;


    private const float Epsilon =
        0.000001f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Cylinder Vertical Fractions"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        // =====================================================
        // CHECK SELECTION
        // =====================================================

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Vertical Fractions",
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
                "C2 Cylinder Vertical Fractions",
                "Please select C2_CylinderSliceResults.",
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
                "C2 Cylinder Vertical Fractions",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // =====================================================
        // REMOVE OLD VERTICAL RESULTS IF TOOL IS RUN AGAIN
        // =====================================================

        DeleteExistingChild(
            selected.transform,
            "C2_CylinderVertical_1_2"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderVertical_1_3"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderVertical_1_4"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderVertical_3_4"
        );


        // =====================================================
        // GENERATE ALL 4 FRACTIONS
        // =====================================================

        CreateVerticalResult(
            selected.transform,
            "C2_CylinderVertical_1_2",
            "C2_CylinderVerticalHalf",
            0.5f,
            materialA,
            materialB
        );


        CreateVerticalResult(
            selected.transform,
            "C2_CylinderVertical_1_3",
            "C2_CylinderVerticalThird",
            1f / 3f,
            materialA,
            materialB
        );


        CreateVerticalResult(
            selected.transform,
            "C2_CylinderVertical_1_4",
            "C2_CylinderVerticalQuarter",
            0.25f,
            materialA,
            materialB
        );


        CreateVerticalResult(
            selected.transform,
            "C2_CylinderVertical_3_4",
            "C2_CylinderVerticalThreeQuarter",
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
            "C2 Cylinder: All Vertical fractions generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Cylinder Vertical Fractions",
            "Cylinder Vertical 1/2, 1/3, " +
            "1/4 and 3/4 generated successfully.",
            "OK"
        );
    }


    // =========================================================
    // CREATE ONE RESULT
    // =========================================================

    private static void CreateVerticalResult(
        Transform parent,
        string resultName,
        string piecePrefix,
        float targetFraction,
        Material materialA,
        Material materialB
    )
    {
        List<Vector2> fullCircle =
            CreateCirclePolygon();


        float cutX =
            FindCutXForFraction(
                fullCircle,
                targetFraction
            );


        // Piece A / Blue = selected fraction.
        List<Vector2> leftPolygon =
            ClipPolygonByX(
                fullCircle,
                cutX,
                true
            );


        // Piece B / Orange = remaining fraction.
        List<Vector2> rightPolygon =
            ClipPolygonByX(
                fullCircle,
                cutX,
                false
            );


        Mesh leftMesh =
            BuildExtrudedSectionMesh(
                leftPolygon,
                cutX,
                true
            );


        Mesh rightMesh =
            BuildExtrudedSectionMesh(
                rightPolygon,
                cutX,
                false
            );


        string leftName =
            piecePrefix +
            "_Left";


        string rightName =
            piecePrefix +
            "_Right";


        leftMesh.name =
            leftName +
            "_Mesh";


        rightMesh.name =
            rightName +
            "_Mesh";


        string leftMeshPath =
            OutputFolder +
            "/" +
            leftName +
            "_Mesh.asset";


        string rightMeshPath =
            OutputFolder +
            "/" +
            rightName +
            "_Mesh.asset";


        AssetDatabase.DeleteAsset(
            leftMeshPath
        );


        AssetDatabase.DeleteAsset(
            rightMeshPath
        );


        AssetDatabase.CreateAsset(
            leftMesh,
            leftMeshPath
        );


        AssetDatabase.CreateAsset(
            rightMesh,
            rightMeshPath
        );


        // =====================================================
        // RESULT GROUP
        // =====================================================

        GameObject resultGroup =
            new GameObject(
                resultName
            );


        Undo.RegisterCreatedObjectUndo(
            resultGroup,
            "Create Cylinder Vertical Result"
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
        // PIECES
        // =====================================================

        CreatePiece(
            resultGroup.transform,
            leftName,
            leftMesh,
            materialA
        );


        CreatePiece(
            resultGroup.transform,
            rightName,
            rightMesh,
            materialB
        );


        resultGroup.SetActive(
            false
        );


        float totalArea =
            CalculatePolygonArea(
                fullCircle
            );


        float actualLeftFraction =
            CalculatePolygonArea(
                leftPolygon
            ) /
            totalArea;


        Debug.Log(
            "C2 Cylinder: " +
            resultName +
            " generated. Cut X = " +
            cutX.ToString("F6") +
            ", Actual Left Fraction = " +
            actualLeftFraction.ToString("F4")
        );
    }


    // =========================================================
    // CREATE 32-SIDED CIRCULAR CROSS-SECTION
    // =========================================================

    private static List<Vector2>
        CreateCirclePolygon()
    {
        List<Vector2> polygon =
            new List<Vector2>();


        for (
            int segment = 0;
            segment < Segments;
            segment++
        )
        {
            float angle =
                (
                    segment /
                    (float)Segments
                ) *
                Mathf.PI *
                2f;


            float x =
                Mathf.Cos(angle) *
                Radius;


            float z =
                Mathf.Sin(angle) *
                Radius;


            polygon.Add(
                new Vector2(
                    x,
                    z
                )
            );
        }


        return polygon;
    }


    // =========================================================
    // FIND CUT X USING BINARY SEARCH
    // =========================================================

    private static float FindCutXForFraction(
        List<Vector2> fullPolygon,
        float targetFraction
    )
    {
        float totalArea =
            CalculatePolygonArea(
                fullPolygon
            );


        float minX =
            -Radius;


        float maxX =
            Radius;


        // 64 iterations gives much more precision
        // than we need for this educational mesh.

        for (
            int iteration = 0;
            iteration < 64;
            iteration++
        )
        {
            float cutX =
                (
                    minX +
                    maxX
                ) *
                0.5f;


            List<Vector2> leftPolygon =
                ClipPolygonByX(
                    fullPolygon,
                    cutX,
                    true
                );


            float leftArea =
                CalculatePolygonArea(
                    leftPolygon
                );


            float fraction =
                leftArea /
                totalArea;


            if (
                fraction <
                targetFraction
            )
            {
                minX =
                    cutX;
            }
            else
            {
                maxX =
                    cutX;
            }
        }


        return
            (
                minX +
                maxX
            ) *
            0.5f;
    }


    // =========================================================
    // CLIP POLYGON AGAINST X = CUT
    // =========================================================

    private static List<Vector2>
        ClipPolygonByX(
            List<Vector2> inputPolygon,
            float cutX,
            bool keepLeft
        )
    {
        List<Vector2> output =
            new List<Vector2>();


        if (
            inputPolygon == null ||
            inputPolygon.Count < 3
        )
        {
            return output;
        }


        for (
            int index = 0;
            index < inputPolygon.Count;
            index++
        )
        {
            Vector2 current =
                inputPolygon[index];


            Vector2 next =
                inputPolygon[
                    (index + 1) %
                    inputPolygon.Count
                ];


            bool currentInside =
                IsInsideX(
                    current,
                    cutX,
                    keepLeft
                );


            bool nextInside =
                IsInsideX(
                    next,
                    cutX,
                    keepLeft
                );


            // -------------------------------------------------
            // INSIDE → INSIDE
            // -------------------------------------------------

            if (
                currentInside &&
                nextInside
            )
            {
                AddUniquePoint(
                    output,
                    next
                );
            }

            // -------------------------------------------------
            // INSIDE → OUTSIDE
            // -------------------------------------------------

            else if (
                currentInside &&
                !nextInside
            )
            {
                AddUniquePoint(
                    output,
                    FindXIntersection(
                        current,
                        next,
                        cutX
                    )
                );
            }

            // -------------------------------------------------
            // OUTSIDE → INSIDE
            // -------------------------------------------------

            else if (
                !currentInside &&
                nextInside
            )
            {
                AddUniquePoint(
                    output,
                    FindXIntersection(
                        current,
                        next,
                        cutX
                    )
                );


                AddUniquePoint(
                    output,
                    next
                );
            }
        }


        return output;
    }


    // =========================================================
    // POINT INSIDE HALF-PLANE?
    // =========================================================

    private static bool IsInsideX(
        Vector2 point,
        float cutX,
        bool keepLeft
    )
    {
        if (keepLeft)
        {
            return
                point.x <=
                cutX + Epsilon;
        }


        return
            point.x >=
            cutX - Epsilon;
    }


    // =========================================================
    // INTERSECTION WITH X = CUT
    // =========================================================

    private static Vector2 FindXIntersection(
        Vector2 a,
        Vector2 b,
        float cutX
    )
    {
        float deltaX =
            b.x -
            a.x;


        if (
            Mathf.Abs(
                deltaX
            ) <=
            Epsilon
        )
        {
            return
                new Vector2(
                    cutX,
                    a.y
                );
        }


        float t =
            (
                cutX -
                a.x
            ) /
            deltaX;


        return
            new Vector2(
                cutX,
                Mathf.Lerp(
                    a.y,
                    b.y,
                    t
                )
            );
    }


    // =========================================================
    // PREVENT DUPLICATE POLYGON POINTS
    // =========================================================

    private static void AddUniquePoint(
        List<Vector2> points,
        Vector2 point
    )
    {
        if (points.Count > 0)
        {
            Vector2 previous =
                points[
                    points.Count - 1
                ];


            if (
                Vector2.Distance(
                    previous,
                    point
                ) <=
                0.00001f
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
    // POLYGON AREA
    // =========================================================

    private static float CalculatePolygonArea(
        List<Vector2> polygon
    )
    {
        if (
            polygon == null ||
            polygon.Count < 3
        )
        {
            return 0f;
        }


        float signedArea =
            0f;


        for (
            int index = 0;
            index < polygon.Count;
            index++
        )
        {
            Vector2 current =
                polygon[index];


            Vector2 next =
                polygon[
                    (index + 1) %
                    polygon.Count
                ];


            signedArea +=
                current.x *
                next.y;


            signedArea -=
                next.x *
                current.y;
        }


        return
            Mathf.Abs(
                signedArea
            ) *
            0.5f;
    }


    // =========================================================
    // BUILD EXTRUDED CYLINDER SECTION
    // =========================================================

    private static Mesh BuildExtrudedSectionMesh(
        List<Vector2> polygon,
        float cutX,
        bool isLeftPiece
    )
    {
        Mesh mesh =
            new Mesh();


        List<Vector3> vertices =
            new List<Vector3>();


        List<Vector3> normals =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        // =====================================================
        // BOTTOM CAP
        // =====================================================

        AddCap(
            polygon,
            0f,
            false,
            vertices,
            normals,
            triangles
        );


        // =====================================================
        // TOP CAP
        // =====================================================

        AddCap(
            polygon,
            Height,
            true,
            vertices,
            normals,
            triangles
        );


        // =====================================================
        // SIDE WALLS
        // =====================================================

        AddSideWalls(
            polygon,
            cutX,
            isLeftPiece,
            vertices,
            normals,
            triangles
        );


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


    // =========================================================
    // ADD TOP OR BOTTOM CAP
    // =========================================================

    private static void AddCap(
        List<Vector2> polygon,
        float y,
        bool top,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<int> triangles
    )
    {
        Vector2 centre2D =
            CalculateAveragePoint(
                polygon
            );


        int centreIndex =
            vertices.Count;


        vertices.Add(
            new Vector3(
                centre2D.x,
                y,
                centre2D.y
            )
        );


        Vector3 capNormal =
            top
                ? Vector3.up
                : Vector3.down;


        normals.Add(
            capNormal
        );


        int ringStart =
            vertices.Count;


        for (
            int index = 0;
            index < polygon.Count;
            index++
        )
        {
            Vector2 point =
                polygon[index];


            vertices.Add(
                new Vector3(
                    point.x,
                    y,
                    point.y
                )
            );


            normals.Add(
                capNormal
            );
        }


        for (
            int index = 0;
            index < polygon.Count;
            index++
        )
        {
            int nextIndex =
                (
                    index + 1
                ) %
                polygon.Count;


            int current =
                ringStart +
                index;


            int next =
                ringStart +
                nextIndex;


            if (top)
            {
                // +Y normal.

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
            else
            {
                // -Y normal.

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
        }
    }


    // =========================================================
    // ADD CURVED WALL + FLAT CUT WALL
    // =========================================================

    private static void AddSideWalls(
        List<Vector2> polygon,
        float cutX,
        bool isLeftPiece,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<int> triangles
    )
    {
        for (
            int index = 0;
            index < polygon.Count;
            index++
        )
        {
            Vector2 a =
                polygon[index];


            Vector2 b =
                polygon[
                    (index + 1) %
                    polygon.Count
                ];


            bool isCutEdge =
                Mathf.Abs(
                    a.x -
                    cutX
                ) <=
                0.00002f &&

                Mathf.Abs(
                    b.x -
                    cutX
                ) <=
                0.00002f;


            Vector3 normalA;

            Vector3 normalB;


            if (isCutEdge)
            {
                // Left piece cut face points toward +X.
                // Right piece cut face points toward -X.

                Vector3 cutNormal =
                    isLeftPiece
                        ? Vector3.right
                        : Vector3.left;


                normalA =
                    cutNormal;


                normalB =
                    cutNormal;
            }
            else
            {
                // Radial normals keep the outer
                // cylinder surface visually smooth.

                normalA =
                    new Vector3(
                        a.x,
                        0f,
                        a.y
                    ).normalized;


                normalB =
                    new Vector3(
                        b.x,
                        0f,
                        b.y
                    ).normalized;
            }


            int startIndex =
                vertices.Count;


            // Bottom A
            vertices.Add(
                new Vector3(
                    a.x,
                    0f,
                    a.y
                )
            );

            normals.Add(
                normalA
            );


            // Top A
            vertices.Add(
                new Vector3(
                    a.x,
                    Height,
                    a.y
                )
            );

            normals.Add(
                normalA
            );


            // Top B
            vertices.Add(
                new Vector3(
                    b.x,
                    Height,
                    b.y
                )
            );

            normals.Add(
                normalB
            );


            // Bottom B
            vertices.Add(
                new Vector3(
                    b.x,
                    0f,
                    b.y
                )
            );

            normals.Add(
                normalB
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


            triangles.Add(
                startIndex
            );

            triangles.Add(
                startIndex + 2
            );

            triangles.Add(
                startIndex + 3
            );
        }
    }


    // =========================================================
    // AVERAGE POINT FOR CAP FAN
    // =========================================================

    private static Vector2 CalculateAveragePoint(
        List<Vector2> polygon
    )
    {
        Vector2 total =
            Vector2.zero;


        for (
            int index = 0;
            index < polygon.Count;
            index++
        )
        {
            total +=
                polygon[index];
        }


        return
            total /
            polygon.Count;
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
            "Create Cylinder Vertical Piece"
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


        MeshFilter meshFilter =
            piece.AddComponent<MeshFilter>();


        MeshRenderer meshRenderer =
            piece.AddComponent<MeshRenderer>();


        MeshCollider meshCollider =
            piece.AddComponent<MeshCollider>();


        Rigidbody rigidbody =
            piece.AddComponent<Rigidbody>();


        meshFilter.sharedMesh =
            mesh;


        meshRenderer.sharedMaterial =
            material;


        meshCollider.sharedMesh =
            mesh;


        meshCollider.convex =
            true;


        meshCollider.isTrigger =
            false;


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
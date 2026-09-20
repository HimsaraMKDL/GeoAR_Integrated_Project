using UnityEditor;
using UnityEngine;

public static class C2_CylinderHorizontalFractionsMeshGenerator
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


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Cylinder Horizontal Fractions"
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
                "C2 Cylinder Horizontal Fractions",
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
                "C2 Cylinder Horizontal Fractions",
                "Please select C2_CylinderSliceResults.",
                "OK"
            );

            return;
        }


        // =====================================================
        // CHECK GENERATED MESH FOLDER
        // =====================================================

        if (
            !AssetDatabase.IsValidFolder(
                OutputFolder
            )
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Cylinder Horizontal Fractions",
                "Generated Cylinder mesh folder is missing:\n\n" +
                OutputFolder,
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
                "C2 Cylinder Horizontal Fractions",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // =====================================================
        // DELETE OLD HORIZONTAL RESULTS
        // =====================================================

        DeleteExistingChild(
            selected.transform,
            "C2_CylinderHorizontal_1_2"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderHorizontal_1_3"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderHorizontal_1_4"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_CylinderHorizontal_3_4"
        );


        // =====================================================
        // GENERATE 1/2
        // =====================================================

        CreateHorizontalResult(
            selected.transform,
            "C2_CylinderHorizontal_1_2",
            "C2_CylinderHorizontalHalf",
            0.5f,
            materialA,
            materialB
        );


        // =====================================================
        // GENERATE 1/3
        // =====================================================

        CreateHorizontalResult(
            selected.transform,
            "C2_CylinderHorizontal_1_3",
            "C2_CylinderHorizontalThird",
            1f / 3f,
            materialA,
            materialB
        );


        // =====================================================
        // GENERATE 1/4
        // =====================================================

        CreateHorizontalResult(
            selected.transform,
            "C2_CylinderHorizontal_1_4",
            "C2_CylinderHorizontalQuarter",
            0.25f,
            materialA,
            materialB
        );


        // =====================================================
        // GENERATE 3/4
        // =====================================================

        CreateHorizontalResult(
            selected.transform,
            "C2_CylinderHorizontal_3_4",
            "C2_CylinderHorizontalThreeQuarter",
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
            "C2 Cylinder: All Horizontal fractions generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Cylinder Horizontal Fractions",
            "Cylinder Horizontal 1/2, 1/3, " +
            "1/4 and 3/4 generated successfully.",
            "OK"
        );
    }


    // =========================================================
    // CREATE ONE HORIZONTAL RESULT
    // =========================================================

    private static void CreateHorizontalResult(
        Transform parent,
        string resultName,
        string piecePrefix,
        float selectedFraction,
        Material materialA,
        Material materialB
    )
    {
        float cutY =
            Height *
            selectedFraction;


        // =====================================================
        // BOTTOM PIECE
        // =====================================================

        Mesh bottomMesh =
            BuildCylinderSectionMesh(
                0f,
                cutY
            );


        // =====================================================
        // TOP PIECE
        // =====================================================

        Mesh topMesh =
            BuildCylinderSectionMesh(
                cutY,
                Height
            );


        string bottomName =
            piecePrefix +
            "_Bottom";


        string topName =
            piecePrefix +
            "_Top";


        bottomMesh.name =
            bottomName +
            "_Mesh";


        topMesh.name =
            topName +
            "_Mesh";


        string bottomPath =
            OutputFolder +
            "/" +
            bottomName +
            "_Mesh.asset";


        string topPath =
            OutputFolder +
            "/" +
            topName +
            "_Mesh.asset";


        // =====================================================
        // REPLACE OLD MESH ASSETS IF GENERATOR RUN AGAIN
        // =====================================================

        AssetDatabase.DeleteAsset(
            bottomPath
        );


        AssetDatabase.DeleteAsset(
            topPath
        );


        AssetDatabase.CreateAsset(
            bottomMesh,
            bottomPath
        );


        AssetDatabase.CreateAsset(
            topMesh,
            topPath
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
            "Create Cylinder Horizontal Result"
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
        // CREATE BOTTOM
        // =====================================================

        CreatePiece(
            resultGroup.transform,
            bottomName,
            bottomMesh,
            materialA
        );


        // =====================================================
        // CREATE TOP
        // =====================================================

        CreatePiece(
            resultGroup.transform,
            topName,
            topMesh,
            materialB
        );


        resultGroup.SetActive(
            false
        );


        float actualBottomFraction =
            (
                cutY -
                0f
            ) /
            Height;


        float actualTopFraction =
            (
                Height -
                cutY
            ) /
            Height;


        Debug.Log(
            "C2 Cylinder: " +
            resultName +
            " generated. Cut Y = " +
            cutY.ToString("F6") +
            ", Bottom Fraction = " +
            actualBottomFraction.ToString("F4") +
            ", Top Fraction = " +
            actualTopFraction.ToString("F4")
        );
    }


    // =========================================================
    // BUILD A COMPLETE CYLINDER SECTION
    // =========================================================
    //
    // yMin = bottom of this piece
    // yMax = top of this piece
    //
    // This creates:
    //
    // - smooth curved outside wall
    // - flat closed bottom
    // - flat closed top
    //
    // For a sliced piece, one of these flat caps becomes
    // the newly exposed slicing face.
    // =========================================================

    private static Mesh BuildCylinderSectionMesh(
        float yMin,
        float yMax
    )
    {
        Mesh mesh =
            new Mesh();


        // =====================================================
        // VERTEX COUNTS
        // =====================================================

        int sideVertexCount =
            Segments *
            2;


        int bottomCapVertexCount =
            Segments +
            1;


        int topCapVertexCount =
            Segments +
            1;


        int totalVertexCount =
            sideVertexCount +
            bottomCapVertexCount +
            topCapVertexCount;


        Vector3[] vertices =
            new Vector3[
                totalVertexCount
            ];


        Vector3[] normals =
            new Vector3[
                totalVertexCount
            ];


        int triangleCount =
            (
                Segments *
                2
            ) +
            Segments +
            Segments;


        int[] triangles =
            new int[
                triangleCount *
                3
            ];


        int vertexIndex =
            0;


        int triangleIndex =
            0;


        // =====================================================
        // CURVED SIDE - BOTTOM RING
        // =====================================================

        int sideBottomStart =
            vertexIndex;


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


            vertices[
                vertexIndex
            ] =
                new Vector3(
                    x,
                    yMin,
                    z
                );


            normals[
                vertexIndex
            ] =
                new Vector3(
                    x,
                    0f,
                    z
                ).normalized;


            vertexIndex++;
        }


        // =====================================================
        // CURVED SIDE - TOP RING
        // =====================================================

        int sideTopStart =
            vertexIndex;


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


            vertices[
                vertexIndex
            ] =
                new Vector3(
                    x,
                    yMax,
                    z
                );


            normals[
                vertexIndex
            ] =
                new Vector3(
                    x,
                    0f,
                    z
                ).normalized;


            vertexIndex++;
        }


        // =====================================================
        // CURVED SIDE TRIANGLES
        // =====================================================

        for (
            int segment = 0;
            segment < Segments;
            segment++
        )
        {
            int next =
                (
                    segment + 1
                ) %
                Segments;


            int bottomCurrent =
                sideBottomStart +
                segment;


            int bottomNext =
                sideBottomStart +
                next;


            int topCurrent =
                sideTopStart +
                segment;


            int topNext =
                sideTopStart +
                next;


            // Triangle 1

            triangles[
                triangleIndex++
            ] =
                bottomCurrent;


            triangles[
                triangleIndex++
            ] =
                topCurrent;


            triangles[
                triangleIndex++
            ] =
                topNext;


            // Triangle 2

            triangles[
                triangleIndex++
            ] =
                bottomCurrent;


            triangles[
                triangleIndex++
            ] =
                topNext;


            triangles[
                triangleIndex++
            ] =
                bottomNext;
        }


        // =====================================================
        // BOTTOM CAP CENTRE
        // =====================================================

        int bottomCentre =
            vertexIndex;


        vertices[
            vertexIndex
        ] =
            new Vector3(
                0f,
                yMin,
                0f
            );


        normals[
            vertexIndex
        ] =
            Vector3.down;


        vertexIndex++;


        // =====================================================
        // BOTTOM CAP RING
        // =====================================================

        int bottomCapRingStart =
            vertexIndex;


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


            vertices[
                vertexIndex
            ] =
                new Vector3(
                    x,
                    yMin,
                    z
                );


            normals[
                vertexIndex
            ] =
                Vector3.down;


            vertexIndex++;
        }


        // =====================================================
        // BOTTOM CAP TRIANGLES
        // =====================================================

        for (
            int segment = 0;
            segment < Segments;
            segment++
        )
        {
            int next =
                (
                    segment + 1
                ) %
                Segments;


            int currentIndex =
                bottomCapRingStart +
                segment;


            int nextIndex =
                bottomCapRingStart +
                next;


            // -Y outward normal.

            triangles[
                triangleIndex++
            ] =
                bottomCentre;


            triangles[
                triangleIndex++
            ] =
                currentIndex;


            triangles[
                triangleIndex++
            ] =
                nextIndex;
        }


        // =====================================================
        // TOP CAP CENTRE
        // =====================================================

        int topCentre =
            vertexIndex;


        vertices[
            vertexIndex
        ] =
            new Vector3(
                0f,
                yMax,
                0f
            );


        normals[
            vertexIndex
        ] =
            Vector3.up;


        vertexIndex++;


        // =====================================================
        // TOP CAP RING
        // =====================================================

        int topCapRingStart =
            vertexIndex;


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


            vertices[
                vertexIndex
            ] =
                new Vector3(
                    x,
                    yMax,
                    z
                );


            normals[
                vertexIndex
            ] =
                Vector3.up;


            vertexIndex++;
        }


        // =====================================================
        // TOP CAP TRIANGLES
        // =====================================================

        for (
            int segment = 0;
            segment < Segments;
            segment++
        )
        {
            int next =
                (
                    segment + 1
                ) %
                Segments;


            int currentIndex =
                topCapRingStart +
                segment;


            int nextIndex =
                topCapRingStart +
                next;


            // +Y outward normal.

            triangles[
                triangleIndex++
            ] =
                topCentre;


            triangles[
                triangleIndex++
            ] =
                nextIndex;


            triangles[
                triangleIndex++
            ] =
                currentIndex;
        }


        // =====================================================
        // APPLY TO MESH
        // =====================================================

        mesh.vertices =
            vertices;


        mesh.normals =
            normals;


        mesh.triangles =
            triangles;


        mesh.RecalculateBounds();


        return mesh;
    }


    // =========================================================
    // CREATE PHYSICS-READY RESULT PIECE
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
            "Create Cylinder Horizontal Piece"
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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_PrismHorizontalFractionsMeshGenerator
{
    // =========================================================
    // PATHS
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedPrismMeshes";

    private const string PieceAMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartA_Mat.mat";

    private const string PieceBMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartB_Mat.mat";


    // =========================================================
    // PRISM DIMENSIONS
    // =========================================================

    private const float HalfWidth = 0.15f;
    private const float Height = 0.20f;
    private const float Depth = 0.16f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Prism Horizontal Fractions"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Horizontal Fractions",
                "Open C2_PrismExperience.prefab and " +
                "select C2_PrismSliceResults first.",
                "OK"
            );

            return;
        }


        if (selected.name !=
            "C2_PrismSliceResults")
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Horizontal Fractions",
                "Please select C2_PrismSliceResults.",
                "OK"
            );

            return;
        }


        EnsureFolder(
            OutputFolder
        );


        Material materialA =
            AssetDatabase.LoadAssetAtPath<Material>(
                PieceAMaterialPath
            );


        Material materialB =
            AssetDatabase.LoadAssetAtPath<Material>(
                PieceBMaterialPath
            );


        if (materialA == null ||
            materialB == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Horizontal Fractions",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // Remove only the old horizontal groups
        // if the generator is run again.

        DeleteExistingChild(
            selected.transform,
            "C2_PrismHorizontal_1_2"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismHorizontal_1_3"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismHorizontal_1_4"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismHorizontal_3_4"
        );


        // =====================================================
        // CREATE ALL FOUR
        // =====================================================

        CreateHorizontalResult(
            selected.transform,
            "C2_PrismHorizontal_1_2",
            "C2_PrismHorizontalHalf",
            0.5f,
            materialA,
            materialB
        );


        CreateHorizontalResult(
            selected.transform,
            "C2_PrismHorizontal_1_3",
            "C2_PrismHorizontalThird",
            1f / 3f,
            materialA,
            materialB
        );


        CreateHorizontalResult(
            selected.transform,
            "C2_PrismHorizontal_1_4",
            "C2_PrismHorizontalQuarter",
            0.25f,
            materialA,
            materialB
        );


        CreateHorizontalResult(
            selected.transform,
            "C2_PrismHorizontal_3_4",
            "C2_PrismHorizontalThreeQuarter",
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
            "C2 Prism: All Horizontal fractions generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Prism Horizontal Fractions",
            "Prism Horizontal 1/2, 1/3, " +
            "1/4 and 3/4 were generated successfully.",
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
        float fraction,
        Material materialA,
        Material materialB
    )
    {
        float cutY =
            CalculateCutY(
                fraction
            );


        float cutHalfWidth =
            CalculateHalfWidthAtY(
                cutY
            );


        // =====================================================
        // BOTTOM POLYGON
        //
        // Blue = selected fraction
        // =====================================================

        List<Vector2> bottomPolygon =
            new List<Vector2>
            {
                new Vector2(
                    -HalfWidth,
                    0f
                ),

                new Vector2(
                    HalfWidth,
                    0f
                ),

                new Vector2(
                    cutHalfWidth,
                    cutY
                ),

                new Vector2(
                    -cutHalfWidth,
                    cutY
                )
            };


        // =====================================================
        // TOP POLYGON
        //
        // Orange = complement
        // =====================================================

        List<Vector2> topPolygon =
            new List<Vector2>
            {
                new Vector2(
                    -cutHalfWidth,
                    cutY
                ),

                new Vector2(
                    cutHalfWidth,
                    cutY
                ),

                new Vector2(
                    0f,
                    Height
                )
            };


        Mesh bottomMesh =
            BuildExtrudedMesh(
                bottomPolygon,
                Depth
            );


        Mesh topMesh =
            BuildExtrudedMesh(
                topPolygon,
                Depth
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


        string bottomMeshPath =
            OutputFolder +
            "/" +
            bottomName +
            "_Mesh.asset";


        string topMeshPath =
            OutputFolder +
            "/" +
            topName +
            "_Mesh.asset";


        AssetDatabase.DeleteAsset(
            bottomMeshPath
        );


        AssetDatabase.DeleteAsset(
            topMeshPath
        );


        AssetDatabase.CreateAsset(
            bottomMesh,
            bottomMeshPath
        );


        AssetDatabase.CreateAsset(
            topMesh,
            topMeshPath
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
            "Create C2 Prism Horizontal Result"
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
            bottomName,
            bottomMesh,
            materialA
        );


        CreatePiece(
            resultGroup.transform,
            topName,
            topMesh,
            materialB
        );


        resultGroup.SetActive(
            false
        );


        Debug.Log(
            "C2 Prism: " +
            resultName +
            " generated. Cut Y = " +
            cutY.ToString("F6") +
            ", Half Width = " +
            cutHalfWidth.ToString("F6")
        );
    }


    // =========================================================
    // FRACTION → CORRECT HORIZONTAL CUT Y
    // =========================================================

    private static float CalculateCutY(
        float fraction
    )
    {
        return
            Height *
            (
                1f -
                Mathf.Sqrt(
                    1f - fraction
                )
            );
    }


    // =========================================================
    // TRIANGLE HALF-WIDTH AT Y
    // =========================================================

    private static float CalculateHalfWidthAtY(
        float y
    )
    {
        return
            HalfWidth *
            (
                1f -
                (y / Height)
            );
    }


    // =========================================================
    // CREATE ONE PIECE
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
            "Create C2 Prism Horizontal Piece"
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
    // EXTRUDE POLYGON
    // =========================================================

    private static Mesh BuildExtrudedMesh(
        List<Vector2> polygon,
        float depth
    )
    {
        Mesh mesh =
            new Mesh();


        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        float frontZ =
            -depth * 0.5f;


        float backZ =
            depth * 0.5f;


        // Front
        AddPolygonFace(
            polygon,
            frontZ,
            true,
            vertices,
            triangles
        );


        // Back
        AddPolygonFace(
            polygon,
            backZ,
            false,
            vertices,
            triangles
        );


        // Side walls
        for (
            int i = 0;
            i < polygon.Count;
            i++
        )
        {
            Vector2 a =
                polygon[i];


            Vector2 b =
                polygon[
                    (i + 1) %
                    polygon.Count
                ];


            int startIndex =
                vertices.Count;


            vertices.Add(
                new Vector3(
                    a.x,
                    a.y,
                    frontZ
                )
            );


            vertices.Add(
                new Vector3(
                    b.x,
                    b.y,
                    frontZ
                )
            );


            vertices.Add(
                new Vector3(
                    b.x,
                    b.y,
                    backZ
                )
            );


            vertices.Add(
                new Vector3(
                    a.x,
                    a.y,
                    backZ
                )
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


        mesh.SetVertices(
            vertices
        );


        mesh.SetTriangles(
            triangles,
            0
        );


        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        return mesh;
    }


    // =========================================================
    // FRONT / BACK FACE
    // =========================================================

    private static void AddPolygonFace(
        List<Vector2> polygon,
        float z,
        bool reverse,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        int startIndex =
            vertices.Count;


        for (
            int i = 0;
            i < polygon.Count;
            i++
        )
        {
            vertices.Add(
                new Vector3(
                    polygon[i].x,
                    polygon[i].y,
                    z
                )
            );
        }


        for (
            int i = 1;
            i < polygon.Count - 1;
            i++
        )
        {
            if (reverse)
            {
                triangles.Add(
                    startIndex
                );

                triangles.Add(
                    startIndex + i + 1
                );

                triangles.Add(
                    startIndex + i
                );
            }
            else
            {
                triangles.Add(
                    startIndex
                );

                triangles.Add(
                    startIndex + i
                );

                triangles.Add(
                    startIndex + i + 1
                );
            }
        }
    }


    // =========================================================
    // DELETE OLD GROUP
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


    // =========================================================
    // FOLDER
    // =========================================================

    private static void EnsureFolder(
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
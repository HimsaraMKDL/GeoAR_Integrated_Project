using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_PrismVerticalFractionsMeshGenerator
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
        "Tools/C2/Generate Prism Remaining Vertical Fractions"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Vertical Fractions",
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
                "C2 Prism Vertical Fractions",
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
                "C2 Prism Vertical Fractions",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // Only remove the Stage 33 groups.
        // The already-working Stage 32 1/2 result is untouched.

        DeleteExistingChild(
            selected.transform,
            "C2_PrismVertical_1_3"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismVertical_1_4"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismVertical_3_4"
        );


        // =====================================================
        // GENERATE 1/3
        // =====================================================

        CreateVerticalResult(
            selected.transform,
            "C2_PrismVertical_1_3",
            "C2_PrismVerticalThird",
            1f / 3f,
            materialA,
            materialB
        );


        // =====================================================
        // GENERATE 1/4
        // =====================================================

        CreateVerticalResult(
            selected.transform,
            "C2_PrismVertical_1_4",
            "C2_PrismVerticalQuarter",
            0.25f,
            materialA,
            materialB
        );


        // =====================================================
        // GENERATE 3/4
        // =====================================================

        CreateVerticalResult(
            selected.transform,
            "C2_PrismVertical_3_4",
            "C2_PrismVerticalThreeQuarter",
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
            "C2 Prism: Remaining Vertical fractions generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Prism Vertical Fractions",
            "Prism Vertical 1/3, 1/4 and 3/4 " +
            "were generated successfully.",
            "OK"
        );
    }


    // =========================================================
    // CREATE ONE FRACTION RESULT
    // =========================================================

    private static void CreateVerticalResult(
        Transform parent,
        string resultName,
        string piecePrefix,
        float fraction,
        Material materialA,
        Material materialB
    )
    {
        float cutX =
            CalculateCutX(
                fraction
            );


        float cutTopY =
            CalculateTriangleTopY(
                cutX
            );


        List<Vector2> leftPolygon;
        List<Vector2> rightPolygon;


        // =====================================================
        // CUT LEFT OF CENTRE
        // 1/3 and 1/4
        // =====================================================

        if (cutX <= 0f)
        {
            leftPolygon =
                new List<Vector2>
                {
                    new Vector2(
                        -HalfWidth,
                        0f
                    ),

                    new Vector2(
                        cutX,
                        0f
                    ),

                    new Vector2(
                        cutX,
                        cutTopY
                    )
                };


            rightPolygon =
                new List<Vector2>
                {
                    new Vector2(
                        cutX,
                        0f
                    ),

                    new Vector2(
                        HalfWidth,
                        0f
                    ),

                    new Vector2(
                        0f,
                        Height
                    ),

                    new Vector2(
                        cutX,
                        cutTopY
                    )
                };
        }

        // =====================================================
        // CUT RIGHT OF CENTRE
        // 3/4
        // =====================================================

        else
        {
            leftPolygon =
                new List<Vector2>
                {
                    new Vector2(
                        -HalfWidth,
                        0f
                    ),

                    new Vector2(
                        cutX,
                        0f
                    ),

                    new Vector2(
                        cutX,
                        cutTopY
                    ),

                    new Vector2(
                        0f,
                        Height
                    )
                };


            rightPolygon =
                new List<Vector2>
                {
                    new Vector2(
                        cutX,
                        0f
                    ),

                    new Vector2(
                        HalfWidth,
                        0f
                    ),

                    new Vector2(
                        cutX,
                        cutTopY
                    )
                };
        }


        Mesh leftMesh =
            BuildExtrudedMesh(
                leftPolygon,
                Depth
            );


        Mesh rightMesh =
            BuildExtrudedMesh(
                rightPolygon,
                Depth
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
            "Create C2 Prism Vertical Result"
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


        Debug.Log(
            "C2 Prism: " +
            resultName +
            " generated. Cut X = " +
            cutX.ToString("F6") +
            ", Top Y = " +
            cutTopY.ToString("F6")
        );
    }


    // =========================================================
    // FRACTION → CORRECT CUT X
    // =========================================================

    private static float CalculateCutX(
        float fraction
    )
    {
        if (fraction <= 0.5f)
        {
            return
                -HalfWidth +
                HalfWidth *
                Mathf.Sqrt(
                    2f * fraction
                );
        }


        return
            HalfWidth -
            HalfWidth *
            Mathf.Sqrt(
                2f *
                (1f - fraction)
            );
    }


    // =========================================================
    // TRIANGLE TOP Y AT X
    // =========================================================

    private static float CalculateTriangleTopY(
        float x
    )
    {
        float normalizedX =
            Mathf.Abs(x) /
            HalfWidth;


        return
            Height *
            (1f - normalizedX);
    }


    // =========================================================
    // CREATE PIECE
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
            "Create C2 Prism Vertical Piece"
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
    // EXTRUDE 2D POLYGON
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


        AddPolygonFace(
            polygon,
            frontZ,
            true,
            vertices,
            triangles
        );


        AddPolygonFace(
            polygon,
            backZ,
            false,
            vertices,
            triangles
        );


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
    // DELETE OLD STAGE 33 GROUP
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
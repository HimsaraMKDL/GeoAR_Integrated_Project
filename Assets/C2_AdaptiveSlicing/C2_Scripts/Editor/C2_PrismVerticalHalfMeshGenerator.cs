using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_PrismVerticalHalfMeshGenerator
{
    // =========================================================
    // PATHS
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedPrismMeshes";

    private const string LeftMeshPath =
        OutputFolder +
        "/C2_PrismVerticalHalf_Left_Mesh.asset";

    private const string RightMeshPath =
        OutputFolder +
        "/C2_PrismVerticalHalf_Right_Mesh.asset";


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
        "Tools/C2/Generate Prism Vertical 1-2 Result"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Vertical 1/2",
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
                "C2 Prism Vertical 1/2",
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
                "C2 Prism Vertical 1/2",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // Remove an old generated result if this tool
        // is accidentally run again.

        Transform oldResult =
            selected.transform.Find(
                "C2_PrismVertical_1_2"
            );


        if (oldResult != null)
        {
            Undo.DestroyObjectImmediate(
                oldResult.gameObject
            );
        }


        // =====================================================
        // LEFT POLYGON
        // =====================================================

        List<Vector2> leftPolygon =
            new List<Vector2>
            {
                new Vector2(
                    -HalfWidth,
                    0f
                ),

                new Vector2(
                    0f,
                    0f
                ),

                new Vector2(
                    0f,
                    Height
                )
            };


        // =====================================================
        // RIGHT POLYGON
        // =====================================================

        List<Vector2> rightPolygon =
            new List<Vector2>
            {
                new Vector2(
                    0f,
                    0f
                ),

                new Vector2(
                    HalfWidth,
                    0f
                ),

                new Vector2(
                    0f,
                    Height
                )
            };


        Mesh leftMesh =
            BuildExtrudedMesh(
                leftPolygon,
                Depth
            );


        leftMesh.name =
            "C2_PrismVerticalHalf_Left_Mesh";


        Mesh rightMesh =
            BuildExtrudedMesh(
                rightPolygon,
                Depth
            );


        rightMesh.name =
            "C2_PrismVerticalHalf_Right_Mesh";


        AssetDatabase.DeleteAsset(
            LeftMeshPath
        );

        AssetDatabase.DeleteAsset(
            RightMeshPath
        );


        AssetDatabase.CreateAsset(
            leftMesh,
            LeftMeshPath
        );


        AssetDatabase.CreateAsset(
            rightMesh,
            RightMeshPath
        );


        // =====================================================
        // RESULT PARENT
        // =====================================================

        GameObject resultGroup =
            new GameObject(
                "C2_PrismVertical_1_2"
            );


        Undo.RegisterCreatedObjectUndo(
            resultGroup,
            "Create C2 Prism Vertical 1/2"
        );


        resultGroup.transform.SetParent(
            selected.transform,
            false
        );


        resultGroup.transform.localPosition =
            Vector3.zero;

        resultGroup.transform.localRotation =
            Quaternion.identity;

        resultGroup.transform.localScale =
            Vector3.one;


        // =====================================================
        // CREATE PIECES
        // =====================================================

        CreatePiece(
            resultGroup.transform,
            "C2_PrismVerticalHalf_Left",
            leftMesh,
            materialA
        );


        CreatePiece(
            resultGroup.transform,
            "C2_PrismVerticalHalf_Right",
            rightMesh,
            materialB
        );


        resultGroup.SetActive(
            false
        );


        EditorUtility.SetDirty(
            selected
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        Debug.Log(
            "C2 Prism: Vertical 1/2 result generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Prism Vertical 1/2",
            "Vertical 1/2 Prism result generated successfully.",
            "OK"
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
            "Create C2 Prism Slice Piece"
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
    // EXTRUDE 2D POLYGON THROUGH DEPTH
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
    // FRONT / BACK POLYGON
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
    // ENSURE FOLDER
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
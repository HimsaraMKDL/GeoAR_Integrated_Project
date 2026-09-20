using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_TetrahedronBaseMeshGenerator
{
    // =========================================================
    // OUTPUT
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedTetrahedronMeshes";

    private const string OutputPath =
        OutputFolder +
        "/C2_Tetrahedron_Full_Mesh.asset";


    // =========================================================
    // TETRAHEDRON SIZE
    // =========================================================

    private const float SideLength =
        0.28f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Full Tetrahedron Mesh"
    )]
    public static void Generate()
    {
        EnsureFolder(
            OutputFolder
        );


        Mesh tetrahedronMesh =
            BuildTetrahedronMesh();


        tetrahedronMesh.name =
            "C2_Tetrahedron_Full_Mesh";


        AssetDatabase.DeleteAsset(
            OutputPath
        );


        AssetDatabase.CreateAsset(
            tetrahedronMesh,
            OutputPath
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorGUIUtility.PingObject(
            tetrahedronMesh
        );


        Debug.Log(
            "C2 Tetrahedron: Full mesh generated successfully."
        );


        EditorUtility.DisplayDialog(
            "C2 Tetrahedron Generator",
            "Full Tetrahedron mesh created successfully.",
            "OK"
        );
    }


    // =========================================================
    // BUILD TETRAHEDRON
    // =========================================================

    private static Mesh BuildTetrahedronMesh()
    {
        float halfWidth =
            SideLength * 0.5f;


        float frontZ =
            -Mathf.Sqrt(3f) *
            SideLength /
            6f;


        float backZ =
            Mathf.Sqrt(3f) *
            SideLength /
            3f;


        float height =
            Mathf.Sqrt(
                2f / 3f
            ) *
            SideLength;


        Vector3 baseLeft =
            new Vector3(
                -halfWidth,
                0f,
                frontZ
            );


        Vector3 baseRight =
            new Vector3(
                halfWidth,
                0f,
                frontZ
            );


        Vector3 baseBack =
            new Vector3(
                0f,
                0f,
                backZ
            );


        Vector3 apex =
            new Vector3(
                0f,
                height,
                0f
            );


        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        // Base — outward normal faces downward.
        AddTriangleFace(
            vertices,
            triangles,
            baseLeft,
            baseRight,
            baseBack
        );


        // Front face.
        AddTriangleFace(
            vertices,
            triangles,
            baseLeft,
            apex,
            baseRight
        );


        // Right/back face.
        AddTriangleFace(
            vertices,
            triangles,
            baseRight,
            apex,
            baseBack
        );


        // Left/back face.
        AddTriangleFace(
            vertices,
            triangles,
            baseBack,
            apex,
            baseLeft
        );


        Mesh mesh =
            new Mesh();


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
    // TRIANGLE FACE
    // =========================================================

    private static void AddTriangleFace(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c
    )
    {
        int startIndex =
            vertices.Count;


        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);


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
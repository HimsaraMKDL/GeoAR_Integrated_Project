using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_PrismBaseMeshGenerator
{
    // =========================================================
    // OUTPUT
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedPrismMeshes";

    private const string OutputPath =
        OutputFolder +
        "/C2_TriangularPrism_Full_Mesh.asset";


    // =========================================================
    // PRISM DIMENSIONS
    // =========================================================

    private const float Width = 0.30f;
    private const float Height = 0.20f;
    private const float Depth = 0.16f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Full Triangular Prism Mesh"
    )]
    public static void GeneratePrism()
    {
        EnsureFolder(
            OutputFolder
        );


        Mesh prismMesh =
            BuildTriangularPrismMesh();


        prismMesh.name =
            "C2_TriangularPrism_Full_Mesh";


        AssetDatabase.DeleteAsset(
            OutputPath
        );


        AssetDatabase.CreateAsset(
            prismMesh,
            OutputPath
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorGUIUtility.PingObject(
            prismMesh
        );


        Debug.Log(
            "C2 Prism: Full triangular prism mesh " +
            "generated successfully."
        );


        EditorUtility.DisplayDialog(
            "C2 Prism Generator",
            "Full triangular prism mesh created successfully.",
            "OK"
        );
    }


    // =========================================================
    // BUILD TRIANGULAR PRISM
    // =========================================================

    private static Mesh BuildTriangularPrismMesh()
    {
        float halfWidth =
            Width * 0.5f;


        float halfDepth =
            Depth * 0.5f;


        // Front triangle
        Vector3 frontLeft =
            new Vector3(
                -halfWidth,
                0f,
                -halfDepth
            );


        Vector3 frontRight =
            new Vector3(
                halfWidth,
                0f,
                -halfDepth
            );


        Vector3 frontTop =
            new Vector3(
                0f,
                Height,
                -halfDepth
            );


        // Back triangle
        Vector3 backLeft =
            new Vector3(
                -halfWidth,
                0f,
                halfDepth
            );


        Vector3 backRight =
            new Vector3(
                halfWidth,
                0f,
                halfDepth
            );


        Vector3 backTop =
            new Vector3(
                0f,
                Height,
                halfDepth
            );


        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        // -----------------------------------------------------
        // FRONT TRIANGLE
        // Normal points toward -Z
        // -----------------------------------------------------

        AddTriangleFace(
            vertices,
            triangles,
            frontLeft,
            frontTop,
            frontRight
        );


        // -----------------------------------------------------
        // BACK TRIANGLE
        // Normal points toward +Z
        // -----------------------------------------------------

        AddTriangleFace(
            vertices,
            triangles,
            backLeft,
            backRight,
            backTop
        );


        // -----------------------------------------------------
        // BOTTOM RECTANGLE
        // -----------------------------------------------------

        AddQuadFace(
            vertices,
            triangles,
            frontLeft,
            frontRight,
            backRight,
            backLeft
        );


        // -----------------------------------------------------
        // LEFT SLOPED RECTANGLE
        // -----------------------------------------------------

        AddQuadFace(
            vertices,
            triangles,
            frontLeft,
            backLeft,
            backTop,
            frontTop
        );


        // -----------------------------------------------------
        // RIGHT SLOPED RECTANGLE
        // -----------------------------------------------------

        AddQuadFace(
            vertices,
            triangles,
            frontRight,
            frontTop,
            backTop,
            backRight
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
    // QUAD FACE
    // =========================================================

    private static void AddQuadFace(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d
    )
    {
        int startIndex =
            vertices.Count;


        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);


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
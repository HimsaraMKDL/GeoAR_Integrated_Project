using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_CylinderBaseMeshGenerator
{
    // =========================================================
    // OUTPUT PATHS
    // =========================================================

    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedCylinderMeshes";


    private const string OutputPath =
        OutputFolder +
        "/C2_Cylinder_Full_Mesh.asset";


    // =========================================================
    // CYLINDER DIMENSIONS
    // =========================================================

    private const float Radius =
        0.14f;


    private const float Height =
        0.20f;


    private const int Segments =
        32;


    // =========================================================
    // UNITY MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Full Cylinder Mesh"
    )]
    public static void GenerateFullCylinderMesh()
    {
        EnsureFolder(
            OutputFolder
        );


        Mesh cylinderMesh =
            BuildCylinderMesh();


        cylinderMesh.name =
            "C2_Cylinder_Full_Mesh";


        AssetDatabase.DeleteAsset(
            OutputPath
        );


        AssetDatabase.CreateAsset(
            cylinderMesh,
            OutputPath
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        Object generatedAsset =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                OutputPath
            );


        Selection.activeObject =
            generatedAsset;


        EditorGUIUtility.PingObject(
            generatedAsset
        );


        Debug.Log(
            "C2 Cylinder: Full cylinder mesh generated at " +
            OutputPath
        );


        EditorUtility.DisplayDialog(
            "C2 Cylinder Generator",
            "Full Cylinder mesh generated successfully.\n\n" +
            OutputPath,
            "OK"
        );
    }


    // =========================================================
    // BUILD FULL CYLINDER
    // =========================================================

    private static Mesh BuildCylinderMesh()
    {
        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        // =====================================================
        // CURVED SIDE - BOTTOM RING
        // =====================================================

        int sideBottomStart =
            vertices.Count;


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


            vertices.Add(
                new Vector3(
                    x,
                    0f,
                    z
                )
            );
        }


        // =====================================================
        // CURVED SIDE - TOP RING
        // =====================================================

        int sideTopStart =
            vertices.Count;


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


            vertices.Add(
                new Vector3(
                    x,
                    Height,
                    z
                )
            );
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
            int nextSegment =
                (
                    segment + 1
                ) %
                Segments;


            int bottomCurrent =
                sideBottomStart +
                segment;


            int bottomNext =
                sideBottomStart +
                nextSegment;


            int topCurrent =
                sideTopStart +
                segment;


            int topNext =
                sideTopStart +
                nextSegment;


            // Triangle 1 - outward facing.

            triangles.Add(
                bottomCurrent
            );

            triangles.Add(
                topCurrent
            );

            triangles.Add(
                topNext
            );


            // Triangle 2 - outward facing.

            triangles.Add(
                bottomCurrent
            );

            triangles.Add(
                topNext
            );

            triangles.Add(
                bottomNext
            );
        }


        // =====================================================
        // BOTTOM CAP
        // =====================================================
        //
        // Separate vertices are used so that the flat bottom
        // does not share normals with the curved side.
        // =====================================================

        int bottomCenterIndex =
            vertices.Count;


        vertices.Add(
            new Vector3(
                0f,
                0f,
                0f
            )
        );


        int bottomCapRingStart =
            vertices.Count;


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


            vertices.Add(
                new Vector3(
                    Mathf.Cos(angle) *
                        Radius,

                    0f,

                    Mathf.Sin(angle) *
                        Radius
                )
            );
        }


        for (
            int segment = 0;
            segment < Segments;
            segment++
        )
        {
            int nextSegment =
                (
                    segment + 1
                ) %
                Segments;


            int current =
                bottomCapRingStart +
                segment;


            int next =
                bottomCapRingStart +
                nextSegment;


            // Bottom normal must face DOWN (-Y).

            triangles.Add(
                bottomCenterIndex
            );

            triangles.Add(
                current
            );

            triangles.Add(
                next
            );
        }


        // =====================================================
        // TOP CAP
        // =====================================================

        int topCenterIndex =
            vertices.Count;


        vertices.Add(
            new Vector3(
                0f,
                Height,
                0f
            )
        );


        int topCapRingStart =
            vertices.Count;


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


            vertices.Add(
                new Vector3(
                    Mathf.Cos(angle) *
                        Radius,

                    Height,

                    Mathf.Sin(angle) *
                        Radius
                )
            );
        }


        for (
            int segment = 0;
            segment < Segments;
            segment++
        )
        {
            int nextSegment =
                (
                    segment + 1
                ) %
                Segments;


            int current =
                topCapRingStart +
                segment;


            int next =
                topCapRingStart +
                nextSegment;


            // Top normal must face UP (+Y).

            triangles.Add(
                topCenterIndex
            );

            triangles.Add(
                next
            );

            triangles.Add(
                current
            );
        }


        // =====================================================
        // CREATE UNITY MESH
        // =====================================================

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


        return
            mesh;
    }


    // =========================================================
    // ENSURE OUTPUT FOLDER
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
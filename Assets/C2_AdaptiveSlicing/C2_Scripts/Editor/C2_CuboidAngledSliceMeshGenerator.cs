using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_CuboidAngledSliceMeshGenerator
{
    // =========================================================
    // ASSET PATHS
    // =========================================================

    private const string ModelsRoot =
        "Assets/C2_AdaptiveSlicing/C2_Models";

    private const string GeneratedFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedCuboidAngledMeshes";

    private const string PieceAMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartA_Mat.mat";

    private const string PieceBMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartB_Mat.mat";


    // =========================================================
    // CUBOID DIMENSIONS
    // =========================================================

    private const float MinX = -0.15f;
    private const float MaxX = 0.15f;

    private const float MinY = 0f;
    private const float MaxY = 0.18f;

    private const float Width = 0.30f;
    private const float Height = 0.18f;
    private const float Depth = 0.16f;


    // =========================================================
    // UNITY MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Cuboid Angled Slice Results"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Cuboid Angled Generator",
                "Open C2_CuboidExperience.prefab and " +
                "select C2_CuboidSliceResults first.",
                "OK"
            );

            return;
        }


        if (selected.name !=
            "C2_CuboidSliceResults")
        {
            EditorUtility.DisplayDialog(
                "C2 Cuboid Angled Generator",
                "The selected GameObject must be " +
                "C2_CuboidSliceResults.",
                "OK"
            );

            return;
        }


        EnsureFolder(
            ModelsRoot
        );

        EnsureFolder(
            GeneratedFolder
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
                "C2 Cuboid Angled Generator",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // Remove old generated groups if the tool
        // is being run again.

        DeleteExistingChild(
            selected.transform,
            "C2_CuboidAngled_1_2"
        );

        DeleteExistingChild(
            selected.transform,
            "C2_CuboidAngled_1_3"
        );

        DeleteExistingChild(
            selected.transform,
            "C2_CuboidAngled_1_4"
        );

        DeleteExistingChild(
            selected.transform,
            "C2_CuboidAngled_3_4"
        );


        CreateAngledResult(
            selected.transform,
            "C2_CuboidAngled_1_2",
            "C2_CuboidAngledHalf",
            0.50f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            "C2_CuboidAngled_1_3",
            "C2_CuboidAngledThird",
            1f / 3f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            "C2_CuboidAngled_1_4",
            "C2_CuboidAngledQuarter",
            0.25f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            "C2_CuboidAngled_3_4",
            "C2_CuboidAngledThreeQuarter",
            0.75f,
            materialA,
            materialB
        );


        EditorUtility.SetDirty(
            selected
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorUtility.DisplayDialog(
            "C2 Cuboid Angled Generator",
            "All four Cuboid angled slice " +
            "results were generated successfully.",
            "OK"
        );


        Debug.Log(
            "C2 Cuboid Generator: " +
            "All angled results generated."
        );
    }


    // =========================================================
    // CREATE ONE RESULT GROUP
    // =========================================================

    private static void CreateAngledResult(
        Transform parent,
        string resultName,
        string piecePrefix,
        float fraction,
        Material materialA,
        Material materialB
    )
    {
        GameObject resultGroup =
            new GameObject(
                resultName
            );


        Undo.RegisterCreatedObjectUndo(
            resultGroup,
            "Create C2 Cuboid Angled Result"
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


        float normalizedOffset =
            CalculateNormalizedOffset(
                fraction
            );


        List<Vector2> rectangle =
            new List<Vector2>
            {
                new Vector2(
                    MinX,
                    MinY
                ),

                new Vector2(
                    MaxX,
                    MinY
                ),

                new Vector2(
                    MaxX,
                    MaxY
                ),

                new Vector2(
                    MinX,
                    MaxY
                )
            };


        // Blue Piece A:
        // below the diagonal line.

        List<Vector2> pieceAPolygon =
            ClipPolygon(
                rectangle,
                normalizedOffset,
                true
            );


        // Orange Piece B:
        // above the diagonal line.

        List<Vector2> pieceBPolygon =
            ClipPolygon(
                rectangle,
                normalizedOffset,
                false
            );


        CreatePiece(
            resultGroup.transform,
            piecePrefix + "_PieceA",
            pieceAPolygon,
            materialA
        );


        CreatePiece(
            resultGroup.transform,
            piecePrefix + "_PieceB",
            pieceBPolygon,
            materialB
        );


        resultGroup.SetActive(
            false
        );
    }


    // =========================================================
    // FRACTION → NORMALIZED DIAGONAL OFFSET
    //
    // In normalized coordinates:
    //
    // v = u + c
    //
    // where u and v both range from 0 to 1.
    // =========================================================

    private static float CalculateNormalizedOffset(
        float fraction
    )
    {
        if (fraction <= 0.5f)
        {
            return
                Mathf.Sqrt(
                    2f * fraction
                ) - 1f;
        }


        return
            1f -
            Mathf.Sqrt(
                2f *
                (1f - fraction)
            );
    }


    // =========================================================
    // NORMALIZED LINE VALUE
    // =========================================================

    private static float EvaluateLine(
        Vector2 point,
        float normalizedOffset
    )
    {
        float u =
            (point.x - MinX) /
            Width;


        float v =
            (point.y - MinY) /
            Height;


        // Line:
        //
        // v = u + c
        //
        // v - u - c = 0

        return
            v -
            u -
            normalizedOffset;
    }


    // =========================================================
    // CLIP RECTANGLE AGAINST DIAGONAL
    // =========================================================

    private static List<Vector2> ClipPolygon(
        List<Vector2> input,
        float normalizedOffset,
        bool keepBelow
    )
    {
        List<Vector2> output =
            new List<Vector2>();


        if (input == null ||
            input.Count < 3)
        {
            return output;
        }


        for (
            int i = 0;
            i < input.Count;
            i++
        )
        {
            Vector2 current =
                input[i];


            Vector2 next =
                input[
                    (i + 1) %
                    input.Count
                ];


            float currentValue =
                EvaluateLine(
                    current,
                    normalizedOffset
                );


            float nextValue =
                EvaluateLine(
                    next,
                    normalizedOffset
                );


            bool currentInside =
                keepBelow
                    ? currentValue <= 0.00001f
                    : currentValue >= -0.00001f;


            bool nextInside =
                keepBelow
                    ? nextValue <= 0.00001f
                    : nextValue >= -0.00001f;


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
            else if (
                currentInside &&
                !nextInside
            )
            {
                Vector2 intersection =
                    CalculateIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    );


                AddUniquePoint(
                    output,
                    intersection
                );
            }
            else if (
                !currentInside &&
                nextInside
            )
            {
                Vector2 intersection =
                    CalculateIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    );


                AddUniquePoint(
                    output,
                    intersection
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
    // EDGE / LINE INTERSECTION
    // =========================================================

    private static Vector2 CalculateIntersection(
        Vector2 start,
        Vector2 end,
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
            ) < 0.000001f
        )
        {
            return start;
        }


        float t =
            startValue /
            denominator;


        return Vector2.Lerp(
            start,
            end,
            t
        );
    }


    private static void AddUniquePoint(
        List<Vector2> points,
        Vector2 newPoint
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
                    newPoint
                ) < 0.00001f
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
    // CREATE ONE 3D PIECE
    // =========================================================

    private static void CreatePiece(
        Transform parent,
        string pieceName,
        List<Vector2> polygon,
        Material material
    )
    {
        if (polygon == null ||
            polygon.Count < 3)
        {
            Debug.LogError(
                "C2 Cuboid Generator: " +
                "Invalid polygon for " +
                pieceName
            );

            return;
        }


        GameObject piece =
            new GameObject(
                pieceName
            );


        Undo.RegisterCreatedObjectUndo(
            piece,
            "Create C2 Cuboid Angled Piece"
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


        Mesh generatedMesh =
            BuildExtrudedMesh(
                polygon,
                Depth
            );


        generatedMesh.name =
            pieceName +
            "_Mesh";


        string meshPath =
            GeneratedFolder +
            "/" +
            pieceName +
            "_Mesh.asset";


        AssetDatabase.DeleteAsset(
            meshPath
        );


        AssetDatabase.CreateAsset(
            generatedMesh,
            meshPath
        );


        MeshFilter meshFilter =
            piece.AddComponent<MeshFilter>();


        MeshRenderer meshRenderer =
            piece.AddComponent<MeshRenderer>();


        MeshCollider meshCollider =
            piece.AddComponent<MeshCollider>();


        Rigidbody rigidbody =
            piece.AddComponent<Rigidbody>();


        meshFilter.sharedMesh =
            generatedMesh;


        meshRenderer.sharedMaterial =
            material;


        meshCollider.sharedMesh =
            generatedMesh;

        meshCollider.convex =
            true;


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
    // EXTRUDE THE 2D POLYGON THROUGH CUBOID DEPTH
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


        // Front face
        AddPolygonFace(
            polygon,
            frontZ,
            true,
            vertices,
            triangles
        );


        // Back face
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
    // CREATE FRONT / BACK POLYGON FACE
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
    // DELETE OLD GENERATED GROUP
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
    // CREATE ASSET FOLDER
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
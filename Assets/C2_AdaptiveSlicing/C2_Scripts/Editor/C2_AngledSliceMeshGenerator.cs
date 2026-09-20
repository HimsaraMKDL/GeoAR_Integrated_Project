using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_AngledSliceMeshGenerator
{
    // =========================================================
    // C2 PATHS
    // =========================================================

    private const string ModelsRoot =
        "Assets/C2_AdaptiveSlicing/C2_Models";

    private const string GeneratedFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/C2_GeneratedAngledMeshes";

    private const string PieceAMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/C2_SlicePartA_Mat.mat";

    private const string PieceBMaterialPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/C2_SlicePartB_Mat.mat";


    // =========================================================
    // CUBE DIMENSIONS
    // =========================================================

    private const float CubeMinX = -0.10f;
    private const float CubeMaxX = 0.10f;

    private const float CubeMinY = 0.00f;
    private const float CubeMaxY = 0.20f;

    private const float CubeDepth = 0.20f;


    // =========================================================
    // MENU COMMAND
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Angled Cube Slice Results"
    )]
    public static void GenerateAngledCubeSliceResults()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Angled Slice Generator",
                "Select C2_CubeSliceResults in Prefab Mode first.",
                "OK"
            );

            return;
        }


        if (selected.name !=
            "C2_CubeSliceResults")
        {
            EditorUtility.DisplayDialog(
                "C2 Angled Slice Generator",
                "The selected object must be C2_CubeSliceResults.",
                "OK"
            );

            return;
        }


        EnsureFolder(
            "Assets/C2_AdaptiveSlicing"
        );

        EnsureFolder(
            ModelsRoot
        );

        EnsureFolder(
            GeneratedFolder
        );


        Material pieceAMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                PieceAMaterialPath
            );


        Material pieceBMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                PieceBMaterialPath
            );


        if (pieceAMaterial == null ||
            pieceBMaterial == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Angled Slice Generator",
                "C2_SlicePartA_Mat or C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // -----------------------------------------------------
        // Remove old generated angled result groups.
        // This makes the generator safe to run again.
        // -----------------------------------------------------

        DeleteExistingChild(
            selected.transform,
            "C2_Angled_1_2"
        );

        DeleteExistingChild(
            selected.transform,
            "C2_Angled_1_3"
        );

        DeleteExistingChild(
            selected.transform,
            "C2_Angled_1_4"
        );

        DeleteExistingChild(
            selected.transform,
            "C2_Angled_3_4"
        );


        // -----------------------------------------------------
        // Create all four angled fractions.
        // -----------------------------------------------------

        CreateAngledResult(
            selected.transform,
            "C2_Angled_1_2",
            "C2_AngledHalf",
            0.50f,
            pieceAMaterial,
            pieceBMaterial
        );


        CreateAngledResult(
            selected.transform,
            "C2_Angled_1_3",
            "C2_AngledThird",
            1f / 3f,
            pieceAMaterial,
            pieceBMaterial
        );


        CreateAngledResult(
            selected.transform,
            "C2_Angled_1_4",
            "C2_AngledQuarter",
            0.25f,
            pieceAMaterial,
            pieceBMaterial
        );


        CreateAngledResult(
            selected.transform,
            "C2_Angled_3_4",
            "C2_AngledThreeQuarter",
            0.75f,
            pieceAMaterial,
            pieceBMaterial
        );


        EditorUtility.SetDirty(
            selected
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorUtility.DisplayDialog(
            "C2 Angled Slice Generator",
            "All four angled Cube slice results were generated successfully.",
            "OK"
        );


        Debug.Log(
            "C2 Generator: Angled Cube slice results generated."
        );
    }


    // =========================================================
    // CREATE ONE FRACTION RESULT
    // =========================================================

    private static void CreateAngledResult(
        Transform parent,
        string resultName,
        string piecePrefix,
        float targetFraction,
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
            "Create C2 Angled Result"
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


        float cutB =
            CalculateCutB(
                targetFraction
            );


        List<Vector2> square =
            new List<Vector2>
            {
                new Vector2(
                    CubeMinX,
                    CubeMinY
                ),

                new Vector2(
                    CubeMaxX,
                    CubeMinY
                ),

                new Vector2(
                    CubeMaxX,
                    CubeMaxY
                ),

                new Vector2(
                    CubeMinX,
                    CubeMaxY
                )
            };


        // Piece A = region below y = x + b
        List<Vector2> pieceAPolygon =
            ClipPolygon(
                square,
                cutB,
                true
            );


        // Piece B = region above y = x + b
        List<Vector2> pieceBPolygon =
            ClipPolygon(
                square,
                cutB,
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


        // Result groups must start inactive.
        resultGroup.SetActive(
            false
        );
    }


    // =========================================================
    // CALCULATE DIAGONAL OFFSET
    //
    // Line:
    // y = x + b
    //
    // Produces the requested area / volume fraction.
    // =========================================================

    private static float CalculateCutB(
        float fraction
    )
    {
        float normalizedOffset;


        if (fraction <= 0.5f)
        {
            normalizedOffset =
                Mathf.Sqrt(
                    2f * fraction
                ) - 1f;
        }
        else
        {
            normalizedOffset =
                1f -
                Mathf.Sqrt(
                    2f *
                    (1f - fraction)
                );
        }


        return
            0.10f +
            (0.20f * normalizedOffset);
    }


    // =========================================================
    // CLIP SQUARE AGAINST CUT LINE
    // =========================================================

    private static List<Vector2> ClipPolygon(
        List<Vector2> inputPolygon,
        float cutB,
        bool keepBelow
    )
    {
        List<Vector2> output =
            new List<Vector2>();


        if (inputPolygon == null ||
            inputPolygon.Count < 3)
        {
            return output;
        }


        for (
            int i = 0;
            i < inputPolygon.Count;
            i++
        )
        {
            Vector2 current =
                inputPolygon[i];


            Vector2 next =
                inputPolygon[
                    (i + 1) %
                    inputPolygon.Count
                ];


            float currentValue =
                EvaluateLine(
                    current,
                    cutB
                );


            float nextValue =
                EvaluateLine(
                    next,
                    cutB
                );


            bool currentInside =
                keepBelow
                    ? currentValue <= 0.00001f
                    : currentValue >= -0.00001f;


            bool nextInside =
                keepBelow
                    ? nextValue <= 0.00001f
                    : nextValue >= -0.00001f;


            if (currentInside &&
                nextInside)
            {
                AddPointIfUnique(
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
                    GetLineIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    );


                AddPointIfUnique(
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
                    GetLineIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    );


                AddPointIfUnique(
                    output,
                    intersection
                );


                AddPointIfUnique(
                    output,
                    next
                );
            }
        }


        return output;
    }


    // =========================================================
    // LINE EQUATION
    //
    // y = x + b
    // y - x - b = 0
    // =========================================================

    private static float EvaluateLine(
        Vector2 point,
        float cutB
    )
    {
        return
            point.y -
            point.x -
            cutB;
    }


    private static Vector2 GetLineIntersection(
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


    private static void AddPointIfUnique(
        List<Vector2> points,
        Vector2 newPoint
    )
    {
        if (points.Count > 0)
        {
            Vector2 lastPoint =
                points[
                    points.Count - 1
                ];


            if (
                Vector2.Distance(
                    lastPoint,
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
    // CREATE ONE PHYSICS PIECE
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
                "C2 Generator: Invalid polygon for " +
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
            "Create C2 Angled Piece"
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
                CubeDepth
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
    // BUILD EXTRUDED 3D PRISM
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


        // -----------------------------------------------------
        // FRONT FACE
        // Outward normal = -Z
        // -----------------------------------------------------

        AddPolygonFace(
            polygon,
            frontZ,
            true,
            vertices,
            triangles
        );


        // -----------------------------------------------------
        // BACK FACE
        // Outward normal = +Z
        // -----------------------------------------------------

        AddPolygonFace(
            polygon,
            backZ,
            false,
            vertices,
            triangles
        );


        // -----------------------------------------------------
        // SIDE FACES
        // -----------------------------------------------------

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
    // ADD FRONT / BACK FACE
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
    // REMOVE OLD RESULT GROUP
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
    // CREATE FOLDER WHEN NEEDED
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
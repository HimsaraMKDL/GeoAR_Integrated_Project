using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class C2_PrismAngledFractionsMeshGenerator
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
    private const float Width = 0.30f;
    private const float Height = 0.20f;
    private const float Depth = 0.16f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/C2/Generate Prism Angled Fractions"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Angled Fractions",
                "Open C2_PrismExperience.prefab and " +
                "select C2_PrismSliceResults first.",
                "OK"
            );

            return;
        }


        if (
            selected.name !=
            "C2_PrismSliceResults"
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Angled Fractions",
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


        if (
            materialA == null ||
            materialB == null
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Prism Angled Fractions",
                "C2_SlicePartA_Mat or " +
                "C2_SlicePartB_Mat could not be found.",
                "OK"
            );

            return;
        }


        // Remove only existing angled results.
        // Vertical and Horizontal results stay untouched.

        DeleteExistingChild(
            selected.transform,
            "C2_PrismAngled_1_2"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismAngled_1_3"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismAngled_1_4"
        );


        DeleteExistingChild(
            selected.transform,
            "C2_PrismAngled_3_4"
        );


        // =====================================================
        // GENERATE ALL FOUR
        // =====================================================

        CreateAngledResult(
            selected.transform,
            "C2_PrismAngled_1_2",
            "C2_PrismAngledHalf",
            0.5f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            "C2_PrismAngled_1_3",
            "C2_PrismAngledThird",
            1f / 3f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            "C2_PrismAngled_1_4",
            "C2_PrismAngledQuarter",
            0.25f,
            materialA,
            materialB
        );


        CreateAngledResult(
            selected.transform,
            "C2_PrismAngled_3_4",
            "C2_PrismAngledThreeQuarter",
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
            "C2 Prism: All Angled fractions generated."
        );


        EditorUtility.DisplayDialog(
            "C2 Prism Angled Fractions",
            "Prism Angled 1/2, 1/3, 1/4 " +
            "and 3/4 were generated successfully.",
            "OK"
        );
    }


    // =========================================================
    // CREATE ONE ANGLED RESULT
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
        float offset =
            CalculateAngledOffset(
                fraction
            );


        // Original triangular face.
        // Counter-clockwise order.

        List<Vector2> fullTriangle =
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
                    0f,
                    Height
                )
            };


        // Piece A / Blue
        // Keeps area BELOW the angled line.

        List<Vector2> pieceAPolygon =
            ClipPolygon(
                fullTriangle,
                offset,
                true
            );


        // Piece B / Orange
        // Keeps area ABOVE the angled line.

        List<Vector2> pieceBPolygon =
            ClipPolygon(
                fullTriangle,
                offset,
                false
            );


        if (
            pieceAPolygon.Count < 3 ||
            pieceBPolygon.Count < 3
        )
        {
            Debug.LogError(
                "C2 Prism: Invalid clipped polygon for " +
                resultName
            );

            return;
        }


        Mesh pieceAMesh =
            BuildExtrudedMesh(
                pieceAPolygon,
                Depth
            );


        Mesh pieceBMesh =
            BuildExtrudedMesh(
                pieceBPolygon,
                Depth
            );


        string pieceAName =
            piecePrefix +
            "_PieceA";


        string pieceBName =
            piecePrefix +
            "_PieceB";


        pieceAMesh.name =
            pieceAName +
            "_Mesh";


        pieceBMesh.name =
            pieceBName +
            "_Mesh";


        string pieceAMeshPath =
            OutputFolder +
            "/" +
            pieceAName +
            "_Mesh.asset";


        string pieceBMeshPath =
            OutputFolder +
            "/" +
            pieceBName +
            "_Mesh.asset";


        AssetDatabase.DeleteAsset(
            pieceAMeshPath
        );


        AssetDatabase.DeleteAsset(
            pieceBMeshPath
        );


        AssetDatabase.CreateAsset(
            pieceAMesh,
            pieceAMeshPath
        );


        AssetDatabase.CreateAsset(
            pieceBMesh,
            pieceBMeshPath
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
            "Create C2 Prism Angled Result"
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
            pieceAName,
            pieceAMesh,
            materialA
        );


        CreatePiece(
            resultGroup.transform,
            pieceBName,
            pieceBMesh,
            materialB
        );


        resultGroup.SetActive(
            false
        );


        Debug.Log(
            "C2 Prism: " +
            resultName +
            " generated. Offset C = " +
            offset.ToString("F6")
        );
    }


    // =========================================================
    // FRACTION → DIAGONAL OFFSET
    //
    // normalized line:
    //
    // v = u + c
    //
    // Piece A is below the line.
    // =========================================================

    private static float CalculateAngledOffset(
        float fraction
    )
    {
        if (fraction <= (2f / 3f))
        {
            return
                Mathf.Sqrt(
                    1.5f * fraction
                ) -
                1f;
        }


        return
            (
                1f -
                Mathf.Sqrt(
                    3f *
                    (1f - fraction)
                )
            ) *
            0.5f;
    }


    // =========================================================
    // CLIP TRIANGLE AGAINST ANGLED LINE
    // =========================================================

    private static List<Vector2> ClipPolygon(
        List<Vector2> inputPolygon,
        float offset,
        bool keepBelow
    )
    {
        List<Vector2> output =
            new List<Vector2>();


        if (
            inputPolygon == null ||
            inputPolygon.Count == 0
        )
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
                SignedLineValue(
                    current,
                    offset
                );


            float nextValue =
                SignedLineValue(
                    next,
                    offset
                );


            bool currentInside =
                keepBelow
                    ? currentValue <= 0f
                    : currentValue >= 0f;


            bool nextInside =
                keepBelow
                    ? nextValue <= 0f
                    : nextValue >= 0f;


            // Current inside, next inside
            if (
                currentInside &&
                nextInside
            )
            {
                output.Add(
                    next
                );
            }

            // Current inside, next outside
            else if (
                currentInside &&
                !nextInside
            )
            {
                output.Add(
                    CalculateIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    )
                );
            }

            // Current outside, next inside
            else if (
                !currentInside &&
                nextInside
            )
            {
                output.Add(
                    CalculateIntersection(
                        current,
                        next,
                        currentValue,
                        nextValue
                    )
                );


                output.Add(
                    next
                );
            }
        }


        return output;
    }


    // =========================================================
    // SIGNED LINE VALUE
    // =========================================================

    private static float SignedLineValue(
        Vector2 point,
        float offset
    )
    {
        float u =
            (
                point.x +
                HalfWidth
            ) /
            Width;


        float v =
            point.y /
            Height;


        return
            v -
            u -
            offset;
    }


    // =========================================================
    // EDGE / LINE INTERSECTION
    // =========================================================

    private static Vector2 CalculateIntersection(
        Vector2 a,
        Vector2 b,
        float valueA,
        float valueB
    )
    {
        float denominator =
            valueA -
            valueB;


        if (
            Mathf.Abs(
                denominator
            ) <
            0.000001f
        )
        {
            return
                (a + b) *
                0.5f;
        }


        float t =
            valueA /
            denominator;


        return
            Vector2.Lerp(
                a,
                b,
                t
            );
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
            "Create C2 Prism Angled Piece"
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
    // EXTRUDE 2D POLYGON THROUGH PRISM DEPTH
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
            -depth *
            0.5f;


        float backZ =
            depth *
            0.5f;


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
    // DELETE EXISTING ANGLED RESULT
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
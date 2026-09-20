using UnityEditor;
using UnityEngine;

public static class C2_TetrahedronAngledFractionsMeshGenerator
{
    private const string OutputFolder =
        "Assets/C2_AdaptiveSlicing/C2_Models/" +
        "C2_GeneratedTetrahedronMeshes";


    private const string SourceMeshPath =
        OutputFolder +
        "/C2_Tetrahedron_Full_Mesh.asset";


    private const string MaterialAPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartA_Mat.mat";


    private const string MaterialBPath =
        "Assets/C2_AdaptiveSlicing/C2_Materials/" +
        "C2_SlicePartB_Mat.mat";


    [MenuItem(
        "Tools/C2/Generate Tetrahedron Angled Fractions"
    )]
    public static void Generate()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (
            selected == null ||
            selected.name !=
            "C2_TetrahedronSliceResults"
        )
        {
            EditorUtility.DisplayDialog(
                "C2 Tetrahedron Angled",
                "Open C2_TetrahedronExperience.prefab and " +
                "select C2_TetrahedronSliceResults first.",
                "OK"
            );

            return;
        }


        Mesh sourceMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                SourceMeshPath
            );


        Material materialA =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialAPath
            );


        Material materialB =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialBPath
            );


        if (
            sourceMesh == null ||
            materialA == null ||
            materialB == null
        )
        {
            Debug.LogError(
                "C2 Tetrahedron Angled: Required assets missing."
            );

            return;
        }


        Vector3 normal =
            new Vector3(
                -1f,
                1f,
                0f
            ).normalized;


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronAngled_1_2",
            "C2_TetrahedronAngledHalf_PieceA",
            "C2_TetrahedronAngledHalf_PieceB",
            normal,
            0.5f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronAngled_1_3",
            "C2_TetrahedronAngledThird_PieceA",
            "C2_TetrahedronAngledThird_PieceB",
            normal,
            1f / 3f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronAngled_1_4",
            "C2_TetrahedronAngledQuarter_PieceA",
            "C2_TetrahedronAngledQuarter_PieceB",
            normal,
            0.25f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronAngled_3_4",
            "C2_TetrahedronAngledThreeQuarter_PieceA",
            "C2_TetrahedronAngledThreeQuarter_PieceB",
            normal,
            0.75f
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorUtility.DisplayDialog(
            "C2 Tetrahedron Angled",
            "All Tetrahedron angled fractions generated.",
            "OK"
        );
    }


    private static void GenerateOne(
        Transform parent,
        Mesh sourceMesh,
        Material materialA,
        Material materialB,
        string resultName,
        string pieceA,
        string pieceB,
        Vector3 normal,
        float fraction
    )
    {
        float offset =
            C2_ConvexMeshSliceUtility
                .GenerateFractionResult(
                    parent,
                    sourceMesh,
                    OutputFolder,
                    resultName,
                    pieceA,
                    pieceB,
                    normal,
                    fraction,
                    materialA,
                    materialB
                );


        Debug.Log(
            resultName +
            " generated. Offset = " +
            offset.ToString("F6")
        );
    }
}
using UnityEditor;
using UnityEngine;

public static class C2_TetrahedronHorizontalFractionsMeshGenerator
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
        "Tools/C2/Generate Tetrahedron Horizontal Fractions"
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
                "C2 Tetrahedron Horizontal",
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
                "C2 Tetrahedron Horizontal: Required assets missing."
            );

            return;
        }


        Vector3 normal =
            Vector3.up;


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronHorizontal_1_2",
            "C2_TetrahedronHorizontalHalf_Bottom",
            "C2_TetrahedronHorizontalHalf_Top",
            normal,
            0.5f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronHorizontal_1_3",
            "C2_TetrahedronHorizontalThird_Bottom",
            "C2_TetrahedronHorizontalThird_Top",
            normal,
            1f / 3f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronHorizontal_1_4",
            "C2_TetrahedronHorizontalQuarter_Bottom",
            "C2_TetrahedronHorizontalQuarter_Top",
            normal,
            0.25f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronHorizontal_3_4",
            "C2_TetrahedronHorizontalThreeQuarter_Bottom",
            "C2_TetrahedronHorizontalThreeQuarter_Top",
            normal,
            0.75f
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorUtility.DisplayDialog(
            "C2 Tetrahedron Horizontal",
            "All Tetrahedron horizontal fractions generated.",
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
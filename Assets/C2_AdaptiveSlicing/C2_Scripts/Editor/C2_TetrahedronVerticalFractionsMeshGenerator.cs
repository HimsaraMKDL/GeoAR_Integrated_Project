using UnityEditor;
using UnityEngine;

public static class C2_TetrahedronVerticalFractionsMeshGenerator
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
        "Tools/C2/Generate Tetrahedron Vertical Fractions"
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
                "C2 Tetrahedron Vertical",
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
                "C2 Tetrahedron Vertical: Required assets are missing."
            );

            return;
        }


        Vector3 normal =
            Vector3.right;


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronVertical_1_2",
            "C2_TetrahedronVerticalHalf_Left",
            "C2_TetrahedronVerticalHalf_Right",
            normal,
            0.5f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronVertical_1_3",
            "C2_TetrahedronVerticalThird_Left",
            "C2_TetrahedronVerticalThird_Right",
            normal,
            1f / 3f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronVertical_1_4",
            "C2_TetrahedronVerticalQuarter_Left",
            "C2_TetrahedronVerticalQuarter_Right",
            normal,
            0.25f
        );


        GenerateOne(
            selected.transform,
            sourceMesh,
            materialA,
            materialB,
            "C2_TetrahedronVertical_3_4",
            "C2_TetrahedronVerticalThreeQuarter_Left",
            "C2_TetrahedronVerticalThreeQuarter_Right",
            normal,
            0.75f
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        EditorUtility.DisplayDialog(
            "C2 Tetrahedron Vertical",
            "All Tetrahedron vertical fractions generated.",
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
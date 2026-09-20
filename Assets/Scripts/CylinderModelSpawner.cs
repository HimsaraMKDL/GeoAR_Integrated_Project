using System.Collections.Generic;
using UnityEngine;

public class CylinderModelSpawner : MonoBehaviour
{
    [Header("Cylinder Folder")]
    [SerializeField]
    private NetToCylinderFolder netToCylinderFolder;

    public void CreateCylinder(
        CylinderGuidedConfig config,
        List<CylinderGeneratedFace> sourceFaces,
        RectTransform puzzleBoard,
        float distanceFromCamera)
    {
        if (netToCylinderFolder == null)
        {
            Debug.LogError(
                "CylinderModelSpawner: NetToCylinderFolder is not assigned."
            );

            return;
        }

        netToCylinderFolder.CreateCylinder(
            config,
            sourceFaces,
            puzzleBoard,
            distanceFromCamera
        );
    }

    public void ClearCylinder()
    {
        if (netToCylinderFolder != null)
        {
            netToCylinderFolder.ClearModel();
        }
    }

    public Transform GetCurrentModelRoot()
    {
        if (netToCylinderFolder == null)
            return null;

        return netToCylinderFolder.GetCurrentModelRoot();
    }
}
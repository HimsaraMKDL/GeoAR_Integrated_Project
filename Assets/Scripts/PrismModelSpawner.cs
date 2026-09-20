using System.Collections.Generic;
using UnityEngine;

public class PrismModelSpawner : MonoBehaviour
{
    [Header("Folder System")]
    public NetToPrismFolder netToPrismFolder;

    public void CreatePrismFromGuidedNet(
        PrismGuidedConfig config,
        List<PrismGeneratedFace> faces,
        RectTransform puzzleBoard,
        float distanceFromCamera)
    {
        if (netToPrismFolder == null)
        {
            Debug.LogError("NetToPrismFolder is not assigned.");
            return;
        }

        netToPrismFolder.CreateAndFoldFromGuidedNet(
            config,
            faces,
            puzzleBoard,
            distanceFromCamera
        );
    }

    public void ClearPrism()
    {
        if (netToPrismFolder != null)
        {
            netToPrismFolder.ClearModel();
        }
    }

    public Transform GetCurrentModelRoot()
    {
        if (netToPrismFolder == null)
            return null;

        return netToPrismFolder.GetCurrentModelRoot();
    }

    public NetToPrismFolder GetFolder()
    {
        return netToPrismFolder;
    }
}
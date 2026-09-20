using System.Collections.Generic;
using UnityEngine;

public class CubeModelSpawner : MonoBehaviour
{
    [Header("Folder System")]
    public NetToCubeFolder netToCubeFolder;

    
    public void CreateCubeFromCanvasNet(
        HashSet<Vector2Int> detectedFaces,
        RectTransform drawingArea,
        int gridWidth,
        int gridHeight,
        float distanceFromCamera
    )
    {
        if (netToCubeFolder == null)
        {
            Debug.LogError("NetToCubeFolder is not assigned.");
            return;
        }

        netToCubeFolder.CreateAndFoldFromCanvas(
            detectedFaces,
            drawingArea,
            gridWidth,
            gridHeight,
            distanceFromCamera
        );
    }

    
    public void ClearCube()
    {
        if (netToCubeFolder != null)
            netToCubeFolder.ClearModel();
    }

    
    public Transform GetCurrentModelRoot()
    {
        if (netToCubeFolder == null)
            return null;

        return netToCubeFolder.GetCurrentModelRoot();
    }
}
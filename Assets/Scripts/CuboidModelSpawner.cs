using System;
using System.Collections.Generic;
using UnityEngine;

public class CuboidModelSpawner : MonoBehaviour
{
    [Header("Folder System")]
    [SerializeField]
    private NetToCuboidFolder netToCuboidFolder;

    public event Action CuboidFoldCompleted
    {
        add
        {
            if (netToCuboidFolder != null)
            {
                netToCuboidFolder.CuboidFoldCompleted += value;
            }
        }
        remove
        {
            if (netToCuboidFolder != null)
            {
                netToCuboidFolder.CuboidFoldCompleted -= value;
            }
        }
    }

    public void CreateCuboidFromCanvasNet(
        IReadOnlyList<CuboidFace> detectedFaces,
        RectTransform drawingArea,
        int gridWidth,
        int gridHeight,
        Vector3Int inferredDimensions,
        float distanceFromCamera)
    {
        if (netToCuboidFolder == null)
        {
            Debug.LogError("NetToCuboidFolder is not assigned.");
            return;
        }

        if (detectedFaces == null || detectedFaces.Count != 6)
        {
            Debug.LogError("Exactly 6 cuboid faces are required.");
            return;
        }

        netToCuboidFolder.CreateAndFoldFromCanvas(
            detectedFaces,
            drawingArea,
            gridWidth,
            gridHeight,
            inferredDimensions,
            distanceFromCamera
        );
    }

    public void ClearCuboid()
    {
        if (netToCuboidFolder != null)
        {
            netToCuboidFolder.ClearModel();
        }
    }

    public Transform GetCurrentModelRoot()
    {
        if (netToCuboidFolder == null)
        {
            return null;
        }

        return netToCuboidFolder.GetCurrentModelRoot();
    }
}
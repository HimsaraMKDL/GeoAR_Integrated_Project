using System.Collections.Generic;
using UnityEngine;

public class TetrahedronModelSpawner : MonoBehaviour
{
    [Header("Folder System")]
    [SerializeField]
    private NetToTetrahedronFolder netToTetrahedronFolder;

    public void CreateTetrahedron(
        List<TetrahedronTriangleFace> faces,
        TetrahedronGridDrawManager drawManager,
        float distanceFromCamera)
    {
        if (netToTetrahedronFolder == null)
        {
            Debug.LogError(
                "TetrahedronModelSpawner: NetToTetrahedronFolder is not assigned."
            );

            return;
        }

        netToTetrahedronFolder.CreateAndFold(
            faces,
            drawManager,
            distanceFromCamera
        );
    }

    public void ClearTetrahedron()
    {
        if (netToTetrahedronFolder != null)
        {
            netToTetrahedronFolder.ClearModel();
        }
    }

    public Transform GetCurrentModelRoot()
    {
        if (netToTetrahedronFolder == null)
            return null;

        return netToTetrahedronFolder.GetCurrentModelRoot();
    }
}
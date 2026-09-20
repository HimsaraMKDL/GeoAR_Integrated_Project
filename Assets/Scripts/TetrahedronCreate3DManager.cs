using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TetrahedronCreate3DManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TetrahedronNetValidator validator;

    [SerializeField]
    private TetrahedronTriangleDetector detector;

    [SerializeField]
    private TetrahedronGridDrawManager drawManager;

    [SerializeField]
    private TetrahedronModelSpawner modelSpawner;

    [SerializeField]
    private ARModeManager arModeManager;


    [Header("UI")]
    [SerializeField]
    private GameObject drawingCanvas;

    [SerializeField]
    private Text statusText;


    [Header("3D Settings")]
    [SerializeField]
    private float distanceFromCamera = 2.5f;


    [Header("AR Transition")]
    [SerializeField]
    private float arStartDelay = 8f;


    private bool creationStarted = false;


    // ==================================================
    // CREATE 3D
    // ==================================================

    public void Create3DTetrahedron()
    {
        if (creationStarted)
            return;


        // ----------------------------------------------
        // VALIDATION CHECK
        // ----------------------------------------------

        if (validator == null)
        {
            Debug.LogError(
                "TetrahedronCreate3DManager: Validator is not assigned."
            );

            return;
        }


        if (!validator.CurrentNetValid)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Validate a correct tetrahedron net first.";
            }

            return;
        }


        // ----------------------------------------------
        // REFERENCE CHECK
        // ----------------------------------------------

        if (detector == null)
        {
            Debug.LogError(
                "TetrahedronCreate3DManager: Triangle detector is not assigned."
            );

            return;
        }


        if (drawManager == null)
        {
            Debug.LogError(
                "TetrahedronCreate3DManager: Draw manager is not assigned."
            );

            return;
        }


        if (modelSpawner == null)
        {
            Debug.LogError(
                "TetrahedronCreate3DManager: Model spawner is not assigned."
            );

            return;
        }


        // ----------------------------------------------
        // REFRESH DETECTED TRIANGLES
        // ----------------------------------------------

        detector.DetectTriangles();


        List<TetrahedronTriangleFace> faces =
            new List<TetrahedronTriangleFace>(
                detector.DetectedTriangles
            );


        if (faces.Count != 4)
        {
            Debug.LogError(
                $"Tetrahedron requires exactly 4 faces. Found: {faces.Count}."
            );

            return;
        }


        creationStarted = true;


        if (statusText != null)
        {
            statusText.text =
                "Your tetrahedron net is folding into 3D.";
        }


        // ----------------------------------------------
        // CREATE + FOLD MODEL
        // ----------------------------------------------

        modelSpawner.CreateTetrahedron(
            faces,
            drawManager,
            distanceFromCamera
        );


        // ----------------------------------------------
        // GET GENERATED MODEL ROOT
        // ----------------------------------------------

        Transform modelRoot =
            modelSpawner.GetCurrentModelRoot();


        if (modelRoot == null)
        {
            Debug.LogError(
                "Tetrahedron model root was not created."
            );

            creationStarted = false;

            if (drawingCanvas != null)
            {
                drawingCanvas.SetActive(true);
            }

            return;
        }


        // ==================================================
        // RESEARCH DATA — 3D GENERATED
        // ==================================================

        /*
         * The model root exists successfully.
         * Therefore record that the valid net was
         * converted into a 3D tetrahedron.
         */

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance
                .Log3DGeneration();

            Debug.Log(
                "Tetrahedron 3D generation logged."
            );
        }
        else
        {
            Debug.LogWarning(
                "Tetrahedron 3D generation could not be logged: " +
                "SessionDataLogger instance is missing."
            );
        }


        // ----------------------------------------------
        // HIDE DRAWING CANVAS
        // ----------------------------------------------

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }


        // ----------------------------------------------
        // START AR AFTER FOLDING / PREVIEW
        // ----------------------------------------------

        if (arModeManager != null)
        {
            arModeManager.StartARAfterDelay(
                arStartDelay,
                modelRoot
            );


            Debug.Log(
                $"Tetrahedron ready for AR transition in " +
                $"{arStartDelay:0.0} seconds."
            );
        }
        else
        {
            Debug.LogWarning(
                "TetrahedronCreate3DManager: " +
                "ARModeManager is not assigned. " +
                "Tetrahedron will remain in 3D preview mode."
            );
        }


        Debug.Log(
            "Tetrahedron 3D creation started."
        );
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetCreationState()
    {
        creationStarted = false;


        if (modelSpawner != null)
        {
            modelSpawner.ClearTetrahedron();
        }


        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(true);
        }


        Debug.Log(
            "Tetrahedron 3D creation state reset."
        );
    }
}
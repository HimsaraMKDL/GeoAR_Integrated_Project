using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidedCylinderCreate3DManager :
    MonoBehaviour
{
    [Header("Cylinder References")]

    [SerializeField]
    private CylinderGuidedSetupManager setupManager;

    [SerializeField]
    private CylinderGuidedNetValidator netValidator;

    [SerializeField]
    private CylinderModelSpawner cylinderModelSpawner;

    [SerializeField]
    private RectTransform puzzleBoard;


    [Header("AR")]

    [SerializeField]
    private ARModeManager arModeManager;

    [Tooltip(
        "Time from Create 3D click until AR mode starts. " +
        "This includes folding + final preview time."
    )]
    [SerializeField]
    private float arDelay = 10f;


    [Header("UI")]

    [SerializeField]
    private GameObject drawingCanvas;

    [SerializeField]
    private Text statusText;


    [Header("3D Settings")]

    [SerializeField]
    private float distanceFromCamera = 2.5f;


    private bool creationStarted = false;


    // ==================================================
    // CREATE 3D CYLINDER
    // ==================================================

    public void Create3DCylinder()
    {
        if (creationStarted)
            return;


        // ----------------------------------------------
        // VALIDATOR
        // ----------------------------------------------

        if (netValidator == null)
        {
            Debug.LogError(
                "Cylinder validator is not assigned."
            );

            return;
        }


        if (!netValidator.CurrentNetValid)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Please validate a correct " +
                    "cylinder net first.";
            }

            return;
        }


        // ----------------------------------------------
        // SETUP
        // ----------------------------------------------

        if (setupManager == null)
        {
            Debug.LogError(
                "Cylinder setup manager " +
                "is not assigned."
            );

            return;
        }


        if (puzzleBoard == null)
        {
            Debug.LogError(
                "Cylinder PuzzleBoard " +
                "is not assigned."
            );

            return;
        }


        CylinderGuidedConfig config =
            setupManager.GetConfig();


        if (config == null ||
            !config.IsReady)
        {
            Debug.LogError(
                "Cylinder configuration " +
                "is incomplete."
            );

            return;
        }


        // ----------------------------------------------
        // GET STUDENT NET FACES
        // ----------------------------------------------

        CylinderGeneratedFace[] foundFaces =
            puzzleBoard.GetComponentsInChildren<
                CylinderGeneratedFace>(
                false
            );


        if (foundFaces.Length != 3)
        {
            Debug.LogError(
                $"Expected 3 cylinder faces " +
                $"on board, found " +
                $"{foundFaces.Length}."
            );

            return;
        }


        List<CylinderGeneratedFace> faces =
            new List<CylinderGeneratedFace>(
                foundFaces
            );


        // ----------------------------------------------
        // SPAWNER
        // ----------------------------------------------

        if (cylinderModelSpawner == null)
        {
            Debug.LogError(
                "CylinderModelSpawner " +
                "is not assigned."
            );

            return;
        }


        creationStarted = true;


        if (statusText != null)
        {
            statusText.text =
                "Your cylinder net is " +
                "folding into 3D.";
        }


        // ----------------------------------------------
        // CREATE 3D BEFORE HIDING CANVAS
        // ----------------------------------------------

        cylinderModelSpawner.CreateCylinder(
            config,
            faces,
            puzzleBoard,
            distanceFromCamera
        );


        /*
         * Get the newly-created model root.
         *
         * The root already exists immediately,
         * even though its folding animation
         * is still running.
         */

        Transform modelRoot =
            cylinderModelSpawner
                .GetCurrentModelRoot();


        if (modelRoot == null)
        {
            Debug.LogError(
                "Cylinder 3D model root " +
                "was not created."
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
         * Therefore record that the valid cylinder net
         * was converted into a 3D cylinder.
         */

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance
                .Log3DGeneration();

            Debug.Log(
                "Cylinder 3D generation logged."
            );
        }
        else
        {
            Debug.LogWarning(
                "Cylinder 3D generation could not be logged: " +
                "SessionDataLogger instance is missing."
            );
        }


        // ----------------------------------------------
        // HIDE 2D UI
        // ----------------------------------------------

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(
                false
            );
        }


        Debug.Log(
            "Cylinder UI captured and " +
            "Canvas hidden."
        );


        // ----------------------------------------------
        // SCHEDULE AR
        // ----------------------------------------------

        if (arModeManager != null)
        {
            arModeManager.StartARAfterDelay(
                arDelay,
                modelRoot
            );

            Debug.Log(
                $"Cylinder AR transition " +
                $"scheduled after {arDelay} seconds."
            );
        }
        else
        {
            Debug.LogWarning(
                "ARModeManager is not assigned. " +
                "Cylinder will remain in " +
                "normal 3D preview mode."
            );
        }
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetCreationState()
    {
        creationStarted = false;


        if (cylinderModelSpawner != null)
        {
            cylinderModelSpawner
                .ClearCylinder();
        }


        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(
                true
            );
        }


        /*
         * Return from AR to normal mode
         * if Reset is ever called after AR.
         */

        if (arModeManager != null)
        {
            arModeManager.SetARMode(
                false
            );
        }
    }
}
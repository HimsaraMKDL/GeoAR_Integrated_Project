using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidedCylinderCreate3DManager : MonoBehaviour
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


    [Header("Manual AR Transition")]

    [SerializeField]
    private Button cylinderGoToARButton;

    [SerializeField]
    private GameObject mainBackground;


    [Header("UI")]

    [SerializeField]
    private GameObject drawingCanvas;

    [SerializeField]
    private Text statusText;


    [Header("3D Settings")]

    [SerializeField]
    private float distanceFromCamera = 2.5f;


    private bool creationStarted = false;
    private Transform pendingARModelRoot;


    // ==================================================
    // INITIALIZATION & LISTENERS
    // ==================================================

    private void Start()
    {
        if (cylinderGoToARButton != null)
        {
            cylinderGoToARButton.gameObject.SetActive(false);

            cylinderGoToARButton.onClick.RemoveListener(GoToAR);
            cylinderGoToARButton.onClick.AddListener(GoToAR);
        }

        if (cylinderModelSpawner != null)
        {
            cylinderModelSpawner.CylinderFoldCompleted -= HandleCylinderFoldCompleted;
            cylinderModelSpawner.CylinderFoldCompleted += HandleCylinderFoldCompleted;
        }
    }


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
            Debug.LogError("Cylinder validator is not assigned.");
            return;
        }

        if (!netValidator.CurrentNetValid)
        {
            if (statusText != null)
            {
                statusText.text = "Please validate a correct cylinder net first.";
            }

            return;
        }


        // ----------------------------------------------
        // SETUP
        // ----------------------------------------------

        if (setupManager == null)
        {
            Debug.LogError("Cylinder setup manager is not assigned.");
            return;
        }

        if (puzzleBoard == null)
        {
            Debug.LogError("Cylinder PuzzleBoard is not assigned.");
            return;
        }

        CylinderGuidedConfig config = setupManager.GetConfig();

        if (config == null || !config.IsReady)
        {
            Debug.LogError("Cylinder configuration is incomplete.");
            return;
        }


        // ----------------------------------------------
        // GET STUDENT NET FACES
        // ----------------------------------------------

        CylinderGeneratedFace[] foundFaces =
            puzzleBoard.GetComponentsInChildren<CylinderGeneratedFace>(false);

        if (foundFaces.Length != 3)
        {
            Debug.LogError($"Expected 3 cylinder faces on board, found {foundFaces.Length}.");
            return;
        }

        List<CylinderGeneratedFace> faces = new List<CylinderGeneratedFace>(foundFaces);


        // ----------------------------------------------
        // SPAWNER
        // ----------------------------------------------

        if (cylinderModelSpawner == null)
        {
            Debug.LogError("CylinderModelSpawner is not assigned.");
            return;
        }

        creationStarted = true;

        if (statusText != null)
        {
            statusText.text = "Your cylinder net is folding into 3D.";
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
         * The root already exists immediately,
         * even though its folding animation is still running.
         */
        pendingARModelRoot = cylinderModelSpawner.GetCurrentModelRoot();

        if (pendingARModelRoot == null)
        {
            Debug.LogError("Cylinder 3D model root was not created.");

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

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance.Log3DGeneration();
            Debug.Log("Cylinder 3D generation logged.");
        }
        else
        {
            Debug.LogWarning(
                "Cylinder 3D generation could not be logged: " +
                "SessionDataLogger instance is missing."
            );
        }


        // ----------------------------------------------
        // HIDE MAIN BACKGROUND
        // ----------------------------------------------

        if (mainBackground != null)
        {
            mainBackground.SetActive(false);
        }


        // ----------------------------------------------
        // HIDE CYLINDER WORKSPACE
        // ----------------------------------------------

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }


        // ----------------------------------------------
        // KEEP AR BUTTON HIDDEN WHILE FOLDING
        // ----------------------------------------------

        if (cylinderGoToARButton != null)
        {
            cylinderGoToARButton.gameObject.SetActive(false);
        }

        Debug.Log("Cylinder 3D folding started. Waiting for fold completion.");
    }


    // ==================================================
    // FOLD COMPLETE & AR TRANSITION
    // ==================================================

    private void HandleCylinderFoldCompleted()
    {
        if (pendingARModelRoot == null)
        {
            return;
        }

        if (cylinderGoToARButton != null)
        {
            cylinderGoToARButton.gameObject.SetActive(true);
        }

        Debug.Log("Cylinder folding complete. Go To AR button is now visible.");
    }

    public void GoToAR()
    {
        if (pendingARModelRoot == null)
        {
            Debug.LogError("No Cylinder model is ready for AR.");
            return;
        }

        if (arModeManager == null)
        {
            Debug.LogError("ARModeManager is not assigned.");
            return;
        }

        if (cylinderGoToARButton != null)
        {
            cylinderGoToARButton.gameObject.SetActive(false);
        }

        arModeManager.StartARAfterDelay(0f, pendingARModelRoot);

        Debug.Log("Cylinder Go To AR clicked. Starting AR placement mode.");
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetCreationState()
    {
        creationStarted = false;
        pendingARModelRoot = null;

        if (cylinderGoToARButton != null)
        {
            cylinderGoToARButton.gameObject.SetActive(false);
        }

        if (mainBackground != null)
        {
            mainBackground.SetActive(true);
        }

        if (cylinderModelSpawner != null)
        {
            cylinderModelSpawner.ClearCylinder();
        }

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(true);
        }

        /*
         * Return from AR to normal mode
         * if Reset is ever called after AR.
         */
        if (arModeManager != null)
        {
            arModeManager.SetARMode(false);
        }
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (cylinderModelSpawner != null)
        {
            cylinderModelSpawner.CylinderFoldCompleted -= HandleCylinderFoldCompleted;
        }
    }
}
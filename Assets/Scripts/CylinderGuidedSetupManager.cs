using UnityEngine;
using UnityEngine.UI;

public class CylinderGuidedSetupManager : MonoBehaviour
{
    // ==================================================
    // CYLINDER GEOMETRY
    // ==================================================

    [Header("Cylinder Geometry")]

    [SerializeField]
    private float fixedRadius = 2f;


    [Header("Height Options")]

    [SerializeField]
    private float shortHeight = 2f;

    [SerializeField]
    private float mediumHeight = 3f;

    [SerializeField]
    private float longHeight = 4f;


    // ==================================================
    // PANELS
    // ==================================================

    [Header("Panels")]

    [SerializeField]
    private GameObject heightChoicePanel;


    // ==================================================
    // PUZZLE
    // ==================================================

    [Header("Puzzle")]

    [SerializeField]
    private GameObject puzzleBoard;

    [SerializeField]
    private Transform faceTray;


    // ==================================================
    // PUZZLE UI
    // ==================================================

    [Header("Puzzle UI")]

    [SerializeField]
    private GameObject rotateButton;

    [SerializeField]
    private GameObject resetButton;

    [SerializeField]
    private GameObject validateButton;

    [SerializeField]
    private GameObject create3DButton;

    [SerializeField]
    private GameObject guidedStatusObject;

    [SerializeField]
    private GameObject guidedProgressObject;

    [SerializeField]
    private GameObject guidedTitleObject;


    // ==================================================
    // TEXT
    // ==================================================

    [Header("UI Text")]

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text progressText;


    // ==================================================
    // HEIGHT BUTTONS
    // ==================================================

    [Header("Height Buttons")]

    [SerializeField]
    private Button shortHeightButton;

    [SerializeField]
    private Button mediumHeightButton;

    [SerializeField]
    private Button longHeightButton;


    // ==================================================
    // OTHER SYSTEMS
    // ==================================================

    [Header("Face Generation")]

    [SerializeField]
    private CylinderGuidedFaceGenerator faceGenerator;


    [Header("Managers")]

    [SerializeField]
    private CylinderEdgeSnapManager snapManager;

    [SerializeField]
    private CylinderGuidedNetValidator netValidator;

    [SerializeField]
    private CylinderGuidedInteractionManager interactionManager;


    // ==================================================
    // RUNTIME DATA
    // ==================================================

    private CylinderGuidedConfig currentConfig;

    private bool heightSelected = false;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        ResetGuidedSetup();
    }


    // ==================================================
    // HEIGHT SELECTION
    // ==================================================

    public void SelectShortHeight()
    {
        SelectHeight(
            shortHeight,
            "Short"
        );
    }


    public void SelectMediumHeight()
    {
        SelectHeight(
            mediumHeight,
            "Medium"
        );
    }


    public void SelectLongHeight()
    {
        SelectHeight(
            longHeight,
            "Long"
        );
    }


    private void SelectHeight(
        float selectedHeight,
        string heightName)
    {
        // ----------------------------------------------
        // CREATE CONFIGURATION
        // ----------------------------------------------

        currentConfig =
            new CylinderGuidedConfig(
                fixedRadius,
                selectedHeight
            );

        heightSelected = true;


        if (currentConfig == null ||
            !currentConfig.IsReady)
        {
            Debug.LogError(
                "Cylinder configuration could not be created."
            );

            heightSelected = false;

            return;
        }


        Debug.Log(
            $"Cylinder height selected: {heightName}"
        );

        Debug.Log(
            $"Radius = {currentConfig.radius}"
        );

        Debug.Log(
            $"Height = {currentConfig.height}"
        );

        Debug.Log(
            $"Circumference = {currentConfig.Circumference}"
        );


        // ==================================================
        // RESEARCH DATA — START CYLINDER ACTIVITY
        // ==================================================

        /*
         * Start the Cylinder research session only after
         * a valid cylinder height has been selected.
         *
         * This creates Attempt 1.
         *
         * Do NOT put this inside ResetCurrentPuzzle(),
         * because resetting the puzzle should not create
         * a new research attempt.
         */

        if (SessionDataLogger.Instance != null)
        {
            SessionDataLogger.Instance
                .StartActivitySession(
                    "Cylinder"
                );

            SessionDataLogger.Instance
                .StartAttempt();

            Debug.Log(
                "Cylinder research session started. " +
                "Attempt 1."
            );
        }
        else
        {
            Debug.LogWarning(
                "Cylinder research session could not be started: " +
                "SessionDataLogger instance is missing."
            );
        }


        // ----------------------------------------------
        // HIDE HEIGHT SELECTION
        // ----------------------------------------------

        if (heightChoicePanel != null)
        {
            heightChoicePanel.SetActive(
                false
            );
        }


        // ----------------------------------------------
        // SHOW PUZZLE UI
        // ----------------------------------------------

        SetPuzzleUIVisible(
            true
        );


        // ----------------------------------------------
        // RESET OLD RUNTIME STATE
        // ----------------------------------------------

        if (snapManager != null)
        {
            snapManager.ResetSnapping();
        }


        if (netValidator != null)
        {
            netValidator.ResetValidation();
        }


        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }


        // ----------------------------------------------
        // GENERATE 3 FACES
        // ----------------------------------------------

        if (faceGenerator != null)
        {
            faceGenerator.GenerateFaces();
        }
        else
        {
            Debug.LogError(
                "CylinderGuidedFaceGenerator is not assigned."
            );
        }


        // ----------------------------------------------
        // TEXT
        // ----------------------------------------------

        if (statusText != null)
        {
            statusText.text =
                "Drag the 3 faces onto the board.";
        }


        if (progressText != null)
        {
            progressText.text =
                "Step 2 of 3: Build the cylinder net";
        }


        Debug.Log(
            "Cylinder configuration ready and faces generated."
        );
    }


    // ==================================================
    // FULL / INITIAL RESET
    // ==================================================

    public void ResetGuidedSetup()
    {
        currentConfig = null;

        heightSelected = false;


        // ----------------------------------------------
        // CLEAR GENERATED FACES
        // ----------------------------------------------

        if (faceGenerator != null)
        {
            faceGenerator.ClearGeneratedFaces();
        }
        else
        {
            ClearFaceTray();
        }


        // ----------------------------------------------
        // RESET MANAGERS
        // ----------------------------------------------

        if (snapManager != null)
        {
            snapManager.ResetSnapping();
        }


        if (netValidator != null)
        {
            netValidator.ResetValidation();
        }


        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }


        // ----------------------------------------------
        // ALSO HIDE INDIVIDUAL PUZZLE UI
        // ----------------------------------------------

        SetPuzzleUIVisible(
            false
        );


        // ----------------------------------------------
        // SHOW HEIGHT SELECTION
        // ----------------------------------------------

        if (heightChoicePanel != null)
        {
            heightChoicePanel.SetActive(
                true
            );
        }


        // ----------------------------------------------
        // RESET TEXT VALUES
        // ----------------------------------------------

        if (statusText != null)
        {
            statusText.text =
                "Choose the height of your cylinder.";
        }


        if (progressText != null)
        {
            progressText.text =
                "Step 1 of 3";
        }


        Debug.Log(
            "Cylinder guided setup reset."
        );
    }


    // ==================================================
    // RESET CURRENT PUZZLE ONLY
    // ==================================================

    /*
     * Keeps the currently selected height.
     *
     * Does NOT return to the
     * Short / Medium / Long screen.
     *
     * All pieces return to FaceTray.
     *
     * IMPORTANT:
     * This does NOT start a new research attempt.
     */

    public void ResetCurrentPuzzle()
    {
        if (!heightSelected ||
            currentConfig == null)
        {
            return;
        }


        CylinderDynamicFaceDrag[] faces =
            FindObjectsOfType<
                CylinderDynamicFaceDrag>();


        foreach (CylinderDynamicFaceDrag face
                 in faces)
        {
            if (face == null ||
                face.FaceData == null)
            {
                continue;
            }


            face.ReturnToTray();
        }


        // ----------------------------------------------
        // RESET SNAP
        // ----------------------------------------------

        if (snapManager != null)
        {
            snapManager.ResetSnapping();
        }


        // ----------------------------------------------
        // RESET VALIDATION
        // ----------------------------------------------

        if (netValidator != null)
        {
            netValidator.ResetValidation();
        }


        // ----------------------------------------------
        // RESET SELECTION
        // ----------------------------------------------

        if (interactionManager != null)
        {
            interactionManager.ClearSelection();
        }


        // ----------------------------------------------
        // KEEP PUZZLE UI ACTIVE
        // ----------------------------------------------

        SetPuzzleUIVisible(
            true
        );


        // ----------------------------------------------
        // UI TEXT
        // ----------------------------------------------

        if (statusText != null)
        {
            statusText.text =
                "Build the cylinder net again.";
        }


        if (progressText != null)
        {
            progressText.text =
                "Step 2 of 3: Build the cylinder net";
        }


        Debug.Log(
            "Cylinder puzzle reset. All faces returned to FaceTray."
        );
    }


    // ==================================================
    // PUZZLE UI VISIBILITY
    // ==================================================

    private void SetPuzzleUIVisible(
        bool visible)
    {
        if (puzzleBoard != null)
        {
            puzzleBoard.SetActive(
                visible
            );
        }


        if (faceTray != null)
        {
            faceTray.gameObject.SetActive(
                visible
            );
        }


        if (rotateButton != null)
        {
            rotateButton.SetActive(
                visible
            );
        }


        if (resetButton != null)
        {
            resetButton.SetActive(
                visible
            );
        }


        if (validateButton != null)
        {
            validateButton.SetActive(
                visible
            );
        }


        if (create3DButton != null)
        {
            create3DButton.SetActive(
                visible
            );
        }


        if (guidedStatusObject != null)
        {
            guidedStatusObject.SetActive(
                visible
            );
        }


        if (guidedProgressObject != null)
        {
            guidedProgressObject.SetActive(
                visible
            );
        }


        if (guidedTitleObject != null)
        {
            guidedTitleObject.SetActive(
                visible
            );
        }
    }


    // ==================================================
    // CLEAR FACE TRAY
    // ==================================================

    private void ClearFaceTray()
    {
        if (faceTray == null)
            return;


        for (int index =
                 faceTray.childCount - 1;
             index >= 0;
             index--)
        {
            Transform child =
                faceTray.GetChild(
                    index
                );


            if (child == null)
                continue;


            child.gameObject.SetActive(
                false
            );

            Destroy(
                child.gameObject
            );
        }
    }


    // ==================================================
    // PUBLIC GETTERS
    // ==================================================

    public CylinderGuidedConfig GetConfig()
    {
        return currentConfig;
    }


    public bool HasSelectedHeight()
    {
        return heightSelected;
    }
}
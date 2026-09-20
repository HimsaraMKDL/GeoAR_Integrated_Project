using UnityEngine;
using UnityEngine.UI;

public class CylinderGuidedNetValidator :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RectTransform puzzleBoard;

    [SerializeField]
    private CylinderGuidedSetupManager setupManager;

    [SerializeField]
    private CylinderEdgeSnapManager snapManager;


    [Header("UI")]

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text progressText;

    [SerializeField]
    private Button create3DButton;


    [Header("Geometry Tolerance")]

    [SerializeField]
    private float geometryTolerance = 0.05f;


    private bool currentNetValid = false;


    public bool CurrentNetValid =>
        currentNetValid;


    // ==================================================
    // VALIDATE
    // ==================================================

    public void ValidateCylinderNet()
    {
        currentNetValid = false;

        // ----------------------------------------------
        // BASIC REFERENCES
        // ----------------------------------------------

        if (puzzleBoard == null)
        {
            Fail(
                "PuzzleBoard is not assigned."
            );

            return;
        }

        if (setupManager == null)
        {
            Fail(
                "Cylinder setup manager is missing."
            );

            return;
        }

        if (snapManager == null)
        {
            Fail(
                "Cylinder snap manager is missing."
            );

            return;
        }


        CylinderGuidedConfig config =
            setupManager.GetConfig();

        if (config == null ||
            !config.IsReady)
        {
            Fail(
                "Choose a cylinder height first."
            );

            return;
        }


        // ----------------------------------------------
        // FIND FACES ON BOARD
        // ----------------------------------------------

        CylinderGeneratedFace[] faces =
            puzzleBoard.GetComponentsInChildren<
                CylinderGeneratedFace>(
                false
            );

        if (faces.Length != 3)
        {
            Fail(
                "Place all 3 faces on the board."
            );

            Debug.LogWarning(
                $"Cylinder validation: " +
                $"expected 3 faces, found " +
                $"{faces.Length}."
            );

            return;
        }


        int circleCount = 0;
        int rectangleCount = 0;

        CylinderGeneratedFace circle1 = null;
        CylinderGeneratedFace circle2 = null;
        CylinderGeneratedFace rectangle = null;


        foreach (CylinderGeneratedFace face
                 in faces)
        {
            if (face == null)
                continue;


            if (face.IsCircle)
            {
                circleCount++;

                if (circle1 == null)
                {
                    circle1 = face;
                }
                else
                {
                    circle2 = face;
                }
            }


            if (face.IsRectangle)
            {
                rectangleCount++;

                rectangle = face;
            }
        }


        if (circleCount != 2)
        {
            Fail(
                "A cylinder net needs 2 circles."
            );

            return;
        }


        if (rectangleCount != 1)
        {
            Fail(
                "A cylinder net needs 1 rectangle."
            );

            return;
        }


        // ----------------------------------------------
        // SNAP CHECK
        // ----------------------------------------------

        if (!snapManager.HasBothCirclesSnapped)
        {
            Fail(
                "Connect one circle to each side " +
                "of the rectangle."
            );

            Debug.LogWarning(
                "Cylinder validation: " +
                "both circles are not snapped."
            );

            return;
        }


        CylinderGeneratedFace topCircle =
            snapManager.GetTopCircle();

        CylinderGeneratedFace bottomCircle =
            snapManager.GetBottomCircle();


        if (topCircle == null ||
            bottomCircle == null)
        {
            Fail(
                "The cylinder net is not fully connected."
            );

            return;
        }


        if (topCircle == bottomCircle)
        {
            Fail(
                "Use two different circles."
            );

            return;
        }


        // ----------------------------------------------
        // CIRCLE GEOMETRY
        // ----------------------------------------------

        if (Mathf.Abs(
                circle1.radius -
                circle2.radius)
            > geometryTolerance)
        {
            Fail(
                "Both circles must have the same size."
            );

            Debug.LogWarning(
                "Cylinder validation: " +
                "circle radii do not match."
            );

            return;
        }


        if (Mathf.Abs(
                circle1.radius -
                config.radius)
            > geometryTolerance)
        {
            Fail(
                "The circle size does not match " +
                "this cylinder."
            );

            return;
        }


        // ----------------------------------------------
        // RECTANGLE GEOMETRY
        // ----------------------------------------------

        float expectedCircumference =
            2f *
            Mathf.PI *
            config.radius;


        if (Mathf.Abs(
                rectangle.circumference -
                expectedCircumference)
            > geometryTolerance)
        {
            Fail(
                "The rectangle length does not " +
                "match the circles."
            );

            Debug.LogWarning(
                $"Cylinder validation: " +
                $"rectangle circumference = " +
                $"{rectangle.circumference:F3}, " +
                $"expected = " +
                $"{expectedCircumference:F3}."
            );

            return;
        }


        if (Mathf.Abs(
                rectangle.cylinderHeight -
                config.height)
            > geometryTolerance)
        {
            Fail(
                "The rectangle height does not " +
                "match the selected cylinder height."
            );

            return;
        }


        // ==================================================
        // SUCCESS
        // ==================================================

        currentNetValid = true;


        if (statusText != null)
        {
            statusText.text =
                "Excellent! This is a valid cylinder net.";
        }


        if (progressText != null)
        {
            progressText.text =
                "Step 3 of 3: Cylinder net completed";
        }


        if (create3DButton != null)
        {
            create3DButton.interactable =
                true;
        }


        // ==================================================
        // RESEARCH DATA — VALID
        // ==================================================

        LogValidationResult(
            true,
            "Excellent! This is a valid cylinder net.",
            faces.Length
        );


        Debug.Log(
            "CYLINDER VALIDATION SUCCESSFUL: " +
            "2 equal circles, 1 rectangle, " +
            "opposite connections and correct geometry."
        );
    }


    // ==================================================
    // FAILURE
    // ==================================================

    private void Fail(
        string message)
    {
        currentNetValid = false;


        if (statusText != null)
        {
            statusText.text =
                message;
        }


        if (progressText != null)
        {
            progressText.text =
                "Check your cylinder net.";
        }


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }


        // ==================================================
        // RESEARCH DATA — INVALID
        // ==================================================

        int faceCount =
            GetCurrentFaceCount();


        LogValidationResult(
            false,
            message,
            faceCount
        );


        Debug.LogWarning(
            "Cylinder validation failed: " +
            message
        );
    }


    // ==================================================
    // RESEARCH LOGGING
    // ==================================================

    private void LogValidationResult(
        bool isValid,
        string message,
        int faceCount)
    {
        if (SessionDataLogger.Instance == null)
        {
            Debug.LogWarning(
                "Cylinder validation could not be logged: " +
                "SessionDataLogger instance is missing."
            );

            return;
        }


        SessionDataLogger.Instance.LogValidation(
            "Cylinder",
            isValid,
            message,
            faceCount
        );


        Debug.Log(
            "Cylinder validation logged: " +
            (isValid ? "VALID" : "INVALID") +
            " | Faces: " +
            faceCount
        );
    }


    // ==================================================
    // CURRENT FACE COUNT
    // ==================================================

    private int GetCurrentFaceCount()
    {
        if (puzzleBoard == null)
            return 0;


        CylinderGeneratedFace[] faces =
            puzzleBoard.GetComponentsInChildren<
                CylinderGeneratedFace>(
                false
            );


        return faces.Length;
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetValidation()
    {
        currentNetValid = false;


        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class C2_AppUIManager : MonoBehaviour
{
    // =========================================================
    // UI STATES
    // =========================================================

    public enum C2_UIState
    {
        Start,
        Login,
        Scan,
        ShapeSelection,
        Placement,
        Slicing,
        Result
    }


    // =========================================================
    // PANELS
    // =========================================================

    [Header("Main UI Panels")]

    [SerializeField]
    private GameObject startPanel;


    [SerializeField]
    private GameObject loginPanel;


    [SerializeField]
    private GameObject scanPanel;


    [SerializeField]
    private GameObject shapeSelectionPanel;


    [SerializeField]
    private GameObject placementPanel;


    [SerializeField]
    private GameObject slicingPanel;


    [SerializeField]
    private GameObject resultPanel;


    // =========================================================
    // AR REFERENCES
    // =========================================================

    [Header("AR References")]

    [SerializeField]
    private ARPlaneManager planeManager;


    [SerializeField]
    private C2_ARPlacementManager placementManager;


    // =========================================================
    // SHAPE SELECTION BUTTONS
    // =========================================================

    [Header("Shape Selection Buttons")]

    [SerializeField]
    private Button cubeShapeButton;


    [SerializeField]
    private Button cuboidShapeButton;


    [SerializeField]
    private Button prismShapeButton;


    [SerializeField]
    private Button tetrahedronShapeButton;


    [SerializeField]
    private Button cylinderShapeButton;


    // =========================================================
    // PLACEMENT UI
    // =========================================================

    [Header("Placement Shape Text")]

    [SerializeField]
    private Text selectedShapePlacementText;


    // =========================================================
    // SLICING TEXT
    // =========================================================

    [Header("Slicing Text")]

    [SerializeField]
    private Text slicingInstructionText;


    [SerializeField]
    private Text selectedSliceText;


    // =========================================================
    // DIRECTION BUTTONS
    // =========================================================

    [Header("Direction Buttons")]

    [SerializeField]
    private Button verticalButton;


    [SerializeField]
    private Button horizontalButton;


    [SerializeField]
    private Button angledButton;


    // =========================================================
    // FRACTION BUTTONS
    // =========================================================

    [Header("Fraction Buttons")]

    [SerializeField]
    private Button halfButton;


    [SerializeField]
    private Button thirdButton;


    [SerializeField]
    private Button quarterButton;


    [SerializeField]
    private Button threeQuarterButton;


    // =========================================================
    // RESULT
    // =========================================================

    [Header("Result Text")]

    [SerializeField]
    private Text resultTitleText;


    [SerializeField]
    private Text resultInstructionText;


    // =========================================================
    // COLORS
    // =========================================================

    [Header("Selection Colors")]

    [SerializeField]
    private Color normalButtonColor =
        new Color(
            0.92f,
            0.96f,
            1f,
            1f
        );


    [SerializeField]
    private Color selectedButtonColor =
        new Color(
            0.31f,
            0.65f,
            1f,
            1f
        );


    // =========================================================
    // CURRENT STATE
    // =========================================================

    [Header("Current State - Read Only During Play")]

    [SerializeField]
    private C2_UIState currentState =
        C2_UIState.Start;


    [Header("Current Shape - Read Only During Play")]

    [SerializeField]
    private string selectedShape =
        "";


    [Header("Current Slice Selection - Read Only During Play")]

    [SerializeField]
    private string selectedDirection =
        "";


    [SerializeField]
    private string selectedFraction =
        "";


    // =========================================================
    // PUBLIC VALUES
    // =========================================================

    public C2_UIState CurrentState
        => currentState;


    public string SelectedShape
        => selectedShape;


    // =========================================================
    // UNITY START
    // =========================================================

    private void Start()
    {
        ClearShapeSelection();

        ClearSliceSelection();

        ShowStartPanel();
    }


    // =========================================================
    // UNITY UPDATE
    // =========================================================

    private void Update()
    {
        if (
            currentState !=
            C2_UIState.Scan
        )
        {
            return;
        }


        if (planeManager == null)
        {
            return;
        }


        if (
            placementManager != null &&
            placementManager.ShapePlaced
        )
        {
            return;
        }


        if (
            planeManager.trackables.count >
            0
        )
        {
            ShowShapeSelectionPanel();
        }
    }


    // =========================================================
    // START AR
    // =========================================================

    public void StartAR()
    {
        ClearShapeSelection();

        ClearSliceSelection();

        ShowLoginPanel();


        Debug.Log(
            "C2 UI: Start AR selected. Showing Student Login."
        );
    }


    // =========================================================
    // SHAPE SELECTION
    // =========================================================

    public void SetShapeSelection(
        string shape
    )
    {
        selectedShape =
            shape;


        ClearSliceSelection();

        UpdateShapeButtons();

        UpdatePlacementShapeText();

        RefreshSelectionUI();
    }


    public void ClearShapeSelection()
    {
        selectedShape =
            "";


        UpdateShapeButtons();

        UpdatePlacementShapeText();
    }


    // =========================================================
    // SHAPE BUTTON HIGHLIGHTS
    // =========================================================

    private void UpdateShapeButtons()
    {
        SetButtonSelected(
            cubeShapeButton,
            selectedShape == "Cube"
        );


        SetButtonSelected(
            cuboidShapeButton,
            selectedShape == "Cuboid"
        );


        SetButtonSelected(
            prismShapeButton,
            selectedShape == "Prism"
        );


        SetButtonSelected(
            tetrahedronShapeButton,
            selectedShape == "Tetrahedron"
        );


        SetButtonSelected(
            cylinderShapeButton,
            selectedShape == "Cylinder"
        );
    }


    // =========================================================
    // PLACEMENT SHAPE TEXT
    // =========================================================

    private void UpdatePlacementShapeText()
    {
        if (
            selectedShapePlacementText ==
            null
        )
        {
            return;
        }


        if (
            string.IsNullOrEmpty(
                selectedShape
            )
        )
        {
            selectedShapePlacementText.text =
                "Choose a shape first.";

            return;
        }


        selectedShapePlacementText.text =
            "Selected: " +
            selectedShape.ToUpper() +
            "\nTap the blue surface to place it.";
    }


    // =========================================================
    // DIRECTION SELECTION
    // =========================================================

    public void SetDirectionSelection(
        string direction
    )
    {
        selectedDirection =
            direction;


        RefreshSelectionUI();
    }


    // =========================================================
    // FRACTION SELECTION
    // =========================================================

    public void SetFractionSelection(
        string fraction
    )
    {
        selectedFraction =
            fraction;


        RefreshSelectionUI();
    }


    // =========================================================
    // CLEAR SLICE SELECTION
    // =========================================================

    public void ClearSliceSelection()
    {
        selectedDirection =
            "";


        selectedFraction =
            "";


        RefreshSelectionUI();
    }


    // =========================================================
    // REFRESH SELECTION UI
    // =========================================================

    private void RefreshSelectionUI()
    {
        UpdateButtonHighlights();

        UpdateSelectedSliceText();

        UpdateSlicingInstruction();
    }


    // =========================================================
    // SELECTED SLICE TEXT
    // =========================================================

    private void UpdateSelectedSliceText()
    {
        if (selectedSliceText == null)
        {
            return;
        }


        string shapeText =
            string.IsNullOrEmpty(
                selectedShape
            )
                ? "SHAPE"
                : selectedShape.ToUpper();


        bool hasDirection =
            !string.IsNullOrEmpty(
                selectedDirection
            );


        bool hasFraction =
            !string.IsNullOrEmpty(
                selectedFraction
            );


        if (
            !hasDirection &&
            !hasFraction
        )
        {
            selectedSliceText.text =
                shapeText +
                "  •  No slice selected";

            return;
        }


        if (
            hasDirection &&
            !hasFraction
        )
        {
            selectedSliceText.text =
                shapeText +
                "  •  " +
                selectedDirection.ToUpper() +
                "  •  Choose a fraction";

            return;
        }


        if (
            !hasDirection &&
            hasFraction
        )
        {
            selectedSliceText.text =
                shapeText +
                "  •  " +
                selectedFraction +
                "  •  Choose a direction";

            return;
        }


        selectedSliceText.text =
            shapeText +
            "  •  " +
            selectedDirection.ToUpper() +
            "  •  " +
            selectedFraction;
    }


    // =========================================================
    // SLICING INSTRUCTION
    // =========================================================

    private void UpdateSlicingInstruction()
    {
        if (
            slicingInstructionText ==
            null
        )
        {
            return;
        }


        bool hasDirection =
            !string.IsNullOrEmpty(
                selectedDirection
            );


        bool hasFraction =
            !string.IsNullOrEmpty(
                selectedFraction
            );


        if (
            !hasDirection &&
            !hasFraction
        )
        {
            slicingInstructionText.text =
                "Choose a direction and a fraction.";

            return;
        }


        if (!hasDirection)
        {
            slicingInstructionText.text =
                "Now choose a slicing direction.";

            return;
        }


        if (!hasFraction)
        {
            slicingInstructionText.text =
                "Now choose a fraction.";

            return;
        }


        slicingInstructionText.text =
            "Trace the yellow line from one point " +
            "to the other.";
    }


    // =========================================================
    // BUTTON HIGHLIGHTS
    // =========================================================

    private void UpdateButtonHighlights()
    {
        SetButtonSelected(
            verticalButton,
            selectedDirection ==
                "Vertical"
        );


        SetButtonSelected(
            horizontalButton,
            selectedDirection ==
                "Horizontal"
        );


        SetButtonSelected(
            angledButton,
            selectedDirection ==
                "Angled"
        );


        SetButtonSelected(
            halfButton,
            selectedFraction ==
                "1/2"
        );


        SetButtonSelected(
            thirdButton,
            selectedFraction ==
                "1/3"
        );


        SetButtonSelected(
            quarterButton,
            selectedFraction ==
                "1/4"
        );


        SetButtonSelected(
            threeQuarterButton,
            selectedFraction ==
                "3/4"
        );
    }


    // =========================================================
    // GENERIC BUTTON COLOR
    // =========================================================

    private void SetButtonSelected(
        Button button,
        bool selected
    )
    {
        if (
            button == null ||
            button.image == null
        )
        {
            return;
        }


        button.image.color =
            selected
                ? selectedButtonColor
                : normalButtonColor;
    }


    // =========================================================
    // RESULT TEXT
    // =========================================================

    private void UpdateResultText()
    {
        if (resultTitleText != null)
        {
            resultTitleText.text =
                "GREAT JOB!";
        }


        if (
            resultInstructionText ==
            null
        )
        {
            return;
        }


        string shapeText =
            string.IsNullOrEmpty(
                selectedShape
            )
                ? "Shape"
                : selectedShape;


        string directionText =
            string.IsNullOrEmpty(
                selectedDirection
            )
                ? "slice"
                : selectedDirection;


        string fractionText =
            string.IsNullOrEmpty(
                selectedFraction
            )
                ? ""
                : selectedFraction;


        if (
            string.IsNullOrEmpty(
                fractionText
            )
        )
        {
            resultInstructionText.text =
                "You successfully sliced the " +
                shapeText.ToLower() +
                "!\n\n" +

                GetShapeLearningText(
                    selectedShape
                ) +
                "\n\n" +

                "Move around the model to explore " +
                "it from different sides.";

            return;
        }


        string complement =
            GetComplementFraction(
                fractionText
            );


        string equation =
            GetFractionEquation(
                fractionText
            );


        string directionLearning =
            GetDirectionLearningText(
                selectedDirection
            );


        string shapeLearning =
            GetShapeLearningText(
                selectedShape
            );


        resultInstructionText.text =
            shapeText +
            " " +
            directionText +
            " " +
            fractionText +
            " slice complete!\n\n" +

            "Blue part = " +
            fractionText +
            "\n" +

            "Orange part = " +
            complement +
            "\n" +

            equation +
            "\n\n" +

            directionLearning +
            "\n" +

            shapeLearning +
            "\n\n" +

            "Move around the model to explore " +
            "it from different sides.";
    }


    // =========================================================
    // COMPLEMENT FRACTION
    // =========================================================

    private string GetComplementFraction(
        string fraction
    )
    {
        switch (fraction)
        {
            case "1/2":
                return "1/2";

            case "1/3":
                return "2/3";

            case "1/4":
                return "3/4";

            case "3/4":
                return "1/4";

            default:
                return "";
        }
    }


    // =========================================================
    // FRACTION EQUATION
    // =========================================================

    private string GetFractionEquation(
        string fraction
    )
    {
        switch (fraction)
        {
            case "1/2":
                return
                    "1/2 + 1/2 = 1 whole";

            case "1/3":
                return
                    "1/3 + 2/3 = 1 whole";

            case "1/4":
                return
                    "1/4 + 3/4 = 1 whole";

            case "3/4":
                return
                    "3/4 + 1/4 = 1 whole";

            default:
                return "";
        }
    }


    // =========================================================
    // DIRECTION EDUCATION
    // =========================================================

    private string GetDirectionLearningText(
        string direction
    )
    {
        switch (direction)
        {
            case "Vertical":
                return
                    "A vertical slice goes from " +
                    "top to bottom.";

            case "Horizontal":
                return
                    "A horizontal slice goes from " +
                    "side to side.";

            case "Angled":
                return
                    "An angled slice cuts across " +
                    "the shape diagonally.";

            default:
                return "";
        }
    }


    // =========================================================
    // SHAPE EDUCATION
    // =========================================================

    private string GetShapeLearningText(
        string shape
    )
    {
        switch (shape)
        {
            case "Cube":
                return
                    "A cube has 6 equal square faces.";

            case "Cuboid":
                return
                    "A cuboid is a box-shaped solid " +
                    "with rectangular faces.";

            case "Prism":
                return
                    "A triangular prism has 2 triangular " +
                    "faces and 3 rectangular faces.";

            case "Tetrahedron":
                return
                    "A tetrahedron has 4 triangular faces, " +
                    "4 vertices and 6 edges.";

            case "Cylinder":
                return
                    "A cylinder has 2 circular faces " +
                    "and 1 curved surface.";

            default:
                return "";
        }
    }


    // =========================================================
    // SHOW START PANEL
    // =========================================================

    public void ShowStartPanel()
    {
        currentState =
            C2_UIState.Start;


        SetOnlyPanelActive(
            startPanel
        );
    }


    // =========================================================
    // SHOW LOGIN PANEL
    // =========================================================

    public void ShowLoginPanel()
    {
        currentState =
            C2_UIState.Login;


        SetOnlyPanelActive(
            loginPanel
        );
    }


    // =========================================================
    // SHOW SCAN PANEL
    // =========================================================

    public void ShowScanPanel()
    {
        currentState =
            C2_UIState.Scan;


        SetOnlyPanelActive(
            scanPanel
        );
    }


    // =========================================================
    // SHOW SHAPE SELECTION PANEL
    // =========================================================

    public void ShowShapeSelectionPanel()
    {
        currentState =
            C2_UIState.ShapeSelection;


        SetOnlyPanelActive(
            shapeSelectionPanel
        );
    }


    // =========================================================
    // SHOW PLACEMENT PANEL
    // =========================================================

    public void ShowPlacementPanel()
    {
        currentState =
            C2_UIState.Placement;


        UpdatePlacementShapeText();


        SetOnlyPanelActive(
            placementPanel
        );
    }


    // =========================================================
    // SHOW SLICING PANEL
    // =========================================================

    public void ShowSlicingPanel()
    {
        currentState =
            C2_UIState.Slicing;


        RefreshSelectionUI();


        SetOnlyPanelActive(
            slicingPanel
        );
    }


    // =========================================================
    // SHOW RESULT PANEL
    // =========================================================

    public void ShowResultPanel()
    {
        currentState =
            C2_UIState.Result;


        UpdateResultText();


        SetOnlyPanelActive(
            resultPanel
        );
    }


    // =========================================================
    // PANEL VISIBILITY
    // =========================================================

    private void SetOnlyPanelActive(
        GameObject panelToShow
    )
    {
        if (startPanel != null)
        {
            startPanel.SetActive(
                panelToShow ==
                startPanel
            );
        }


        if (loginPanel != null)
        {
            loginPanel.SetActive(
                panelToShow ==
                loginPanel
            );
        }


        if (scanPanel != null)
        {
            scanPanel.SetActive(
                panelToShow ==
                scanPanel
            );
        }


        if (
            shapeSelectionPanel !=
            null
        )
        {
            shapeSelectionPanel.SetActive(
                panelToShow ==
                shapeSelectionPanel
            );
        }


        if (placementPanel != null)
        {
            placementPanel.SetActive(
                panelToShow ==
                placementPanel
            );
        }


        if (slicingPanel != null)
        {
            slicingPanel.SetActive(
                panelToShow ==
                slicingPanel
            );
        }


        if (resultPanel != null)
        {
            resultPanel.SetActive(
                panelToShow ==
                resultPanel
            );
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class CuboidInvalidPopupManager : MonoBehaviour
{
    // ==================================================
    // PANELS
    // ==================================================

    [Header("Panels")]

    [SerializeField]
    private GameObject invalidPopupPanel;

    [SerializeField]
    private GameObject correctionPanel;

    [SerializeField]
    private GameObject cuboidWorkspacePanel;


    // ==================================================
    // POPUP TEXT
    // ==================================================

    [Header("Popup Text")]

    [SerializeField]
    private Text popupTitleText;

    [SerializeField]
    private Text popupMessageText;


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]

    [SerializeField]
    private CuboidDrawManager cuboidDrawManager;

    [SerializeField]
    private GeometryMenuManager geometryMenuManager;


    // ==================================================
    // CORRECTION
    // ==================================================

    [Header("Correction")]

    [SerializeField]
    private CuboidCorrectionManagerV2 cuboidCorrectionManager;

    [SerializeField]
    private CuboidCorrectionGuidanceAnimator
        cuboidCorrectionGuidanceAnimator;


    private void Start()
    {
        HideAllCorrectionUI();
    }


    // ==================================================
    // INVALID POPUP
    // ==================================================

    public void ShowInvalidCuboidPopup(
        string validationMessage)
    {
        if (invalidPopupPanel == null)
        {
            Debug.LogError(
                "CuboidInvalidPopupPanel is not assigned."
            );

            return;
        }

        if (popupTitleText != null)
        {
            popupTitleText.text =
                "Almost there!";
        }

        if (popupMessageText != null)
        {
            popupMessageText.text =
                "This net needs a small fix.\n" +
                "Would you like some help?";
        }

        invalidPopupPanel.SetActive(true);

        if (correctionPanel != null)
        {
            correctionPanel.SetActive(false);
        }

        Debug.Log(
            "Cuboid invalid popup opened. " +
            validationMessage
        );
    }


    // ==================================================
    // HELP ME FIX IT
    // ==================================================

    public void OpenCorrectionScreen()
    {
        /*
         * Do NOT clear original Cuboid drawing.
         */

        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(false);
        }

        if (cuboidWorkspacePanel != null)
        {
            cuboidWorkspacePanel.SetActive(false);
        }

        if (correctionPanel != null)
        {
            correctionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "CuboidCorrectionPanel is not assigned."
            );

            return;
        }


        // ----------------------------------------------
        // BUILD CORRECTION
        // ----------------------------------------------

        if (cuboidCorrectionManager != null)
        {
            cuboidCorrectionManager
                .PrepareCorrection();
        }
        else
        {
            Debug.LogError(
                "CuboidCorrectionManagerV2 is not assigned."
            );

            return;
        }


        // ----------------------------------------------
        // START GUIDANCE
        // ----------------------------------------------

        if (cuboidCorrectionGuidanceAnimator != null)
        {
            cuboidCorrectionGuidanceAnimator
                .PlayGuidance();
        }
        else
        {
            Debug.LogWarning(
                "CuboidCorrectionGuidanceAnimator is not assigned."
            );
        }


        Debug.Log(
            "Cuboid correction screen opened. " +
            "Original drawing preserved."
        );
    }


    // ==================================================
    // TRY AGAIN
    // ==================================================

    public void TryAgain()
    {
        if (cuboidCorrectionGuidanceAnimator != null)
        {
            cuboidCorrectionGuidanceAnimator
                .ResetGuidance();
        }


        if (correctionPanel != null)
        {
            correctionPanel.SetActive(false);
        }


        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(false);
        }


        if (cuboidWorkspacePanel != null)
        {
            cuboidWorkspacePanel.SetActive(true);
        }


        Debug.Log(
            "Returned to Cuboid drawing. " +
            "Original net is preserved."
        );
    }


    // ==================================================
    // HOME
    // ==================================================

    public void GoHome()
    {
        if (cuboidCorrectionGuidanceAnimator != null)
        {
            cuboidCorrectionGuidanceAnimator
                .ResetGuidance();
        }


        if (cuboidDrawManager != null)
        {
            cuboidDrawManager
                .ClearDrawing();
        }


        HideAllCorrectionUI();


        if (geometryMenuManager != null)
        {
            geometryMenuManager
                .ShowMainMenu();
        }
        else
        {
            Debug.LogError(
                "GeometryMenuManager is not assigned."
            );
        }


        Debug.Log(
            "Cuboid correction cancelled. Returned Home."
        );
    }


    // ==================================================
    // HIDE POPUP
    // ==================================================

    public void HideInvalidPopup()
    {
        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(false);
        }
    }


    // ==================================================
    // HIDE ALL
    // ==================================================

    public void HideAllCorrectionUI()
    {
        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(false);
        }

        if (correctionPanel != null)
        {
            correctionPanel.SetActive(false);
        }
    }
}
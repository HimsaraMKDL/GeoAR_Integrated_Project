using UnityEngine;
using UnityEngine.UI;

public class InvalidNetPopupManager : MonoBehaviour
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
    private GameObject cubeWorkspacePanel;


    // ==================================================
    // TEXT
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
    private GridDrawManager cubeDrawManager;

    [SerializeField]
    private GeometryMenuManager geometryMenuManager;

    [SerializeField]
    private CubeCorrectionManager cubeCorrectionManager;

    [SerializeField]
    private CubeCorrectionGuidanceAnimator cubeCorrectionGuidanceAnimator;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        HideAllCorrectionUI();
    }


    // ==================================================
    // SHOW INVALID CUBE POPUP
    // ==================================================

    public void ShowInvalidCubePopup(
        string validationMessage)
    {
        if (invalidPopupPanel == null)
        {
            Debug.LogError(
                "CubeInvalidPopupPanel is not assigned."
            );

            return;
        }


        if (popupTitleText != null)
        {
            popupTitleText.text =
                "Almost there!";
        }


        /*
         * For the child-facing popup,
         * keep the wording simple.
         *
         * The technical validator message
         * can still be kept in the Console.
         */
        if (popupMessageText != null)
        {
            popupMessageText.text =
                "This net needs a small fix.\n" +
                "Would you like some help?";
        }


        invalidPopupPanel.SetActive(
            true
        );


        if (correctionPanel != null)
        {
            correctionPanel.SetActive(
                false
            );
        }


        Debug.Log(
            "Cube invalid popup opened. " +
            validationMessage
        );
    }


    // ==================================================
    // HOME
    // ==================================================

    public void GoHome()
    {
        /*
         * Home means start fresh.
         * Therefore clear the existing cube activity.
         */

        if (cubeDrawManager != null)
        {
            cubeDrawManager.ClearDrawing();
        }


        HideAllCorrectionUI();


        if (geometryMenuManager != null)
        {
            geometryMenuManager.ShowMainMenu();
        }
        else
        {
            Debug.LogError(
                "GeometryMenuManager is not assigned."
            );
        }


        Debug.Log(
            "Cube correction cancelled. " +
            "Returned Home with a fresh state."
        );
    }


    // ==================================================
    // HELP ME FIX IT
    // ==================================================

    public void OpenCorrectionScreen()
    {
        /*
         * IMPORTANT:
         *
         * Do NOT call ClearDrawing().
         *
         * The child's original Cube drawing
         * remains in GridDrawManager memory.
         */

        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(
                false
            );
        }


        if (cubeWorkspacePanel != null)
        {
            cubeWorkspacePanel.SetActive(
                false
            );
        }


        if (correctionPanel != null)
        {
            correctionPanel.SetActive(
                true
            );
        }

        if (cubeCorrectionManager != null)
        {
            cubeCorrectionManager.PrepareCorrection();
        }

        if (cubeCorrectionGuidanceAnimator != null)
        {
            cubeCorrectionGuidanceAnimator.PlayGuidance();
        }

        Debug.Log(
            "Cube correction screen opened. " +
            "Original drawing preserved."
        );
    }


    // ==================================================
    // TRY AGAIN
    // ==================================================

    public void TryAgain()
    {
        if (cubeCorrectionGuidanceAnimator != null)
        {
            cubeCorrectionGuidanceAnimator.ResetGuidance();
        }

        /*
         * Return to the same Cube workspace.
         *
         * Because ClearDrawing() is NOT called,
         * the student's previous net remains.
         */

        if (correctionPanel != null)
        {
            correctionPanel.SetActive(
                false
            );
        }


        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(
                false
            );
        }


        if (cubeWorkspacePanel != null)
        {
            cubeWorkspacePanel.SetActive(
                true
            );
        }


        Debug.Log(
            "Returned to Cube drawing. " +
            "Original net is preserved for correction."
        );
    }


    // ==================================================
    // HIDE
    // ==================================================

    public void HideInvalidPopup()
    {
        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(
                false
            );
        }
    }


    public void HideAllCorrectionUI()
    {
        if (invalidPopupPanel != null)
        {
            invalidPopupPanel.SetActive(
                false
            );
        }


        if (correctionPanel != null)
        {
            correctionPanel.SetActive(
                false
            );
        }
    }
}
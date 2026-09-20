using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidedPrismCreate3DManager : MonoBehaviour
{
    [Header("Guided Prism References")]
    [SerializeField]
    private PrismGuidedSetupManager setupManager;

    [SerializeField]
    private PrismGuidedNetValidator netValidator;

    [SerializeField]
    private PrismModelSpawner prismModelSpawner;

    [SerializeField]
    private RectTransform puzzleBoard;

    [Header("AR")]
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

    [SerializeField]
    private float previewBeforeAR = 10f;

    private bool creationStarted = false;
    private bool foldingCompleted = false;

    public void Create3DPrism()
    {
        if (creationStarted)
            return;

        if (netValidator == null)
        {
            Debug.LogError(
                "PrismGuidedNetValidator is not assigned."
            );
            return;
        }

        if (!netValidator.CurrentNetValid)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Please validate a correct prism net first.";
            }

            return;
        }

        if (setupManager == null)
        {
            Debug.LogError(
                "PrismGuidedSetupManager is not assigned."
            );
            return;
        }

        PrismGuidedConfig config =
            setupManager.GetConfig();

        if (config == null ||
            !config.IsReady)
        {
            Debug.LogError(
                "Guided prism configuration is incomplete."
            );
            return;
        }

        if (puzzleBoard == null)
        {
            Debug.LogError(
                "PuzzleBoard is not assigned."
            );
            return;
        }

        PrismGeneratedFace[] foundFaces =
            puzzleBoard.GetComponentsInChildren<
                PrismGeneratedFace>(
                false
            );

        if (foundFaces.Length != 5)
        {
            Debug.LogError(
                $"Expected 5 prism faces, found {foundFaces.Length}."
            );
            return;
        }

        if (prismModelSpawner == null)
        {
            Debug.LogError(
                "PrismModelSpawner is not assigned."
            );
            return;
        }

        creationStarted = true;
        foldingCompleted = false;

        if (statusText != null)
        {
            statusText.text =
                "Watch your net fold into a prism!";
        }

        List<PrismGeneratedFace> faces =
            new List<PrismGeneratedFace>(
                foundFaces
            );

        NetToPrismFolder folder =
            prismModelSpawner.GetFolder();

        if (folder == null)
        {
            Debug.LogError(
                "NetToPrismFolder is missing."
            );

            creationStarted = false;
            return;
        }

        folder.FoldingCompleted -=
            HandleFoldingCompleted;

        folder.FoldingCompleted +=
            HandleFoldingCompleted;

        /*
         * Source UI must still be active here.
         * NetToPrismFolder reads current UI
         * positions, rotations and flip states.
         */
        prismModelSpawner.CreatePrismFromGuidedNet(
            config,
            faces,
            puzzleBoard,
            distanceFromCamera
        );

        /*
         * UI positions have now been captured.
         * Hide the canvas so the world-space
         * net/folding animation becomes visible.
         */
        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }

        Debug.Log(
            "Guided Prism folding sequence started."
        );
    }

    private void HandleFoldingCompleted()
    {
        if (foldingCompleted)
            return;

        foldingCompleted = true;

        Debug.Log(
            "Guided Prism folding completed."
        );

        StartCoroutine(
            PreviewThenStartAR()
        );
    }

    private IEnumerator PreviewThenStartAR()
    {
        Debug.Log(
            $"Showing completed prism for {previewBeforeAR} seconds."
        );

        yield return new WaitForSeconds(
            previewBeforeAR
        );

        if (prismModelSpawner == null)
            yield break;

        Transform modelRoot =
            prismModelSpawner
                .GetCurrentModelRoot();

        if (modelRoot == null)
        {
            Debug.LogError(
                "Cannot start AR. Prism model root is missing."
            );
            yield break;
        }

        if (arModeManager == null)
        {
            Debug.LogError(
                "ARModeManager is not assigned."
            );
            yield break;
        }

        /*
         * ARModeManager already contains the
         * working Cube AR pipeline.
         */
        arModeManager.StartARAfterDelay(
            0f,
            modelRoot
        );

        Debug.Log(
            "Prism sent to AR placement system."
        );
    }

    public void ResetCreationState()
    {
        StopAllCoroutines();

        creationStarted = false;
        foldingCompleted = false;

        if (prismModelSpawner != null)
        {
            NetToPrismFolder folder =
                prismModelSpawner.GetFolder();

            if (folder != null)
            {
                folder.FoldingCompleted -=
                    HandleFoldingCompleted;
            }

            prismModelSpawner.ClearPrism();
        }

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (prismModelSpawner == null)
            return;

        NetToPrismFolder folder =
            prismModelSpawner.GetFolder();

        if (folder != null)
        {
            folder.FoldingCompleted -=
                HandleFoldingCompleted;
        }
    }
}
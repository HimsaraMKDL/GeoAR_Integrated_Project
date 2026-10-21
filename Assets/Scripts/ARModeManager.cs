using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ARModeManager : MonoBehaviour
{
    // ==================================================
    // NORMAL MODE
    // ==================================================

    [Header("Normal Mode")]
    public Camera normalCamera;


    // ==================================================
    // AR MODE
    // ==================================================

    [Header("AR Mode Objects")]
    public GameObject arSession;
    public GameObject xrOrigin;
    public GameObject arPlacedModelManager;


    // ==================================================
    // UI
    // ==================================================

    [Header("Normal UI")]
    public GameObject drawingCanvas;

    [Header("AR Overlay UI")]
    [SerializeField]
    private GameObject arOverlayCanvas;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        /*
         * Application always starts
         * in normal non-AR mode.
         */
        SetARMode(false);
    }


    // ==================================================
    // AR MODE TOGGLE
    // ==================================================

    public void SetARMode(bool active)
    {
        // ----------------------------------------------
        // NORMAL CAMERA
        // ----------------------------------------------

        if (normalCamera != null)
        {
            normalCamera.gameObject.SetActive(!active);
        }


        // ----------------------------------------------
        // AR SYSTEM
        // ----------------------------------------------

        if (arSession != null)
        {
            arSession.SetActive(active);
        }

        if (xrOrigin != null)
        {
            xrOrigin.SetActive(active);
        }

        if (arPlacedModelManager != null)
        {
            arPlacedModelManager.SetActive(active);
        }


        // ----------------------------------------------
        // AR OVERLAY UI
        // ----------------------------------------------

        if (arOverlayCanvas != null)
        {
            arOverlayCanvas.SetActive(active);
        }
    }


    // ==================================================
    // START AR DIRECTLY
    // ==================================================

    public IEnumerator StartARAfterCreate()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }

        SetARMode(true);

        yield return null;

        Debug.Log(
            "AR camera started after 3D model creation."
        );
    }


    // ==================================================
    // START AR AFTER DELAY
    // ==================================================

    public void StartARAfterDelay(
        float delay,
        Transform modelRoot)
    {
        StartCoroutine(
            StartARAfterDelayRoutine(
                delay,
                modelRoot
            )
        );
    }


    private IEnumerator StartARAfterDelayRoutine(
        float delay,
        Transform modelRoot)
    {
        // ----------------------------------------------
        // WAIT FOR 3D PREVIEW
        // ----------------------------------------------

        yield return new WaitForSeconds(delay);


        // ----------------------------------------------
        // HIDE NORMAL DRAWING UI
        // ----------------------------------------------

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(false);
        }


        // ----------------------------------------------
        // START AR
        // ----------------------------------------------

        SetARMode(true);


        /*
         * Give AR system a short moment
         * to initialize.
         */
        yield return new WaitForSeconds(0.5f);


        // ----------------------------------------------
        // PASS MODEL TO AR PLACEMENT
        // ----------------------------------------------

        if (arPlacedModelManager != null &&
            modelRoot != null)
        {
            ARPlacedModelManager manager =
                arPlacedModelManager.GetComponent<
                    ARPlacedModelManager>();

            if (manager != null)
            {
                manager.SetModelToPlace(modelRoot);

                Debug.Log(
                    "Model ready for AR placement. " +
                    "Tap on a detected plane."
                );
            }
            else
            {
                Debug.LogError(
                    "ARPlacedModelManager script " +
                    "not found on assigned GameObject."
                );
            }
        }
    }


    // ==================================================
    // SHOW MAIN MENU
    // ==================================================

    /*
     * Use this function for:
     *
     * ARHomeButton
     *      On Click()
     *          ARModeManager -> ShowMainMenu()
     *
     * The current scene is reloaded so every geometry
     * activity returns to a completely clean state.
     *
     * After reload, GeometryMenuManager.Start()
     * can show the Main Menu again.
     */
    public void ShowMainMenu()
    {
        Debug.Log(
            "Show Main Menu requested from AR."
        );

        ReloadGeometryScene();
    }


    // ==================================================
    // OLD RETURN HOME FUNCTION
    // ==================================================

    /*
     * Kept for compatibility with any existing button
     * that is already connected to ReturnHomeFromAR().
     *
     * Both functions now do the same thing.
     */
    public void ReturnHomeFromAR()
    {
        Debug.Log(
            "ReturnHomeFromAR called."
        );

        ShowMainMenu();
    }


    // ==================================================
    // RELOAD CURRENT SCENE
    // ==================================================

    private void ReloadGeometryScene()
    {
        /*
         * Reloading the active scene gives a clean start:
         *
         * - AR mode is stopped
         * - placed AR model is removed
         * - generated 3D model is removed
         * - Cube drawing is cleared
         * - Cuboid drawing is cleared
         * - Tetrahedron drawing is cleared
         * - Prism faces are cleared
         * - Cylinder faces are cleared
         * - validation states are reset
         * - Create 3D state is reset
         * - AR placement state is reset
         * - temporary UI is reset
         *
         * GeometryMenuManager.Start()
         * should then display MainMenuPanel.
         */

        Scene currentScene =
            SceneManager.GetActiveScene();

        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(
                currentScene.buildIndex
            );
        }
        else
        {
            /*
             * Fallback for Editor testing if for some
             * reason the scene does not have a valid
             * Build Settings index.
             */
            SceneManager.LoadScene(
                currentScene.name
            );
        }
    }
}
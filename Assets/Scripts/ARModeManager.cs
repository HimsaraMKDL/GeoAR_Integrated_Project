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
            normalCamera.gameObject.SetActive(
                !active
            );
        }


        // ----------------------------------------------
        // AR SYSTEM
        // ----------------------------------------------

        if (arSession != null)
        {
            arSession.SetActive(
                active
            );
        }


        if (xrOrigin != null)
        {
            xrOrigin.SetActive(
                active
            );
        }


        if (arPlacedModelManager != null)
        {
            arPlacedModelManager.SetActive(
                active
            );
        }


        // ----------------------------------------------
        // AR OVERLAY
        // ----------------------------------------------

        if (arOverlayCanvas != null)
        {
            arOverlayCanvas.SetActive(
                active
            );
        }
    }


    // ==================================================
    // START AR DIRECTLY
    // ==================================================

    public IEnumerator StartARAfterCreate()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(
                false
            );
        }


        SetARMode(
            true
        );


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

        yield return
            new WaitForSeconds(
                delay
            );


        // ----------------------------------------------
        // HIDE NORMAL DRAWING UI
        // ----------------------------------------------

        if (drawingCanvas != null)
        {
            drawingCanvas.SetActive(
                false
            );
        }


        // ----------------------------------------------
        // START AR
        // ----------------------------------------------

        SetARMode(
            true
        );


        /*
         * Give AR system a short moment
         * to initialize.
         */
        yield return
            new WaitForSeconds(
                0.5f
            );


        // ----------------------------------------------
        // PASS MODEL TO AR PLACEMENT
        // ----------------------------------------------

        if (arPlacedModelManager != null &&
            modelRoot != null)
        {
            ARPlacedModelManager manager =
                arPlacedModelManager
                    .GetComponent<
                        ARPlacedModelManager>();


            if (manager != null)
            {
                manager.SetModelToPlace(
                    modelRoot
                );


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
    // RETURN HOME FROM AR
    // ==================================================

    public void ReturnHomeFromAR()
    {
        Debug.Log(
            "Returning Home from AR. " +
            "Resetting complete geometry activity."
        );


        /*
         * Reloading the active scene gives us
         * a guaranteed clean start:
         *
         * - old cube net removed
         * - old cuboid net removed
         * - old prism faces removed
         * - old cylinder faces removed
         * - generated 3D model removed
         * - AR placement cleared
         * - validation states cleared
         * - depth / height selections cleared
         *
         * GeometryMenuManager.Start()
         * then opens MainMenuPanel again.
         */

        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }
}
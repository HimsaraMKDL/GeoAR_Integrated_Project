using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class ARPlacedModelManager : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;

    public ARPlaneManager planeManager;


    [Header("Face Interaction")]
    [SerializeField]
    private ARFaceColorController faceColorController;


    [Header("Model Root")]
    public Transform modelToPlace;


    [Header("Settings")]
    public bool allowReposition = true;


    // ==================================================
    // AR GUIDANCE UI
    // ==================================================

    [Header("AR Guidance UI")]
    [SerializeField]
    private Text surfaceStatusText;

    [SerializeField]
    private string searchingMessage =
        "Move your phone slowly to find a surface...";

    [SerializeField]
    private string surfaceFoundMessage =
        "Surface detected! Tap to place the model.";


    // ==================================================
    // RUNTIME
    // ==================================================

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private bool hasPlaced = false;


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (modelToPlace == null)
            return;


        UpdateSurfaceGuidance();


        if (Input.touchCount == 0)
            return;


        Touch touch =
            Input.GetTouch(0);


        if (touch.phase != TouchPhase.Began)
        {
            return;
        }


        // ==================================================
        // MODEL ALREADY PLACED
        // ==================================================

        if (hasPlaced)
        {
            /*
             * If the user touched the active model,
             * DO NOT reposition it.
             *
             * ARFaceColorController handles
             * the short model interaction.
             */

            if (faceColorController != null &&
                faceColorController
                    .IsScreenPointOnActiveModel(
                        touch.position))
            {
                return;
            }


            if (!allowReposition)
            {
                return;
            }
        }


        // ==================================================
        // AR PLANE RAYCAST
        // ==================================================

        if (raycastManager == null)
        {
            return;
        }


        if (raycastManager.Raycast(
                touch.position,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose =
                hits[0].pose;


            // ----------------------------------------------
            // POSITION
            // ----------------------------------------------

            modelToPlace.position =
                hitPose.position;


            // ----------------------------------------------
            // ROTATION
            // ----------------------------------------------

            Camera activeCamera =
                Camera.main;


            if (activeCamera != null)
            {
                modelToPlace.rotation =
                    Quaternion.Euler(
                        0f,
                        activeCamera.transform
                            .eulerAngles.y,
                        0f
                    );
            }


            // ----------------------------------------------
            // ACTIVATE MODEL
            // ----------------------------------------------

            modelToPlace.gameObject.SetActive(
                true
            );


            hasPlaced = true;


            // ==================================================
            // RESEARCH DATA — AR PLACEMENT
            // ==================================================

            /*
             * AR placement is logged only after:
             *
             * 1. A valid AR plane was detected.
             * 2. The model position was assigned.
             * 3. The model was activated.
             * 4. hasPlaced became true.
             *
             * Therefore this represents an actual
             * successful AR placement.
             */

            if (SessionDataLogger.Instance != null)
            {
                SessionDataLogger.Instance
                    .LogARPlacement();

                Debug.Log(
                    "AR placement logged for research data."
                );
            }
            else
            {
                Debug.LogWarning(
                    "AR placement could not be logged: " +
                    "SessionDataLogger instance is missing."
                );
            }


            // ----------------------------------------------
            // HIDE GUIDANCE MESSAGE
            // ----------------------------------------------

            if (surfaceStatusText != null)
            {
                surfaceStatusText.gameObject
                    .SetActive(false);
            }


            // ----------------------------------------------
            // FACE COLORING
            // ----------------------------------------------

            if (faceColorController != null)
            {
                faceColorController
                    .SetActiveModel(
                        modelToPlace
                    );
            }


            // ----------------------------------------------
            // HIDE PLANES
            // ----------------------------------------------

            HidePlanes();


            Debug.Log(
                "Model placed on AR plane."
            );
        }
    }


    // ==================================================
    // SURFACE GUIDANCE
    // ==================================================

    private void UpdateSurfaceGuidance()
    {
        if (surfaceStatusText == null)
            return;


        if (hasPlaced)
        {
            surfaceStatusText.gameObject
                .SetActive(false);

            return;
        }


        if (raycastManager == null)
            return;


        Vector2 screenCenter =
            new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );


        if (raycastManager.Raycast(
                screenCenter,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            surfaceStatusText.gameObject
                .SetActive(true);

            surfaceStatusText.text =
                surfaceFoundMessage;
        }
        else
        {
            surfaceStatusText.gameObject
                .SetActive(true);

            surfaceStatusText.text =
                searchingMessage;
        }
    }


    // ==================================================
    // SET MODEL
    // ==================================================

    public void SetModelToPlace(
        Transform modelRoot)
    {
        /*
         * Clear previous face-color target.
         */

        if (faceColorController != null)
        {
            faceColorController
                .SetActiveModel(null);
        }


        modelToPlace =
            modelRoot;


        if (modelToPlace != null)
        {
            modelToPlace.gameObject
                .SetActive(false);
        }


        hasPlaced = false;


        ShowPlanes();


        // ----------------------------------------------
        // SHOW GUIDANCE
        // ----------------------------------------------

        if (surfaceStatusText != null)
        {
            surfaceStatusText.gameObject
                .SetActive(true);

            surfaceStatusText.text =
                searchingMessage;
        }
    }


    // ==================================================
    // PLANES — HIDE
    // ==================================================

    private void HidePlanes()
    {
        if (planeManager == null)
            return;


        foreach (ARPlane plane
                 in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }


        planeManager.enabled = false;
    }


    // ==================================================
    // PLANES — SHOW
    // ==================================================

    private void ShowPlanes()
    {
        if (planeManager == null)
            return;


        planeManager.enabled = true;


        foreach (ARPlane plane
                 in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }
    }
}
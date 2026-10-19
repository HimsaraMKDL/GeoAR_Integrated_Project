using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class C4_ARPlacedModelManager : MonoBehaviour
{
    // =========================================================
    // NORMAL MODE
    // =========================================================

    [Header("Normal Mode")]
    [SerializeField] private GameObject normalMainCamera;
    [SerializeField] private GameObject modelPreviewSystem;


    // =========================================================
    // AR MODE
    // =========================================================

    [Header("AR Mode")]
    [SerializeField] private GameObject arSessionObject;
    [SerializeField] private GameObject xrOriginObject;
    [SerializeField] private GameObject arCameraObject;


    // =========================================================
    // AR MANAGERS
    // =========================================================

    [Header("AR Managers")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARPlaneManager arPlaneManager;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField] private GameObject validationCanvas;
    [SerializeField] private GameObject arPlacementCanvas;


    // =========================================================
    // NAVIGATION
    // =========================================================

    [Header("Navigation")]
    [SerializeField] private GameObject sketchInputCanvas;


    // =========================================================
    // LEARNING
    // =========================================================

    [Header("Learning")]
    [SerializeField] private LearningFlowManager learningFlowManager;


    // =========================================================
    // MODEL
    // =========================================================

    [Header("Model")]
    [SerializeField] private ModelDisplayManager modelDisplayManager;


    // =========================================================
    // INITIAL AR SIZE
    // =========================================================

    [Header("Initial AR Size")]
    [SerializeField] private float arScaleMultiplier = 0.10f;


    // =========================================================
    // SURFACE PLACEMENT
    // =========================================================

    [Header("Surface Placement")]
    [Tooltip("Small offset above detected plane to avoid clipping.")]
    [SerializeField] private float surfaceOffset = 0.002f;


    // =========================================================
    // TOUCH ROTATION
    // =========================================================

    [Header("Touch Rotation")]
    [SerializeField] private float rotationSpeed = 0.20f;


    // =========================================================
    // PINCH ZOOM
    // =========================================================

    [Header("Pinch Zoom")]
    [SerializeField] private float pinchScaleSpeed = 0.005f;

    [Tooltip("Smallest size relative to initial AR size.")]
    [SerializeField] private float minZoomMultiplier = 0.75f;

    [Tooltip("Largest size relative to initial AR size.")]
    [SerializeField] private float maxZoomMultiplier = 3.0f;


    // =========================================================
    // INTERNAL AR REFERENCES
    // =========================================================

    private ARCameraManager arCameraManager;
    private ARCameraBackground arCameraBackground;

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    // Root used for rotate + zoom.
    private GameObject placedModelRoot;

    // Actual cloned 3D model.
    private GameObject placedModelVisual;

    private bool arPlacementMode = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (arCameraObject != null)
        {
            arCameraManager =
                arCameraObject.GetComponent<ARCameraManager>();

            arCameraBackground =
                arCameraObject.GetComponent<ARCameraBackground>();
        }
    }


    private void Start()
    {
        SetNormalMode();
    }


    // =========================================================
    // NORMAL MODE
    // =========================================================

    private void SetNormalMode()
    {
        arPlacementMode = false;


        // Hide AR placement UI.
        if (arPlacementCanvas != null)
        {
            arPlacementCanvas.SetActive(false);
        }


        // Disable plane detection.
        if (arPlaneManager != null)
        {
            arPlaneManager.enabled = false;
        }


        // Hide plane visualization.
        SetPlaneVisualizationVisible(false);


        // Disable AR camera background.
        if (arCameraBackground != null)
        {
            arCameraBackground.enabled = false;
        }


        // Disable AR camera.
        if (arCameraObject != null)
        {
            arCameraObject.tag = "Untagged";
            arCameraObject.SetActive(false);
        }


        // Enable normal camera.
        if (normalMainCamera != null)
        {
            normalMainCamera.SetActive(true);
            normalMainCamera.tag = "MainCamera";
        }


        // Keep preview system available in normal mode.
        if (modelPreviewSystem != null)
        {
            modelPreviewSystem.SetActive(true);
        }


        Debug.Log(
            "Normal learning mode active."
        );
    }


    // =========================================================
    // ENTER AR
    // =========================================================

    public void EnterARPlacement()
    {
        // -----------------------------------------------------
        // CHECK MODEL
        // -----------------------------------------------------

        if (modelDisplayManager == null ||
            modelDisplayManager.CurrentModel == null)
        {
            Debug.LogWarning(
                "No completed 3D model is available for AR."
            );

            return;
        }


        Debug.Log(
            "Entering AR placement mode."
        );


        // =====================================================
        // RESEARCH LOGGING:
        // AR VIEW OPENED
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.LogAROpened();

            Debug.Log(
                "Research logger: AR opened saved."
            );
        }


        arPlacementMode = true;


        // -----------------------------------------------------
        // HIDE NORMAL UI
        // -----------------------------------------------------

        if (validationCanvas != null)
        {
            validationCanvas.SetActive(false);
        }


        if (sketchInputCanvas != null)
        {
            sketchInputCanvas.SetActive(false);
        }


        if (modelPreviewSystem != null)
        {
            modelPreviewSystem.SetActive(false);
        }


        // -----------------------------------------------------
        // DISABLE NORMAL CAMERA
        // -----------------------------------------------------

        if (normalMainCamera != null)
        {
            normalMainCamera.tag = "Untagged";
            normalMainCamera.SetActive(false);
        }


        // -----------------------------------------------------
        // START AR SESSION
        // -----------------------------------------------------

        if (arSessionObject != null)
        {
            arSessionObject.SetActive(true);
        }


        if (xrOriginObject != null)
        {
            xrOriginObject.SetActive(true);
        }


        // -----------------------------------------------------
        // ENABLE AR CAMERA
        // -----------------------------------------------------

        if (arCameraObject != null)
        {
            arCameraObject.SetActive(true);
            arCameraObject.tag = "MainCamera";
        }


        if (arCameraManager != null)
        {
            arCameraManager.enabled = true;
        }


        if (arCameraBackground != null)
        {
            arCameraBackground.enabled = true;
        }


        // -----------------------------------------------------
        // ENABLE HORIZONTAL PLANE DETECTION
        // -----------------------------------------------------

        if (arPlaneManager != null)
        {
            arPlaneManager.requestedDetectionMode =
                PlaneDetectionMode.Horizontal;

            arPlaneManager.enabled = true;
        }


        // Show planes until model is placed.
        SetPlaneVisualizationVisible(true);


        // -----------------------------------------------------
        // SHOW AR UI
        // -----------------------------------------------------

        if (arPlacementCanvas != null)
        {
            arPlacementCanvas.SetActive(true);
        }


        Debug.Log(
            "AR active. Move the phone slowly and detect a horizontal surface."
        );
    }


    // =========================================================
    // TOUCH INPUT
    // =========================================================

    private void Update()
    {
        if (!arPlacementMode)
        {
            return;
        }


        if (Input.touchCount == 0)
        {
            return;
        }


        // =====================================================
        // NO MODEL PLACED YET
        // SINGLE TAP = PLACE MODEL
        // =====================================================

        if (placedModelRoot == null)
        {
            if (Input.touchCount != 1)
            {
                return;
            }


            Touch touch =
                Input.GetTouch(0);


            if (touch.phase != TouchPhase.Began)
            {
                return;
            }


            if (IsTouchOverUI(touch))
            {
                return;
            }


            TryPlaceModel(
                touch.position
            );


            return;
        }


        // =====================================================
        // MODEL EXISTS
        // ONE FINGER = ROTATE
        // =====================================================

        if (Input.touchCount == 1)
        {
            Touch touch =
                Input.GetTouch(0);


            if (IsTouchOverUI(touch))
            {
                return;
            }


            if (touch.phase == TouchPhase.Moved)
            {
                float rotationAmount =
                    -touch.deltaPosition.x *
                    rotationSpeed;


                placedModelRoot.transform.Rotate(
                    Vector3.up,
                    rotationAmount,
                    Space.World
                );
            }


            return;
        }


        // =====================================================
        // TWO FINGERS = PINCH ZOOM
        // =====================================================

        if (Input.touchCount >= 2)
        {
            Touch touch0 =
                Input.GetTouch(0);

            Touch touch1 =
                Input.GetTouch(1);


            if (IsTouchOverUI(touch0) ||
                IsTouchOverUI(touch1))
            {
                return;
            }


            HandlePinchZoom(
                touch0,
                touch1
            );
        }
    }


    // =========================================================
    // UI TOUCH CHECK
    // =========================================================

    private bool IsTouchOverUI(
        Touch touch)
    {
        return
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(
                touch.fingerId
            );
    }


    // =========================================================
    // HORIZONTAL PLANE RAYCAST
    // =========================================================

    private bool TryGetHorizontalPlaneHit(
        Vector2 screenPosition,
        out Pose hitPose)
    {
        hitPose = default;


        if (arRaycastManager == null)
        {
            Debug.LogError(
                "ARRaycastManager is not assigned."
            );

            return false;
        }


        bool foundHit =
            arRaycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon
            );


        if (!foundHit ||
            hits.Count == 0)
        {
            Debug.Log(
                "No detected AR plane at this position."
            );

            return false;
        }


        // -----------------------------------------------------
        // FIND STABLE HORIZONTAL-UP PLANE
        // -----------------------------------------------------

        foreach (ARRaycastHit hit in hits)
        {
            ARPlane plane = null;


            if (arPlaneManager != null)
            {
                plane =
                    arPlaneManager.GetPlane(
                        hit.trackableId
                    );
            }


            if (plane == null)
            {
                continue;
            }


            if (plane.trackingState !=
                TrackingState.Tracking)
            {
                continue;
            }


            if (plane.alignment !=
                PlaneAlignment.HorizontalUp)
            {
                continue;
            }


            hitPose =
                hit.pose;


            return true;
        }


        Debug.Log(
            "A plane was detected, but it is not a stable horizontal-up plane."
        );


        return false;
    }


    // =========================================================
    // PLACE MODEL
    // =========================================================

    private void TryPlaceModel(
        Vector2 screenPosition)
    {
        if (!TryGetHorizontalPlaneHit(
                screenPosition,
                out Pose placementPose))
        {
            return;
        }


        if (modelDisplayManager == null)
        {
            Debug.LogError(
                "ModelDisplayManager is not assigned."
            );

            return;
        }


        GameObject previewModel =
            modelDisplayManager.CurrentModel;


        if (previewModel == null)
        {
            Debug.LogError(
                "Preview model is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // REMOVE OLD PLACEMENT IF ONE EXISTS
        // -----------------------------------------------------

        if (placedModelRoot != null)
        {
            Destroy(
                placedModelRoot
            );

            placedModelRoot = null;
            placedModelVisual = null;
        }


        // -----------------------------------------------------
        // CREATE CLEAN MANIPULATION ROOT
        // -----------------------------------------------------

        placedModelRoot =
            new GameObject(
                "AR_Placed_Model_Root"
            );


        placedModelRoot.transform.position =
            placementPose.position;


        placedModelRoot.transform.rotation =
            Quaternion.Euler(
                0f,
                placementPose.rotation.eulerAngles.y,
                0f
            );


        placedModelRoot.transform.localScale =
            Vector3.one;


        // -----------------------------------------------------
        // CLONE ACTUAL MODEL
        // -----------------------------------------------------

        placedModelVisual =
            Instantiate(
                previewModel
            );


        placedModelVisual.name =
            previewModel.name +
            "_AR";


        placedModelVisual.SetActive(
            true
        );


        // -----------------------------------------------------
        // REMOVE PREVIEW ROTATION SCRIPT
        // -----------------------------------------------------

        ModelRotate[] rotateScripts =
            placedModelVisual
                .GetComponentsInChildren<ModelRotate>(
                    true
                );


        foreach (ModelRotate rotateScript
                 in rotateScripts)
        {
            Destroy(
                rotateScript
            );
        }


        // -----------------------------------------------------
        // APPLY INITIAL AR SIZE
        // -----------------------------------------------------

        placedModelVisual.transform.localScale =
            previewModel.transform.lossyScale *
            arScaleMultiplier;


        placedModelVisual.transform.rotation =
            placedModelRoot.transform.rotation;


        placedModelVisual.transform.position =
            placementPose.position;


        // -----------------------------------------------------
        // ALIGN MODEL BOTTOM TO DETECTED SURFACE
        // -----------------------------------------------------

        if (TryGetModelBounds(
                placedModelVisual,
                out Bounds modelBounds))
        {
            Vector3 correction =
                new Vector3(
                    placementPose.position.x -
                    modelBounds.center.x,

                    (placementPose.position.y +
                     surfaceOffset) -
                    modelBounds.min.y,

                    placementPose.position.z -
                    modelBounds.center.z
                );


            placedModelVisual.transform.position +=
                correction;
        }
        else
        {
            placedModelVisual.transform.position =
                placementPose.position +
                Vector3.up *
                surfaceOffset;
        }


        // -----------------------------------------------------
        // PARENT TO ROOT
        // -----------------------------------------------------

        placedModelVisual.transform.SetParent(
            placedModelRoot.transform,
            true
        );


        // -----------------------------------------------------
        // HIDE PLANE VISUALIZATION
        // -----------------------------------------------------

        SetPlaneVisualizationVisible(
            false
        );


        if (arPlaneManager != null)
        {
            arPlaneManager.enabled =
                false;
        }


        // =====================================================
        // RESEARCH LOGGING:
        // MODEL SUCCESSFULLY PLACED
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.LogARModelPlaced();

            Debug.Log(
                "Research logger: AR model placement saved."
            );
        }


        Debug.Log(
            "AR model placed on horizontal plane. " +
            "Drag with one finger to rotate. " +
            "Pinch with two fingers to resize."
        );
    }


    // =========================================================
    // MODEL BOUNDS
    // =========================================================

    private bool TryGetModelBounds(
        GameObject model,
        out Bounds combinedBounds)
    {
        Renderer[] renderers =
            model.GetComponentsInChildren<Renderer>(
                true
            );


        if (renderers == null ||
            renderers.Length == 0)
        {
            combinedBounds =
                new Bounds();

            return false;
        }


        bool boundsStarted =
            false;


        combinedBounds =
            new Bounds();


        foreach (Renderer renderer
                 in renderers)
        {
            if (!renderer.enabled)
            {
                continue;
            }


            if (!boundsStarted)
            {
                combinedBounds =
                    renderer.bounds;

                boundsStarted =
                    true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    renderer.bounds
                );
            }
        }


        return boundsStarted;
    }


    // =========================================================
    // PINCH ZOOM
    // =========================================================

    private void HandlePinchZoom(
        Touch touch0,
        Touch touch1)
    {
        if (placedModelRoot == null)
        {
            return;
        }


        Vector2 previousPosition0 =
            touch0.position -
            touch0.deltaPosition;


        Vector2 previousPosition1 =
            touch1.position -
            touch1.deltaPosition;


        float previousDistance =
            Vector2.Distance(
                previousPosition0,
                previousPosition1
            );


        float currentDistance =
            Vector2.Distance(
                touch0.position,
                touch1.position
            );


        if (previousDistance <= 0.01f)
        {
            return;
        }


        float distanceDifference =
            currentDistance -
            previousDistance;


        float scaleChange =
            distanceDifference *
            pinchScaleSpeed;


        float currentScale =
            placedModelRoot
                .transform
                .localScale
                .x;


        float targetScale =
            currentScale *
            (1f + scaleChange);


        targetScale =
            Mathf.Clamp(
                targetScale,
                minZoomMultiplier,
                maxZoomMultiplier
            );


        placedModelRoot
            .transform
            .localScale =
            Vector3.one *
            targetScale;
    }


    // =========================================================
    // PLANE VISIBILITY
    // =========================================================

    private void SetPlaneVisualizationVisible(
        bool visible)
    {
        if (arPlaneManager == null)
        {
            return;
        }


        foreach (ARPlane plane
                 in arPlaneManager.trackables)
        {
            Renderer[] renderers =
                plane.GetComponentsInChildren<Renderer>(
                    true
                );


            foreach (Renderer renderer
                     in renderers)
            {
                renderer.enabled =
                    visible;
            }
        }
    }


    // =========================================================
    // CLEAR PLACED MODEL
    // =========================================================

    public void ClearPlacedModel()
    {
        if (placedModelRoot != null)
        {
            Destroy(
                placedModelRoot
            );


            placedModelRoot = null;
            placedModelVisual = null;
        }


        if (arPlacementMode)
        {
            if (arPlaneManager != null)
            {
                arPlaneManager.enabled =
                    true;


                arPlaneManager.requestedDetectionMode =
                    PlaneDetectionMode.Horizontal;
            }


            SetPlaneVisualizationVisible(
                true
            );
        }
    }


    // =========================================================
    // EXIT AR -> RETURN TO FIRST SKETCH PAGE
    // =========================================================

    public void ExitARPlacement()
    {
        Debug.Log(
            "Leaving AR placement mode."
        );


        arPlacementMode =
            false;


        // -----------------------------------------------------
        // DESTROY PLACED AR MODEL
        // -----------------------------------------------------

        if (placedModelRoot != null)
        {
            Destroy(
                placedModelRoot
            );


            placedModelRoot =
                null;

            placedModelVisual =
                null;
        }


        // -----------------------------------------------------
        // HIDE AR PLANES
        // -----------------------------------------------------

        SetPlaneVisualizationVisible(
            false
        );


        if (arPlaneManager != null)
        {
            arPlaneManager.enabled =
                false;
        }


        // -----------------------------------------------------
        // HIDE AR UI
        // -----------------------------------------------------

        if (arPlacementCanvas != null)
        {
            arPlacementCanvas.SetActive(
                false
            );
        }


        // -----------------------------------------------------
        // DISABLE AR CAMERA BACKGROUND
        // -----------------------------------------------------

        if (arCameraBackground != null)
        {
            arCameraBackground.enabled =
                false;
        }


        // -----------------------------------------------------
        // DISABLE AR CAMERA
        // -----------------------------------------------------

        if (arCameraObject != null)
        {
            arCameraObject.tag =
                "Untagged";


            arCameraObject.SetActive(
                false
            );
        }


        // -----------------------------------------------------
        // ENABLE NORMAL CAMERA
        // -----------------------------------------------------

        if (normalMainCamera != null)
        {
            normalMainCamera.SetActive(
                true
            );


            normalMainCamera.tag =
                "MainCamera";
        }


        // -----------------------------------------------------
        // ENABLE NORMAL PREVIEW SYSTEM
        // -----------------------------------------------------

        if (modelPreviewSystem != null)
        {
            modelPreviewSystem.SetActive(
                true
            );
        }


        // -----------------------------------------------------
        // DO NOT RETURN TO CHALLENGE PAGE
        // -----------------------------------------------------

        if (validationCanvas != null)
        {
            validationCanvas.SetActive(
                false
            );
        }


        // -----------------------------------------------------
        // RESET PREVIOUS LEARNING STATE
        // -----------------------------------------------------

        if (learningFlowManager != null)
        {
            learningFlowManager.ResetLearningState();
        }


        // -----------------------------------------------------
        // RETURN TO FIRST SKETCH DRAWING PAGE
        // -----------------------------------------------------

        if (sketchInputCanvas != null)
        {
            sketchInputCanvas.SetActive(
                true
            );
        }


        // =====================================================
        // RESEARCH LOGGING:
        // COMPLETE THIS OBJECT TESTING SESSION
        //
        // Same participant can now draw another object.
        // Next Continue click creates a NEW SessionID.
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.CompleteSession();

            Debug.Log(
                "Research logger: Current object session completed."
            );
        }


        Debug.Log(
            "Returned to Sketch Input screen."
        );
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

public class C2_CuboidSliceTraceDetector : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Required References")]

    [SerializeField]
    private C2_CuboidSlicingManager
        cuboidSlicingManager;

    [SerializeField]
    private Transform sliceStartPoint;

    [SerializeField]
    private Transform sliceEndPoint;


    [Tooltip(
        "Leave empty. Main Camera is found automatically."
    )]

    [SerializeField]
    private Camera viewCamera;


    // =========================================================
    // TRACE SETTINGS
    // =========================================================

    [Header("Trace Settings")]

    [SerializeField]
    private float startTolerancePixels =
        180f;

    [SerializeField]
    private float endTolerancePixels =
        200f;


    [Range(0.2f, 1f)]

    [SerializeField]
    private float minimumDragRatio =
        0.45f;


    [SerializeField]
    private bool allowReverseTrace =
        true;


    // =========================================================
    // TESTING
    // =========================================================

    [Header("Testing")]

    [SerializeField]
    private bool autoPrepareVerticalHalfForTesting =
        false;

    [SerializeField]
    private bool enableMouseTestingInEditor =
        true;

    [SerializeField]
    private bool blockTouchesOverUI =
        true;

    [SerializeField]
    private bool showDebugStatus =
        false;


    // =========================================================
    // INTERNAL TRACE STATE
    // =========================================================

    private bool isTracing =
        false;

    private bool startedFromStartPoint =
        true;

    private Vector2 traceStartInputPosition;


    private string debugStatus =
        "Select Vertical + 1/2.";


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        FindCamera();


        if (
            autoPrepareVerticalHalfForTesting &&
            cuboidSlicingManager != null
        )
        {
            cuboidSlicingManager
                .SelectVertical();

            cuboidSlicingManager
                .SelectHalf();


            debugStatus =
                "Drag GREEN to ORANGE.";


            Debug.Log(
                "C2 Cuboid Trace: " +
                "Vertical + 1/2 prepared."
            );
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (viewCamera == null)
        {
            FindCamera();
        }


        if (cuboidSlicingManager == null ||
            sliceStartPoint == null ||
            sliceEndPoint == null ||
            viewCamera == null)
        {
            return;
        }


        if (!cuboidSlicingManager.CanTrace)
        {
            return;
        }


        // -----------------------------------------------------
        // MOBILE TOUCH INPUT
        // -----------------------------------------------------

        if (Input.touchCount > 0)
        {
            HandleTouchInput();

            return;
        }


        // -----------------------------------------------------
        // UNITY EDITOR MOUSE TESTING
        // -----------------------------------------------------

#if UNITY_EDITOR

        if (enableMouseTestingInEditor)
        {
            HandleMouseInput();
        }

#endif
    }


    // =========================================================
    // CAMERA
    // =========================================================

    private void FindCamera()
    {
        if (viewCamera == null)
        {
            viewCamera =
                Camera.main;
        }
    }


    // =========================================================
    // TOUCH
    // =========================================================

    private void HandleTouchInput()
    {
        Touch touch =
            Input.GetTouch(0);


        switch (touch.phase)
        {
            case TouchPhase.Began:

                if (
                    blockTouchesOverUI &&
                    EventSystem.current != null &&
                    EventSystem.current
                        .IsPointerOverGameObject(
                            touch.fingerId
                        )
                )
                {
                    return;
                }


                TryBeginTrace(
                    touch.position
                );

                break;


            case TouchPhase.Moved:

            case TouchPhase.Stationary:

                if (isTracing)
                {
                    debugStatus =
                        "Tracing...";
                }

                break;


            case TouchPhase.Ended:

                if (isTracing)
                {
                    TryFinishTrace(
                        touch.position
                    );
                }

                break;


            case TouchPhase.Canceled:

                CancelTrace();

                break;
        }
    }


    // =========================================================
    // MOUSE TESTING
    // =========================================================

#if UNITY_EDITOR

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (
                blockTouchesOverUI &&
                EventSystem.current != null &&
                EventSystem.current
                    .IsPointerOverGameObject()
            )
            {
                return;
            }


            TryBeginTrace(
                Input.mousePosition
            );
        }


        if (
            Input.GetMouseButtonUp(0) &&
            isTracing
        )
        {
            TryFinishTrace(
                Input.mousePosition
            );
        }
    }

#endif


    // =========================================================
    // BEGIN TRACE
    // =========================================================

    private void TryBeginTrace(
        Vector2 inputPosition
    )
    {
        if (
            !GetGuideScreenPoints(
                out Vector2 startScreen,
                out Vector2 endScreen
            )
        )
        {
            return;
        }


        float startTolerance =
            GetResponsiveTolerance(
                startTolerancePixels,
                0.10f
            );


        float distanceToStart =
            Vector2.Distance(
                inputPosition,
                startScreen
            );


        float distanceToEnd =
            Vector2.Distance(
                inputPosition,
                endScreen
            );


        if (
            distanceToStart <=
            startTolerance
        )
        {
            isTracing =
                true;

            startedFromStartPoint =
                true;

            traceStartInputPosition =
                inputPosition;


            debugStatus =
                "Started from GREEN.";


            return;
        }


        if (
            allowReverseTrace &&
            distanceToEnd <=
                startTolerance
        )
        {
            isTracing =
                true;

            startedFromStartPoint =
                false;

            traceStartInputPosition =
                inputPosition;


            debugStatus =
                "Started from ORANGE.";


            return;
        }


        debugStatus =
            "Start closer to a marker.";
    }


    // =========================================================
    // FINISH TRACE
    // =========================================================

    private void TryFinishTrace(
        Vector2 inputPosition
    )
    {
        if (
            !GetGuideScreenPoints(
                out Vector2 startScreen,
                out Vector2 endScreen
            )
        )
        {
            CancelTrace();

            return;
        }


        Vector2 targetPoint =
            startedFromStartPoint
                ? endScreen
                : startScreen;


        float endTolerance =
            GetResponsiveTolerance(
                endTolerancePixels,
                0.11f
            );


        float distanceToTarget =
            Vector2.Distance(
                inputPosition,
                targetPoint
            );


        float guideLength =
            Vector2.Distance(
                startScreen,
                endScreen
            );


        float dragDistance =
            Vector2.Distance(
                traceStartInputPosition,
                inputPosition
            );


        float minimumRequiredDistance =
            guideLength *
            minimumDragRatio;


        bool reachedTarget =
            distanceToTarget <=
            endTolerance;


        bool draggedEnough =
            dragDistance >=
            minimumRequiredDistance;


        isTracing =
            false;


        if (
            reachedTarget &&
            draggedEnough
        )
        {
            debugStatus =
                "SUCCESS!";


            Debug.Log(
                "C2 Cuboid Trace: SUCCESS."
            );


            cuboidSlicingManager
                .CompleteTracingAndSlice();


            return;
        }


        debugStatus =
            "Incomplete trace. Try again.";
    }


    // =========================================================
    // WORLD TO SCREEN
    // =========================================================

    private bool GetGuideScreenPoints(
        out Vector2 startScreen,
        out Vector2 endScreen
    )
    {
        startScreen =
            Vector2.zero;

        endScreen =
            Vector2.zero;


        Vector3 start3D =
            viewCamera.WorldToScreenPoint(
                sliceStartPoint.position
            );


        Vector3 end3D =
            viewCamera.WorldToScreenPoint(
                sliceEndPoint.position
            );


        if (
            start3D.z <= 0f ||
            end3D.z <= 0f
        )
        {
            return false;
        }


        startScreen =
            new Vector2(
                start3D.x,
                start3D.y
            );


        endScreen =
            new Vector2(
                end3D.x,
                end3D.y
            );


        return true;
    }


    // =========================================================
    // RESPONSIVE TOLERANCE
    // =========================================================

    private float GetResponsiveTolerance(
        float minimumPixels,
        float screenFraction
    )
    {
        float shortSide =
            Mathf.Min(
                Screen.width,
                Screen.height
            );


        float responsiveValue =
            shortSide *
            screenFraction;


        return Mathf.Max(
            minimumPixels,
            responsiveValue
        );
    }


    // =========================================================
    // CANCEL
    // =========================================================

    private void CancelTrace()
    {
        isTracing =
            false;


        debugStatus =
            "Trace cancelled.";
    }


    // =========================================================
    // OPTIONAL DEBUG
    // =========================================================

    private void OnGUI()
    {
        if (!showDebugStatus)
        {
            return;
        }


        GUIStyle style =
            new GUIStyle(
                GUI.skin.box
            );


        style.fontSize =
            24;

        style.alignment =
            TextAnchor.MiddleCenter;

        style.wordWrap =
            true;


        GUI.Box(
            new Rect(
                20f,
                20f,
                Screen.width - 40f,
                90f
            ),
            debugStatus,
            style
        );
    }
}
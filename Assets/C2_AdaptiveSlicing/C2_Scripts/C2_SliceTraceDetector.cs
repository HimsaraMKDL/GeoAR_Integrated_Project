using UnityEngine;
using UnityEngine.EventSystems;

public class C2_SliceTraceDetector : MonoBehaviour
{
    // ---------------------------------------------------------
    // REFERENCES
    // ---------------------------------------------------------

    [Header("Required References")]
    [SerializeField] private C2_CubeSlicingManager cubeSlicingManager;
    [SerializeField] private Transform sliceStartPoint;
    [SerializeField] private Transform sliceEndPoint;

    [Tooltip("Leave empty. Main Camera is found automatically.")]
    [SerializeField] private Camera arCamera;


    // ---------------------------------------------------------
    // TRACE SETTINGS
    // ---------------------------------------------------------

    [Header("Stage 10 Trace Settings")]

    [Tooltip("How close the finger must begin to a marker.")]
    [SerializeField] private float startTolerancePixels = 180f;

    [Tooltip("How close the finger must finish to the opposite marker.")]
    [SerializeField] private float endTolerancePixels = 200f;

    [Tooltip("Minimum drag distance compared with guide length.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float minimumDragRatio = 0.45f;

    [SerializeField] private bool allowReverseTrace = true;


    // ---------------------------------------------------------
    // TEMPORARY TEST SETTINGS
    // ---------------------------------------------------------

    [Header("Stage 10 Testing")]

    [Tooltip("Automatically prepares Vertical + 1/2 for Stage 10.")]
    [SerializeField] private bool autoPrepareVerticalHalfForTesting = true;

    [Tooltip(
        "Keep OFF during Stage 10 testing. " +
        "We will enable UI blocking after the final Canvas is created."
    )]
    [SerializeField] private bool blockTouchesOverUI = false;

    [Tooltip("Shows tracing status directly on the phone screen.")]
    [SerializeField] private bool showDebugStatus = true;


    // ---------------------------------------------------------
    // INTERNAL VALUES
    // ---------------------------------------------------------

    private bool isTracing = false;

    private bool startedFromStartPoint = true;

    private Vector2 traceStartTouchPosition;

    private string debugStatus =
        "Touch GREEN point and drag to ORANGE.";


    // ---------------------------------------------------------
    // START
    // ---------------------------------------------------------

    private void Start()
    {
        FindARCamera();

        if (autoPrepareVerticalHalfForTesting &&
            cubeSlicingManager != null)
        {
            cubeSlicingManager.SelectVertical();
            cubeSlicingManager.SelectHalf();

            debugStatus =
                "Touch GREEN point and drag to ORANGE.";

            Debug.Log(
                "C2 Trace Test: Vertical + 1/2 prepared."
            );
        }
    }


    // ---------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------

    private void Update()
    {
        if (arCamera == null)
        {
            FindARCamera();
        }


        if (cubeSlicingManager == null)
        {
            debugStatus =
                "ERROR: Cube Slicing Manager missing.";

            return;
        }


        if (sliceStartPoint == null ||
            sliceEndPoint == null)
        {
            debugStatus =
                "ERROR: Start or End Point missing.";

            return;
        }


        if (arCamera == null)
        {
            debugStatus =
                "ERROR: Main Camera not found.";

            return;
        }


        if (!cubeSlicingManager.CanTrace)
        {
            return;
        }


        if (Input.touchCount == 0)
        {
            return;
        }


        Touch touch =
            Input.GetTouch(0);


        switch (touch.phase)
        {
            case TouchPhase.Began:

                TryBeginTrace(
                    touch.position,
                    touch.fingerId
                );

                break;


            case TouchPhase.Moved:

            case TouchPhase.Stationary:

                if (isTracing)
                {
                    debugStatus =
                        "Tracing... move to opposite point.";
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


    // ---------------------------------------------------------
    // FIND CAMERA
    // ---------------------------------------------------------

    private void FindARCamera()
    {
        if (arCamera == null)
        {
            arCamera =
                Camera.main;
        }
    }


    // ---------------------------------------------------------
    // BEGIN TRACE
    // ---------------------------------------------------------

    private void TryBeginTrace(
        Vector2 touchPosition,
        int fingerId
    )
    {
        if (blockTouchesOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(fingerId))
        {
            debugStatus =
                "Touch started on UI.";

            return;
        }


        if (!GetGuideScreenPoints(
                out Vector2 startScreen,
                out Vector2 endScreen))
        {
            debugStatus =
                "Guide is not visible to camera.";

            return;
        }


        float startTolerance =
            GetResponsiveTolerance(
                startTolerancePixels,
                0.10f
            );


        float distanceToStart =
            Vector2.Distance(
                touchPosition,
                startScreen
            );


        float distanceToEnd =
            Vector2.Distance(
                touchPosition,
                endScreen
            );


        // ---------------------------------------------
        // GREEN → ORANGE
        // ---------------------------------------------

        if (distanceToStart <=
            startTolerance)
        {
            isTracing =
                true;

            startedFromStartPoint =
                true;

            traceStartTouchPosition =
                touchPosition;

            debugStatus =
                "Tracing started from GREEN.";

            Debug.Log(
                "C2 Trace: Started from GREEN."
            );

            return;
        }


        // ---------------------------------------------
        // ORANGE → GREEN
        // ---------------------------------------------

        if (allowReverseTrace &&
            distanceToEnd <=
            startTolerance)
        {
            isTracing =
                true;

            startedFromStartPoint =
                false;

            traceStartTouchPosition =
                touchPosition;

            debugStatus =
                "Tracing started from ORANGE.";

            Debug.Log(
                "C2 Trace: Started from ORANGE."
            );

            return;
        }


        debugStatus =
            "Start closer to GREEN or ORANGE point.";

        Debug.Log(
            "C2 Trace: Touch was not close enough to a marker."
        );
    }


    // ---------------------------------------------------------
    // FINISH TRACE
    // ---------------------------------------------------------

    private void TryFinishTrace(
        Vector2 touchPosition
    )
    {
        if (!GetGuideScreenPoints(
                out Vector2 startScreen,
                out Vector2 endScreen))
        {
            CancelTrace();

            return;
        }


        Vector2 targetPoint;

        if (startedFromStartPoint)
        {
            targetPoint =
                endScreen;
        }
        else
        {
            targetPoint =
                startScreen;
        }


        float endTolerance =
            GetResponsiveTolerance(
                endTolerancePixels,
                0.11f
            );


        float distanceToTarget =
            Vector2.Distance(
                touchPosition,
                targetPoint
            );


        float guideLength =
            Vector2.Distance(
                startScreen,
                endScreen
            );


        float actualDragDistance =
            Vector2.Distance(
                traceStartTouchPosition,
                touchPosition
            );


        float minimumRequiredDistance =
            guideLength *
            minimumDragRatio;


        bool reachedTarget =
            distanceToTarget <=
            endTolerance;


        bool draggedEnough =
            actualDragDistance >=
            minimumRequiredDistance;


        isTracing =
            false;


        // ---------------------------------------------
        // SUCCESS
        // ---------------------------------------------

        if (reachedTarget &&
            draggedEnough)
        {
            debugStatus =
                "SUCCESS! Trace completed.";

            Debug.Log(
                "C2 Trace: SUCCESS."
            );


            cubeSlicingManager
                .CompleteTracingAndSlice();

            return;
        }


        // ---------------------------------------------
        // FAILED
        // ---------------------------------------------

        if (!reachedTarget)
        {
            debugStatus =
                "Release closer to opposite point.";
        }
        else
        {
            debugStatus =
                "Drag farther along the guide.";
        }


        Debug.Log(
            "C2 Trace: Incomplete. Try again."
        );
    }


    // ---------------------------------------------------------
    // CONVERT 3D GUIDE TO PHONE SCREEN
    // ---------------------------------------------------------

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
            arCamera.WorldToScreenPoint(
                sliceStartPoint.position
            );


        Vector3 end3D =
            arCamera.WorldToScreenPoint(
                sliceEndPoint.position
            );


        if (start3D.z <= 0f ||
            end3D.z <= 0f)
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


    // ---------------------------------------------------------
    // RESPONSIVE TOLERANCE
    // ---------------------------------------------------------

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


    // ---------------------------------------------------------
    // CANCEL TRACE
    // ---------------------------------------------------------

    private void CancelTrace()
    {
        isTracing =
            false;

        debugStatus =
            "Trace cancelled. Try again.";
    }


    // ---------------------------------------------------------
    // TEMPORARY PHONE DEBUG MESSAGE
    // ---------------------------------------------------------

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
            Mathf.RoundToInt(
                Mathf.Clamp(
                    Screen.width * 0.045f,
                    24f,
                    42f
                )
            );


        style.alignment =
            TextAnchor.MiddleCenter;

        style.wordWrap =
            true;


        GUI.Box(
            new Rect(
                20f,
                25f,
                Screen.width - 40f,
                120f
            ),
            debugStatus,
            style
        );
    }
}
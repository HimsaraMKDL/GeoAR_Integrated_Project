using System.Collections.Generic;
using UnityEngine;

public class C2_TetrahedronSlicingManager : MonoBehaviour
{
    // =========================================================
    // OPTIONS
    // =========================================================

    public enum C2_TetrahedronSlicingDirection
    {
        None,
        Vertical,
        Horizontal,
        Angled
    }


    public enum C2_TetrahedronSliceFraction
    {
        None,
        OneHalf,
        OneThird,
        OneQuarter,
        ThreeQuarter
    }


    // =========================================================
    // TETRAHEDRON DIMENSIONS
    // =========================================================

    // Full Tetrahedron:
    //
    // Side Length = 0.28
    // Half Width  = 0.14
    // Height      = 0.228619043
    //
    // Guide is positioned slightly toward the camera.

    private const float HalfWidth =
        0.14f;


    private const float TetrahedronHeight =
        0.228619043f;


    private const float GuideZ =
        -0.095f;


    // =========================================================
    // MAIN REFERENCES
    // =========================================================

    [Header("Tetrahedron References")]

    [SerializeField]
    private GameObject fullTetrahedron;


    [SerializeField]
    private GameObject sliceGuide;


    [SerializeField]
    private GameObject tetrahedronSliceResults;


    // =========================================================
    // GUIDE REFERENCES
    // =========================================================

    [Header("Slice Guide References")]

    [SerializeField]
    private Transform sliceStartPoint;


    [SerializeField]
    private Transform sliceEndPoint;


    [SerializeField]
    private LineRenderer sliceGuideLine;


    // =========================================================
    // VERTICAL RESULTS
    // =========================================================

    [Header("Vertical 1/2 Result")]

    [SerializeField]
    private GameObject verticalHalfResult;


    [SerializeField]
    private Rigidbody verticalHalfLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalHalfRightRigidbody;


    [Header("Vertical 1/3 Result")]

    [SerializeField]
    private GameObject verticalThirdResult;


    [SerializeField]
    private Rigidbody verticalThirdLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalThirdRightRigidbody;


    [Header("Vertical 1/4 Result")]

    [SerializeField]
    private GameObject verticalQuarterResult;


    [SerializeField]
    private Rigidbody verticalQuarterLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalQuarterRightRigidbody;


    [Header("Vertical 3/4 Result")]

    [SerializeField]
    private GameObject verticalThreeQuarterResult;


    [SerializeField]
    private Rigidbody verticalThreeQuarterLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalThreeQuarterRightRigidbody;


    // =========================================================
    // HORIZONTAL RESULTS
    // =========================================================

    [Header("Horizontal 1/2 Result")]

    [SerializeField]
    private GameObject horizontalHalfResult;


    [SerializeField]
    private Rigidbody horizontalHalfBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalHalfTopRigidbody;


    [Header("Horizontal 1/3 Result")]

    [SerializeField]
    private GameObject horizontalThirdResult;


    [SerializeField]
    private Rigidbody horizontalThirdBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalThirdTopRigidbody;


    [Header("Horizontal 1/4 Result")]

    [SerializeField]
    private GameObject horizontalQuarterResult;


    [SerializeField]
    private Rigidbody horizontalQuarterBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalQuarterTopRigidbody;


    [Header("Horizontal 3/4 Result")]

    [SerializeField]
    private GameObject horizontalThreeQuarterResult;


    [SerializeField]
    private Rigidbody horizontalThreeQuarterBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalThreeQuarterTopRigidbody;


    // =========================================================
    // ANGLED RESULTS
    // =========================================================

    [Header("Angled 1/2 Result")]

    [SerializeField]
    private GameObject angledHalfResult;


    [SerializeField]
    private Rigidbody angledHalfPieceA;


    [SerializeField]
    private Rigidbody angledHalfPieceB;


    [Header("Angled 1/3 Result")]

    [SerializeField]
    private GameObject angledThirdResult;


    [SerializeField]
    private Rigidbody angledThirdPieceA;


    [SerializeField]
    private Rigidbody angledThirdPieceB;


    [Header("Angled 1/4 Result")]

    [SerializeField]
    private GameObject angledQuarterResult;


    [SerializeField]
    private Rigidbody angledQuarterPieceA;


    [SerializeField]
    private Rigidbody angledQuarterPieceB;


    [Header("Angled 3/4 Result")]

    [SerializeField]
    private GameObject angledThreeQuarterResult;


    [SerializeField]
    private Rigidbody angledThreeQuarterPieceA;


    [SerializeField]
    private Rigidbody angledThreeQuarterPieceB;


    // =========================================================
    // PHYSICS
    // =========================================================

    [Header("Slice Physics Settings")]

    [SerializeField]
    private float separationImpulse =
        0.25f;


    [SerializeField]
    private float upwardImpulse =
        0.30f;


    [SerializeField]
    private float torqueImpulse =
        0f;


    // =========================================================
    // CURRENT STATE
    // =========================================================

    [Header("Current Selection - Read Only During Play")]

    [SerializeField]
    private C2_TetrahedronSlicingDirection
        selectedDirection =
            C2_TetrahedronSlicingDirection.None;


    [SerializeField]
    private C2_TetrahedronSliceFraction
        selectedFraction =
            C2_TetrahedronSliceFraction.None;


    [Header("Trace State - Read Only During Play")]

    [SerializeField]
    private bool traceCompleted =
        false;


    // =========================================================
    // SHARED MAIN APP UI
    // =========================================================

    private C2_AppUIManager uiManager;


    // =========================================================
    // INITIAL PHYSICS STATE
    // =========================================================

    private struct C2_TetrahedronPieceInitialState
    {
        public Vector3 localPosition;

        public Quaternion localRotation;


        public C2_TetrahedronPieceInitialState(
            Vector3 position,
            Quaternion rotation
        )
        {
            localPosition =
                position;


            localRotation =
                rotation;
        }
    }


    private readonly
        Dictionary<
            Rigidbody,
            C2_TetrahedronPieceInitialState
        >
        initialPieceStates =
            new Dictionary<
                Rigidbody,
                C2_TetrahedronPieceInitialState
            >();


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public C2_TetrahedronSlicingDirection
        SelectedDirection
            => selectedDirection;


    public C2_TetrahedronSliceFraction
        SelectedFraction
            => selectedFraction;


    public bool TraceCompleted
        => traceCompleted;


    public bool HasCompleteSelection
    {
        get
        {
            return
                selectedDirection !=
                    C2_TetrahedronSlicingDirection.None &&

                selectedFraction !=
                    C2_TetrahedronSliceFraction.None;
        }
    }


    public bool CanTrace
    {
        get
        {
            if (traceCompleted)
            {
                return false;
            }


            if (
                !IsImplementedFraction(
                    selectedFraction
                )
            )
            {
                return false;
            }


            return
                selectedDirection ==
                    C2_TetrahedronSlicingDirection.Vertical ||

                selectedDirection ==
                    C2_TetrahedronSlicingDirection.Horizontal ||

                selectedDirection ==
                    C2_TetrahedronSlicingDirection.Angled;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CaptureAllInitialPieceStates();

        PrepareInitialState();
    }


    // =========================================================
    // SHARED UI INJECTION
    // =========================================================

    public void SetUIManager(
        C2_AppUIManager manager
    )
    {
        uiManager =
            manager;
    }


    // =========================================================
    // IMPLEMENTED FRACTIONS
    // =========================================================

    private bool IsImplementedFraction(
        C2_TetrahedronSliceFraction fraction
    )
    {
        return
            fraction ==
                C2_TetrahedronSliceFraction.OneHalf ||

            fraction ==
                C2_TetrahedronSliceFraction.OneThird ||

            fraction ==
                C2_TetrahedronSliceFraction.OneQuarter ||

            fraction ==
                C2_TetrahedronSliceFraction.ThreeQuarter;
    }


    // =========================================================
    // CAPTURE ALL INITIAL PIECE STATES
    // =========================================================

    private void CaptureAllInitialPieceStates()
    {
        initialPieceStates.Clear();


        // -----------------------------------------------------
        // VERTICAL
        // -----------------------------------------------------

        CapturePiece(
            verticalHalfLeftRigidbody
        );

        CapturePiece(
            verticalHalfRightRigidbody
        );


        CapturePiece(
            verticalThirdLeftRigidbody
        );

        CapturePiece(
            verticalThirdRightRigidbody
        );


        CapturePiece(
            verticalQuarterLeftRigidbody
        );

        CapturePiece(
            verticalQuarterRightRigidbody
        );


        CapturePiece(
            verticalThreeQuarterLeftRigidbody
        );

        CapturePiece(
            verticalThreeQuarterRightRigidbody
        );


        // -----------------------------------------------------
        // HORIZONTAL
        // -----------------------------------------------------

        CapturePiece(
            horizontalHalfBottomRigidbody
        );

        CapturePiece(
            horizontalHalfTopRigidbody
        );


        CapturePiece(
            horizontalThirdBottomRigidbody
        );

        CapturePiece(
            horizontalThirdTopRigidbody
        );


        CapturePiece(
            horizontalQuarterBottomRigidbody
        );

        CapturePiece(
            horizontalQuarterTopRigidbody
        );


        CapturePiece(
            horizontalThreeQuarterBottomRigidbody
        );

        CapturePiece(
            horizontalThreeQuarterTopRigidbody
        );


        // -----------------------------------------------------
        // ANGLED
        // -----------------------------------------------------

        CapturePiece(
            angledHalfPieceA
        );

        CapturePiece(
            angledHalfPieceB
        );


        CapturePiece(
            angledThirdPieceA
        );

        CapturePiece(
            angledThirdPieceB
        );


        CapturePiece(
            angledQuarterPieceA
        );

        CapturePiece(
            angledQuarterPieceB
        );


        CapturePiece(
            angledThreeQuarterPieceA
        );

        CapturePiece(
            angledThreeQuarterPieceB
        );
    }


    // =========================================================
    // CAPTURE ONE PIECE
    // =========================================================

    private void CapturePiece(
        Rigidbody piece
    )
    {
        if (piece == null)
        {
            return;
        }


        if (
            initialPieceStates.ContainsKey(
                piece
            )
        )
        {
            return;
        }


        initialPieceStates.Add(
            piece,
            new C2_TetrahedronPieceInitialState(
                piece.transform.localPosition,
                piece.transform.localRotation
            )
        );
    }


    // =========================================================
    // DIRECTION — VERTICAL
    // =========================================================

    public void SelectVertical()
    {
        selectedDirection =
            C2_TetrahedronSlicingDirection.Vertical;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: Vertical selected."
        );
    }


    // =========================================================
    // DIRECTION — HORIZONTAL
    // =========================================================

    public void SelectHorizontal()
    {
        selectedDirection =
            C2_TetrahedronSlicingDirection.Horizontal;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: Horizontal selected."
        );
    }


    // =========================================================
    // DIRECTION — ANGLED
    // =========================================================

    public void SelectAngled()
    {
        selectedDirection =
            C2_TetrahedronSlicingDirection.Angled;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: Angled selected."
        );
    }


    // =========================================================
    // FRACTION — 1/2
    // =========================================================

    public void SelectHalf()
    {
        selectedFraction =
            C2_TetrahedronSliceFraction.OneHalf;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: 1/2 selected."
        );
    }


    // =========================================================
    // FRACTION — 1/3
    // =========================================================

    public void SelectThird()
    {
        selectedFraction =
            C2_TetrahedronSliceFraction.OneThird;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: 1/3 selected."
        );
    }


    // =========================================================
    // FRACTION — 1/4
    // =========================================================

    public void SelectQuarter()
    {
        selectedFraction =
            C2_TetrahedronSliceFraction.OneQuarter;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: 1/4 selected."
        );
    }


    // =========================================================
    // FRACTION — 3/4
    // =========================================================

    public void SelectThreeQuarter()
    {
        selectedFraction =
            C2_TetrahedronSliceFraction.ThreeQuarter;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Tetrahedron: 3/4 selected."
        );
    }


    // =========================================================
    // EVALUATE CURRENT SELECTION
    // =========================================================

    private void EvaluateSelection()
    {
        HideGuide();


        if (!HasCompleteSelection)
        {
            return;
        }


        if (
            !IsImplementedFraction(
                selectedFraction
            )
        )
        {
            return;
        }


        switch (selectedDirection)
        {
            case
                C2_TetrahedronSlicingDirection.Vertical:

                ConfigureSelectedVerticalGuide();

                ShowGuide();

                break;


            case
                C2_TetrahedronSlicingDirection.Horizontal:

                ConfigureSelectedHorizontalGuide();

                ShowGuide();

                break;


            case
                C2_TetrahedronSlicingDirection.Angled:

                ConfigureSelectedAngledGuide();

                ShowGuide();

                break;
        }
    }


    // =========================================================
    // VERTICAL GUIDE
    // =========================================================

    private void ConfigureSelectedVerticalGuide()
    {
        float cutX =
            GetVerticalCutOffset();


        float topY =
            CalculateTriangleTopY(
                cutX
            );


        SetGuidePositions(
            new Vector3(
                cutX,
                0f,
                GuideZ
            ),

            new Vector3(
                cutX,
                topY,
                GuideZ
            )
        );
    }


    // =========================================================
    // VERTICAL FRACTION OFFSETS
    // =========================================================

    private float GetVerticalCutOffset()
    {
        switch (selectedFraction)
        {
            case
                C2_TetrahedronSliceFraction.OneHalf:

                return
                    0f;


            case
                C2_TetrahedronSliceFraction.OneThird:

                return
                    -0.017699f;


            case
                C2_TetrahedronSliceFraction.OneQuarter:

                return
                    -0.028882f;


            case
                C2_TetrahedronSliceFraction.ThreeQuarter:

                return
                    0.028882f;


            default:

                return
                    0f;
        }
    }


    // =========================================================
    // TRIANGLE TOP HEIGHT AT X
    // =========================================================

    private float CalculateTriangleTopY(
        float x
    )
    {
        return
            TetrahedronHeight *
            (
                1f -
                Mathf.Abs(x) /
                HalfWidth
            );
    }


    // =========================================================
    // HORIZONTAL GUIDE
    // =========================================================

    private void ConfigureSelectedHorizontalGuide()
    {
        float cutY =
            GetHorizontalCutOffset();


        float halfWidthAtY =
            CalculateHalfWidthAtY(
                cutY
            );


        SetGuidePositions(
            new Vector3(
                -halfWidthAtY,
                cutY,
                GuideZ
            ),

            new Vector3(
                halfWidthAtY,
                cutY,
                GuideZ
            )
        );
    }


    // =========================================================
    // HORIZONTAL FRACTION OFFSETS
    // =========================================================

    private float GetHorizontalCutOffset()
    {
        switch (selectedFraction)
        {
            case
                C2_TetrahedronSliceFraction.OneHalf:

                return
                    0.047164f;


            case
                C2_TetrahedronSliceFraction.OneThird:

                return
                    0.028902f;


            case
                C2_TetrahedronSliceFraction.OneQuarter:

                return
                    0.020905f;


            case
                C2_TetrahedronSliceFraction.ThreeQuarter:

                return
                    0.084598f;


            default:

                return
                    0f;
        }
    }


    // =========================================================
    // TRIANGLE WIDTH AT Y
    // =========================================================

    private float CalculateHalfWidthAtY(
        float y
    )
    {
        float normalizedHeight =
            Mathf.Clamp01(
                y /
                TetrahedronHeight
            );


        return
            HalfWidth *
            (
                1f -
                normalizedHeight
            );
    }


    // =========================================================
    // ANGLED GUIDE
    // =========================================================

    private void ConfigureSelectedAngledGuide()
    {
        Vector2 planeNormal2D =
            new Vector2(
                -1f,
                1f
            ).normalized;


        float planeOffset =
            GetAngledCutOffset();


        List<Vector2> intersections =
            FindTriangleLineIntersections(
                planeNormal2D,
                planeOffset
            );


        if (
            intersections.Count !=
            2
        )
        {
            Debug.LogError(
                "C2 Tetrahedron: Could not calculate " +
                "exactly two Angled guide endpoints."
            );


            return;
        }


        intersections.Sort(
            (first, second) =>
            {
                int yComparison =
                    first.y.CompareTo(
                        second.y
                    );


                if (yComparison != 0)
                {
                    return
                        yComparison;
                }


                return
                    first.x.CompareTo(
                        second.x
                    );
            }
        );


        Vector2 start2D =
            intersections[0];


        Vector2 end2D =
            intersections[1];


        SetGuidePositions(
            new Vector3(
                start2D.x,
                start2D.y,
                GuideZ
            ),

            new Vector3(
                end2D.x,
                end2D.y,
                GuideZ
            )
        );
    }


    // =========================================================
    // ANGLED FRACTION OFFSETS
    // =========================================================

    private float GetAngledCutOffset()
    {
        switch (selectedFraction)
        {
            case
                C2_TetrahedronSliceFraction.OneHalf:

                return
                    0.041774f;


            case
                C2_TetrahedronSliceFraction.OneThird:

                return
                    0.021129f;


            case
                C2_TetrahedronSliceFraction.OneQuarter:

                return
                    0.009583f;


            case
                C2_TetrahedronSliceFraction.ThreeQuarter:

                return
                    0.072948f;


            default:

                return
                    0f;
        }
    }


    // =========================================================
    // FIND ANGLED LINE / FRONT TRIANGLE INTERSECTIONS
    // =========================================================

    private List<Vector2>
        FindTriangleLineIntersections(
            Vector2 planeNormal,
            float planeOffset
        )
    {
        List<Vector2> intersections =
            new List<Vector2>();


        Vector2 baseLeft =
            new Vector2(
                -HalfWidth,
                0f
            );


        Vector2 baseRight =
            new Vector2(
                HalfWidth,
                0f
            );


        Vector2 apex =
            new Vector2(
                0f,
                TetrahedronHeight
            );


        AddLineEdgeIntersection(
            baseLeft,
            baseRight,
            planeNormal,
            planeOffset,
            intersections
        );


        AddLineEdgeIntersection(
            baseRight,
            apex,
            planeNormal,
            planeOffset,
            intersections
        );


        AddLineEdgeIntersection(
            apex,
            baseLeft,
            planeNormal,
            planeOffset,
            intersections
        );


        return
            intersections;
    }


    // =========================================================
    // ADD ANGLED LINE / EDGE INTERSECTION
    // =========================================================

    private void AddLineEdgeIntersection(
        Vector2 a,
        Vector2 b,
        Vector2 planeNormal,
        float planeOffset,
        List<Vector2> results
    )
    {
        const float epsilon =
            0.000001f;


        float valueA =
            Vector2.Dot(
                planeNormal,
                a
            ) -
            planeOffset;


        float valueB =
            Vector2.Dot(
                planeNormal,
                b
            ) -
            planeOffset;


        if (
            Mathf.Abs(
                valueA
            ) <=
            epsilon
        )
        {
            AddUniquePoint(
                results,
                a
            );
        }


        if (
            Mathf.Abs(
                valueB
            ) <=
            epsilon
        )
        {
            AddUniquePoint(
                results,
                b
            );
        }


        if (
            valueA *
            valueB <
            0f
        )
        {
            float interpolation =
                valueA /
                (
                    valueA -
                    valueB
                );


            Vector2 intersection =
                Vector2.Lerp(
                    a,
                    b,
                    interpolation
                );


            AddUniquePoint(
                results,
                intersection
            );
        }
    }


    // =========================================================
    // PREVENT DUPLICATE GUIDE POINTS
    // =========================================================

    private void AddUniquePoint(
        List<Vector2> points,
        Vector2 newPoint
    )
    {
        const float tolerance =
            0.00001f;


        foreach (
            Vector2 point in
            points
        )
        {
            if (
                Vector2.Distance(
                    point,
                    newPoint
                ) <=
                tolerance
            )
            {
                return;
            }
        }


        points.Add(
            newPoint
        );
    }


    // =========================================================
    // SET GUIDE POSITIONS
    // =========================================================

    private void SetGuidePositions(
        Vector3 startPosition,
        Vector3 endPosition
    )
    {
        if (sliceStartPoint != null)
        {
            sliceStartPoint.localPosition =
                startPosition;
        }


        if (sliceEndPoint != null)
        {
            sliceEndPoint.localPosition =
                endPosition;
        }


        if (sliceGuideLine != null)
        {
            sliceGuideLine.useWorldSpace =
                false;


            sliceGuideLine.positionCount =
                2;


            sliceGuideLine.SetPosition(
                0,
                startPosition
            );


            sliceGuideLine.SetPosition(
                1,
                endPosition
            );
        }
    }


    // =========================================================
    // SHOW GUIDE
    // =========================================================

    private void ShowGuide()
    {
        if (sliceGuide != null)
        {
            sliceGuide.SetActive(
                true
            );
        }
    }


    // =========================================================
    // HIDE GUIDE
    // =========================================================

    private void HideGuide()
    {
        if (sliceGuide != null)
        {
            sliceGuide.SetActive(
                false
            );
        }
    }


    // =========================================================
    // SUCCESSFUL TRACE
    // =========================================================

    public void CompleteTracingAndSlice()
    {
        if (!CanTrace)
        {
            return;
        }


        switch (selectedDirection)
        {
            case
                C2_TetrahedronSlicingDirection.Vertical:

                CompleteVerticalSlice();

                break;


            case
                C2_TetrahedronSlicingDirection.Horizontal:

                CompleteHorizontalSlice();

                break;


            case
                C2_TetrahedronSlicingDirection.Angled:

                CompleteAngledSlice();

                break;
        }
    }


    // =========================================================
    // VERTICAL RESULTS
    // =========================================================

    private void CompleteVerticalSlice()
    {
        switch (selectedFraction)
        {
            case
                C2_TetrahedronSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    verticalHalfResult,
                    verticalHalfLeftRigidbody,
                    verticalHalfRightRigidbody
                );

                break;


            case
                C2_TetrahedronSliceFraction.OneThird:

                PerformPhysicalSlice(
                    verticalThirdResult,
                    verticalThirdLeftRigidbody,
                    verticalThirdRightRigidbody
                );

                break;


            case
                C2_TetrahedronSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    verticalQuarterResult,
                    verticalQuarterLeftRigidbody,
                    verticalQuarterRightRigidbody
                );

                break;


            case
                C2_TetrahedronSliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    verticalThreeQuarterResult,
                    verticalThreeQuarterLeftRigidbody,
                    verticalThreeQuarterRightRigidbody
                );

                break;
        }
    }


    // =========================================================
    // HORIZONTAL RESULTS
    // =========================================================

    private void CompleteHorizontalSlice()
    {
        switch (selectedFraction)
        {
            case
                C2_TetrahedronSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    horizontalHalfResult,
                    horizontalHalfBottomRigidbody,
                    horizontalHalfTopRigidbody
                );

                break;


            case
                C2_TetrahedronSliceFraction.OneThird:

                PerformPhysicalSlice(
                    horizontalThirdResult,
                    horizontalThirdBottomRigidbody,
                    horizontalThirdTopRigidbody
                );

                break;


            case
                C2_TetrahedronSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    horizontalQuarterResult,
                    horizontalQuarterBottomRigidbody,
                    horizontalQuarterTopRigidbody
                );

                break;


            case
                C2_TetrahedronSliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    horizontalThreeQuarterResult,
                    horizontalThreeQuarterBottomRigidbody,
                    horizontalThreeQuarterTopRigidbody
                );

                break;
        }
    }


    // =========================================================
    // ANGLED RESULTS
    // =========================================================

    private void CompleteAngledSlice()
    {
        switch (selectedFraction)
        {
            case
                C2_TetrahedronSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    angledHalfResult,
                    angledHalfPieceA,
                    angledHalfPieceB
                );

                break;


            case
                C2_TetrahedronSliceFraction.OneThird:

                PerformPhysicalSlice(
                    angledThirdResult,
                    angledThirdPieceA,
                    angledThirdPieceB
                );

                break;


            case
                C2_TetrahedronSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    angledQuarterResult,
                    angledQuarterPieceA,
                    angledQuarterPieceB
                );

                break;


            case
                C2_TetrahedronSliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    angledThreeQuarterResult,
                    angledThreeQuarterPieceA,
                    angledThreeQuarterPieceB
                );

                break;
        }
    }


    // =========================================================
    // GENERIC PHYSICAL SLICE
    // =========================================================

    private void PerformPhysicalSlice(
        GameObject resultObject,
        Rigidbody pieceA,
        Rigidbody pieceB
    )
    {
        if (
            fullTetrahedron == null ||
            tetrahedronSliceResults == null ||
            resultObject == null ||
            pieceA == null ||
            pieceB == null
        )
        {
            Debug.LogError(
                "C2 Tetrahedron: Required slicing " +
                "references are missing."
            );


            return;
        }


        traceCompleted =
            true;


        HideGuide();


        fullTetrahedron.SetActive(
            false
        );


        HideAllSliceResults();


        tetrahedronSliceResults.SetActive(
            true
        );


        resultObject.SetActive(
            true
        );


        Physics.SyncTransforms();


        EnablePiecePhysics(
            pieceA
        );


        EnablePiecePhysics(
            pieceB
        );


        Vector3 forceA =
            (-transform.right *
                separationImpulse) +

            (transform.up *
                upwardImpulse);


        Vector3 forceB =
            (transform.right *
                separationImpulse) +

            (transform.up *
                upwardImpulse);


        pieceA.AddForce(
            forceA,
            ForceMode.Impulse
        );


        pieceB.AddForce(
            forceB,
            ForceMode.Impulse
        );


        if (torqueImpulse > 0f)
        {
            pieceA.AddTorque(
                transform.forward *
                    torqueImpulse,
                ForceMode.Impulse
            );


            pieceB.AddTorque(
                -transform.forward *
                    torqueImpulse,
                ForceMode.Impulse
            );
        }


        if (uiManager != null)
        {
            uiManager.ShowResultPanel();
        }


        Debug.Log(
            "C2 Tetrahedron: " +
            selectedDirection +
            " " +
            selectedFraction +
            " physical slice completed."
        );
    }


    // =========================================================
    // ENABLE PHYSICS
    // =========================================================

    private void EnablePiecePhysics(
        Rigidbody piece
    )
    {
        if (piece == null)
        {
            return;
        }


        piece.velocity =
            Vector3.zero;


        piece.angularVelocity =
            Vector3.zero;


        piece.useGravity =
            true;


        piece.isKinematic =
            false;


        piece.WakeUp();
    }


    // =========================================================
    // RESET ONE PIECE
    // =========================================================

    private void ResetPiece(
        Rigidbody piece,
        C2_TetrahedronPieceInitialState state
    )
    {
        if (piece == null)
        {
            return;
        }


        if (!piece.isKinematic)
        {
            piece.velocity =
                Vector3.zero;


            piece.angularVelocity =
                Vector3.zero;
        }


        piece.useGravity =
            false;


        piece.isKinematic =
            true;


        piece.transform.localPosition =
            state.localPosition;


        piece.transform.localRotation =
            state.localRotation;


        piece.Sleep();
    }


    // =========================================================
    // RESET ALL PIECES
    // =========================================================

    private void ResetAllSlicePieces()
    {
        foreach (
            KeyValuePair<
                Rigidbody,
                C2_TetrahedronPieceInitialState
            >
            pair in
            initialPieceStates
        )
        {
            ResetPiece(
                pair.Key,
                pair.Value
            );
        }
    }


    // =========================================================
    // HIDE ALL RESULTS
    // =========================================================

    private void HideAllSliceResults()
    {
        // -----------------------------------------------------
        // VERTICAL
        // -----------------------------------------------------

        SetResultInactive(
            verticalHalfResult
        );

        SetResultInactive(
            verticalThirdResult
        );

        SetResultInactive(
            verticalQuarterResult
        );

        SetResultInactive(
            verticalThreeQuarterResult
        );


        // -----------------------------------------------------
        // HORIZONTAL
        // -----------------------------------------------------

        SetResultInactive(
            horizontalHalfResult
        );

        SetResultInactive(
            horizontalThirdResult
        );

        SetResultInactive(
            horizontalQuarterResult
        );

        SetResultInactive(
            horizontalThreeQuarterResult
        );


        // -----------------------------------------------------
        // ANGLED
        // -----------------------------------------------------

        SetResultInactive(
            angledHalfResult
        );

        SetResultInactive(
            angledThirdResult
        );

        SetResultInactive(
            angledQuarterResult
        );

        SetResultInactive(
            angledThreeQuarterResult
        );
    }


    // =========================================================
    // HIDE ONE RESULT
    // =========================================================

    private void SetResultInactive(
        GameObject result
    )
    {
        if (result != null)
        {
            result.SetActive(
                false
            );
        }
    }


    // =========================================================
    // RESTORE UNSLICED VISUAL STATE
    // =========================================================

    private void RestoreUnslicedVisualState()
    {
        HideGuide();


        ResetAllSlicePieces();


        HideAllSliceResults();


        if (
            tetrahedronSliceResults !=
            null
        )
        {
            tetrahedronSliceResults.SetActive(
                false
            );
        }


        if (fullTetrahedron != null)
        {
            fullTetrahedron.SetActive(
                true
            );
        }


        Physics.SyncTransforms();
    }


    // =========================================================
    // TRY SAME SLICE AGAIN
    // =========================================================

    public void TryAgainCurrentSlice()
    {
        traceCompleted =
            false;


        RestoreUnslicedVisualState();


        if (HasCompleteSelection)
        {
            EvaluateSelection();
        }


        Debug.Log(
            "C2 Tetrahedron: Try Again complete."
        );
    }


    // =========================================================
    // RESET TETRAHEDRON
    // =========================================================

    public void ResetTetrahedron()
    {
        selectedDirection =
            C2_TetrahedronSlicingDirection.None;


        selectedFraction =
            C2_TetrahedronSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();


        Debug.Log(
            "C2 Tetrahedron: Reset complete."
        );
    }


    // =========================================================
    // PREPARE INITIAL STATE
    // =========================================================

    private void PrepareInitialState()
    {
        selectedDirection =
            C2_TetrahedronSlicingDirection.None;


        selectedFraction =
            C2_TetrahedronSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();
    }
}
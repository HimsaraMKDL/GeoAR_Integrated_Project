using System.Collections.Generic;
using UnityEngine;

public class C2_CylinderSlicingManager : MonoBehaviour
{
    // =========================================================
    // OPTIONS
    // =========================================================

    public enum C2_CylinderSlicingDirection
    {
        None,
        Vertical,
        Horizontal,
        Angled
    }


    public enum C2_CylinderSliceFraction
    {
        None,
        OneHalf,
        OneThird,
        OneQuarter,
        ThreeQuarter
    }


    // =========================================================
    // CYLINDER DIMENSIONS
    // =========================================================

    private const float Radius =
        0.14f;


    private const float Height =
        0.20f;


    private const float GuideZ =
        -0.155f;


    // =========================================================
    // MAIN REFERENCES
    // =========================================================

    [Header("Cylinder References")]

    [SerializeField]
    private GameObject fullCylinder;


    [SerializeField]
    private GameObject sliceGuide;


    [SerializeField]
    private GameObject cylinderSliceResults;


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
    // VERTICAL 1/2
    // =========================================================

    [Header("Vertical 1/2 Result")]

    [SerializeField]
    private GameObject verticalHalfResult;


    [SerializeField]
    private Rigidbody verticalHalfLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalHalfRightRigidbody;


    // =========================================================
    // VERTICAL 1/3
    // =========================================================

    [Header("Vertical 1/3 Result")]

    [SerializeField]
    private GameObject verticalThirdResult;


    [SerializeField]
    private Rigidbody verticalThirdLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalThirdRightRigidbody;


    // =========================================================
    // VERTICAL 1/4
    // =========================================================

    [Header("Vertical 1/4 Result")]

    [SerializeField]
    private GameObject verticalQuarterResult;


    [SerializeField]
    private Rigidbody verticalQuarterLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalQuarterRightRigidbody;


    // =========================================================
    // VERTICAL 3/4
    // =========================================================

    [Header("Vertical 3/4 Result")]

    [SerializeField]
    private GameObject verticalThreeQuarterResult;


    [SerializeField]
    private Rigidbody verticalThreeQuarterLeftRigidbody;


    [SerializeField]
    private Rigidbody verticalThreeQuarterRightRigidbody;


    // =========================================================
    // HORIZONTAL 1/2
    // =========================================================

    [Header("Horizontal 1/2 Result")]

    [SerializeField]
    private GameObject horizontalHalfResult;


    [SerializeField]
    private Rigidbody horizontalHalfBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalHalfTopRigidbody;


    // =========================================================
    // HORIZONTAL 1/3
    // =========================================================

    [Header("Horizontal 1/3 Result")]

    [SerializeField]
    private GameObject horizontalThirdResult;


    [SerializeField]
    private Rigidbody horizontalThirdBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalThirdTopRigidbody;


    // =========================================================
    // HORIZONTAL 1/4
    // =========================================================

    [Header("Horizontal 1/4 Result")]

    [SerializeField]
    private GameObject horizontalQuarterResult;


    [SerializeField]
    private Rigidbody horizontalQuarterBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalQuarterTopRigidbody;


    // =========================================================
    // HORIZONTAL 3/4
    // =========================================================

    [Header("Horizontal 3/4 Result")]

    [SerializeField]
    private GameObject horizontalThreeQuarterResult;


    [SerializeField]
    private Rigidbody horizontalThreeQuarterBottomRigidbody;


    [SerializeField]
    private Rigidbody horizontalThreeQuarterTopRigidbody;


    // =========================================================
    // ANGLED 1/2
    // =========================================================

    [Header("Angled 1/2 Result")]

    [SerializeField]
    private GameObject angledHalfResult;


    [SerializeField]
    private Rigidbody angledHalfPieceA;


    [SerializeField]
    private Rigidbody angledHalfPieceB;


    // =========================================================
    // ANGLED 1/3
    // =========================================================

    [Header("Angled 1/3 Result")]

    [SerializeField]
    private GameObject angledThirdResult;


    [SerializeField]
    private Rigidbody angledThirdPieceA;


    [SerializeField]
    private Rigidbody angledThirdPieceB;


    // =========================================================
    // ANGLED 1/4
    // =========================================================

    [Header("Angled 1/4 Result")]

    [SerializeField]
    private GameObject angledQuarterResult;


    [SerializeField]
    private Rigidbody angledQuarterPieceA;


    [SerializeField]
    private Rigidbody angledQuarterPieceB;


    // =========================================================
    // ANGLED 3/4
    // =========================================================

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
    private C2_CylinderSlicingDirection selectedDirection =
        C2_CylinderSlicingDirection.None;


    [SerializeField]
    private C2_CylinderSliceFraction selectedFraction =
        C2_CylinderSliceFraction.None;


    [Header("Trace State - Read Only During Play")]

    [SerializeField]
    private bool traceCompleted =
        false;


    // =========================================================
    // SHARED UI
    // =========================================================

    private C2_AppUIManager uiManager;


    // =========================================================
    // INITIAL PHYSICS STATE
    // =========================================================

    private struct C2_CylinderPieceInitialState
    {
        public Vector3 localPosition;

        public Quaternion localRotation;


        public C2_CylinderPieceInitialState(
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
        Dictionary<Rigidbody, C2_CylinderPieceInitialState>
        initialPieceStates =
            new Dictionary<Rigidbody, C2_CylinderPieceInitialState>();


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public C2_CylinderSlicingDirection SelectedDirection
        => selectedDirection;


    public C2_CylinderSliceFraction SelectedFraction
        => selectedFraction;


    public bool TraceCompleted
        => traceCompleted;


    public bool HasCompleteSelection
    {
        get
        {
            return
                selectedDirection !=
                    C2_CylinderSlicingDirection.None &&

                selectedFraction !=
                    C2_CylinderSliceFraction.None;
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
                    C2_CylinderSlicingDirection.Vertical ||

                selectedDirection ==
                    C2_CylinderSlicingDirection.Horizontal ||

                selectedDirection ==
                    C2_CylinderSlicingDirection.Angled;
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
    // UI INJECTION
    // =========================================================

    public void SetUIManager(
        C2_AppUIManager manager
    )
    {
        uiManager =
            manager;
    }


    // =========================================================
    // VALID FRACTIONS
    // =========================================================

    private bool IsImplementedFraction(
        C2_CylinderSliceFraction fraction
    )
    {
        return
            fraction ==
                C2_CylinderSliceFraction.OneHalf ||

            fraction ==
                C2_CylinderSliceFraction.OneThird ||

            fraction ==
                C2_CylinderSliceFraction.OneQuarter ||

            fraction ==
                C2_CylinderSliceFraction.ThreeQuarter;
    }


    // =========================================================
    // CAPTURE INITIAL STATES
    // =========================================================

    private void CaptureAllInitialPieceStates()
    {
        initialPieceStates.Clear();


        // Vertical

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


        // Horizontal

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


        // Angled

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
            new C2_CylinderPieceInitialState(
                piece.transform.localPosition,
                piece.transform.localRotation
            )
        );
    }


    // =========================================================
    // DIRECTION SELECTION
    // =========================================================

    public void SelectVertical()
    {
        selectedDirection =
            C2_CylinderSlicingDirection.Vertical;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: Vertical selected."
        );
    }


    public void SelectHorizontal()
    {
        selectedDirection =
            C2_CylinderSlicingDirection.Horizontal;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: Horizontal selected."
        );
    }


    public void SelectAngled()
    {
        selectedDirection =
            C2_CylinderSlicingDirection.Angled;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: Angled selected."
        );
    }


    // =========================================================
    // FRACTION SELECTION
    // =========================================================

    public void SelectHalf()
    {
        selectedFraction =
            C2_CylinderSliceFraction.OneHalf;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: 1/2 selected."
        );
    }


    public void SelectThird()
    {
        selectedFraction =
            C2_CylinderSliceFraction.OneThird;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: 1/3 selected."
        );
    }


    public void SelectQuarter()
    {
        selectedFraction =
            C2_CylinderSliceFraction.OneQuarter;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: 1/4 selected."
        );
    }


    public void SelectThreeQuarter()
    {
        selectedFraction =
            C2_CylinderSliceFraction.ThreeQuarter;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cylinder: 3/4 selected."
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
                C2_CylinderSlicingDirection.Vertical:

                ConfigureVerticalGuide();

                ShowGuide();

                break;


            case
                C2_CylinderSlicingDirection.Horizontal:

                ConfigureHorizontalGuide();

                ShowGuide();

                break;


            case
                C2_CylinderSlicingDirection.Angled:

                ConfigureAngledGuide();

                break;
        }
    }


    // =========================================================
    // VERTICAL GUIDE
    // =========================================================

    private void ConfigureVerticalGuide()
    {
        float cutX =
            GetVerticalCutOffset();


        SetGuidePositions(
            new Vector3(
                cutX,
                0f,
                GuideZ
            ),

            new Vector3(
                cutX,
                Height,
                GuideZ
            )
        );
    }


    private float GetVerticalCutOffset()
    {
        switch (selectedFraction)
        {
            case
                C2_CylinderSliceFraction.OneHalf:

                return
                    0f;


            case
                C2_CylinderSliceFraction.OneThird:

                return
                    -0.036966f;


            case
                C2_CylinderSliceFraction.OneQuarter:

                return
                    -0.056367f;


            case
                C2_CylinderSliceFraction.ThreeQuarter:

                return
                    0.056367f;


            default:

                return
                    0f;
        }
    }


    // =========================================================
    // HORIZONTAL GUIDE
    // =========================================================

    private void ConfigureHorizontalGuide()
    {
        float cutY =
            GetHorizontalCutOffset();


        SetGuidePositions(
            new Vector3(
                -Radius,
                cutY,
                GuideZ
            ),

            new Vector3(
                Radius,
                cutY,
                GuideZ
            )
        );
    }


    private float GetHorizontalCutOffset()
    {
        switch (selectedFraction)
        {
            case
                C2_CylinderSliceFraction.OneHalf:

                return
                    0.10f;


            case
                C2_CylinderSliceFraction.OneThird:

                return
                    Height /
                    3f;


            case
                C2_CylinderSliceFraction.OneQuarter:

                return
                    0.05f;


            case
                C2_CylinderSliceFraction.ThreeQuarter:

                return
                    0.15f;


            default:

                return
                    0f;
        }
    }


    // =========================================================
    // ANGLED GUIDE
    // =========================================================

    private void ConfigureAngledGuide()
    {
        Vector2 planeNormal =
            new Vector2(
                -1f,
                1f
            ).normalized;


        float planeOffset =
            GetAngledCutOffset();


        List<Vector2> intersections =
            FindRectangleLineIntersections(
                planeNormal,
                planeOffset
            );


        if (
            intersections.Count !=
            2
        )
        {
            Debug.LogError(
                "C2 Cylinder: Could not calculate exactly " +
                "two Angled guide endpoints."
            );


            HideGuide();

            return;
        }


        // Use the lower point as the visible start marker.

        intersections.Sort(
            delegate (
                Vector2 first,
                Vector2 second
            )
            {
                int yCompare =
                    first.y.CompareTo(
                        second.y
                    );


                if (yCompare != 0)
                {
                    return
                        yCompare;
                }


                return
                    first.x.CompareTo(
                        second.x
                    );
            }
        );


        Vector2 start =
            intersections[0];


        Vector2 end =
            intersections[1];


        SetGuidePositions(
            new Vector3(
                start.x,
                start.y,
                GuideZ
            ),

            new Vector3(
                end.x,
                end.y,
                GuideZ
            )
        );


        ShowGuide();
    }


    private float GetAngledCutOffset()
    {
        switch (selectedFraction)
        {
            case
                C2_CylinderSliceFraction.OneHalf:

                return
                    0.070711f;


            case
                C2_CylinderSliceFraction.OneThird:

                return
                    0.041471f;


            case
                C2_CylinderSliceFraction.OneQuarter:

                return
                    0.024864f;


            case
                C2_CylinderSliceFraction.ThreeQuarter:

                return
                    0.116558f;


            default:

                return
                    0f;
        }
    }


    // =========================================================
    // ANGLED LINE / CYLINDER FRONT RECTANGLE
    // =========================================================
    //
    // From the front camera view, the visible Cylinder
    // silhouette is:
    //
    // X = -0.14 ... +0.14
    // Y =  0.00 ...  0.20
    //
    // The visual guide is placed at Z = -0.155.
    // =========================================================

    private List<Vector2>
        FindRectangleLineIntersections(
            Vector2 planeNormal,
            float planeOffset
        )
    {
        List<Vector2> intersections =
            new List<Vector2>();


        Vector2 bottomLeft =
            new Vector2(
                -Radius,
                0f
            );


        Vector2 bottomRight =
            new Vector2(
                Radius,
                0f
            );


        Vector2 topRight =
            new Vector2(
                Radius,
                Height
            );


        Vector2 topLeft =
            new Vector2(
                -Radius,
                Height
            );


        AddLineSegmentIntersection(
            bottomLeft,
            bottomRight,
            planeNormal,
            planeOffset,
            intersections
        );


        AddLineSegmentIntersection(
            bottomRight,
            topRight,
            planeNormal,
            planeOffset,
            intersections
        );


        AddLineSegmentIntersection(
            topRight,
            topLeft,
            planeNormal,
            planeOffset,
            intersections
        );


        AddLineSegmentIntersection(
            topLeft,
            bottomLeft,
            planeNormal,
            planeOffset,
            intersections
        );


        return
            intersections;
    }


    private void AddLineSegmentIntersection(
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
            AddUniqueGuidePoint(
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
            AddUniqueGuidePoint(
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
            float t =
                valueA /
                (
                    valueA -
                    valueB
                );


            Vector2 intersection =
                Vector2.Lerp(
                    a,
                    b,
                    t
                );


            AddUniqueGuidePoint(
                results,
                intersection
            );
        }
    }


    private void AddUniqueGuidePoint(
        List<Vector2> points,
        Vector2 newPoint
    )
    {
        const float tolerance =
            0.00001f;


        for (
            int index = 0;
            index < points.Count;
            index++
        )
        {
            if (
                Vector2.Distance(
                    points[index],
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
    // APPLY GUIDE POSITIONS
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


    private void ShowGuide()
    {
        if (sliceGuide != null)
        {
            sliceGuide.SetActive(
                true
            );
        }
    }


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
                C2_CylinderSlicingDirection.Vertical:

                CompleteVerticalSlice();

                break;


            case
                C2_CylinderSlicingDirection.Horizontal:

                CompleteHorizontalSlice();

                break;


            case
                C2_CylinderSlicingDirection.Angled:

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
            case C2_CylinderSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    verticalHalfResult,
                    verticalHalfLeftRigidbody,
                    verticalHalfRightRigidbody
                );

                break;


            case C2_CylinderSliceFraction.OneThird:

                PerformPhysicalSlice(
                    verticalThirdResult,
                    verticalThirdLeftRigidbody,
                    verticalThirdRightRigidbody
                );

                break;


            case C2_CylinderSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    verticalQuarterResult,
                    verticalQuarterLeftRigidbody,
                    verticalQuarterRightRigidbody
                );

                break;


            case C2_CylinderSliceFraction.ThreeQuarter:

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
            case C2_CylinderSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    horizontalHalfResult,
                    horizontalHalfBottomRigidbody,
                    horizontalHalfTopRigidbody
                );

                break;


            case C2_CylinderSliceFraction.OneThird:

                PerformPhysicalSlice(
                    horizontalThirdResult,
                    horizontalThirdBottomRigidbody,
                    horizontalThirdTopRigidbody
                );

                break;


            case C2_CylinderSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    horizontalQuarterResult,
                    horizontalQuarterBottomRigidbody,
                    horizontalQuarterTopRigidbody
                );

                break;


            case C2_CylinderSliceFraction.ThreeQuarter:

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
            case C2_CylinderSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    angledHalfResult,
                    angledHalfPieceA,
                    angledHalfPieceB
                );

                break;


            case C2_CylinderSliceFraction.OneThird:

                PerformPhysicalSlice(
                    angledThirdResult,
                    angledThirdPieceA,
                    angledThirdPieceB
                );

                break;


            case C2_CylinderSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    angledQuarterResult,
                    angledQuarterPieceA,
                    angledQuarterPieceB
                );

                break;


            case C2_CylinderSliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    angledThreeQuarterResult,
                    angledThreeQuarterPieceA,
                    angledThreeQuarterPieceB
                );

                break;
        }
    }


    // =========================================================
    // PHYSICAL SLICE
    // =========================================================

    private void PerformPhysicalSlice(
        GameObject resultObject,
        Rigidbody pieceA,
        Rigidbody pieceB
    )
    {
        if (
            fullCylinder == null ||
            cylinderSliceResults == null ||
            resultObject == null ||
            pieceA == null ||
            pieceB == null
        )
        {
            Debug.LogError(
                "C2 Cylinder: Required slicing references are missing."
            );


            return;
        }


        traceCompleted =
            true;


        HideGuide();


        fullCylinder.SetActive(
            false
        );


        HideAllSliceResults();


        cylinderSliceResults.SetActive(
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


        // We intentionally keep the same stable
        // separation behavior already used by the
        // existing slicing architecture.

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
            "C2 Cylinder: " +
            selectedDirection +
            " " +
            selectedFraction +
            " physical slice completed."
        );
    }


    // =========================================================
    // ENABLE PIECE PHYSICS
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
        C2_CylinderPieceInitialState state
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
                C2_CylinderPieceInitialState
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
        // Vertical

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


        // Horizontal

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


        // Angled

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


        if (cylinderSliceResults != null)
        {
            cylinderSliceResults.SetActive(
                false
            );
        }


        if (fullCylinder != null)
        {
            fullCylinder.SetActive(
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
            "C2 Cylinder: Try Again complete."
        );
    }


    // =========================================================
    // RESET CYLINDER
    // =========================================================

    public void ResetCylinder()
    {
        selectedDirection =
            C2_CylinderSlicingDirection.None;


        selectedFraction =
            C2_CylinderSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();


        Debug.Log(
            "C2 Cylinder: Reset complete."
        );
    }


    // =========================================================
    // INITIAL STATE
    // =========================================================

    private void PrepareInitialState()
    {
        selectedDirection =
            C2_CylinderSlicingDirection.None;


        selectedFraction =
            C2_CylinderSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();
    }
}
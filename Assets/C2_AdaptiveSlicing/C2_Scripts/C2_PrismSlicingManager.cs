using System.Collections.Generic;
using UnityEngine;

public class C2_PrismSlicingManager : MonoBehaviour
{
    // =========================================================
    // OPTIONS
    // =========================================================

    public enum C2_PrismSlicingDirection
    {
        None,
        Vertical,
        Horizontal,
        Angled
    }


    public enum C2_PrismSliceFraction
    {
        None,
        OneHalf,
        OneThird,
        OneQuarter,
        ThreeQuarter
    }


    // =========================================================
    // PRISM DIMENSIONS
    // =========================================================

    private const float HalfWidth = 0.15f;
    private const float Width = 0.30f;
    private const float PrismHeight = 0.20f;
    private const float GuideZ = -0.085f;


    // =========================================================
    // MAIN REFERENCES
    // =========================================================

    [Header("Prism References")]
    [SerializeField] private GameObject fullPrism;
    [SerializeField] private GameObject sliceGuide;
    [SerializeField] private GameObject prismSliceResults;


    // =========================================================
    // GUIDE REFERENCES
    // =========================================================

    [Header("Slice Guide References")]
    [SerializeField] private Transform sliceStartPoint;
    [SerializeField] private Transform sliceEndPoint;
    [SerializeField] private LineRenderer sliceGuideLine;


    // =========================================================
    // VERTICAL RESULTS
    // =========================================================

    [Header("Vertical 1/2 Result")]
    [SerializeField] private GameObject verticalHalfResult;
    [SerializeField] private Rigidbody verticalHalfLeftRigidbody;
    [SerializeField] private Rigidbody verticalHalfRightRigidbody;


    [Header("Vertical 1/3 Result")]
    [SerializeField] private GameObject verticalThirdResult;
    [SerializeField] private Rigidbody verticalThirdLeftRigidbody;
    [SerializeField] private Rigidbody verticalThirdRightRigidbody;


    [Header("Vertical 1/4 Result")]
    [SerializeField] private GameObject verticalQuarterResult;
    [SerializeField] private Rigidbody verticalQuarterLeftRigidbody;
    [SerializeField] private Rigidbody verticalQuarterRightRigidbody;


    [Header("Vertical 3/4 Result")]
    [SerializeField] private GameObject verticalThreeQuarterResult;
    [SerializeField] private Rigidbody verticalThreeQuarterLeftRigidbody;
    [SerializeField] private Rigidbody verticalThreeQuarterRightRigidbody;


    // =========================================================
    // HORIZONTAL RESULTS
    // =========================================================

    [Header("Horizontal 1/2 Result")]
    [SerializeField] private GameObject horizontalHalfResult;
    [SerializeField] private Rigidbody horizontalHalfBottomRigidbody;
    [SerializeField] private Rigidbody horizontalHalfTopRigidbody;


    [Header("Horizontal 1/3 Result")]
    [SerializeField] private GameObject horizontalThirdResult;
    [SerializeField] private Rigidbody horizontalThirdBottomRigidbody;
    [SerializeField] private Rigidbody horizontalThirdTopRigidbody;


    [Header("Horizontal 1/4 Result")]
    [SerializeField] private GameObject horizontalQuarterResult;
    [SerializeField] private Rigidbody horizontalQuarterBottomRigidbody;
    [SerializeField] private Rigidbody horizontalQuarterTopRigidbody;


    [Header("Horizontal 3/4 Result")]
    [SerializeField] private GameObject horizontalThreeQuarterResult;
    [SerializeField] private Rigidbody horizontalThreeQuarterBottomRigidbody;
    [SerializeField] private Rigidbody horizontalThreeQuarterTopRigidbody;


    // =========================================================
    // ANGLED RESULTS
    // =========================================================

    [Header("Angled 1/2 Result")]
    [SerializeField] private GameObject angledHalfResult;
    [SerializeField] private Rigidbody angledHalfPieceA;
    [SerializeField] private Rigidbody angledHalfPieceB;


    [Header("Angled 1/3 Result")]
    [SerializeField] private GameObject angledThirdResult;
    [SerializeField] private Rigidbody angledThirdPieceA;
    [SerializeField] private Rigidbody angledThirdPieceB;


    [Header("Angled 1/4 Result")]
    [SerializeField] private GameObject angledQuarterResult;
    [SerializeField] private Rigidbody angledQuarterPieceA;
    [SerializeField] private Rigidbody angledQuarterPieceB;


    [Header("Angled 3/4 Result")]
    [SerializeField] private GameObject angledThreeQuarterResult;
    [SerializeField] private Rigidbody angledThreeQuarterPieceA;
    [SerializeField] private Rigidbody angledThreeQuarterPieceB;


    // =========================================================
    // PHYSICS
    // =========================================================

    [Header("Slice Physics Settings")]
    [SerializeField] private float separationImpulse = 0.25f;
    [SerializeField] private float upwardImpulse = 0.30f;
    [SerializeField] private float torqueImpulse = 0f;


    // =========================================================
    // STATE
    // =========================================================

    [Header("Current Selection - Read Only During Play")]
    [SerializeField]
    private C2_PrismSlicingDirection selectedDirection =
        C2_PrismSlicingDirection.None;


    [SerializeField]
    private C2_PrismSliceFraction selectedFraction =
        C2_PrismSliceFraction.None;


    [Header("Trace State - Read Only During Play")]
    [SerializeField]
    private bool traceCompleted = false;


    private C2_AppUIManager uiManager;


    // =========================================================
    // INITIAL PHYSICS STATE
    // =========================================================

    private struct C2_PrismPieceInitialState
    {
        public Vector3 localPosition;
        public Quaternion localRotation;


        public C2_PrismPieceInitialState(
            Vector3 position,
            Quaternion rotation
        )
        {
            localPosition = position;
            localRotation = rotation;
        }
    }


    private readonly
        Dictionary<Rigidbody, C2_PrismPieceInitialState>
        initialPieceStates =
            new Dictionary<Rigidbody, C2_PrismPieceInitialState>();


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public C2_PrismSlicingDirection SelectedDirection
        => selectedDirection;


    public C2_PrismSliceFraction SelectedFraction
        => selectedFraction;


    public bool TraceCompleted
        => traceCompleted;


    public bool HasCompleteSelection
    {
        get
        {
            return
                selectedDirection !=
                    C2_PrismSlicingDirection.None &&

                selectedFraction !=
                    C2_PrismSliceFraction.None;
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
                    C2_PrismSlicingDirection.Vertical ||

                selectedDirection ==
                    C2_PrismSlicingDirection.Horizontal ||

                selectedDirection ==
                    C2_PrismSlicingDirection.Angled;
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
    // FUTURE MAIN UI
    // =========================================================

    public void SetUIManager(
        C2_AppUIManager manager
    )
    {
        uiManager = manager;
    }


    // =========================================================
    // IMPLEMENTED FRACTIONS
    // =========================================================

    private bool IsImplementedFraction(
        C2_PrismSliceFraction fraction
    )
    {
        return
            fraction ==
                C2_PrismSliceFraction.OneHalf ||

            fraction ==
                C2_PrismSliceFraction.OneThird ||

            fraction ==
                C2_PrismSliceFraction.OneQuarter ||

            fraction ==
                C2_PrismSliceFraction.ThreeQuarter;
    }


    // =========================================================
    // CAPTURE PHYSICS STATES
    // =========================================================

    private void CaptureAllInitialPieceStates()
    {
        initialPieceStates.Clear();


        // Vertical
        CapturePiece(verticalHalfLeftRigidbody);
        CapturePiece(verticalHalfRightRigidbody);

        CapturePiece(verticalThirdLeftRigidbody);
        CapturePiece(verticalThirdRightRigidbody);

        CapturePiece(verticalQuarterLeftRigidbody);
        CapturePiece(verticalQuarterRightRigidbody);

        CapturePiece(verticalThreeQuarterLeftRigidbody);
        CapturePiece(verticalThreeQuarterRightRigidbody);


        // Horizontal
        CapturePiece(horizontalHalfBottomRigidbody);
        CapturePiece(horizontalHalfTopRigidbody);

        CapturePiece(horizontalThirdBottomRigidbody);
        CapturePiece(horizontalThirdTopRigidbody);

        CapturePiece(horizontalQuarterBottomRigidbody);
        CapturePiece(horizontalQuarterTopRigidbody);

        CapturePiece(horizontalThreeQuarterBottomRigidbody);
        CapturePiece(horizontalThreeQuarterTopRigidbody);


        // Angled
        CapturePiece(angledHalfPieceA);
        CapturePiece(angledHalfPieceB);

        CapturePiece(angledThirdPieceA);
        CapturePiece(angledThirdPieceB);

        CapturePiece(angledQuarterPieceA);
        CapturePiece(angledQuarterPieceB);

        CapturePiece(angledThreeQuarterPieceA);
        CapturePiece(angledThreeQuarterPieceB);
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
            new C2_PrismPieceInitialState(
                piece.transform.localPosition,
                piece.transform.localRotation
            )
        );
    }


    // =========================================================
    // DIRECTIONS
    // =========================================================

    public void SelectVertical()
    {
        selectedDirection =
            C2_PrismSlicingDirection.Vertical;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Prism: Vertical selected."
        );
    }


    public void SelectHorizontal()
    {
        selectedDirection =
            C2_PrismSlicingDirection.Horizontal;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Prism: Horizontal selected."
        );
    }


    public void SelectAngled()
    {
        selectedDirection =
            C2_PrismSlicingDirection.Angled;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Prism: Angled selected."
        );
    }


    // =========================================================
    // FRACTIONS
    // =========================================================

    public void SelectHalf()
    {
        selectedFraction =
            C2_PrismSliceFraction.OneHalf;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectThird()
    {
        selectedFraction =
            C2_PrismSliceFraction.OneThird;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectQuarter()
    {
        selectedFraction =
            C2_PrismSliceFraction.OneQuarter;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectThreeQuarter()
    {
        selectedFraction =
            C2_PrismSliceFraction.ThreeQuarter;


        traceCompleted = false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    // =========================================================
    // EVALUATE SELECTION
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
            case C2_PrismSlicingDirection.Vertical:

                ConfigureSelectedVerticalGuide();

                ShowGuide();

                break;


            case C2_PrismSlicingDirection.Horizontal:

                ConfigureSelectedHorizontalGuide();

                ShowGuide();

                break;


            case C2_PrismSlicingDirection.Angled:

                ConfigureSelectedAngledGuide();

                ShowGuide();

                break;
        }
    }


    // =========================================================
    // FRACTION VALUE
    // =========================================================

    private float GetSelectedFractionValue()
    {
        switch (selectedFraction)
        {
            case C2_PrismSliceFraction.OneHalf:

                return 0.5f;


            case C2_PrismSliceFraction.OneThird:

                return 1f / 3f;


            case C2_PrismSliceFraction.OneQuarter:

                return 0.25f;


            case C2_PrismSliceFraction.ThreeQuarter:

                return 0.75f;


            default:

                return 0.5f;
        }
    }


    // =========================================================
    // VERTICAL GUIDE
    // =========================================================

    private void ConfigureSelectedVerticalGuide()
    {
        float fraction =
            GetSelectedFractionValue();


        float cutX =
            CalculateVerticalCutX(
                fraction
            );


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


    private float CalculateVerticalCutX(
        float fraction
    )
    {
        if (fraction <= 0.5f)
        {
            return
                -HalfWidth +
                HalfWidth *
                Mathf.Sqrt(
                    2f * fraction
                );
        }


        return
            HalfWidth -
            HalfWidth *
            Mathf.Sqrt(
                2f *
                (1f - fraction)
            );
    }


    private float CalculateTriangleTopY(
        float x
    )
    {
        return
            PrismHeight *
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
        float fraction =
            GetSelectedFractionValue();


        float cutY =
            CalculateHorizontalCutY(
                fraction
            );


        float cutHalfWidth =
            CalculateHalfWidthAtY(
                cutY
            );


        SetGuidePositions(
            new Vector3(
                -cutHalfWidth,
                cutY,
                GuideZ
            ),

            new Vector3(
                cutHalfWidth,
                cutY,
                GuideZ
            )
        );
    }


    private float CalculateHorizontalCutY(
        float fraction
    )
    {
        return
            PrismHeight *
            (
                1f -
                Mathf.Sqrt(
                    1f - fraction
                )
            );
    }


    private float CalculateHalfWidthAtY(
        float y
    )
    {
        return
            HalfWidth *
            (
                1f -
                y /
                PrismHeight
            );
    }


    // =========================================================
    // ANGLED GUIDE
    // =========================================================

    private void ConfigureSelectedAngledGuide()
    {
        float fraction =
            GetSelectedFractionValue();


        float offset =
            CalculateAngledOffset(
                fraction
            );


        List<Vector2> intersections =
            FindAngledGuideIntersections(
                offset
            );


        if (intersections.Count != 2)
        {
            Debug.LogError(
                "C2 Prism: Could not calculate " +
                "two Angled guide endpoints."
            );

            return;
        }


        // Keep visual order consistent:
        // lower endpoint first, upper endpoint second.

        intersections.Sort(
            (a, b) =>
            {
                int yCompare =
                    a.y.CompareTo(
                        b.y
                    );


                if (yCompare != 0)
                {
                    return yCompare;
                }


                return
                    a.x.CompareTo(
                        b.x
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
    // FRACTION → ANGLED OFFSET
    // =========================================================

    private float CalculateAngledOffset(
        float fraction
    )
    {
        if (
            fraction <=
            (2f / 3f)
        )
        {
            return
                Mathf.Sqrt(
                    1.5f *
                    fraction
                ) -
                1f;
        }


        return
            (
                1f -
                Mathf.Sqrt(
                    3f *
                    (1f - fraction)
                )
            ) *
            0.5f;
    }


    // =========================================================
    // FIND LINE / TRIANGLE INTERSECTIONS
    // =========================================================

    private List<Vector2>
        FindAngledGuideIntersections(
            float offset
        )
    {
        List<Vector2> points =
            new List<Vector2>();


        Vector2 left =
            new Vector2(
                -HalfWidth,
                0f
            );


        Vector2 right =
            new Vector2(
                HalfWidth,
                0f
            );


        Vector2 top =
            new Vector2(
                0f,
                PrismHeight
            );


        AddEdgeIntersection(
            left,
            right,
            offset,
            points
        );


        AddEdgeIntersection(
            right,
            top,
            offset,
            points
        );


        AddEdgeIntersection(
            top,
            left,
            offset,
            points
        );


        return points;
    }


    private void AddEdgeIntersection(
        Vector2 a,
        Vector2 b,
        float offset,
        List<Vector2> results
    )
    {
        float valueA =
            SignedAngledLineValue(
                a,
                offset
            );


        float valueB =
            SignedAngledLineValue(
                b,
                offset
            );


        const float epsilon =
            0.000001f;


        if (
            Mathf.Abs(valueA) <=
            epsilon
        )
        {
            AddUniquePoint(
                results,
                a
            );
        }


        if (
            Mathf.Abs(valueB) <=
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


            AddUniquePoint(
                results,
                intersection
            );
        }
    }


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


    private float SignedAngledLineValue(
        Vector2 point,
        float offset
    )
    {
        float u =
            (
                point.x +
                HalfWidth
            ) /
            Width;


        float v =
            point.y /
            PrismHeight;


        return
            v -
            u -
            offset;
    }


    // =========================================================
    // GUIDE POSITIONS
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
    // TRACE COMPLETE
    // =========================================================

    public void CompleteTracingAndSlice()
    {
        if (!CanTrace)
        {
            return;
        }


        switch (selectedDirection)
        {
            case C2_PrismSlicingDirection.Vertical:

                CompleteVerticalSlice();

                break;


            case C2_PrismSlicingDirection.Horizontal:

                CompleteHorizontalSlice();

                break;


            case C2_PrismSlicingDirection.Angled:

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
            case C2_PrismSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    verticalHalfResult,
                    verticalHalfLeftRigidbody,
                    verticalHalfRightRigidbody
                );

                break;


            case C2_PrismSliceFraction.OneThird:

                PerformPhysicalSlice(
                    verticalThirdResult,
                    verticalThirdLeftRigidbody,
                    verticalThirdRightRigidbody
                );

                break;


            case C2_PrismSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    verticalQuarterResult,
                    verticalQuarterLeftRigidbody,
                    verticalQuarterRightRigidbody
                );

                break;


            case C2_PrismSliceFraction.ThreeQuarter:

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
            case C2_PrismSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    horizontalHalfResult,
                    horizontalHalfBottomRigidbody,
                    horizontalHalfTopRigidbody
                );

                break;


            case C2_PrismSliceFraction.OneThird:

                PerformPhysicalSlice(
                    horizontalThirdResult,
                    horizontalThirdBottomRigidbody,
                    horizontalThirdTopRigidbody
                );

                break;


            case C2_PrismSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    horizontalQuarterResult,
                    horizontalQuarterBottomRigidbody,
                    horizontalQuarterTopRigidbody
                );

                break;


            case C2_PrismSliceFraction.ThreeQuarter:

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
            case C2_PrismSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    angledHalfResult,
                    angledHalfPieceA,
                    angledHalfPieceB
                );

                break;


            case C2_PrismSliceFraction.OneThird:

                PerformPhysicalSlice(
                    angledThirdResult,
                    angledThirdPieceA,
                    angledThirdPieceB
                );

                break;


            case C2_PrismSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    angledQuarterResult,
                    angledQuarterPieceA,
                    angledQuarterPieceB
                );

                break;


            case C2_PrismSliceFraction.ThreeQuarter:

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
            fullPrism == null ||
            prismSliceResults == null ||
            resultObject == null ||
            pieceA == null ||
            pieceB == null
        )
        {
            Debug.LogError(
                "C2 Prism: Required slicing references " +
                "are missing."
            );

            return;
        }


        traceCompleted =
            true;


        HideGuide();


        fullPrism.SetActive(
            false
        );


        HideAllSliceResults();


        prismSliceResults.SetActive(
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
            "C2 Prism: " +
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
    // RESET PIECES
    // =========================================================

    private void ResetPiece(
        Rigidbody piece,
        C2_PrismPieceInitialState state
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


    private void ResetAllSlicePieces()
    {
        foreach (
            KeyValuePair<Rigidbody, C2_PrismPieceInitialState>
            pair in initialPieceStates
        )
        {
            ResetPiece(
                pair.Key,
                pair.Value
            );
        }
    }


    // =========================================================
    // RESULT VISIBILITY
    // =========================================================

    private void HideAllSliceResults()
    {
        // Vertical
        SetResultInactive(verticalHalfResult);
        SetResultInactive(verticalThirdResult);
        SetResultInactive(verticalQuarterResult);
        SetResultInactive(verticalThreeQuarterResult);


        // Horizontal
        SetResultInactive(horizontalHalfResult);
        SetResultInactive(horizontalThirdResult);
        SetResultInactive(horizontalQuarterResult);
        SetResultInactive(horizontalThreeQuarterResult);


        // Angled
        SetResultInactive(angledHalfResult);
        SetResultInactive(angledThirdResult);
        SetResultInactive(angledQuarterResult);
        SetResultInactive(angledThreeQuarterResult);
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
    // RESTORE UNSLICED STATE
    // =========================================================

    private void RestoreUnslicedVisualState()
    {
        HideGuide();


        ResetAllSlicePieces();


        HideAllSliceResults();


        if (prismSliceResults != null)
        {
            prismSliceResults.SetActive(
                false
            );
        }


        if (fullPrism != null)
        {
            fullPrism.SetActive(
                true
            );
        }


        Physics.SyncTransforms();
    }


    // =========================================================
    // TRY AGAIN
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
            "C2 Prism: Try Again complete."
        );
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetPrism()
    {
        selectedDirection =
            C2_PrismSlicingDirection.None;


        selectedFraction =
            C2_PrismSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();


        Debug.Log(
            "C2 Prism: Reset complete."
        );
    }


    // =========================================================
    // INITIAL
    // =========================================================

    private void PrepareInitialState()
    {
        selectedDirection =
            C2_PrismSlicingDirection.None;


        selectedFraction =
            C2_PrismSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();
    }
}
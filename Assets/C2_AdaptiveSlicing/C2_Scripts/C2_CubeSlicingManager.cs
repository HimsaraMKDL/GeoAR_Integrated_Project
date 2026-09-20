using System.Collections.Generic;
using UnityEngine;

public class C2_CubeSlicingManager : MonoBehaviour
{
    // =========================================================
    // OPTIONS
    // =========================================================

    public enum C2_SlicingDirection
    {
        None,
        Vertical,
        Horizontal,
        Angled
    }

    public enum C2_SliceFraction
    {
        None,
        OneHalf,
        OneThird,
        OneQuarter,
        ThreeQuarter
    }


    // =========================================================
    // MAIN REFERENCES
    // =========================================================

    [Header("Cube References")]
    [SerializeField] private GameObject fullCube;
    [SerializeField] private GameObject sliceGuide;
    [SerializeField] private GameObject cubeSliceResults;


    [Header("Slice Guide References")]
    [SerializeField] private Transform sliceStartPoint;
    [SerializeField] private Transform sliceEndPoint;
    [SerializeField] private LineRenderer sliceGuideLine;


    // =========================================================
    // VERTICAL RESULTS
    // =========================================================

    [Header("Vertical 1/2 Result")]
    [SerializeField] private GameObject verticalHalfResult;
    [SerializeField] private Rigidbody verticalHalfPieceA;
    [SerializeField] private Rigidbody verticalHalfPieceB;


    [Header("Vertical 1/3 Result")]
    [SerializeField] private GameObject verticalThirdResult;
    [SerializeField] private Rigidbody verticalThirdPieceA;
    [SerializeField] private Rigidbody verticalThirdPieceB;


    [Header("Vertical 1/4 Result")]
    [SerializeField] private GameObject verticalQuarterResult;
    [SerializeField] private Rigidbody verticalQuarterPieceA;
    [SerializeField] private Rigidbody verticalQuarterPieceB;


    [Header("Vertical 3/4 Result")]
    [SerializeField] private GameObject verticalThreeQuarterResult;
    [SerializeField] private Rigidbody verticalThreeQuarterPieceA;
    [SerializeField] private Rigidbody verticalThreeQuarterPieceB;


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
    // CURRENT STATE
    // =========================================================

    [Header("Current Selection - Read Only During Play")]

    [SerializeField]
    private C2_SlicingDirection selectedDirection =
        C2_SlicingDirection.None;


    [SerializeField]
    private C2_SliceFraction selectedFraction =
        C2_SliceFraction.None;


    [Header("Trace State - Read Only During Play")]

    [SerializeField]
    private bool traceCompleted = false;


    // =========================================================
    // UI
    // =========================================================

    private C2_AppUIManager uiManager;


    // =========================================================
    // SAVED PHYSICS STATES
    // =========================================================

    private struct C2_PieceInitialState
    {
        public Vector3 localPosition;
        public Quaternion localRotation;


        public C2_PieceInitialState(
            Vector3 position,
            Quaternion rotation
        )
        {
            localPosition = position;
            localRotation = rotation;
        }
    }


    private readonly
        Dictionary<Rigidbody, C2_PieceInitialState>
        initialPieceStates =
            new Dictionary<Rigidbody, C2_PieceInitialState>();


    // =========================================================
    // PUBLIC VALUES
    // =========================================================

    public C2_SlicingDirection SelectedDirection
        => selectedDirection;


    public C2_SliceFraction SelectedFraction
        => selectedFraction;


    public bool TraceCompleted
        => traceCompleted;


    public bool HasCompleteSelection
    {
        get
        {
            return
                selectedDirection !=
                    C2_SlicingDirection.None &&

                selectedFraction !=
                    C2_SliceFraction.None;
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


            if (!IsImplementedFraction(
                    selectedFraction))
            {
                return false;
            }


            return
                selectedDirection ==
                    C2_SlicingDirection.Vertical ||

                selectedDirection ==
                    C2_SlicingDirection.Horizontal ||

                selectedDirection ==
                    C2_SlicingDirection.Angled;
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
    // UI CONNECTION
    // =========================================================

    public void SetUIManager(
        C2_AppUIManager manager
    )
    {
        uiManager =
            manager;
    }


    // =========================================================
    // FRACTION CHECK
    // =========================================================

    private bool IsImplementedFraction(
        C2_SliceFraction fraction
    )
    {
        return
            fraction ==
                C2_SliceFraction.OneHalf ||

            fraction ==
                C2_SliceFraction.OneThird ||

            fraction ==
                C2_SliceFraction.OneQuarter ||

            fraction ==
                C2_SliceFraction.ThreeQuarter;
    }


    // =========================================================
    // CAPTURE ALL PHYSICS PIECES
    // =========================================================

    private void CaptureAllInitialPieceStates()
    {
        initialPieceStates.Clear();


        // Vertical
        CapturePiece(verticalHalfPieceA);
        CapturePiece(verticalHalfPieceB);

        CapturePiece(verticalThirdPieceA);
        CapturePiece(verticalThirdPieceB);

        CapturePiece(verticalQuarterPieceA);
        CapturePiece(verticalQuarterPieceB);

        CapturePiece(verticalThreeQuarterPieceA);
        CapturePiece(verticalThreeQuarterPieceB);


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


        if (initialPieceStates.ContainsKey(
                piece))
        {
            return;
        }


        initialPieceStates.Add(
            piece,
            new C2_PieceInitialState(
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
            C2_SlicingDirection.Vertical;

        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cube Slicing: Vertical selected."
        );
    }


    public void SelectHorizontal()
    {
        selectedDirection =
            C2_SlicingDirection.Horizontal;

        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cube Slicing: Horizontal selected."
        );
    }


    public void SelectAngled()
    {
        selectedDirection =
            C2_SlicingDirection.Angled;

        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cube Slicing: Angled selected."
        );
    }


    // =========================================================
    // FRACTION SELECTION
    // =========================================================

    public void SelectHalf()
    {
        selectedFraction =
            C2_SliceFraction.OneHalf;

        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectThird()
    {
        selectedFraction =
            C2_SliceFraction.OneThird;

        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectQuarter()
    {
        selectedFraction =
            C2_SliceFraction.OneQuarter;

        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectThreeQuarter()
    {
        selectedFraction =
            C2_SliceFraction.ThreeQuarter;

        traceCompleted =
            false;


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


        if (!IsImplementedFraction(
                selectedFraction))
        {
            return;
        }


        switch (selectedDirection)
        {
            case C2_SlicingDirection.Vertical:

                ConfigureSelectedVerticalGuide();

                ShowGuide();

                break;


            case C2_SlicingDirection.Horizontal:

                ConfigureSelectedHorizontalGuide();

                ShowGuide();

                break;


            case C2_SlicingDirection.Angled:

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
        float guideX =
            0f;


        switch (selectedFraction)
        {
            case C2_SliceFraction.OneHalf:

                guideX = 0f;

                break;


            case C2_SliceFraction.OneThird:

                guideX = -0.0333f;

                break;


            case C2_SliceFraction.OneQuarter:

                guideX = -0.05f;

                break;


            case C2_SliceFraction.ThreeQuarter:

                guideX = 0.05f;

                break;
        }


        SetGuidePositions(
            new Vector3(
                guideX,
                0.20f,
                -0.105f
            ),

            new Vector3(
                guideX,
                0f,
                -0.105f
            )
        );
    }


    // =========================================================
    // HORIZONTAL GUIDE
    // =========================================================

    private void ConfigureSelectedHorizontalGuide()
    {
        float guideY =
            0.10f;


        switch (selectedFraction)
        {
            case C2_SliceFraction.OneHalf:

                guideY = 0.10f;

                break;


            case C2_SliceFraction.OneThird:

                guideY = 0.0667f;

                break;


            case C2_SliceFraction.OneQuarter:

                guideY = 0.05f;

                break;


            case C2_SliceFraction.ThreeQuarter:

                guideY = 0.15f;

                break;
        }


        SetGuidePositions(
            new Vector3(
                -0.10f,
                guideY,
                -0.105f
            ),

            new Vector3(
                0.10f,
                guideY,
                -0.105f
            )
        );
    }


    // =========================================================
    // ANGLED GUIDE
    //
    // All lines use y = x + b.
    // =========================================================

    private void ConfigureSelectedAngledGuide()
    {
        Vector3 startPosition =
            Vector3.zero;


        Vector3 endPosition =
            Vector3.zero;


        switch (selectedFraction)
        {
            // -------------------------------------------------
            // 1/2
            //
            // Full lower-left → upper-right diagonal.
            // -------------------------------------------------

            case C2_SliceFraction.OneHalf:

                startPosition =
                    new Vector3(
                        -0.10f,
                        0f,
                        -0.105f
                    );


                endPosition =
                    new Vector3(
                        0.10f,
                        0.20f,
                        -0.105f
                    );

                break;


            // -------------------------------------------------
            // 1/3
            // -------------------------------------------------

            case C2_SliceFraction.OneThird:

                startPosition =
                    new Vector3(
                        -0.063299f,
                        0f,
                        -0.105f
                    );


                endPosition =
                    new Vector3(
                        0.10f,
                        0.163299f,
                        -0.105f
                    );

                break;


            // -------------------------------------------------
            // 1/4
            // -------------------------------------------------

            case C2_SliceFraction.OneQuarter:

                startPosition =
                    new Vector3(
                        -0.041421f,
                        0f,
                        -0.105f
                    );


                endPosition =
                    new Vector3(
                        0.10f,
                        0.141421f,
                        -0.105f
                    );

                break;


            // -------------------------------------------------
            // 3/4
            // -------------------------------------------------

            case C2_SliceFraction.ThreeQuarter:

                startPosition =
                    new Vector3(
                        -0.10f,
                        0.058579f,
                        -0.105f
                    );


                endPosition =
                    new Vector3(
                        0.041421f,
                        0.20f,
                        -0.105f
                    );

                break;
        }


        SetGuidePositions(
            startPosition,
            endPosition
        );
    }


    // =========================================================
    // SET GUIDE
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
            Debug.LogWarning(
                "C2 Cube Slicing: Current combination cannot be traced."
            );

            return;
        }


        switch (selectedDirection)
        {
            case C2_SlicingDirection.Vertical:

                CompleteVerticalSlice();

                break;


            case C2_SlicingDirection.Horizontal:

                CompleteHorizontalSlice();

                break;


            case C2_SlicingDirection.Angled:

                CompleteAngledSlice();

                break;
        }
    }


    // =========================================================
    // VERTICAL RESULT
    // =========================================================

    private void CompleteVerticalSlice()
    {
        switch (selectedFraction)
        {
            case C2_SliceFraction.OneHalf:

                PerformPhysicalSlice(
                    verticalHalfResult,
                    verticalHalfPieceA,
                    verticalHalfPieceB
                );

                break;


            case C2_SliceFraction.OneThird:

                PerformPhysicalSlice(
                    verticalThirdResult,
                    verticalThirdPieceA,
                    verticalThirdPieceB
                );

                break;


            case C2_SliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    verticalQuarterResult,
                    verticalQuarterPieceA,
                    verticalQuarterPieceB
                );

                break;


            case C2_SliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    verticalThreeQuarterResult,
                    verticalThreeQuarterPieceA,
                    verticalThreeQuarterPieceB
                );

                break;
        }
    }


    // =========================================================
    // HORIZONTAL RESULT
    // =========================================================

    private void CompleteHorizontalSlice()
    {
        switch (selectedFraction)
        {
            case C2_SliceFraction.OneHalf:

                PerformPhysicalSlice(
                    horizontalHalfResult,
                    horizontalHalfBottomRigidbody,
                    horizontalHalfTopRigidbody
                );

                break;


            case C2_SliceFraction.OneThird:

                PerformPhysicalSlice(
                    horizontalThirdResult,
                    horizontalThirdBottomRigidbody,
                    horizontalThirdTopRigidbody
                );

                break;


            case C2_SliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    horizontalQuarterResult,
                    horizontalQuarterBottomRigidbody,
                    horizontalQuarterTopRigidbody
                );

                break;


            case C2_SliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    horizontalThreeQuarterResult,
                    horizontalThreeQuarterBottomRigidbody,
                    horizontalThreeQuarterTopRigidbody
                );

                break;
        }
    }


    // =========================================================
    // ANGLED RESULT
    // =========================================================

    private void CompleteAngledSlice()
    {
        switch (selectedFraction)
        {
            case C2_SliceFraction.OneHalf:

                PerformPhysicalSlice(
                    angledHalfResult,
                    angledHalfPieceA,
                    angledHalfPieceB
                );

                break;


            case C2_SliceFraction.OneThird:

                PerformPhysicalSlice(
                    angledThirdResult,
                    angledThirdPieceA,
                    angledThirdPieceB
                );

                break;


            case C2_SliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    angledQuarterResult,
                    angledQuarterPieceA,
                    angledQuarterPieceB
                );

                break;


            case C2_SliceFraction.ThreeQuarter:

                PerformPhysicalSlice(
                    angledThreeQuarterResult,
                    angledThreeQuarterPieceA,
                    angledThreeQuarterPieceB
                );

                break;
        }
    }


    // =========================================================
    // GENERIC PHYSICAL RESULT
    // =========================================================

    private void PerformPhysicalSlice(
        GameObject resultObject,
        Rigidbody pieceA,
        Rigidbody pieceB
    )
    {
        if (fullCube == null ||
            cubeSliceResults == null ||
            resultObject == null ||
            pieceA == null ||
            pieceB == null)
        {
            Debug.LogError(
                "C2 Cube Slicing: Required slice references are missing."
            );

            return;
        }


        traceCompleted =
            true;


        HideGuide();


        fullCube.SetActive(
            false
        );


        HideAllSliceResults();


        cubeSliceResults.SetActive(
            true
        );


        resultObject.SetActive(
            true
        );


        Physics.SyncTransforms();


        EnableSlicePiecePhysics(
            pieceA
        );


        EnableSlicePiecePhysics(
            pieceB
        );


        Vector3 firstForce =
            (-transform.right * separationImpulse) +
            (transform.up * upwardImpulse);


        Vector3 secondForce =
            (transform.right * separationImpulse) +
            (transform.up * upwardImpulse);


        pieceA.AddForce(
            firstForce,
            ForceMode.Impulse
        );


        pieceB.AddForce(
            secondForce,
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
            "C2 Cube Slicing: " +
            selectedDirection +
            " " +
            selectedFraction +
            " slice completed."
        );
    }


    // =========================================================
    // PHYSICS
    // =========================================================

    private void EnableSlicePiecePhysics(
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


    private void ResetSlicePiece(
        Rigidbody piece,
        C2_PieceInitialState initialState
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
            initialState.localPosition;


        piece.transform.localRotation =
            initialState.localRotation;


        piece.Sleep();
    }


    private void ResetAllSlicePieces()
    {
        foreach (
            KeyValuePair<Rigidbody, C2_PieceInitialState>
            pair in initialPieceStates
        )
        {
            ResetSlicePiece(
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
    // RESTORE FULL CUBE
    // =========================================================

    private void RestoreUnslicedVisualState()
    {
        HideGuide();


        ResetAllSlicePieces();


        HideAllSliceResults();


        if (cubeSliceResults != null)
        {
            cubeSliceResults.SetActive(
                false
            );
        }


        if (fullCube != null)
        {
            fullCube.SetActive(
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
            "C2 Cube Slicing: Try Again completed."
        );
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetCube()
    {
        selectedDirection =
            C2_SlicingDirection.None;


        selectedFraction =
            C2_SliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();


        Debug.Log(
            "C2 Cube Slicing: Cube reset."
        );
    }


    // =========================================================
    // INITIAL STATE
    // =========================================================

    private void PrepareInitialState()
    {
        selectedDirection =
            C2_SlicingDirection.None;


        selectedFraction =
            C2_SliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();
    }
}
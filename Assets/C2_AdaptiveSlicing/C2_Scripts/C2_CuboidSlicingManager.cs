using System.Collections.Generic;
using UnityEngine;

public class C2_CuboidSlicingManager : MonoBehaviour
{
    public enum C2_CuboidSlicingDirection
    {
        None,
        Vertical,
        Horizontal,
        Angled
    }

    public enum C2_CuboidSliceFraction
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

    [Header("Cuboid References")]
    [SerializeField] private GameObject fullCuboid;
    [SerializeField] private GameObject sliceGuide;
    [SerializeField] private GameObject cuboidSliceResults;


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
    private C2_CuboidSlicingDirection selectedDirection =
        C2_CuboidSlicingDirection.None;


    [SerializeField]
    private C2_CuboidSliceFraction selectedFraction =
        C2_CuboidSliceFraction.None;


    [Header("Trace State - Read Only During Play")]

    [SerializeField]
    private bool traceCompleted = false;


    private C2_AppUIManager uiManager;


    // =========================================================
    // INITIAL PHYSICS STATES
    // =========================================================

    private struct C2_CuboidPieceInitialState
    {
        public Vector3 localPosition;
        public Quaternion localRotation;


        public C2_CuboidPieceInitialState(
            Vector3 position,
            Quaternion rotation
        )
        {
            localPosition = position;
            localRotation = rotation;
        }
    }


    private readonly
        Dictionary<Rigidbody, C2_CuboidPieceInitialState>
        initialPieceStates =
            new Dictionary<Rigidbody, C2_CuboidPieceInitialState>();


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public C2_CuboidSlicingDirection SelectedDirection
        => selectedDirection;


    public C2_CuboidSliceFraction SelectedFraction
        => selectedFraction;


    public bool TraceCompleted
        => traceCompleted;


    public bool HasCompleteSelection
    {
        get
        {
            return
                selectedDirection !=
                    C2_CuboidSlicingDirection.None &&
                selectedFraction !=
                    C2_CuboidSliceFraction.None;
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
                    C2_CuboidSlicingDirection.Vertical ||

                selectedDirection ==
                    C2_CuboidSlicingDirection.Horizontal ||

                selectedDirection ==
                    C2_CuboidSlicingDirection.Angled;
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
        C2_CuboidSliceFraction fraction
    )
    {
        return
            fraction ==
                C2_CuboidSliceFraction.OneHalf ||

            fraction ==
                C2_CuboidSliceFraction.OneThird ||

            fraction ==
                C2_CuboidSliceFraction.OneQuarter ||

            fraction ==
                C2_CuboidSliceFraction.ThreeQuarter;
    }


    // =========================================================
    // CAPTURE ALL RIGIDBODIES
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
            new C2_CuboidPieceInitialState(
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
            C2_CuboidSlicingDirection.Vertical;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cuboid: Vertical selected."
        );
    }


    public void SelectHorizontal()
    {
        selectedDirection =
            C2_CuboidSlicingDirection.Horizontal;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cuboid: Horizontal selected."
        );
    }


    public void SelectAngled()
    {
        selectedDirection =
            C2_CuboidSlicingDirection.Angled;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();


        Debug.Log(
            "C2 Cuboid: Angled selected."
        );
    }


    // =========================================================
    // FRACTIONS
    // =========================================================

    public void SelectHalf()
    {
        selectedFraction =
            C2_CuboidSliceFraction.OneHalf;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectThird()
    {
        selectedFraction =
            C2_CuboidSliceFraction.OneThird;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectQuarter()
    {
        selectedFraction =
            C2_CuboidSliceFraction.OneQuarter;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    public void SelectThreeQuarter()
    {
        selectedFraction =
            C2_CuboidSliceFraction.ThreeQuarter;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();

        EvaluateSelection();
    }


    // =========================================================
    // EVALUATE
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
            case C2_CuboidSlicingDirection.Vertical:

                ConfigureSelectedVerticalGuide();

                ShowGuide();

                break;


            case C2_CuboidSlicingDirection.Horizontal:

                ConfigureSelectedHorizontalGuide();

                ShowGuide();

                break;


            case C2_CuboidSlicingDirection.Angled:

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
            case C2_CuboidSliceFraction.OneHalf:

                guideX = 0f;

                break;


            case C2_CuboidSliceFraction.OneThird:

                guideX = -0.05f;

                break;


            case C2_CuboidSliceFraction.OneQuarter:

                guideX = -0.075f;

                break;


            case C2_CuboidSliceFraction.ThreeQuarter:

                guideX = 0.075f;

                break;
        }


        SetGuidePositions(
            new Vector3(
                guideX,
                0.18f,
                -0.085f
            ),

            new Vector3(
                guideX,
                0f,
                -0.085f
            )
        );
    }


    // =========================================================
    // HORIZONTAL GUIDE
    // =========================================================

    private void ConfigureSelectedHorizontalGuide()
    {
        float guideY =
            0.09f;


        switch (selectedFraction)
        {
            case C2_CuboidSliceFraction.OneHalf:

                guideY = 0.09f;

                break;


            case C2_CuboidSliceFraction.OneThird:

                guideY = 0.06f;

                break;


            case C2_CuboidSliceFraction.OneQuarter:

                guideY = 0.045f;

                break;


            case C2_CuboidSliceFraction.ThreeQuarter:

                guideY = 0.135f;

                break;
        }


        SetGuidePositions(
            new Vector3(
                -0.15f,
                guideY,
                -0.085f
            ),

            new Vector3(
                0.15f,
                guideY,
                -0.085f
            )
        );
    }


    // =========================================================
    // ANGLED GUIDE
    // =========================================================

    private void ConfigureSelectedAngledGuide()
    {
        float fraction =
            GetSelectedFractionValue();


        float c =
            CalculateNormalizedAngledOffset(
                fraction
            );


        Vector3 startPosition;

        Vector3 endPosition;


        // Normalized line:
        //
        // v = u + c
        //
        // For c <= 0:
        // starts on bottom and ends on right.
        //
        // For c > 0:
        // starts on left and ends on top.

        if (c <= 0f)
        {
            float startU =
                -c;


            float endV =
                1f + c;


            startPosition =
                new Vector3(
                    -0.15f +
                    (startU * 0.30f),

                    0f,

                    -0.085f
                );


            endPosition =
                new Vector3(
                    0.15f,

                    endV * 0.18f,

                    -0.085f
                );
        }
        else
        {
            float startV =
                c;


            float endU =
                1f - c;


            startPosition =
                new Vector3(
                    -0.15f,

                    startV * 0.18f,

                    -0.085f
                );


            endPosition =
                new Vector3(
                    -0.15f +
                    (endU * 0.30f),

                    0.18f,

                    -0.085f
                );
        }


        SetGuidePositions(
            startPosition,
            endPosition
        );
    }


    private float GetSelectedFractionValue()
    {
        switch (selectedFraction)
        {
            case C2_CuboidSliceFraction.OneHalf:

                return 0.5f;


            case C2_CuboidSliceFraction.OneThird:

                return 1f / 3f;


            case C2_CuboidSliceFraction.OneQuarter:

                return 0.25f;


            case C2_CuboidSliceFraction.ThreeQuarter:

                return 0.75f;
        }


        return 0.5f;
    }


    private float CalculateNormalizedAngledOffset(
        float fraction
    )
    {
        if (fraction <= 0.5f)
        {
            return
                Mathf.Sqrt(
                    2f * fraction
                ) - 1f;
        }


        return
            1f -
            Mathf.Sqrt(
                2f *
                (1f - fraction)
            );
    }


    // =========================================================
    // GUIDE POSITION
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
            case C2_CuboidSlicingDirection.Vertical:

                CompleteVerticalSlice();

                break;


            case C2_CuboidSlicingDirection.Horizontal:

                CompleteHorizontalSlice();

                break;


            case C2_CuboidSlicingDirection.Angled:

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
            case C2_CuboidSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    verticalHalfResult,
                    verticalHalfPieceA,
                    verticalHalfPieceB
                );

                break;


            case C2_CuboidSliceFraction.OneThird:

                PerformPhysicalSlice(
                    verticalThirdResult,
                    verticalThirdPieceA,
                    verticalThirdPieceB
                );

                break;


            case C2_CuboidSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    verticalQuarterResult,
                    verticalQuarterPieceA,
                    verticalQuarterPieceB
                );

                break;


            case C2_CuboidSliceFraction.ThreeQuarter:

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
            case C2_CuboidSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    horizontalHalfResult,
                    horizontalHalfBottomRigidbody,
                    horizontalHalfTopRigidbody
                );

                break;


            case C2_CuboidSliceFraction.OneThird:

                PerformPhysicalSlice(
                    horizontalThirdResult,
                    horizontalThirdBottomRigidbody,
                    horizontalThirdTopRigidbody
                );

                break;


            case C2_CuboidSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    horizontalQuarterResult,
                    horizontalQuarterBottomRigidbody,
                    horizontalQuarterTopRigidbody
                );

                break;


            case C2_CuboidSliceFraction.ThreeQuarter:

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
            case C2_CuboidSliceFraction.OneHalf:

                PerformPhysicalSlice(
                    angledHalfResult,
                    angledHalfPieceA,
                    angledHalfPieceB
                );

                break;


            case C2_CuboidSliceFraction.OneThird:

                PerformPhysicalSlice(
                    angledThirdResult,
                    angledThirdPieceA,
                    angledThirdPieceB
                );

                break;


            case C2_CuboidSliceFraction.OneQuarter:

                PerformPhysicalSlice(
                    angledQuarterResult,
                    angledQuarterPieceA,
                    angledQuarterPieceB
                );

                break;


            case C2_CuboidSliceFraction.ThreeQuarter:

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
        if (fullCuboid == null ||
            cuboidSliceResults == null ||
            resultObject == null ||
            pieceA == null ||
            pieceB == null)
        {
            Debug.LogError(
                "C2 Cuboid: Required slice " +
                "references are missing."
            );

            return;
        }


        traceCompleted =
            true;


        HideGuide();


        fullCuboid.SetActive(
            false
        );


        HideAllSliceResults();


        cuboidSliceResults.SetActive(
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
            "C2 Cuboid: " +
            selectedDirection +
            " " +
            selectedFraction +
            " physical slice completed."
        );
    }


    // =========================================================
    // PHYSICS ENABLE
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
    // RESET PIECE
    // =========================================================

    private void ResetPiece(
        Rigidbody piece,
        C2_CuboidPieceInitialState state
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
            KeyValuePair<Rigidbody, C2_CuboidPieceInitialState>
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
    // RESTORE FULL CUBOID
    // =========================================================

    private void RestoreUnslicedVisualState()
    {
        HideGuide();


        ResetAllSlicePieces();


        HideAllSliceResults();


        if (cuboidSliceResults != null)
        {
            cuboidSliceResults.SetActive(
                false
            );
        }


        if (fullCuboid != null)
        {
            fullCuboid.SetActive(
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
            "C2 Cuboid: Try Again complete."
        );
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetCuboid()
    {
        selectedDirection =
            C2_CuboidSlicingDirection.None;


        selectedFraction =
            C2_CuboidSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();


        Debug.Log(
            "C2 Cuboid: Reset complete."
        );
    }


    // =========================================================
    // INITIAL STATE
    // =========================================================

    private void PrepareInitialState()
    {
        selectedDirection =
            C2_CuboidSlicingDirection.None;


        selectedFraction =
            C2_CuboidSliceFraction.None;


        traceCompleted =
            false;


        RestoreUnslicedVisualState();
    }
}
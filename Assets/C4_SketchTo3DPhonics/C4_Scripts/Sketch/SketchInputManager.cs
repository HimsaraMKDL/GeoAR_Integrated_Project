using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SketchInputManager : MonoBehaviour
{
    // =========================================================
    // SKETCH
    // =========================================================

    [Header("Sketch")]
    [SerializeField] private SketchDrawingPad sketchDrawingPad;


    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]
    [SerializeField] private Button clearButton;
    [SerializeField] private Button continueButton;


    // =========================================================
    // MANAGERS
    // =========================================================

    [Header("Managers")]
    [SerializeField] private LearningFlowManager learningFlowManager;
    [SerializeField] private SentisInferenceManager sentisInferenceManager;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField] private TMP_Text feedbackText;


    // =========================================================
    // CURRENT SKETCH DATA
    // =========================================================

    private string latestSavedSketchPath;
    private float latestSketchSizeRatio;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(
                HandleClearButton
            );
        }


        if (continueButton != null)
        {
            continueButton.onClick.AddListener(
                HandleContinueButton
            );
        }


        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }


    private void OnDestroy()
    {
        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(
                HandleClearButton
            );
        }


        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                HandleContinueButton
            );
        }
    }


    // =========================================================
    // CLEAR BUTTON
    // =========================================================

    private void HandleClearButton()
    {
        if (sketchDrawingPad == null)
        {
            Debug.LogError(
                "SketchInputManager: SketchDrawingPad is not assigned."
            );

            return;
        }


        sketchDrawingPad.ClearDrawing();


        if (feedbackText != null)
        {
            feedbackText.text = "";
        }


        Debug.Log(
            "Sketch cleared."
        );
    }


    // =========================================================
    // CONTINUE BUTTON
    // =========================================================

    private void HandleContinueButton()
    {
        // -----------------------------------------------------
        // CHECK SKETCH DRAWING PAD
        // -----------------------------------------------------

        if (sketchDrawingPad == null)
        {
            Debug.LogError(
                "SketchInputManager: SketchDrawingPad is not assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK WHETHER CHILD HAS DRAWN SOMETHING
        // -----------------------------------------------------

        if (!sketchDrawingPad.HasDrawing)
        {
            if (feedbackText != null)
            {
                feedbackText.text =
                    "Please draw an object before continuing.";
            }


            Debug.LogWarning(
                "Continue pressed without a drawing."
            );


            return;
        }


        // -----------------------------------------------------
        // CHECK AI MANAGER
        // -----------------------------------------------------

        if (sentisInferenceManager == null)
        {
            Debug.LogError(
                "SketchInputManager: SentisInferenceManager is not assigned."
            );


            if (feedbackText != null)
            {
                feedbackText.text =
                    "AI system is not ready.";
            }


            return;
        }


        // -----------------------------------------------------
        // CHECK AI MODEL READY STATE
        // -----------------------------------------------------

        if (!sentisInferenceManager.IsReady)
        {
            Debug.LogWarning(
                "On-device AI is not ready yet."
            );


            if (feedbackText != null)
            {
                feedbackText.text =
                    "AI is loading. Please try again.";
            }


            return;
        }


        // -----------------------------------------------------
        // DISABLE CONTINUE WHILE PROCESSING
        // -----------------------------------------------------

        if (continueButton != null)
        {
            continueButton.interactable =
                false;
        }


        if (feedbackText != null)
        {
            feedbackText.text =
                "Checking your drawing...";
        }


        // =====================================================
        // STEP 1
        // SAVE SKETCH PNG
        // =====================================================

        latestSavedSketchPath =
            sketchDrawingPad.SaveSketchAsPng();


        Debug.Log(
            "Sketch saved to: " +
            latestSavedSketchPath
        );


        // =====================================================
        // STEP 2
        // GET ORIGINAL SKETCH SIZE
        // =====================================================

        latestSketchSizeRatio =
            sketchDrawingPad.GetSketchSizeRatio();


        Debug.Log(
            "Sketch size ratio: " +
            latestSketchSizeRatio.ToString("F2")
        );


        // =====================================================
        // STEP 3
        // START NEW RESEARCH SESSION
        //
        // IMPORTANT:
        // Every submitted drawing gets a NEW SessionID.
        //
        // ParticipantID remains the same.
        //
        // Example:
        // P001 -> Fish -> Session A
        // P001 -> Back
        // P001 -> Sun  -> Session B
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.StartNewSession(
                latestSketchSizeRatio
            );


            Debug.Log(
                "Research session created for submitted sketch."
            );
        }
        else
        {
            Debug.LogWarning(
                "ResearchDataLogger was not found. " +
                "Sketch recognition will continue without research logging."
            );
        }


        // =====================================================
        // STEP 4
        // RUN AI RECOGNITION
        // =====================================================

        sentisInferenceManager.RecognizeSketch(
            sketchDrawingPad.DrawingTexture,
            HandleRecognitionResult
        );
    }


    // =========================================================
    // AI RECOGNITION RESULT
    // =========================================================

    private void HandleRecognitionResult(
        string recognizedLabel,
        float confidence)
    {
        // -----------------------------------------------------
        // ENABLE CONTINUE BUTTON AGAIN
        // -----------------------------------------------------

        if (continueButton != null)
        {
            continueButton.interactable =
                true;
        }


        // -----------------------------------------------------
        // DEBUG AI RESULT
        // -----------------------------------------------------

        Debug.Log(
            "On-device recognized label: " +
            recognizedLabel
        );


        Debug.Log(
            "On-device confidence: " +
            confidence.ToString("F4")
        );


        // =====================================================
        // RESEARCH LOGGING
        //
        // Save:
        // PredictedObject
        // AIConfidence
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.LogAIResult(
                recognizedLabel,
                confidence
            );


            Debug.Log(
                "AI prediction and confidence saved to research log."
            );
        }


        // =====================================================
        // LOW CONFIDENCE / NO VALID LABEL
        // =====================================================

        if (string.IsNullOrEmpty(
                recognizedLabel))
        {
            if (feedbackText != null)
            {
                feedbackText.text =
                    "I am not sure what you drew. " +
                    "Please try drawing it again.";
            }


            Debug.LogWarning(
                "On-device sketch recognition confidence was too low."
            );


            return;
        }


        // =====================================================
        // SUCCESS FEEDBACK
        // =====================================================

        if (feedbackText != null)
        {
            feedbackText.text =
                "Great! I found a " +
                CapitalizeFirstLetter(
                    recognizedLabel
                ) +
                ".";
        }


        // -----------------------------------------------------
        // CHECK LEARNING FLOW MANAGER
        // -----------------------------------------------------

        if (learningFlowManager == null)
        {
            Debug.LogError(
                "SketchInputManager: LearningFlowManager is not assigned."
            );


            return;
        }


        // =====================================================
        // START SPELLING / PRONUNCIATION LEARNING FLOW
        // =====================================================

        learningFlowManager.BeginLearning(
            recognizedLabel,
            latestSketchSizeRatio
        );


        Debug.Log(
            "Learning flow started from on-device AI."
        );
    }


    // =========================================================
    // UTILITY
    // =========================================================

    private string CapitalizeFirstLetter(
        string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }


        if (value.Length == 1)
        {
            return value.ToUpper();
        }


        return char.ToUpper(
            value[0]
        ) +
        value.Substring(1);
    }
}
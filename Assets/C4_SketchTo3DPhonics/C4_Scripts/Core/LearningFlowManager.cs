using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LearningFlowManager : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Managers")]
    [SerializeField] private UIScreenManager uiScreenManager;
    [SerializeField] private ModelDisplayManager modelDisplayManager;

    [Header("Object Information")]
    [SerializeField] private TMP_Text objectTitleText;

    [Header("Task Panels")]
    [SerializeField] private GameObject spellingPanel;
    [SerializeField] private GameObject pronunciationPanel;

    [Header("Task Buttons")]
    [SerializeField] private Button spellingButton;
    [SerializeField] private Button pronunciationButton;

    [Header("Task Status")]
    [SerializeField] private TMP_Text spellingStatusText;
    [SerializeField] private TMP_Text pronunciationStatusText;

    [Header("Spelling UI")]
    [SerializeField] private TMP_InputField wordInputField;
    [SerializeField] private Button checkSpellingButton;
    [SerializeField] private TMP_Text spellingResultText;

    [Header("Pronunciation UI")]
    [SerializeField] private TMP_Text pronunciationResultText;

    [Tooltip("Object containing the 'Press Speak and say the word clearly.' instruction.")]
    [SerializeField] private GameObject pronunciationInstructionObject;

    [Header("Success UI")]
    [SerializeField] private TMP_Text successText;

    [Header("AR")]
    [SerializeField] private GameObject viewInARButton;


    // =========================================================
    // CURRENT LEARNING STATE
    // =========================================================

    private string currentObjectLabel = "";

    private bool spellingDone = false;
    private bool pronunciationDone = false;


    // =========================================================
    // PUBLIC READ-ONLY VALUES
    // =========================================================

    public string CurrentObjectLabel =>
        currentObjectLabel;

    public bool SpellingDone =>
        spellingDone;

    public bool PronunciationDone =>
        pronunciationDone;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }


    // =========================================================
    // BUTTON LISTENERS
    // =========================================================

    private void RegisterButtonListeners()
    {
        if (spellingButton != null)
        {
            spellingButton.onClick.RemoveListener(
                SelectSpellingTask
            );

            spellingButton.onClick.AddListener(
                SelectSpellingTask
            );
        }

        if (pronunciationButton != null)
        {
            pronunciationButton.onClick.RemoveListener(
                SelectPronunciationTask
            );

            pronunciationButton.onClick.AddListener(
                SelectPronunciationTask
            );
        }

        if (checkSpellingButton != null)
        {
            checkSpellingButton.onClick.RemoveListener(
                CheckSpelling
            );

            checkSpellingButton.onClick.AddListener(
                CheckSpelling
            );
        }
    }

    private void RemoveButtonListeners()
    {
        if (spellingButton != null)
        {
            spellingButton.onClick.RemoveListener(
                SelectSpellingTask
            );
        }

        if (pronunciationButton != null)
        {
            pronunciationButton.onClick.RemoveListener(
                SelectPronunciationTask
            );
        }

        if (checkSpellingButton != null)
        {
            checkSpellingButton.onClick.RemoveListener(
                CheckSpelling
            );
        }
    }


    // =========================================================
    // START NEW LEARNING FLOW
    // =========================================================

    public void BeginLearning(
        string objectLabel,
        float sketchSizeRatio)
    {
        if (string.IsNullOrWhiteSpace(objectLabel))
        {
            Debug.LogWarning(
                "BeginLearning received an empty object label."
            );

            return;
        }

        // Reset previous object/task state.
        ResetLearningState();

        // Assign new recognized object label.
        currentObjectLabel =
            objectLabel.Trim().ToLower();

        // Make sure button listeners are still valid.
        RegisterButtonListeners();

        // Set title.
        if (objectTitleText != null)
        {
            objectTitleText.text =
                CapitalizeFirstLetter(
                    currentObjectLabel
                );
        }

        // Open validation/challenge screen.
        if (uiScreenManager != null)
        {
            uiScreenManager.ShowValidationScreen();
        }

        // Display low-opacity initial model.
        if (modelDisplayManager != null)
        {
            modelDisplayManager.DisplayInitialModel(
                currentObjectLabel,
                sketchSizeRatio
            );
        }

        Debug.Log(
            "Learning started for new object: " +
            currentObjectLabel
        );
    }


    // =========================================================
    // SPELLING TASK
    // =========================================================

    private void SelectSpellingTask()
    {
        if (spellingDone)
        {
            return;
        }

        // =====================================================
        // RESEARCH LOGGING:
        // START SPELLING TIMER
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.StartSpellingTask();

            Debug.Log(
                "Research logger: Spelling timer started."
            );
        }

        // Open spelling panel.
        if (spellingPanel != null)
        {
            spellingPanel.SetActive(true);
        }

        // Close pronunciation panel.
        if (pronunciationPanel != null)
        {
            pronunciationPanel.SetActive(false);
        }

        // Clear previous temporary result when reopening.
        if (spellingResultText != null)
        {
            spellingResultText.text = "";
        }

        // Activate text field.
        if (wordInputField != null)
        {
            wordInputField.Select();
            wordInputField.ActivateInputField();
        }

        Debug.Log(
            "Spelling challenge selected."
        );
    }


    private void CheckSpelling()
    {
        if (spellingDone)
        {
            return;
        }

        if (wordInputField == null)
        {
            Debug.LogError(
                "Word Input Field is not assigned."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                currentObjectLabel))
        {
            Debug.LogWarning(
                "No current object label is available for spelling validation."
            );

            return;
        }

        string answer =
            wordInputField.text
                .Trim()
                .ToLower();

        // Empty answer should not count as a real spelling attempt.
        if (string.IsNullOrEmpty(answer))
        {
            if (spellingResultText != null)
            {
                spellingResultText.text =
                    "Please type the word.";
            }

            return;
        }

        bool spellingIsCorrect =
            answer == currentObjectLabel;

        // =====================================================
        // RESEARCH LOGGING:
        // LOG EVERY REAL CHECK ATTEMPT
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance.LogSpellingAttempt(
                answer,
                spellingIsCorrect
            );

            Debug.Log(
                "Research logger: Spelling attempt saved."
            );
        }

        Debug.Log(
            "Spelling check. Typed: " +
            answer +
            " | Expected: " +
            currentObjectLabel
        );

        // =====================================================
        // CORRECT SPELLING
        // =====================================================

        if (spellingIsCorrect)
        {
            spellingDone = true;

            if (spellingResultText != null)
            {
                spellingResultText.text =
                    "Correct!";
            }

            if (spellingStatusText != null)
            {
                spellingStatusText.text =
                    "Spelling: Completed";
            }

            if (spellingButton != null)
            {
                spellingButton.interactable =
                    false;
            }

            if (checkSpellingButton != null)
            {
                checkSpellingButton.interactable =
                    false;
            }

            Debug.Log(
                "Spelling task completed."
            );

            UpdateLearningProgress();
        }

        // =====================================================
        // INCORRECT SPELLING
        // =====================================================

        else
        {
            if (spellingResultText != null)
            {
                spellingResultText.text =
                    "Try again!";
            }

            Debug.Log(
                "Incorrect spelling. Try again."
            );
        }
    }


    // =========================================================
    // PRONUNCIATION TASK
    // =========================================================

    private void SelectPronunciationTask()
    {
        if (pronunciationDone)
        {
            return;
        }

        // IMPORTANT:
        // Research pronunciation timer is NOT started here.
        // It starts when the Speak button is pressed inside
        // OfflinePronunciationManager.StartListening().

        if (pronunciationPanel != null)
        {
            pronunciationPanel.SetActive(true);
        }

        if (spellingPanel != null)
        {
            spellingPanel.SetActive(false);
        }

        if (pronunciationInstructionObject != null)
        {
            pronunciationInstructionObject.SetActive(true);
        }

        // Initially empty.
        // "Ready to listen..." should appear after Speak is pressed.
        if (pronunciationResultText != null)
        {
            pronunciationResultText.text = "";
        }

        Debug.Log(
            "Pronunciation challenge selected. Waiting for Speak button."
        );
    }


    // =========================================================
    // PRONUNCIATION MESSAGE CONTROL
    // =========================================================

    public void SetPronunciationMessage(
        string message)
    {
        if (pronunciationResultText != null)
        {
            pronunciationResultText.text =
                message;
        }
    }


    // =========================================================
    // PRONUNCIATION SUCCESS
    // =========================================================

    public void CompletePronunciationTask()
    {
        if (pronunciationDone)
        {
            return;
        }

        pronunciationDone = true;

        if (pronunciationStatusText != null)
        {
            pronunciationStatusText.text =
                "Pronunciation: Completed";
        }

        if (pronunciationResultText != null)
        {
            pronunciationResultText.text =
                "Pronunciation accepted";
        }

        if (pronunciationButton != null)
        {
            pronunciationButton.interactable =
                false;
        }

        Debug.Log(
            "Pronunciation task completed."
        );

        UpdateLearningProgress();
    }


    // =========================================================
    // PRONUNCIATION FAILED
    // =========================================================

    public void PronunciationFailed(
        string recognizedText)
    {
        if (pronunciationDone)
        {
            return;
        }

        if (pronunciationResultText != null)
        {
            pronunciationResultText.text =
                "Try again. Say \"" +
                CapitalizeFirstLetter(
                    currentObjectLabel
                ) +
                "\" clearly.";
        }

        Debug.Log(
            "Pronunciation failed. Heard: " +
            recognizedText +
            " | Expected: " +
            currentObjectLabel
        );
    }


    // =========================================================
    // LEARNING PROGRESS
    // =========================================================

    private void UpdateLearningProgress()
    {
        int completedTaskCount = 0;

        if (spellingDone)
        {
            completedTaskCount++;
        }

        if (pronunciationDone)
        {
            completedTaskCount++;
        }

        Debug.Log(
            "Learning progress: " +
            completedTaskCount +
            "/2"
        );

        if (modelDisplayManager != null)
        {
            modelDisplayManager.SetProgressStage(
                completedTaskCount
            );
        }

        if (completedTaskCount < 2)
        {
            return;
        }

        // =====================================================
        // BOTH TASKS COMPLETED
        // =====================================================

        if (successText != null)
        {
            successText.gameObject.SetActive(
                true
            );

            successText.text =
                "Excellent! Both challenges completed!";
        }

        if (pronunciationInstructionObject != null)
        {
            pronunciationInstructionObject.SetActive(
                false
            );
        }

        if (pronunciationPanel != null)
        {
            pronunciationPanel.SetActive(
                false
            );
        }

        if (spellingPanel != null)
        {
            spellingPanel.SetActive(
                false
            );
        }

        if (viewInARButton != null)
        {
            viewInARButton.SetActive(
                true
            );
        }

        Debug.Log(
            "Both tasks completed. AR enabled."
        );
    }


    // =========================================================
    // FULL RESET FOR NEXT OBJECT
    // =========================================================

    public void ResetLearningState()
    {
        Debug.Log(
            "Resetting full learning state."
        );

        // -----------------------------------------------------
        // RESET INTERNAL STATE
        // -----------------------------------------------------

        spellingDone = false;
        pronunciationDone = false;

        currentObjectLabel = "";

        // -----------------------------------------------------
        // RE-ENABLE BUTTONS
        // -----------------------------------------------------

        if (spellingButton != null)
        {
            spellingButton.interactable =
                true;
        }

        if (pronunciationButton != null)
        {
            pronunciationButton.interactable =
                true;
        }

        if (checkSpellingButton != null)
        {
            checkSpellingButton.interactable =
                true;
        }

        // -----------------------------------------------------
        // CLEAR SPELLING UI
        // -----------------------------------------------------

        if (wordInputField != null)
        {
            wordInputField.text =
                "";
        }

        if (spellingResultText != null)
        {
            spellingResultText.text =
                "";
        }

        if (spellingStatusText != null)
        {
            spellingStatusText.text =
                "Spelling: Not Completed";
        }

        // -----------------------------------------------------
        // CLEAR PRONUNCIATION UI
        // -----------------------------------------------------

        if (pronunciationResultText != null)
        {
            pronunciationResultText.text =
                "";
        }

        if (pronunciationStatusText != null)
        {
            pronunciationStatusText.text =
                "Pronunciation: Not Completed";
        }

        if (pronunciationInstructionObject != null)
        {
            pronunciationInstructionObject.SetActive(
                true
            );
        }

        // -----------------------------------------------------
        // CLOSE BOTH TASK PANELS
        // -----------------------------------------------------

        if (spellingPanel != null)
        {
            spellingPanel.SetActive(
                false
            );
        }

        if (pronunciationPanel != null)
        {
            pronunciationPanel.SetActive(
                false
            );
        }

        // -----------------------------------------------------
        // RESET SUCCESS UI
        // -----------------------------------------------------

        if (successText != null)
        {
            successText.gameObject.SetActive(
                true
            );

            successText.text =
                "";
        }

        // -----------------------------------------------------
        // HIDE VIEW IN AR
        // -----------------------------------------------------

        if (viewInARButton != null)
        {
            viewInARButton.SetActive(
                false
            );
        }

        Debug.Log(
            "Learning state completely reset."
        );
    }


    // =========================================================
    // PREPARE FOR NEXT SKETCH
    // =========================================================

    public void ResetChallengeSelectionUI()
    {
        Debug.Log(
            "Preparing learning UI for next sketch."
        );

        ResetLearningState();
    }


    // =========================================================
    // RETURN DIRECTLY TO SKETCH PAGE
    // =========================================================

    public void ReturnToSketchScreen()
    {
        ResetLearningState();

        if (uiScreenManager != null)
        {
            uiScreenManager.ShowSketchScreen();
        }

        Debug.Log(
            "Returned to sketch input screen."
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
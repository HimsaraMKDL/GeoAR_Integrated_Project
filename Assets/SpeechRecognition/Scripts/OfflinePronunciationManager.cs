using System;
using UnityEngine;
using UnityEngine.Android;

public class OfflinePronunciationManager : MonoBehaviour
{
    // =========================================================
    // VOSK
    // =========================================================

    [Header("Vosk")]
    [SerializeField] private VoskSpeechToText voskSpeechToText;


    // =========================================================
    // LEARNING FLOW
    // =========================================================

    [Header("Learning Flow")]
    [SerializeField] private LearningFlowManager learningFlowManager;


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Settings")]
    [SerializeField] private bool logAllRecognitionResults = true;

    [SerializeField] private float listeningStartDelay = 1.0f;


    // =========================================================
    // STATE
    // =========================================================

    private bool microphonePermissionGranted = false;

    private bool waitingForPronunciation = false;

    private bool resultProcessedForCurrentAttempt = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (voskSpeechToText != null)
        {
            // We control initialization manually.
            voskSpeechToText.AutoStart = false;
        }
    }


    private void Start()
    {
        CheckMicrophonePermission();


        if (voskSpeechToText != null)
        {
            voskSpeechToText.OnTranscriptionResult -=
                HandleTranscriptionResult;

            voskSpeechToText.OnTranscriptionResult +=
                HandleTranscriptionResult;
        }
        else
        {
            Debug.LogError(
                "OfflinePronunciationManager: VoskSpeechToText is not assigned."
            );
        }
    }


    private void OnDestroy()
    {
        CancelInvoke(
            nameof(ShowListeningMessage)
        );


        if (voskSpeechToText != null)
        {
            voskSpeechToText.OnTranscriptionResult -=
                HandleTranscriptionResult;
        }
    }


    // =========================================================
    // MICROPHONE PERMISSION
    // =========================================================

    private void CheckMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (Permission.HasUserAuthorizedPermission(
                Permission.Microphone))
        {
            microphonePermissionGranted = true;
        }
        else
        {
            microphonePermissionGranted = false;

            Permission.RequestUserPermission(
                Permission.Microphone
            );
        }

#else

        microphonePermissionGranted = true;

#endif
    }


    public bool HasMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        microphonePermissionGranted =
            Permission.HasUserAuthorizedPermission(
                Permission.Microphone
            );

#endif

        return microphonePermissionGranted;
    }


    // =========================================================
    // SPEAK BUTTON
    // =========================================================

    public void StartListening()
    {
        // Prevent multiple presses for same attempt.
        if (waitingForPronunciation)
        {
            Debug.Log(
                "Pronunciation attempt is already active."
            );

            return;
        }


        if (!HasMicrophonePermission())
        {
            CheckMicrophonePermission();


            if (learningFlowManager != null)
            {
                learningFlowManager.SetPronunciationMessage(
                    "Please allow microphone access."
                );
            }


            return;
        }


        if (voskSpeechToText == null)
        {
            Debug.LogError(
                "VoskSpeechToText is not assigned."
            );

            return;
        }


        if (learningFlowManager == null)
        {
            Debug.LogError(
                "LearningFlowManager is not assigned."
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(
                learningFlowManager.CurrentObjectLabel))
        {
            Debug.LogWarning(
                "No pronunciation target word."
            );

            return;
        }


        // =====================================================
        // NEW ATTEMPT
        // =====================================================

        waitingForPronunciation = true;

        resultProcessedForCurrentAttempt = false;


        // =====================================================
        // RESEARCH TIMER
        // =====================================================

        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance
                .StartPronunciationTask();
        }


        learningFlowManager.SetPronunciationMessage(
            "Ready to listen..."
        );


        Debug.Log(
            "Starting pronunciation attempt for: " +
            learningFlowManager.CurrentObjectLabel
        );


        // =====================================================
        // START / REUSE VOSK
        // =====================================================

        if (!voskSpeechToText.IsInitialized &&
            !voskSpeechToText.IsInitializing)
        {
            Debug.Log(
                "Initializing Vosk and starting microphone."
            );


            // IMPORTANT:
            // true = microphone automatically starts
            // after initialization finishes.
            voskSpeechToText.StartVoskStt(
                null,
                default,
                true,
                voskSpeechToText.MaxAlternatives
            );
        }
        else if (voskSpeechToText.IsInitialized)
        {
            Debug.Log(
                "Vosk already initialized. Starting microphone."
            );


            voskSpeechToText.StartRecording();
        }
        else
        {
            Debug.Log(
                "Vosk is currently initializing."
            );
        }


        CancelInvoke(
            nameof(ShowListeningMessage)
        );


        Invoke(
            nameof(ShowListeningMessage),
            listeningStartDelay
        );
    }


    // =========================================================
    // LISTENING MESSAGE
    // =========================================================

    private void ShowListeningMessage()
    {
        if (!waitingForPronunciation)
        {
            return;
        }


        if (learningFlowManager == null)
        {
            return;
        }


        string target =
            learningFlowManager.CurrentObjectLabel;


        learningFlowManager.SetPronunciationMessage(
            "Listening... Say \"" +
            CapitalizeFirstLetter(target) +
            "\" now."
        );
    }


    // =========================================================
    // VOSK RESULT
    // =========================================================

    private void HandleTranscriptionResult(
        string jsonResult)
    {
        if (logAllRecognitionResults)
        {
            Debug.Log(
                "Vosk raw result: " +
                jsonResult
            );
        }


        if (!waitingForPronunciation)
        {
            return;
        }


        if (resultProcessedForCurrentAttempt)
        {
            return;
        }


        if (learningFlowManager == null)
        {
            FinishCurrentAttempt();
            return;
        }


        string targetWord =
            learningFlowManager
                .CurrentObjectLabel
                .Trim()
                .ToLower();


        if (string.IsNullOrEmpty(targetWord))
        {
            FinishCurrentAttempt();
            return;
        }


        RecognitionResult result;


        try
        {
            result =
                new RecognitionResult(
                    jsonResult
                );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not parse Vosk result: " +
                exception.Message
            );


            resultProcessedForCurrentAttempt = true;


            if (ResearchDataLogger.Instance != null)
            {
                ResearchDataLogger.Instance
                    .LogPronunciationAttempt(
                        "",
                        false
                    );
            }


            learningFlowManager.SetPronunciationMessage(
                "I could not understand that. Try again."
            );


            FinishCurrentAttempt();

            return;
        }


        string bestHeardText = "";


        // =====================================================
        // CHECK ALTERNATIVES
        // =====================================================

        foreach (var phrase in result.Phrases)
        {
            if (phrase == null)
            {
                continue;
            }


            string recognizedText =
                phrase.Text
                    .Trim()
                    .ToLower();


            if (string.IsNullOrEmpty(
                    recognizedText))
            {
                continue;
            }


            if (string.IsNullOrEmpty(
                    bestHeardText))
            {
                bestHeardText =
                    recognizedText;
            }


            Debug.Log(
                "Recognized phrase: " +
                recognizedText
            );


            // =================================================
            // CORRECT
            // =================================================

            if (IsTargetWordRecognized(
                    recognizedText,
                    targetWord))
            {
                resultProcessedForCurrentAttempt =
                    true;


                if (ResearchDataLogger.Instance != null)
                {
                    ResearchDataLogger.Instance
                        .LogPronunciationAttempt(
                            recognizedText,
                            true
                        );
                }


                Debug.Log(
                    "Correct pronunciation recognized: " +
                    targetWord
                );


                FinishCurrentAttempt();


                learningFlowManager
                    .CompletePronunciationTask();


                return;
            }
        }


        // =====================================================
        // WRONG
        // =====================================================

        resultProcessedForCurrentAttempt =
            true;


        if (ResearchDataLogger.Instance != null)
        {
            ResearchDataLogger.Instance
                .LogPronunciationAttempt(
                    bestHeardText,
                    false
                );
        }


        Debug.Log(
            "Pronunciation mismatch. Heard: " +
            bestHeardText +
            " | Expected: " +
            targetWord
        );


        FinishCurrentAttempt();


        learningFlowManager.PronunciationFailed(
            bestHeardText
        );
    }


    // =========================================================
    // FINISH ATTEMPT
    // =========================================================

    private void FinishCurrentAttempt()
    {
        CancelInvoke(
            nameof(ShowListeningMessage)
        );


        // Stop only microphone recording.
        // Vosk model stays initialized.
        if (voskSpeechToText != null &&
            voskSpeechToText.IsRecording)
        {
            voskSpeechToText.StopRecording();
        }


        waitingForPronunciation =
            false;


        Debug.Log(
            "Pronunciation attempt finished. " +
            "Vosk remains initialized."
        );
    }


    // =========================================================
    // RESET FOR NEXT OBJECT
    // =========================================================

    public void ResetPronunciationAttempt()
    {
        CancelInvoke(
            nameof(ShowListeningMessage)
        );


        if (voskSpeechToText != null &&
            voskSpeechToText.IsRecording)
        {
            voskSpeechToText.StopRecording();
        }


        waitingForPronunciation =
            false;

        resultProcessedForCurrentAttempt =
            false;


        Debug.Log(
            "Pronunciation manager reset for next attempt."
        );
    }


    // =========================================================
    // WORD MATCH
    // =========================================================

    private bool IsTargetWordRecognized(
        string recognizedText,
        string targetWord)
    {
        if (string.Equals(
                recognizedText,
                targetWord,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }


        string[] words =
            recognizedText.Split(
                new char[]
                {
                    ' ',
                    ',',
                    '.',
                    '!',
                    '?',
                    ';',
                    ':'
                },
                StringSplitOptions.RemoveEmptyEntries
            );


        foreach (string word in words)
        {
            if (string.Equals(
                    word.Trim(),
                    targetWord,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }


        return false;
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
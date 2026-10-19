using System;
using System.IO;
using UnityEngine;

public class ResearchDataLogger : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static ResearchDataLogger Instance { get; private set; }


    // =========================================================
    // PARTICIPANT
    // =========================================================

    [Header("Participant")]
    [SerializeField] private string participantId = "P001";


    // =========================================================
    // CURRENT SESSION
    // =========================================================

    private ResearchSessionData currentSession;

    private float sessionStartTime;
    private float spellingStartTime;
    private float pronunciationStartTime;


    // =========================================================
    // FILE PATHS
    // =========================================================

    private string researchFolderPath;
    private string csvFilePath;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        DontDestroyOnLoad(
            gameObject
        );


        researchFolderPath =
            Path.Combine(
                Application.persistentDataPath,
                "ResearchLogs"
            );


        if (!Directory.Exists(
                researchFolderPath))
        {
            Directory.CreateDirectory(
                researchFolderPath
            );
        }


        csvFilePath =
            Path.Combine(
                researchFolderPath,
                "C4_FreeDraw_UserTesting.csv"
            );


        CreateCsvHeaderIfNeeded();


        Debug.Log(
            "Research logger ready."
        );


        Debug.Log(
            "Research data folder: " +
            researchFolderPath
        );
    }


    // =========================================================
    // PARTICIPANT ID
    // =========================================================

    public void SetParticipantId(
        string id)
    {
        if (string.IsNullOrWhiteSpace(
                id))
        {
            return;
        }


        participantId =
            id.Trim()
                .ToUpper();


        Debug.Log(
            "Participant changed to: " +
            participantId
        );
    }


    // =========================================================
    // START NEW SESSION
    // =========================================================

    public void StartNewSession(
        float sketchSizeRatio)
    {
        currentSession =
            new ResearchSessionData();


        currentSession.participantId =
            participantId;


        // =====================================================
        // CREATE SEQUENTIAL SESSION ID
        //
        // Example:
        //
        // P001_S001
        // P001_S002
        // P001_S003
        //
        // =====================================================

        int nextSessionNumber =
            GetNextSessionNumber(
                participantId
            );


        currentSession.sessionId =
            participantId +
            "_S" +
            nextSessionNumber.ToString(
                "D3"
            );


        currentSession.startTimestamp =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );


        currentSession.endTimestamp =
            "";


        currentSession.sketchSizeRatio =
            sketchSizeRatio;


        // -----------------------------------------------------
        // AI
        // -----------------------------------------------------

        currentSession.predictedObject =
            "";

        currentSession.aiConfidence =
            0f;


        // -----------------------------------------------------
        // SPELLING
        // -----------------------------------------------------

        currentSession.spellingAttempts =
            0;

        currentSession.spellingCompleted =
            false;

        currentSession.lastSpellingInput =
            "";

        currentSession
            .spellingCompletionTimeSeconds =
            0f;


        // -----------------------------------------------------
        // PRONUNCIATION
        // -----------------------------------------------------

        currentSession.pronunciationAttempts =
            0;

        currentSession.pronunciationAccepted =
            false;

        currentSession.lastRecognizedSpeech =
            "";

        currentSession
            .pronunciationCompletionTimeSeconds =
            0f;


        // -----------------------------------------------------
        // AR
        // -----------------------------------------------------

        currentSession.arOpened =
            false;

        currentSession.arModelPlaced =
            false;


        // -----------------------------------------------------
        // SESSION
        // -----------------------------------------------------

        currentSession.totalSessionTimeSeconds =
            0f;

        currentSession.sessionCompleted =
            false;


        sessionStartTime =
            Time.realtimeSinceStartup;


        spellingStartTime =
            0f;


        pronunciationStartTime =
            0f;


        SaveCurrentSessionJson();


        Debug.Log(
            "New research session started. " +
            "Participant: " +
            participantId +
            " | Session: " +
            currentSession.sessionId
        );
    }


    // =========================================================
    // GET NEXT SESSION NUMBER
    // =========================================================

    private int GetNextSessionNumber(
        string participant)
    {
        if (!Directory.Exists(
                researchFolderPath))
        {
            return 1;
        }


        string searchPattern =
            participant +
            "_S*.json";


        string[] existingFiles =
            Directory.GetFiles(
                researchFolderPath,
                searchPattern
            );


        int highestSessionNumber =
            0;


        foreach (string filePath
                 in existingFiles)
        {
            string fileName =
                Path.GetFileNameWithoutExtension(
                    filePath
                );


            // Example:
            // P001_S003


            int sessionIndex =
                fileName.LastIndexOf(
                    "_S",
                    StringComparison.OrdinalIgnoreCase
                );


            if (sessionIndex < 0)
            {
                continue;
            }


            string numberPart =
                fileName.Substring(
                    sessionIndex + 2
                );


            if (int.TryParse(
                    numberPart,
                    out int sessionNumber))
            {
                if (sessionNumber >
                    highestSessionNumber)
                {
                    highestSessionNumber =
                        sessionNumber;
                }
            }
        }


        return highestSessionNumber + 1;
    }


    // =========================================================
    // AI RESULT
    // =========================================================

    public void LogAIResult(
        string predictedObject,
        float confidence)
    {
        if (!HasActiveSession())
        {
            return;
        }


        currentSession.predictedObject =
            predictedObject ?? "";


        currentSession.aiConfidence =
            confidence;


        SaveCurrentSessionJson();


        Debug.Log(
            "AI result logged: " +
            currentSession.predictedObject +
            " | Confidence: " +
            confidence
        );
    }


    // =========================================================
    // SPELLING
    // =========================================================

    public void StartSpellingTask()
    {
        if (!HasActiveSession())
        {
            return;
        }


        if (spellingStartTime <= 0f)
        {
            spellingStartTime =
                Time.realtimeSinceStartup;


            Debug.Log(
                "Spelling timer started."
            );
        }
    }


    public void LogSpellingAttempt(
        string typedWord,
        bool completed)
    {
        if (!HasActiveSession())
        {
            return;
        }


        currentSession.spellingAttempts++;


        currentSession.lastSpellingInput =
            typedWord ?? "";


        if (completed)
        {
            currentSession.spellingCompleted =
                true;


            if (spellingStartTime > 0f)
            {
                currentSession
                    .spellingCompletionTimeSeconds =
                    Time.realtimeSinceStartup -
                    spellingStartTime;
            }
        }


        SaveCurrentSessionJson();


        Debug.Log(
            "Spelling attempt logged. " +
            "Attempts: " +
            currentSession.spellingAttempts +
            " | Completed: " +
            completed
        );
    }


    // =========================================================
    // PRONUNCIATION
    // =========================================================

    public void StartPronunciationTask()
    {
        if (!HasActiveSession())
        {
            return;
        }


        if (pronunciationStartTime <= 0f)
        {
            pronunciationStartTime =
                Time.realtimeSinceStartup;


            Debug.Log(
                "Pronunciation timer started."
            );
        }
    }


    public void LogPronunciationAttempt(
        string recognizedText,
        bool accepted)
    {
        if (!HasActiveSession())
        {
            return;
        }


        currentSession.pronunciationAttempts++;


        currentSession.lastRecognizedSpeech =
            recognizedText ?? "";


        if (accepted)
        {
            currentSession.pronunciationAccepted =
                true;


            if (pronunciationStartTime > 0f)
            {
                currentSession
                    .pronunciationCompletionTimeSeconds =
                    Time.realtimeSinceStartup -
                    pronunciationStartTime;
            }
        }


        SaveCurrentSessionJson();


        Debug.Log(
            "Pronunciation attempt logged. " +
            "Attempts: " +
            currentSession.pronunciationAttempts +
            " | Accepted: " +
            accepted
        );
    }


    // =========================================================
    // AR
    // =========================================================

    public void LogAROpened()
    {
        if (!HasActiveSession())
        {
            return;
        }


        currentSession.arOpened =
            true;


        SaveCurrentSessionJson();


        Debug.Log(
            "AR opened logged."
        );
    }


    public void LogARModelPlaced()
    {
        if (!HasActiveSession())
        {
            return;
        }


        currentSession.arModelPlaced =
            true;


        SaveCurrentSessionJson();


        Debug.Log(
            "AR model placement logged."
        );
    }


    // =========================================================
    // COMPLETE SESSION
    // =========================================================

    public void CompleteSession()
    {
        if (!HasActiveSession())
        {
            return;
        }


        if (currentSession.sessionCompleted)
        {
            Debug.Log(
                "Session already completed: " +
                currentSession.sessionId
            );

            return;
        }


        currentSession.sessionCompleted =
            true;


        currentSession.endTimestamp =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );


        currentSession.totalSessionTimeSeconds =
            Time.realtimeSinceStartup -
            sessionStartTime;


        SaveCurrentSessionJson();


        AppendCurrentSessionToCsv();


        Debug.Log(
            "Research session completed. " +
            "Participant: " +
            currentSession.participantId +
            " | Session: " +
            currentSession.sessionId
        );
    }


    // =========================================================
    // ACTIVE SESSION CHECK
    // =========================================================

    private bool HasActiveSession()
    {
        if (currentSession == null)
        {
            Debug.LogWarning(
                "Research logger: " +
                "No active session."
            );


            return false;
        }


        return true;
    }


    // =========================================================
    // SAVE JSON
    // =========================================================

    private void SaveCurrentSessionJson()
    {
        if (currentSession == null)
        {
            return;
        }


        string json =
            JsonUtility.ToJson(
                currentSession,
                true
            );


        // Session ID already contains participant:
        // P001_S001.json

        string fileName =
            currentSession.sessionId +
            ".json";


        string fullPath =
            Path.Combine(
                researchFolderPath,
                fileName
            );


        File.WriteAllText(
            fullPath,
            json
        );
    }


    // =========================================================
    // CSV HEADER
    // =========================================================

    private void CreateCsvHeaderIfNeeded()
    {
        if (File.Exists(
                csvFilePath))
        {
            return;
        }


        string header =
            "ParticipantID," +
            "SessionID," +
            "StartTimestamp," +
            "EndTimestamp," +

            "PredictedObject," +
            "AIConfidence," +
            "SketchSizeRatio," +

            "SpellingAttempts," +
            "SpellingCompleted," +
            "LastSpellingInput," +
            "SpellingCompletionTimeSeconds," +

            "PronunciationAttempts," +
            "PronunciationAccepted," +
            "LastRecognizedSpeech," +
            "PronunciationCompletionTimeSeconds," +

            "AROpened," +
            "ARModelPlaced," +

            "TotalSessionTimeSeconds," +
            "SessionCompleted";


        File.WriteAllText(
            csvFilePath,
            header +
            Environment.NewLine
        );
    }


    // =========================================================
    // APPEND CSV ROW
    // =========================================================

    private void AppendCurrentSessionToCsv()
    {
        if (currentSession == null)
        {
            return;
        }


        string row =

            Csv(
                currentSession.participantId
            ) + "," +

            Csv(
                currentSession.sessionId
            ) + "," +

            Csv(
                currentSession.startTimestamp
            ) + "," +

            Csv(
                currentSession.endTimestamp
            ) + "," +


            Csv(
                currentSession.predictedObject
            ) + "," +

            currentSession
                .aiConfidence
                .ToString("F4") + "," +

            currentSession
                .sketchSizeRatio
                .ToString("F4") + "," +


            currentSession
                .spellingAttempts + "," +

            currentSession
                .spellingCompleted + "," +

            Csv(
                currentSession.lastSpellingInput
            ) + "," +

            currentSession
                .spellingCompletionTimeSeconds
                .ToString("F2") + "," +


            currentSession
                .pronunciationAttempts + "," +

            currentSession
                .pronunciationAccepted + "," +

            Csv(
                currentSession.lastRecognizedSpeech
            ) + "," +

            currentSession
                .pronunciationCompletionTimeSeconds
                .ToString("F2") + "," +


            currentSession
                .arOpened + "," +

            currentSession
                .arModelPlaced + "," +


            currentSession
                .totalSessionTimeSeconds
                .ToString("F2") + "," +

            currentSession
                .sessionCompleted;


        File.AppendAllText(
            csvFilePath,
            row +
            Environment.NewLine
        );
    }


    // =========================================================
    // CSV ESCAPE
    // =========================================================

    private string Csv(
        string value)
    {
        if (value == null)
        {
            return "\"\"";
        }


        value =
            value.Replace(
                "\"",
                "\"\""
            );


        return "\"" +
               value +
               "\"";
    }


    // =========================================================
    // GET SAVE PATH
    // =========================================================

    public string GetResearchFolderPath()
    {
        return researchFolderPath;
    }
}


// =============================================================
// DATA STRUCTURE
// =============================================================

[Serializable]
public class ResearchSessionData
{
    public string participantId;

    public string sessionId;


    public string startTimestamp;

    public string endTimestamp;


    public string predictedObject;

    public float aiConfidence;

    public float sketchSizeRatio;


    public int spellingAttempts;

    public bool spellingCompleted;

    public string lastSpellingInput;

    public float spellingCompletionTimeSeconds;


    public int pronunciationAttempts;

    public bool pronunciationAccepted;

    public string lastRecognizedSpeech;

    public float pronunciationCompletionTimeSeconds;


    public bool arOpened;

    public bool arModelPlaced;


    public float totalSessionTimeSeconds;

    public bool sessionCompleted;
}
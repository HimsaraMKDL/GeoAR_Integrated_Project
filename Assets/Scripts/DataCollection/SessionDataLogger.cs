using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SessionDataLogger : MonoBehaviour
{
    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Data Settings")]
    [SerializeField]
    private int maximumStudents = 50;

    [SerializeField]
    private int dataExpiryHours = 24;


    // =========================================================
    // DATA PATH
    // =========================================================

    private string researchDataFolder;


    // =========================================================
    // SINGLETON
    // =========================================================

    private static SessionDataLogger instance;


    // =========================================================
    // CURRENT SESSION
    // =========================================================

    private string currentActivity;

    private string currentSessionStudentID;

    private DateTime currentAttemptStartTime;

    private int currentAttemptNumber = 0;

    private bool attemptRunning = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);


        researchDataFolder =
            Path.Combine(
                Application.persistentDataPath,
                "ResearchData"
            );


        EnsureResearchDataFolder();

        RemoveExpiredData();
    }


    // =========================================================
    // PUBLIC ACCESS
    // =========================================================

    public static SessionDataLogger Instance
    {
        get
        {
            return instance;
        }
    }


    // =========================================================
    // START ACTIVITY SESSION
    // =========================================================

    public void StartActivitySession(
        string activityName)
    {
        string studentID =
            StudentProfileManager.CurrentStudentID;


        if (string.IsNullOrEmpty(studentID))
        {
            Debug.LogWarning(
                "SessionDataLogger: No current student ID."
            );

            return;
        }


        if (string.IsNullOrEmpty(activityName))
        {
            Debug.LogWarning(
                "SessionDataLogger: Activity name is empty."
            );

            return;
        }


        // =====================================================
        // IMPORTANT FIX
        // =====================================================
        /*
         * If the SAME student is continuing the SAME activity,
         * do NOT reset the attempt counter.
         *
         * This is important when the student presses
         * Try Again and starts drawing the same activity again.
         */

        if (
            currentSessionStudentID == studentID &&
            currentActivity == activityName
        )
        {
            Debug.Log(
                "Existing activity session continued: " +
                activityName +
                " | Student: " +
                studentID +
                " | Current attempt: " +
                currentAttemptNumber
            );

            return;
        }


        // =====================================================
        // NEW STUDENT OR NEW ACTIVITY
        // =====================================================

        currentSessionStudentID =
            studentID;

        currentActivity =
            activityName;

        currentAttemptNumber =
            0;

        attemptRunning =
            false;


        Debug.Log(
            "Activity session started: " +
            activityName +
            " | Student: " +
            studentID
        );
    }


    // =========================================================
    // START ATTEMPT
    // =========================================================

    public void StartAttempt()
    {
        string studentID =
            StudentProfileManager.CurrentStudentID;


        if (string.IsNullOrEmpty(studentID))
        {
            Debug.LogWarning(
                "SessionDataLogger: No current student ID."
            );

            return;
        }


        if (string.IsNullOrEmpty(currentActivity))
        {
            Debug.LogWarning(
                "SessionDataLogger: No activity session started."
            );

            return;
        }


        /*
         * Safety:
         * If a different student is now active,
         * automatically start a fresh activity session.
         */

        if (
            currentSessionStudentID != studentID
        )
        {
            StartActivitySession(
                currentActivity
            );
        }


        currentAttemptNumber++;

        currentAttemptStartTime =
            DateTime.Now;

        attemptRunning =
            true;


        Debug.Log(
            "Attempt started: " +
            currentAttemptNumber +
            " | " +
            currentActivity +
            " | Student: " +
            studentID
        );
    }


    // =========================================================
    // LOG VALIDATION
    // =========================================================

    public void LogValidation(
        string activityName,
        bool isValid,
        string validationMessage,
        int facesDetected)
    {
        string studentID =
            StudentProfileManager.CurrentStudentID;


        if (string.IsNullOrEmpty(studentID))
        {
            Debug.LogWarning(
                "SessionDataLogger: Cannot save validation. " +
                "No current student ID."
            );

            return;
        }


        if (string.IsNullOrEmpty(activityName))
        {
            Debug.LogWarning(
                "SessionDataLogger: Activity name is empty."
            );

            return;
        }


        // =====================================================
        // START / CONTINUE ACTIVITY SESSION
        // =====================================================

        if (
            currentSessionStudentID != studentID ||
            currentActivity != activityName
        )
        {
            StartActivitySession(
                activityName
            );
        }


        // =====================================================
        // START ATTEMPT IF NECESSARY
        // =====================================================

        if (!attemptRunning)
        {
            StartAttempt();
        }


        DateTime validationTime =
            DateTime.Now;


        double timeTaken =
            (
                validationTime -
                currentAttemptStartTime
            ).TotalSeconds;


        if (timeTaken < 0)
        {
            timeTaken = 0;
        }


        // =====================================================
        // CREATE ATTEMPT DATA
        // =====================================================

        ValidationAttemptData attempt =
            new ValidationAttemptData();


        attempt.attemptNumber =
            currentAttemptNumber;


        attempt.timestamp =
            validationTime.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );


        attempt.validationResult =
            isValid
                ? "Valid"
                : "Invalid";


        attempt.validationMessage =
            validationMessage;


        attempt.facesDetected =
            facesDetected;


        attempt.timeTakenSeconds =
            Math.Round(
                timeTaken,
                2
            );


        attempt.helpUsed =
            false;


        attempt.correctionViewed =
            false;


        attempt.retryPressed =
            false;


        attempt.threeDGenerated =
            false;


        attempt.arPlaced =
            false;


        // =====================================================
        // SAVE
        // =====================================================

        SaveValidationAttempt(
            studentID,
            activityName,
            attempt
        );


        // =====================================================
        // ATTEMPT FINISHED
        // =====================================================

        attemptRunning =
            false;


        Debug.Log(
            "Validation saved | " +
            "Student: " +
            studentID +
            " | Activity: " +
            activityName +
            " | Attempt: " +
            currentAttemptNumber +
            " | Result: " +
            attempt.validationResult
        );
    }


    // =========================================================
    // LOG HELP USAGE
    // =========================================================

    public void LogHelpUsed()
    {
        UpdateLatestAttempt(
            data =>
            {
                data.helpUsed = true;
            }
        );
    }


    // =========================================================
    // LOG CORRECTION VIEWED
    // =========================================================

    public void LogCorrectionViewed()
    {
        UpdateLatestAttempt(
            data =>
            {
                data.correctionViewed = true;
            }
        );
    }


    // =========================================================
    // LOG RETRY
    // =========================================================

    public void LogRetry()
    {
        UpdateLatestAttempt(
            data =>
            {
                data.retryPressed = true;
            }
        );
    }


    // =========================================================
    // LOG 3D GENERATION
    // =========================================================

    public void Log3DGeneration()
    {
        UpdateLatestAttempt(
            data =>
            {
                data.threeDGenerated = true;
            }
        );
    }


    // =========================================================
    // LOG AR PLACEMENT
    // =========================================================

    public void LogARPlacement()
    {
        UpdateLatestAttempt(
            data =>
            {
                data.arPlaced = true;
            }
        );
    }


    // =========================================================
    // SAVE VALIDATION ATTEMPT
    // =========================================================

    private void SaveValidationAttempt(
        string studentID,
        string activityName,
        ValidationAttemptData attempt)
    {
        string studentFolder =
            Path.Combine(
                researchDataFolder,
                studentID
            );


        if (!Directory.Exists(studentFolder))
        {
            Directory.CreateDirectory(
                studentFolder
            );
        }


        string filePath =
            Path.Combine(
                studentFolder,
                activityName +
                "_Session.json"
            );


        ActivitySessionData session;


        // =====================================================
        // LOAD EXISTING SESSION
        // =====================================================

        if (File.Exists(filePath))
        {
            try
            {
                string existingJson =
                    File.ReadAllText(
                        filePath
                    );


                session =
                    JsonUtility.FromJson<ActivitySessionData>(
                        existingJson
                    );
            }
            catch
            {
                session =
                    CreateNewActivitySession(
                        studentID,
                        activityName
                    );
            }
        }
        else
        {
            session =
                CreateNewActivitySession(
                    studentID,
                    activityName
                );
        }


        // =====================================================
        // SAFETY
        // =====================================================

        if (session == null)
        {
            session =
                CreateNewActivitySession(
                    studentID,
                    activityName
                );
        }


        if (session.attempts == null)
        {
            session.attempts =
                new List<ValidationAttemptData>();
        }


        // =====================================================
        // ADD ATTEMPT
        // =====================================================

        session.attempts.Add(
            attempt
        );


        session.lastUpdated =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );


        // =====================================================
        // SAVE JSON
        // =====================================================

        string json =
            JsonUtility.ToJson(
                session,
                true
            );


        File.WriteAllText(
            filePath,
            json
        );
    }


    // =========================================================
    // CREATE NEW ACTIVITY SESSION
    // =========================================================

    private ActivitySessionData
        CreateNewActivitySession(
            string studentID,
            string activityName)
    {
        ActivitySessionData session =
            new ActivitySessionData();


        session.studentID =
            studentID;


        session.activity =
            activityName;


        session.createdAt =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );


        session.lastUpdated =
            session.createdAt;


        session.attempts =
            new List<ValidationAttemptData>();


        return session;
    }


    // =========================================================
    // UPDATE LATEST ATTEMPT
    // =========================================================

    private void UpdateLatestAttempt(
        Action<ValidationAttemptData> updateAction)
    {
        string studentID =
            StudentProfileManager.CurrentStudentID;


        if (string.IsNullOrEmpty(studentID))
        {
            Debug.LogWarning(
                "SessionDataLogger: No current student ID."
            );

            return;
        }


        if (string.IsNullOrEmpty(currentActivity))
        {
            Debug.LogWarning(
                "SessionDataLogger: No current activity."
            );

            return;
        }


        string filePath =
            Path.Combine(
                researchDataFolder,
                studentID,
                currentActivity +
                "_Session.json"
            );


        if (!File.Exists(filePath))
        {
            Debug.LogWarning(
                "SessionDataLogger: Session file not found."
            );

            return;
        }


        try
        {
            string json =
                File.ReadAllText(
                    filePath
                );


            ActivitySessionData session =
                JsonUtility.FromJson<ActivitySessionData>(
                    json
                );


            if (
                session == null ||
                session.attempts == null ||
                session.attempts.Count == 0
            )
            {
                return;
            }


            ValidationAttemptData latestAttempt =
                session.attempts[
                    session.attempts.Count - 1
                ];


            updateAction(
                latestAttempt
            );


            session.lastUpdated =
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss"
                );


            string updatedJson =
                JsonUtility.ToJson(
                    session,
                    true
                );


            File.WriteAllText(
                filePath,
                updatedJson
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "SessionDataLogger update failed: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // ENSURE DATA FOLDER
    // =========================================================

    private void EnsureResearchDataFolder()
    {
        if (
            !Directory.Exists(
                researchDataFolder
            )
        )
        {
            Directory.CreateDirectory(
                researchDataFolder
            );
        }
    }


    // =========================================================
    // 24 HOUR EXPIRY
    // =========================================================

    public void RemoveExpiredData()
    {
        if (
            !Directory.Exists(
                researchDataFolder
            )
        )
        {
            return;
        }


        string[] studentFolders =
            Directory.GetDirectories(
                researchDataFolder
            );


        DateTime now =
            DateTime.Now;


        foreach (
            string folder
            in studentFolders
        )
        {
            try
            {
                string[] files =
                    Directory.GetFiles(
                        folder,
                        "*.json"
                    );


                bool hasRecentFile =
                    false;


                foreach (
                    string file
                    in files
                )
                {
                    DateTime lastWriteTime =
                        File.GetLastWriteTime(
                            file
                        );


                    TimeSpan age =
                        now -
                        lastWriteTime;


                    if (
                        age.TotalHours <
                        dataExpiryHours
                    )
                    {
                        hasRecentFile =
                            true;

                        break;
                    }
                }


                // =================================================
                // DELETE EXPIRED STUDENT DATA
                // =================================================

                if (!hasRecentFile)
                {
                    Directory.Delete(
                        folder,
                        true
                    );


                    Debug.Log(
                        "Expired research data removed: " +
                        Path.GetFileName(folder)
                    );
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Could not remove expired data: " +
                    exception.Message
                );
            }
        }
    }


    // =========================================================
    // GET DATA PATH
    // =========================================================

    public string GetResearchDataPath()
    {
        return researchDataFolder;
    }


    // =========================================================
    // GET CURRENT STUDENT ID
    // =========================================================

    public string GetCurrentStudentID()
    {
        return
            StudentProfileManager.CurrentStudentID;
    }


    // =========================================================
    // DATA CLASSES
    // =========================================================

    [Serializable]
    public class ActivitySessionData
    {
        public string studentID;

        public string activity;

        public string createdAt;

        public string lastUpdated;

        public List<ValidationAttemptData>
            attempts;
    }


    [Serializable]
    public class ValidationAttemptData
    {
        public int attemptNumber;

        public string timestamp;

        public string validationResult;

        public string validationMessage;

        public int facesDetected;

        public double timeTakenSeconds;

        public bool helpUsed;

        public bool correctionViewed;

        public bool retryPressed;

        public bool threeDGenerated;

        public bool arPlaced;
    }
}
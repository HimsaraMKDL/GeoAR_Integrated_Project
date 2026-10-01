using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Name-based login compatible with the existing serialized InputField, Text,
/// Button and C2_AppUIManager. The logger stores the entered name in StudentID,
/// with an independent SessionID. No ST0001 validation and no forced uppercase.
/// </summary>
public class C2_StudentSessionManager : MonoBehaviour
{
    [Header("Existing Login UI References")]
    [SerializeField] private InputField studentIdInputField;
    [SerializeField] private Text loginErrorText;
    [SerializeField] private Button loginContinueButton;

    [Header("Application References")]
    [SerializeField] private C2_AppUIManager uiManager;
    [SerializeField] private C2_ResearchDataLogger researchDataLogger;
    // Optional: assign the AR-placement GameObject to clean up between users.
    // A GameObject reference avoids introducing an additional script dependency.
    [SerializeField] private GameObject placementManagerObject;

    [Header("Current Session - Read Only During Play")]
    [SerializeField] private bool sessionActive;
    [SerializeField] private bool sessionCompleted;
    [SerializeField] private string currentStudentId = "";
    [SerializeField] private string currentSessionId = "";
    [SerializeField] private string sessionStartLocal = "";
    [SerializeField] private string sessionStartUtc = "";
    [SerializeField] private double finalSessionSeconds;

    private DateTimeOffset sessionStartUtcValue;

    public bool SessionActive => sessionActive;
    public bool SessionCompleted => sessionCompleted;
    public string CurrentStudentId => currentStudentId;
    public string CurrentSessionId => currentSessionId;
    public string SessionStartLocal => sessionStartLocal;
    public string SessionStartUtc => sessionStartUtc;
    public double ElapsedSessionSeconds => sessionActive
        ? Math.Max(0.0, (DateTimeOffset.UtcNow - sessionStartUtcValue).TotalSeconds)
        : sessionCompleted ? finalSessionSeconds : 0.0;

    private void Start()
    {
        if (researchDataLogger == null)
            researchDataLogger = C2_ResearchDataLogger.Instance;
        PrepareLoginUI();
    }

    private void PrepareLoginUI()
    {
        if (studentIdInputField != null)
        {
            studentIdInputField.contentType = InputField.ContentType.Standard;
            studentIdInputField.lineType = InputField.LineType.SingleLine;
            studentIdInputField.characterLimit = 50;
            studentIdInputField.readOnly = false;
            studentIdInputField.text = "";
        }
        ClearLoginError();
        if (loginContinueButton != null) loginContinueButton.interactable = true;
    }

    public void LoginStudent()
    {
        if (studentIdInputField == null || uiManager == null)
        {
            Debug.LogError("C2 Login: InputField or UI Manager not assigned.");
            return;
        }
        if (researchDataLogger == null)
            researchDataLogger = C2_ResearchDataLogger.Instance;
        if (researchDataLogger == null)
        {
            ShowLoginError("Research logger is missing. Contact the researcher.");
            Debug.LogError("C2 Login: ResearchDataLogger not found.");
            return;
        }

        string enteredName = NormalizeName(studentIdInputField.text);
        studentIdInputField.text = enteredName;
        if (string.IsNullOrEmpty(enteredName))
        {
            ShowLoginError("Please enter your name to continue.");
            return;
        }
        if (sessionActive)
        {
            // Preserve the active student's data if the login panel reappears.
            if (!string.Equals(enteredName, currentStudentId, StringComparison.Ordinal))
            {
                ShowLoginError("Finish the current session before changing users.");
                return;
            }
            ClearLoginError();
            uiManager.ShowScanPanel();
            return;
        }
        if (sessionCompleted)
        {
            ShowLoginError("Use NEXT STUDENT before starting another session.");
            return;
        }

        DateTimeOffset localNow = DateTimeOffset.Now;
        sessionStartUtcValue = DateTimeOffset.UtcNow;
        currentStudentId = enteredName;
        currentSessionId = "SESSION_" +
            localNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" +
            Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        sessionStartLocal = localNow.ToString(
            "yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        sessionStartUtc = sessionStartUtcValue.ToString(
            "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        finalSessionSeconds = 0;

        if (!researchDataLogger.BeginSession(currentStudentId, currentSessionId,
            sessionStartLocal, sessionStartUtc))
        {
            ResetSessionState();
            ShowLoginError("Research files could not be opened. Contact the researcher.");
            return;
        }
        sessionActive = true;
        researchDataLogger.LogEvent("LOGIN_SUCCESS", result: "Success",
            notes: "Name accepted.");
        ClearLoginError();
        // If the D4 UI Manager is present it records SCAN_STARTED here.
        uiManager.ShowScanPanel();
    }

    public void EndCurrentSession()
    {
        if (!sessionActive || sessionCompleted) return;
        if (researchDataLogger == null) return;
        // These optional UI hooks work with the original D5 UI Manager;
        // they do not cause compilation failures in earlier integrations.
        if (uiManager != null)
            uiManager.SendMessage("PrepareForSessionEnd", SendMessageOptions.DontRequireReceiver);

        double duration = Math.Max(0,
            (DateTimeOffset.UtcNow - sessionStartUtcValue).TotalSeconds);
        bool saved = researchDataLogger.EndSession(
            sessionStartLocal, sessionStartUtc, "CompletedByResearcher");
        if (!saved)
        {
            Debug.LogError("C2 Research: End Session failed. Do not clear this user yet.");
            return;
        }
        finalSessionSeconds = duration;
        sessionActive = false;
        sessionCompleted = true;
        if (uiManager != null)
        {
            uiManager.SendMessage("SetExportTitleText", "SESSION COMPLETE",
                SendMessageOptions.DontRequireReceiver);
            uiManager.SendMessage("SetExportSummaryText", BuildSessionSummary(),
                SendMessageOptions.DontRequireReceiver);
            uiManager.SendMessage("SetExportStatusText",
                "Data saved inside the app. Use SAVE CSV FILES to export.",
                SendMessageOptions.DontRequireReceiver);
            uiManager.SendMessage("ShowExportPanel", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void PrepareNextStudent()
    {
        if (sessionActive || !sessionCompleted) return;
        if (placementManagerObject != null)
            placementManagerObject.SendMessage(
                "ResetPlacement", SendMessageOptions.DontRequireReceiver);
        ResetSessionState();
        PrepareLoginUI();
        if (uiManager != null)
        {
            uiManager.SendMessage("PrepareUIForNextStudent",
                SendMessageOptions.DontRequireReceiver);
            // Earlier UI Managers may not have PrepareUIForNextStudent.
            // ShowLoginPanel is safe to call again in D5.
            uiManager.SendMessage("ShowLoginPanel", SendMessageOptions.DontRequireReceiver);
        }
    }

    private string BuildSessionSummary()
    {
        int sec = Mathf.Max(0, Mathf.RoundToInt((float)finalSessionSeconds));
        return "Name: " + currentStudentId + "\n" +
               "Session Time: " + (sec / 60).ToString("00") + ":" +
               (sec % 60).ToString("00") + "\n\n" +
               "Shape Selections: " + researchDataLogger.ShapeSelectionCount + "\n" +
               "Fraction Checks: " + researchDataLogger.FractionSelectionCount + "\n" +
               "Trace Attempts: " + researchDataLogger.TraceAttemptCount + "\n" +
               "Successful Traces: " + researchDataLogger.SuccessfulTraceCount + "\n" +
               "Successful Slices: " + researchDataLogger.SuccessfulSliceCount;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string result = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return result.Length > 50 ? result.Substring(0, 50) : result;
    }

    private void ResetSessionState()
    {
        sessionActive = false;
        sessionCompleted = false;
        currentStudentId = currentSessionId = "";
        sessionStartLocal = sessionStartUtc = "";
        finalSessionSeconds = 0;
        sessionStartUtcValue = default(DateTimeOffset);
    }

    private void ShowLoginError(string value)
    {
        if (loginErrorText != null) loginErrorText.text = value;
    }

    private void ClearLoginError()
    {
        if (loginErrorText != null) loginErrorText.text = "";
    }
}

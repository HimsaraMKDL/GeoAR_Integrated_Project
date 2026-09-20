using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class C2_StudentSessionManager : MonoBehaviour
{
    // =========================================================
    // STUDENT ID FORMAT
    // =========================================================
    //
    // Required format:
    //
    // ST0001
    //
    // ST + exactly four digits.
    //
    // Examples:
    //
    // ST0001
    // ST0025
    // ST1234
    //
    // =========================================================

    private static readonly Regex StudentIdPattern =
        new Regex(
            @"^ST\d{4}$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant
        );


    // =========================================================
    // LOGIN UI REFERENCES
    // =========================================================

    [Header("Login UI References")]

    [SerializeField]
    private InputField studentIdInputField;


    [SerializeField]
    private Text loginErrorText;


    [SerializeField]
    private Button loginContinueButton;


    // =========================================================
    // APPLICATION REFERENCES
    // =========================================================

    [Header("Application References")]

    [SerializeField]
    private C2_AppUIManager uiManager;


    // =========================================================
    // CURRENT SESSION
    // =========================================================

    [Header("Current Session - Read Only During Play")]

    [SerializeField]
    private bool sessionActive =
        false;


    [SerializeField]
    private string currentStudentId =
        "";


    [SerializeField]
    private string currentSessionId =
        "";


    [SerializeField]
    private string sessionStartLocal =
        "";


    [SerializeField]
    private string sessionStartUtc =
        "";


    // =========================================================
    // INTERNAL TIME VALUES
    // =========================================================

    private DateTimeOffset sessionStartUtcValue;


    // =========================================================
    // PUBLIC SESSION INFORMATION
    // =========================================================

    public bool SessionActive
        => sessionActive;


    public string CurrentStudentId
        => currentStudentId;


    public string CurrentSessionId
        => currentSessionId;


    public string SessionStartLocal
        => sessionStartLocal;


    public string SessionStartUtc
        => sessionStartUtc;


    public double ElapsedSessionSeconds
    {
        get
        {
            if (!sessionActive)
            {
                return 0.0;
            }


            TimeSpan elapsed =
                DateTimeOffset.UtcNow -
                sessionStartUtcValue;


            return
                elapsed.TotalSeconds;
        }
    }


    // =========================================================
    // UNITY START
    // =========================================================

    private void Start()
    {
        PrepareLoginUI();
    }


    // =========================================================
    // PREPARE LOGIN UI
    // =========================================================

    private void PrepareLoginUI()
    {
        if (studentIdInputField != null)
        {
            studentIdInputField.text =
                "";
        }


        ClearLoginError();


        // D1 deliberately disabled this button.
        // D2 now enables it because real validation exists.

        if (loginContinueButton != null)
        {
            loginContinueButton.interactable =
                true;
        }
    }


    // =========================================================
    // LOGIN STUDENT
    // =========================================================

    public void LoginStudent()
    {
        // -----------------------------------------------------
        // REFERENCE CHECK
        // -----------------------------------------------------

        if (studentIdInputField == null)
        {
            Debug.LogError(
                "C2 Research: Student ID InputField reference is missing."
            );

            return;
        }


        if (uiManager == null)
        {
            Debug.LogError(
                "C2 Research: C2_AppUIManager reference is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // READ INPUT
        // -----------------------------------------------------

        string rawStudentId =
            studentIdInputField.text;


        string normalizedStudentId =
            NormalizeStudentId(
                rawStudentId
            );


        // Show the cleaned value back to the user.

        studentIdInputField.text =
            normalizedStudentId;


        // -----------------------------------------------------
        // VALIDATE
        // -----------------------------------------------------

        if (
            !IsValidStudentId(
                normalizedStudentId
            )
        )
        {
            ShowLoginError(
                "Invalid Student ID. Use the format ST0001."
            );


            Debug.LogWarning(
                "C2 Research: Invalid Student ID entered."
            );


            return;
        }


        // -----------------------------------------------------
        // PREVENT A SECOND SESSION FROM BEING CREATED
        // ACCIDENTALLY WHILE ONE IS ALREADY ACTIVE
        // -----------------------------------------------------

        if (sessionActive)
        {
            Debug.LogWarning(
                "C2 Research: A student session is already active. " +
                "Continuing the existing session."
            );


            ClearLoginError();


            uiManager.ShowScanPanel();


            return;
        }


        // -----------------------------------------------------
        // START SESSION
        // -----------------------------------------------------

        StartNewSession(
            normalizedStudentId
        );


        // -----------------------------------------------------
        // MOVE TO AR SCANNING
        // -----------------------------------------------------

        ClearLoginError();


        uiManager.ShowScanPanel();
    }


    // =========================================================
    // NORMALIZE STUDENT ID
    // =========================================================

    private string NormalizeStudentId(
        string value
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                value
            )
        )
        {
            return "";
        }


        return
            value
                .Trim()
                .ToUpperInvariant();
    }


    // =========================================================
    // VALIDATE STUDENT ID
    // =========================================================

    private bool IsValidStudentId(
        string studentId
    )
    {
        if (
            string.IsNullOrEmpty(
                studentId
            )
        )
        {
            return false;
        }


        return
            StudentIdPattern.IsMatch(
                studentId
            );
    }


    // =========================================================
    // START NEW SESSION
    // =========================================================

    private void StartNewSession(
        string studentId
    )
    {
        DateTimeOffset localNow =
            DateTimeOffset.Now;


        DateTimeOffset utcNow =
            DateTimeOffset.UtcNow;


        sessionStartUtcValue =
            utcNow;


        currentStudentId =
            studentId;


        currentSessionId =
            GenerateSessionId(
                studentId,
                localNow
            );


        sessionStartLocal =
            localNow.ToString(
                "yyyy-MM-ddTHH:mm:ss.fffzzz",
                CultureInfo.InvariantCulture
            );


        sessionStartUtc =
            utcNow.ToString(
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture
            );


        sessionActive =
            true;


        Debug.Log(
            "C2 Research: Student session started." +
            "\nStudent ID: " +
            currentStudentId +
            "\nSession ID: " +
            currentSessionId +
            "\nLocal Start: " +
            sessionStartLocal +
            "\nUTC Start: " +
            sessionStartUtc
        );
    }


    // =========================================================
    // GENERATE UNIQUE SESSION ID
    // =========================================================

    private string GenerateSessionId(
        string studentId,
        DateTimeOffset localTime
    )
    {
        string dateTimePart =
            localTime.ToString(
                "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture
            );


        string uniquePart =
            Guid.NewGuid()
                .ToString("N")
                .Substring(0, 4)
                .ToUpperInvariant();


        return
            studentId +
            "_" +
            dateTimePart +
            "_" +
            uniquePart;
    }


    // =========================================================
    // LOGIN ERROR
    // =========================================================

    private void ShowLoginError(
        string message
    )
    {
        if (loginErrorText != null)
        {
            loginErrorText.text =
                message;
        }
    }


    private void ClearLoginError()
    {
        if (loginErrorText != null)
        {
            loginErrorText.text =
                "";
        }
    }
}
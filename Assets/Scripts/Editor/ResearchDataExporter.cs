#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ResearchDataExporter
{
    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/Research Data/Export CSV")]
    public static void ExportCSV()
    {
        string dataFolder = Path.Combine(
            Application.persistentDataPath,
            "ResearchData"
        );

        if (!Directory.Exists(dataFolder))
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "ResearchData folder was not found.\n\n" +
                dataFolder,
                "OK"
            );

            return;
        }

        string[] jsonFiles = Directory.GetFiles(
            dataFolder,
            "*.json",
            SearchOption.AllDirectories
        );

        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Data",
                "No JSON files were found in ResearchData.",
                "OK"
            );

            return;
        }

        List<ResearchAttemptRow> rows =
            new List<ResearchAttemptRow>();


        // =====================================================
        // READ ALL JSON FILES
        // =====================================================

        foreach (string filePath in jsonFiles)
        {
            try
            {
                string json =
                    File.ReadAllText(filePath);

                SessionData session =
                    JsonUtility.FromJson<SessionData>(json);

                if (session == null)
                    continue;

                // -------------------------------------------------
                // Only process session files.
                // Student profile JSON files do not contain
                // an attempts array.
                // -------------------------------------------------

                if (session.attempts == null ||
                    session.attempts.Length == 0)
                {
                    continue;
                }


                // =================================================
                // FLATTEN ATTEMPTS
                // =================================================

                foreach (AttemptData attempt in session.attempts)
                {
                    if (attempt == null)
                        continue;

                    ResearchAttemptRow row =
                        new ResearchAttemptRow();

                    row.studentID =
                        session.studentID;

                    row.activity =
                        session.activity;

                    row.createdAt =
                        session.createdAt;

                    row.lastUpdated =
                        session.lastUpdated;

                    row.attemptNumber =
                        attempt.attemptNumber;

                    row.timestamp =
                        attempt.timestamp;

                    row.validationResult =
                        attempt.validationResult;

                    row.validationMessage =
                        attempt.validationMessage;

                    row.facesDetected =
                        attempt.facesDetected;

                    row.timeTakenSeconds =
                        attempt.timeTakenSeconds;

                    row.helpUsed =
                        attempt.helpUsed;

                    row.correctionViewed =
                        attempt.correctionViewed;

                    row.retryPressed =
                        attempt.retryPressed;

                    row.threeDGenerated =
                        attempt.threeDGenerated;

                    row.arPlaced =
                        attempt.arPlaced;


                    rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "Could not read JSON file:\n" +
                    filePath +
                    "\n\n" +
                    ex.Message
                );
            }
        }


        if (rows.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No Session Data",
                "No activity attempt data was found.",
                "OK"
            );

            return;
        }


        // =========================================================
        // CREATE CSV
        // =========================================================

        StringBuilder csv =
            new StringBuilder();


        // ---------------------------------------------------------
        // HEADER
        // ---------------------------------------------------------

        csv.AppendLine(
            "StudentID," +
            "Activity," +
            "CreatedAt," +
            "LastUpdated," +
            "AttemptNumber," +
            "Timestamp," +
            "ValidationResult," +
            "ValidationMessage," +
            "FacesDetected," +
            "TimeTakenSeconds," +
            "HelpUsed," +
            "CorrectionViewed," +
            "RetryPressed," +
            "ThreeDGenerated," +
            "ARPlaced"
        );


        // =========================================================
        // DATA ROWS
        // =========================================================

        foreach (ResearchAttemptRow row in rows)
        {
            csv.AppendLine(
                EscapeCSV(row.studentID) + "," +
                EscapeCSV(row.activity) + "," +
                EscapeCSV(row.createdAt) + "," +
                EscapeCSV(row.lastUpdated) + "," +
                row.attemptNumber + "," +
                EscapeCSV(row.timestamp) + "," +
                EscapeCSV(row.validationResult) + "," +
                EscapeCSV(row.validationMessage) + "," +
                row.facesDetected + "," +
                row.timeTakenSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture
                ) + "," +
                row.helpUsed + "," +
                row.correctionViewed + "," +
                row.retryPressed + "," +
                row.threeDGenerated + "," +
                row.arPlaced
            );
        }


        // =========================================================
        // SAVE LOCATION
        // =========================================================

        string defaultFileName =
            "ResearchData_Export_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".csv";


        string savePath =
            EditorUtility.SaveFilePanel(
                "Export Research Data",
                "",
                defaultFileName,
                "csv"
            );


        if (string.IsNullOrEmpty(savePath))
        {
            Debug.Log(
                "Research data export cancelled."
            );

            return;
        }


        // =========================================================
        // WRITE FILE
        // =========================================================

        File.WriteAllText(
            savePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );


        // =========================================================
        // FINISHED
        // =========================================================

        Debug.Log(
            "Research data exported successfully.\n" +
            "Rows: " + rows.Count +
            "\nFile: " + savePath
        );


        EditorUtility.DisplayDialog(
            "Export Complete",
            "Research data exported successfully.\n\n" +
            "Attempts exported: " +
            rows.Count +
            "\n\n" +
            "File:\n" +
            savePath,
            "OK"
        );


        // Reveal the exported file
        EditorUtility.RevealInFinder(savePath);
    }


    // =========================================================
    // CSV ESCAPE
    // =========================================================

    private static string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value =
            value.Replace(
                "\"",
                "\"\""
            );

        if (value.Contains(",") ||
            value.Contains("\"") ||
            value.Contains("\n") ||
            value.Contains("\r"))
        {
            return "\"" + value + "\"";
        }

        return value;
    }


    // =========================================================
    // JSON SESSION DATA
    // =========================================================

    [Serializable]
    private class SessionData
    {
        public string studentID;

        public string activity;

        public string createdAt;

        public string lastUpdated;

        public AttemptData[] attempts;
    }


    // =========================================================
    // JSON ATTEMPT DATA
    // =========================================================

    [Serializable]
    private class AttemptData
    {
        public int attemptNumber;

        public string timestamp;

        public string validationResult;

        public string validationMessage;

        public int facesDetected;

        public float timeTakenSeconds;

        public bool helpUsed;

        public bool correctionViewed;

        public bool retryPressed;

        public bool threeDGenerated;

        public bool arPlaced;
    }


    // =========================================================
    // FLATTENED CSV ROW
    // =========================================================

    private class ResearchAttemptRow
    {
        public string studentID;

        public string activity;

        public string createdAt;

        public string lastUpdated;

        public int attemptNumber;

        public string timestamp;

        public string validationResult;

        public string validationMessage;

        public int facesDetected;

        public float timeTakenSeconds;

        public bool helpUsed;

        public bool correctionViewed;

        public bool retryPressed;

        public bool threeDGenerated;

        public bool arPlaced;
    }
}

#endif
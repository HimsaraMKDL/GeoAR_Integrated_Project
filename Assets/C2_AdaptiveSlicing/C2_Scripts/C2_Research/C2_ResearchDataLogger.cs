using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// One local, append-only research logger. No network connection is needed.
/// StudentID stores the entered name; SessionID is a separate generated ID.
/// Produces detailed events, session summaries and a 30-column test-results
/// file matching the user's Final Virtual slicing CSV header.
/// Attach exactly once in the main scene (C2_Managers).
/// </summary>
public class C2_ResearchDataLogger : MonoBehaviour
{
    public static C2_ResearchDataLogger Instance { get; private set; }

    private const string FolderName = "C2_ResearchData";
    private const string EventsFileName = "C2_Events.csv";
    private const string SessionsFileName = "C2_Sessions.csv";
    private const string TestsFileName = "C2_TestResults.csv";

    // Keep the established D3-D5 event and summary headers unchanged.
    private const string EventsHeader =
        "RecordID,StudentID,SessionID,LocalDate,LocalTimestamp,UtcTimestamp," +
        "TimeSinceSessionStartMs,EventType,Shape,Direction,Fraction,AttemptNumber," +
        "Result,DurationMs,TotalEventCount,ShapeSelectionCount," +
        "DirectionSelectionCount,FractionSelectionCount,HalfSelectionCount," +
        "ThirdSelectionCount,QuarterSelectionCount,ThreeQuarterSelectionCount," +
        "TraceAttemptCount,WrongStartCount,IncompleteTraceCount," +
        "SuccessfulTraceCount,SuccessfulSliceCount,TryAgainCount," +
        "ResetChoicesCount,RestartARCount,AppVersion,DeviceModel," +
        "OperatingSystem,Notes";

    private const string SessionsHeader =
        "StudentID,SessionID,StartLocal,StartUtc,EndLocal,EndUtc," +
        "SessionSeconds,TotalEvents,TotalShapeSelections,CubeSelectionCount," +
        "CuboidSelectionCount,PrismSelectionCount,TetrahedronSelectionCount," +
        "CylinderSelectionCount,TotalDirectionSelections,VerticalSelectionCount," +
        "HorizontalSelectionCount,AngledSelectionCount,TotalFractionSelections," +
        "HalfSelectionCount,ThirdSelectionCount,QuarterSelectionCount," +
        "ThreeQuarterSelectionCount,TraceAttempts,WrongStarts,IncompleteTraces," +
        "SuccessfulTraces,SuccessfulSlices,TryAgainCount,ResetChoicesCount," +
        "RestartARCount,AverageTraceDurationMs,AppVersion,DeviceModel," +
        "OperatingSystem,SessionEndReason";

    // EXACT 30-column header from the supplied Final Virtual slicing CSV.
    private const string TestsHeader =
        "RecordID,StudentID,SessionID,LocalDate,LocalTimestamp,UtcTimestamp," +
        "TimeSinceSessionStartMs,EventID,Shape,Direction,Fraction,AttemptNumber," +
        "Result,DurationMs,ShapeSelectionCount,DirectionSelectionCount," +
        "FractionSelectionCount,HalfSelectionCount,ThirdSelectionCount," +
        "QuarterSelectionCount,ThreeQuarterSelectionCount,TraceAttemptCount," +
        "WrongStartCount,IncompleteTraceCount,SuccessfulTraceCount," +
        "SuccessfulSliceCount,TryAgainCount,ResetChoicesCount,RestartARCount," +
        "AppVersion";

    private readonly UTF8Encoding utf8NoBom = new UTF8Encoding(false);

    [Header("Storage - Read Only During Play")]
    [SerializeField] private bool storageReady;
    [SerializeField] private string researchDirectoryPath = "";
    [SerializeField] private string eventsFilePath = "";
    [SerializeField] private string sessionsFilePath = "";
    [SerializeField] private string testResultsFilePath = "";
    [TextArea(2, 5)] [SerializeField] private string lastStorageError = "";

    [Header("Current Session - Read Only During Play")]
    [SerializeField] private bool sessionLoggingActive;
    [SerializeField] private bool sessionSummaryWritten;
    [SerializeField] private string currentStudentId = "";
    [SerializeField] private string currentSessionId = "";
    [SerializeField] private int currentSessionEventCount;
    [SerializeField] private int currentSessionTestCount;
    [SerializeField] private string currentShapeContext = "";
    [SerializeField] private string currentDirectionContext = "";
    [SerializeField] private string currentFractionContext = "";

    [Header("Selection Counts - Read Only During Play")]
    [SerializeField] private int shapeSelectionCount;
    [SerializeField] private int cubeSelectionCount;
    [SerializeField] private int cuboidSelectionCount;
    [SerializeField] private int prismSelectionCount;
    [SerializeField] private int tetrahedronSelectionCount;
    [SerializeField] private int cylinderSelectionCount;
    [SerializeField] private int directionSelectionCount;
    [SerializeField] private int verticalSelectionCount;
    [SerializeField] private int horizontalSelectionCount;
    [SerializeField] private int angledSelectionCount;
    [SerializeField] private int fractionSelectionCount;
    [SerializeField] private int halfSelectionCount;
    [SerializeField] private int thirdSelectionCount;
    [SerializeField] private int quarterSelectionCount;
    [SerializeField] private int threeQuarterSelectionCount;

    [Header("Trace / Other Counts - Read Only During Play")]
    [SerializeField] private int traceAttemptCount;
    [SerializeField] private int wrongStartCount;
    [SerializeField] private int incompleteTraceCount;
    [SerializeField] private int successfulTraceCount;
    [SerializeField] private int successfulSliceCount;
    [SerializeField] private int tryAgainCount;
    [SerializeField] private int resetChoicesCount;
    [SerializeField] private int restartARCount;

    private DateTimeOffset sessionStartUtcValue;
    private int activeTraceAttemptNumber;
    private float activeTraceStartRealtime = -1f;
    private long completedTraceDurationSumMs;
    private int completedTraceDurationCount;
    private bool taskAttempted;
    private bool sliceAlreadyRecorded;
    private float taskStartRealtime = -1f;
    private bool endEventWritten;

    public bool StorageReady => storageReady;
    public bool SessionLoggingActive => sessionLoggingActive;
    public bool SessionSummaryWritten => sessionSummaryWritten;
    public string ResearchDirectoryPath => researchDirectoryPath;
    public string EventsFilePath => eventsFilePath;
    public string SessionsFilePath => sessionsFilePath;
    public string TestResultsFilePath => testResultsFilePath;
    public string CurrentStudentId => currentStudentId;
    public string CurrentSessionId => currentSessionId;
    public int CurrentSessionEventCount => currentSessionEventCount;
    public string CurrentShapeContext => currentShapeContext;
    public string CurrentDirectionContext => currentDirectionContext;
    public string CurrentFractionContext => currentFractionContext;
    public int ShapeSelectionCount => shapeSelectionCount;
    public int CubeSelectionCount => cubeSelectionCount;
    public int CuboidSelectionCount => cuboidSelectionCount;
    public int PrismSelectionCount => prismSelectionCount;
    public int TetrahedronSelectionCount => tetrahedronSelectionCount;
    public int CylinderSelectionCount => cylinderSelectionCount;
    public int DirectionSelectionCount => directionSelectionCount;
    public int VerticalSelectionCount => verticalSelectionCount;
    public int HorizontalSelectionCount => horizontalSelectionCount;
    public int AngledSelectionCount => angledSelectionCount;
    public int FractionSelectionCount => fractionSelectionCount;
    public int HalfSelectionCount => halfSelectionCount;
    public int ThirdSelectionCount => thirdSelectionCount;
    public int QuarterSelectionCount => quarterSelectionCount;
    public int ThreeQuarterSelectionCount => threeQuarterSelectionCount;
    public int TraceAttemptCount => traceAttemptCount;
    public int WrongStartCount => wrongStartCount;
    public int IncompleteTraceCount => incompleteTraceCount;
    public int SuccessfulTraceCount => successfulTraceCount;
    public int SuccessfulSliceCount => successfulSliceCount;
    public int TryAgainCount => tryAgainCount;
    public int ResetChoicesCount => resetChoicesCount;
    public int RestartARCount => restartARCount;
    public long AverageTraceDurationMs => completedTraceDurationCount == 0 ? 0 : completedTraceDurationSumMs / completedTraceDurationCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("C2 Research: Only one ResearchDataLogger is allowed.");
            enabled = false;
            return;
        }
        Instance = this;
        EnsureStorageReady();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private bool EnsureStorageReady()
    {
        try
        {
            researchDirectoryPath = Path.Combine(Application.persistentDataPath, FolderName);
            Directory.CreateDirectory(researchDirectoryPath);
            eventsFilePath = Path.Combine(researchDirectoryPath, EventsFileName);
            sessionsFilePath = Path.Combine(researchDirectoryPath, SessionsFileName);
            testResultsFilePath = Path.Combine(researchDirectoryPath, TestsFileName);
            EnsureHeader(eventsFilePath, EventsHeader);
            EnsureHeader(sessionsFilePath, SessionsHeader);
            EnsureHeader(testResultsFilePath, TestsHeader);
            storageReady = true;
            lastStorageError = "";
            Debug.Log("C2 Research storage: " + researchDirectoryPath);
            return true;
        }
        catch (Exception ex)
        {
            storageReady = false;
            lastStorageError = ex.Message;
            Debug.LogError("C2 Research storage error: " + ex);
            return false;
        }
    }

    private void EnsureHeader(string path, string expectedHeader)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            File.WriteAllText(path, expectedHeader + Environment.NewLine, utf8NoBom);
            return;
        }
        using (var reader = new StreamReader(path, Encoding.UTF8, true))
        {
            string actualHeader = reader.ReadLine();
            if (actualHeader != expectedHeader)
                throw new IOException("Existing CSV header differs. Back up and inspect this file; it was NOT overwritten: " + path);
        }
    }

    public bool BeginSession(string studentName, string sessionId, string startLocal, string startUtc)
    {
        if (sessionLoggingActive) return false;
        if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(sessionId)) return false;
        if (!storageReady && !EnsureStorageReady()) return false;

        currentStudentId = studentName.Trim();
        currentSessionId = sessionId.Trim();
        if (!DateTimeOffset.TryParse(startUtc, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out sessionStartUtcValue))
            sessionStartUtcValue = DateTimeOffset.UtcNow;

        ResetCounters();
        currentShapeContext = currentDirectionContext = currentFractionContext = "";
        sessionSummaryWritten = false;
        endEventWritten = false;
        sessionLoggingActive = true;
        if (!LogEvent("SESSION_STARTED", result: "Success"))
        {
            sessionLoggingActive = false;
            return false;
        }
        return true;
    }

    public bool EndSession(string startLocal, string startUtc, string reason = "CompletedByResearcher")
    {
        if (!sessionLoggingActive || sessionSummaryWritten) return false;
        if (activeTraceAttemptNumber > 0) LogTraceCancelled();
        FlushUnfinishedTaskIfNeeded();
        if (!endEventWritten)
        {
            if (!LogEvent("SESSION_ENDED", currentShapeContext, currentDirectionContext,
                currentFractionContext, result: "Complete", notes: reason)) return false;
            endEventWritten = true;
        }

        DateTimeOffset endLocal = DateTimeOffset.Now;
        DateTimeOffset endUtc = DateTimeOffset.UtcNow;
        double seconds = Math.Max(0, (endUtc - sessionStartUtcValue).TotalSeconds);
        string[] values = {
            currentStudentId, currentSessionId, startLocal, startUtc,
            endLocal.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
            endUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            seconds.ToString("F3", CultureInfo.InvariantCulture), N(currentSessionEventCount),
            N(shapeSelectionCount), N(cubeSelectionCount), N(cuboidSelectionCount),
            N(prismSelectionCount), N(tetrahedronSelectionCount), N(cylinderSelectionCount),
            N(directionSelectionCount), N(verticalSelectionCount), N(horizontalSelectionCount),
            N(angledSelectionCount), N(fractionSelectionCount), N(halfSelectionCount),
            N(thirdSelectionCount), N(quarterSelectionCount), N(threeQuarterSelectionCount),
            N(traceAttemptCount), N(wrongStartCount), N(incompleteTraceCount),
            N(successfulTraceCount), N(successfulSliceCount), N(tryAgainCount),
            N(resetChoicesCount), N(restartARCount), N(AverageTraceDurationMs),
            Application.version, SystemInfo.deviceModel, SystemInfo.operatingSystem, reason
        };
        try
        {
            File.AppendAllText(sessionsFilePath, Csv(values) + Environment.NewLine, utf8NoBom);
            sessionSummaryWritten = true;
            sessionLoggingActive = false;
            Debug.Log("C2 Research session summary saved: " + currentSessionId);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("C2 Research: Could not save session summary: " + ex);
            return false;
        }
    }

    public void SetShapeContext(string shape)
    {
        string value = NormalizeShape(shape);
        if (currentShapeContext == value) return;
        FlushUnfinishedTaskIfNeeded();
        currentShapeContext = value;
        currentDirectionContext = "";
        currentFractionContext = "";
        ResetTraceTracking();
        ResetTaskTracking();
    }

    public void SetDirectionContext(string direction)
    {
        string value = Clean(direction);
        if (currentDirectionContext == value) return;
        FlushUnfinishedTaskIfNeeded();
        currentDirectionContext = value;
        ResetTraceTracking();
        ResetTaskTracking();
    }

    public void SetFractionContext(string fraction)
    {
        string value = Clean(fraction);
        if (currentFractionContext == value) return;
        FlushUnfinishedTaskIfNeeded();
        currentFractionContext = value;
        ResetTraceTracking();
        ResetTaskTracking();
    }

    public void ClearSliceContext()
    {
        FlushUnfinishedTaskIfNeeded();
        currentDirectionContext = currentFractionContext = "";
        ResetTraceTracking();
        ResetTaskTracking();
    }

    public void ClearAllContext()
    {
        FlushUnfinishedTaskIfNeeded();
        currentShapeContext = currentDirectionContext = currentFractionContext = "";
        ResetTraceTracking();
        ResetTaskTracking();
    }

    public bool LogShapeSelected(string shape)
    {
        SetShapeContext(shape);
        return LogEvent("SHAPE_SELECTED", currentShapeContext);
    }

    public bool LogDirectionSelected(string direction)
    {
        SetDirectionContext(direction);
        return LogEvent("DIRECTION_SELECTED", currentShapeContext,
            currentDirectionContext, currentFractionContext);
    }

    public bool LogFractionSelected(string fraction)
    {
        SetFractionContext(fraction);
        return LogEvent("FRACTION_SELECTED", currentShapeContext,
            currentDirectionContext, currentFractionContext);
    }

    public int BeginTrace(bool reverseTrace)
    {
        if (!sessionLoggingActive || endEventWritten) return 0;
        int attempt = traceAttemptCount + 1;
        activeTraceAttemptNumber = attempt;
        activeTraceStartRealtime = Time.realtimeSinceStartup;
        MarkTaskStarted();
        if (!LogEvent("TRACE_STARTED", currentShapeContext, currentDirectionContext,
            currentFractionContext, attempt, "Started", -1,
            reverseTrace ? "Reverse trace." : "Forward trace."))
        {
            ResetTraceTracking();
            return 0;
        }
        return attempt;
    }

    public void LogTraceWrongStart()
    {
        MarkTaskStarted();
        LogEvent("TRACE_WRONG_START", currentShapeContext, currentDirectionContext,
            currentFractionContext, 0, "Fail");
    }

    public void LogTraceIncomplete()
    {
        long duration = ActiveTraceDuration();
        LogEvent("TRACE_INCOMPLETE", currentShapeContext, currentDirectionContext,
            currentFractionContext, activeTraceAttemptNumber, "Fail", duration);
        ResetTraceTracking();
    }

    public void LogTraceSuccess()
    {
        long duration = ActiveTraceDuration();
        LogEvent("TRACE_SUCCESS", currentShapeContext, currentDirectionContext,
            currentFractionContext, activeTraceAttemptNumber, "Success", duration);
        ResetTraceTracking();
    }

    public void LogTraceCancelled()
    {
        if (activeTraceAttemptNumber == 0) return;
        LogEvent("TRACE_CANCELLED", currentShapeContext, currentDirectionContext,
            currentFractionContext, activeTraceAttemptNumber, "Cancelled", ActiveTraceDuration());
        ResetTraceTracking();
    }

    public bool LogSliceCompleted()
    {
        if (sliceAlreadyRecorded || !sessionLoggingActive) return false;
        long duration = TaskDuration();
        if (!LogEvent("SLICE_COMPLETED", currentShapeContext, currentDirectionContext,
            currentFractionContext, result: "Success", durationMs: duration)) return false;
        bool saved = WriteTestResult("Success", duration);
        if (saved)
        {
            sliceAlreadyRecorded = true;
            taskAttempted = false;
        }
        return saved;
    }

    // Call when an explicit testing task finishes without a successful slice.
    // Normal navigation also records unfinished attempted tasks as Fail.
    public bool RecordFailedTest()
    {
        if (!sessionLoggingActive || !taskAttempted || sliceAlreadyRecorded) return false;
        bool saved = WriteTestResult("Fail", TaskDuration());
        if (saved) ResetTaskTracking();
        return saved;
    }

    public bool LogEvent(string eventType, string shape = "", string direction = "",
        string fraction = "", int attemptNumber = 0, string result = "",
        long durationMs = -1, string notes = "")
    {
        if (!sessionLoggingActive || endEventWritten || !storageReady) return false;
        string eventName = Clean(eventType).ToUpperInvariant();
        string cleanShape = NormalizeShape(shape);
        string cleanDirection = Clean(direction);
        string cleanFraction = Clean(fraction);

        if (eventName == "TRY_SAME_SLICE" || eventName == "RESET_CHOICES" || eventName == "RESTART_AR")
            FlushUnfinishedTaskIfNeeded();

        UpdateCounters(eventName, cleanShape, cleanDirection, cleanFraction, durationMs);
        int eventNumber = currentSessionEventCount + 1;
        DateTimeOffset local = DateTimeOffset.Now;
        DateTimeOffset utc = DateTimeOffset.UtcNow;
        string[] values = {
            currentSessionId + "_E" + eventNumber.ToString("D6", CultureInfo.InvariantCulture),
            currentStudentId, currentSessionId,
            local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            local.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
            utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            N(Math.Max(0, (long)(utc - sessionStartUtcValue).TotalMilliseconds)),
            eventName, cleanShape, cleanDirection, cleanFraction,
            attemptNumber <= 0 ? "" : N(attemptNumber), result,
            durationMs < 0 ? "" : N(durationMs), N(eventNumber),
            N(shapeSelectionCount), N(directionSelectionCount), N(fractionSelectionCount),
            N(halfSelectionCount), N(thirdSelectionCount), N(quarterSelectionCount),
            N(threeQuarterSelectionCount), N(traceAttemptCount), N(wrongStartCount),
            N(incompleteTraceCount), N(successfulTraceCount), N(successfulSliceCount),
            N(tryAgainCount), N(resetChoicesCount), N(restartARCount),
            Application.version, SystemInfo.deviceModel, SystemInfo.operatingSystem, notes
        };
        try
        {
            File.AppendAllText(eventsFilePath, Csv(values) + Environment.NewLine, utf8NoBom);
            currentSessionEventCount = eventNumber;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("C2 Research: Event write failed: " + ex);
            return false;
        }
    }

    private bool WriteTestResult(string outcome, long durationMs)
    {
        if (!sessionLoggingActive) return false;
        if (string.IsNullOrEmpty(currentShapeContext) ||
            string.IsNullOrEmpty(currentDirectionContext) ||
            string.IsNullOrEmpty(currentFractionContext))
        {
            Debug.LogWarning("C2 Research: Test result skipped: shape/direction/fraction not all selected. Raw events remain saved.");
            return false;
        }
        DateTimeOffset local = DateTimeOffset.Now;
        DateTimeOffset utc = DateTimeOffset.UtcNow;
        int testNumber = currentSessionTestCount + 1;
        string eventId = "SLICE_" + ShapeToken(currentShapeContext) + "_" +
            testNumber.ToString("D3", CultureInfo.InvariantCulture);
        string[] values = {
            currentSessionId + "_T" + testNumber.ToString("D6", CultureInfo.InvariantCulture),
            currentStudentId, currentSessionId,
            local.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            local.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
            utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            N(Math.Max(0, (long)(utc - sessionStartUtcValue).TotalMilliseconds)),
            eventId, "SELECT " + ShapeToken(currentShapeContext).Replace('_', ' '),
            currentDirectionContext, currentFractionContext, N(traceAttemptCount), outcome,
            durationMs < 0 ? "" : N(durationMs),
            N(shapeSelectionCount), N(directionSelectionCount), N(fractionSelectionCount),
            N(halfSelectionCount), N(thirdSelectionCount), N(quarterSelectionCount),
            N(threeQuarterSelectionCount), N(traceAttemptCount), N(wrongStartCount),
            N(incompleteTraceCount), N(successfulTraceCount), N(successfulSliceCount),
            N(tryAgainCount), N(resetChoicesCount), N(restartARCount), Application.version
        };
        try
        {
            File.AppendAllText(testResultsFilePath, Csv(values) + Environment.NewLine, utf8NoBom);
            currentSessionTestCount = testNumber;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("C2 Research: Test-result write failed: " + ex);
            return false;
        }
    }

    private void FlushUnfinishedTaskIfNeeded()
    {
        if (sessionLoggingActive && taskAttempted && !sliceAlreadyRecorded)
            RecordFailedTest();
    }

    private void MarkTaskStarted()
    {
        if (!taskAttempted)
        {
            taskAttempted = true;
            sliceAlreadyRecorded = false;
            taskStartRealtime = Time.realtimeSinceStartup;
        }
    }

    private long TaskDuration() => taskStartRealtime < 0f ? -1L :
        Math.Max(0L, (long)((Time.realtimeSinceStartup - taskStartRealtime) * 1000f));

    private long ActiveTraceDuration() => activeTraceStartRealtime < 0f ? -1L :
        Math.Max(0L, (long)((Time.realtimeSinceStartup - activeTraceStartRealtime) * 1000f));

    private void ResetTaskTracking()
    {
        taskAttempted = false;
        sliceAlreadyRecorded = false;
        taskStartRealtime = -1f;
    }

    private void ResetTraceTracking()
    {
        activeTraceAttemptNumber = 0;
        activeTraceStartRealtime = -1f;
    }

    private void UpdateCounters(string e, string shape, string direction, string fraction, long duration)
    {
        switch (e)
        {
            case "SHAPE_SELECTED":
                shapeSelectionCount++;
                switch (shape)
                {
                    case "Cube": cubeSelectionCount++; break;
                    case "Cuboid": cuboidSelectionCount++; break;
                    case "Prism": prismSelectionCount++; break;
                    case "Tetrahedron": tetrahedronSelectionCount++; break;
                    case "Cylinder": cylinderSelectionCount++; break;
                }
                break;
            case "DIRECTION_SELECTED":
                directionSelectionCount++;
                switch (direction)
                {
                    case "Vertical": verticalSelectionCount++; break;
                    case "Horizontal": horizontalSelectionCount++; break;
                    case "Angled": angledSelectionCount++; break;
                }
                break;
            case "FRACTION_SELECTED":
                fractionSelectionCount++;
                switch (fraction)
                {
                    case "1/2": halfSelectionCount++; break;
                    case "1/3": thirdSelectionCount++; break;
                    case "1/4": quarterSelectionCount++; break;
                    case "3/4": threeQuarterSelectionCount++; break;
                }
                break;
            case "TRACE_STARTED": traceAttemptCount++; break;
            case "TRACE_WRONG_START": wrongStartCount++; break;
            case "TRACE_INCOMPLETE":
                incompleteTraceCount++;
                AddTraceDuration(duration);
                break;
            case "TRACE_SUCCESS":
                successfulTraceCount++;
                AddTraceDuration(duration);
                break;
            case "SLICE_COMPLETED": successfulSliceCount++; break;
            case "TRY_SAME_SLICE":
                tryAgainCount++;
                ResetTaskTracking();
                break;
            case "RESET_CHOICES": resetChoicesCount++; break;
            case "RESTART_AR": restartARCount++; break;
        }
    }

    private void AddTraceDuration(long duration)
    {
        if (duration < 0) return;
        completedTraceDurationSumMs += duration;
        completedTraceDurationCount++;
    }

    private void ResetCounters()
    {
        currentSessionEventCount = currentSessionTestCount = 0;
        shapeSelectionCount = cubeSelectionCount = cuboidSelectionCount = 0;
        prismSelectionCount = tetrahedronSelectionCount = cylinderSelectionCount = 0;
        directionSelectionCount = verticalSelectionCount = horizontalSelectionCount = angledSelectionCount = 0;
        fractionSelectionCount = halfSelectionCount = thirdSelectionCount = quarterSelectionCount = threeQuarterSelectionCount = 0;
        traceAttemptCount = wrongStartCount = incompleteTraceCount = successfulTraceCount = successfulSliceCount = 0;
        tryAgainCount = resetChoicesCount = restartARCount = 0;
        completedTraceDurationSumMs = 0;
        completedTraceDurationCount = 0;
        ResetTraceTracking();
        ResetTaskTracking();
    }

    private static string N(long number) => number.ToString(CultureInfo.InvariantCulture);
    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static string NormalizeShape(string value)
    {
        switch (Clean(value).ToUpperInvariant().Replace("SELECT ", ""))
        {
            case "CUBE": return "Cube";
            case "CUBOID": return "Cuboid";
            case "PRISM":
            case "TRIANGULAR PRISM": return "Prism";
            case "TETRAHEDRON": return "Tetrahedron";
            case "CYLINDER": return "Cylinder";
            default: return Clean(value);
        }
    }

    private static string ShapeToken(string shape)
    {
        string cleaned = NormalizeShape(shape).ToUpperInvariant();
        return cleaned.Replace(' ', '_');
    }

    // Standard CSV escaping + spreadsheet-formula safety for user-entered names.
    private static string Csv(string[] fields)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0) builder.Append(',');
            string value = fields[i] ?? "";
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0) value = "'" + value;
            bool quotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            value = value.Replace("\"", "\"\"");
            builder.Append(quotes ? "\"" + value + "\"" : value);
        }
        return builder.ToString();
    }
}

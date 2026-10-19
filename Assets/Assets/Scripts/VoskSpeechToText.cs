using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ionic.Zip;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;
using Vosk;

public class VoskSpeechToText : MonoBehaviour
{
    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Vosk Model")]
    [Tooltip("Location of the model, relative to the Streaming Assets folder.")]
    public string ModelPath = "vosk-model-small-ru-0.22.zip";

    [Header("Microphone")]
    [Tooltip("The source of the microphone input.")]
    public VoiceProcessor VoiceProcessor;

    [Header("Recognition")]
    [Tooltip("The max number of alternatives that will be processed.")]
    public int MaxAlternatives = 3;

    [Tooltip("How long should we record before restarting?")]
    public float MaxRecordLength = 5f;

    [Tooltip("Should the recognizer start when the application is launched?")]
    public bool AutoStart = false;

    [Tooltip("The phrases that will be detected. If left empty, all words will be detected.")]
    public List<string> KeyPhrases = new List<string>();


    // =========================================================
    // VOSK
    // =========================================================

    private Model _model;

    private VoskRecognizer _recognizer;

    private bool _recognizerReady = false;


    // =========================================================
    // EVENTS
    // =========================================================

    public Action<string> OnStatusUpdated;

    public Action<string> OnTranscriptionResult;


    // =========================================================
    // MODEL STATE
    // =========================================================

    private string _decompressedModelPath;

    private string _grammar = "";

    private bool _isDecompressing = false;

    private bool _isInitializing = false;

    private bool _didInit = false;


    // =========================================================
    // RECORDING / THREADING
    // =========================================================

    private bool _running = false;

    private Task _recognitionTask;

    private readonly ConcurrentQueue<short[]>
        _threadedBufferQueue =
            new ConcurrentQueue<short[]>();

    private readonly ConcurrentQueue<string>
        _threadedResultQueue =
            new ConcurrentQueue<string>();


    // =========================================================
    // PROFILER
    // =========================================================

    private static readonly ProfilerMarker
        voskRecognizerCreateMarker =
            new ProfilerMarker(
                "VoskRecognizer.Create"
            );

    private static readonly ProfilerMarker
        voskRecognizerReadMarker =
            new ProfilerMarker(
                "VoskRecognizer.AcceptWaveform"
            );


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsInitialized =>
        _didInit;

    public bool IsInitializing =>
        _isInitializing;

    public bool IsRecording =>
        VoiceProcessor != null &&
        VoiceProcessor.IsRecording;


    // =========================================================
    // UNITY - AWAKE
    // =========================================================

    private void Awake()
    {
        if (VoiceProcessor == null)
        {
            VoiceProcessor =
                GetComponent<VoiceProcessor>();


            if (VoiceProcessor == null)
            {
                VoiceProcessor =
                    gameObject.AddComponent<VoiceProcessor>();
            }
        }
    }


    // =========================================================
    // UNITY - START
    // =========================================================

    private void Start()
    {
        if (AutoStart)
        {
            StartVoskStt(
                null,
                default,
                true,
                MaxAlternatives
            );
        }
    }


    // =========================================================
    // UNITY - UPDATE
    // =========================================================

    private void Update()
    {
        while (_threadedResultQueue.TryDequeue(
                   out string voiceResult))
        {
            OnTranscriptionResult?.Invoke(
                voiceResult
            );
        }
    }


    // =========================================================
    // UNITY - DESTROY
    // =========================================================

    private void OnDestroy()
    {
        StopRecording();


        if (VoiceProcessor != null)
        {
            VoiceProcessor.OnFrameCaptured -=
                VoiceProcessorOnOnFrameCaptured;

            VoiceProcessor.OnRecordingStop -=
                VoiceProcessorOnOnRecordingStop;
        }


        if (_recognizer != null)
        {
            _recognizer.Dispose();

            _recognizer = null;
        }


        if (_model != null)
        {
            _model.Dispose();

            _model = null;
        }


        _recognizerReady = false;

        _didInit = false;

        _isInitializing = false;
    }


    // =========================================================
    // INITIALIZE VOSK
    // =========================================================

    public void StartVoskStt(
        List<string> keyPhrases = null,
        string modelPath = default,
        bool startMicrophone = false,
        int maxAlternatives = 3)
    {
        // -----------------------------------------------------
        // ALREADY INITIALIZING
        // -----------------------------------------------------

        if (_isInitializing)
        {
            Debug.Log(
                "Vosk initialization is already in progress."
            );

            return;
        }


        // -----------------------------------------------------
        // ALREADY INITIALIZED
        //
        // IMPORTANT FIX:
        // This is NOT an error anymore.
        // Reuse existing Vosk instance.
        // -----------------------------------------------------

        if (_didInit)
        {
            Debug.Log(
                "Vosk is already initialized. Reusing existing recognizer."
            );


            if (startMicrophone)
            {
                StartRecording();
            }


            return;
        }


        // -----------------------------------------------------
        // MODEL PATH
        // -----------------------------------------------------

        if (!string.IsNullOrEmpty(
                modelPath))
        {
            ModelPath =
                modelPath;
        }


        // -----------------------------------------------------
        // KEY PHRASES
        // -----------------------------------------------------

        if (keyPhrases != null)
        {
            KeyPhrases =
                keyPhrases;
        }


        MaxAlternatives =
            maxAlternatives;


        StartCoroutine(
            DoStartVoskStt(
                startMicrophone
            )
        );
    }


    // =========================================================
    // ACTUAL INITIALIZATION
    // =========================================================

    private IEnumerator DoStartVoskStt(
        bool startMicrophone)
    {
        _isInitializing =
            true;


        OnStatusUpdated?.Invoke(
            "Waiting for microphone..."
        );


        yield return
            WaitForMicrophoneInput();


        // -----------------------------------------------------
        // DECOMPRESS / FIND MODEL
        // -----------------------------------------------------

        yield return
            Decompress();


        if (string.IsNullOrEmpty(
                _decompressedModelPath))
        {
            Debug.LogError(
                "Vosk model path could not be resolved."
            );


            OnStatusUpdated?.Invoke(
                "Vosk model could not be loaded."
            );


            _isInitializing =
                false;


            yield break;
        }


        // -----------------------------------------------------
        // LOAD MODEL
        // -----------------------------------------------------

        OnStatusUpdated?.Invoke(
            "Loading Model from: " +
            _decompressedModelPath
        );


        Debug.Log(
            "Loading Vosk model from: " +
            _decompressedModelPath
        );


        _model =
            new Model(
                _decompressedModelPath
            );


        yield return null;


        // -----------------------------------------------------
        // EVENT SUBSCRIPTIONS
        //
        // Remove first to protect against accidental duplicates.
        // -----------------------------------------------------

        VoiceProcessor.OnFrameCaptured -=
            VoiceProcessorOnOnFrameCaptured;

        VoiceProcessor.OnRecordingStop -=
            VoiceProcessorOnOnRecordingStop;


        VoiceProcessor.OnFrameCaptured +=
            VoiceProcessorOnOnFrameCaptured;

        VoiceProcessor.OnRecordingStop +=
            VoiceProcessorOnOnRecordingStop;


        // -----------------------------------------------------
        // INITIALIZATION COMPLETE
        // -----------------------------------------------------

        _didInit =
            true;

        _isInitializing =
            false;


        OnStatusUpdated?.Invoke(
            "Initialized"
        );


        Debug.Log(
            "Vosk initialized successfully."
        );


        // -----------------------------------------------------
        // START MICROPHONE ONLY WHEN REQUESTED
        //
        // IMPORTANT:
        // Old code called ToggleRecording() unconditionally.
        // That has been removed.
        // -----------------------------------------------------

        if (startMicrophone)
        {
            StartRecording();
        }
    }


    // =========================================================
    // GRAMMAR
    // =========================================================

    private void UpdateGrammar()
    {
        if (KeyPhrases == null ||
            KeyPhrases.Count == 0)
        {
            _grammar =
                "";

            return;
        }


        JSONArray keywords =
            new JSONArray();


        foreach (string keyphrase
                 in KeyPhrases)
        {
            if (string.IsNullOrWhiteSpace(
                    keyphrase))
            {
                continue;
            }


            keywords.Add(
                new JSONString(
                    keyphrase
                        .Trim()
                        .ToLower()
                )
            );
        }


        keywords.Add(
            new JSONString(
                "[unk]"
            )
        );


        _grammar =
            keywords.ToString();
    }


    // =========================================================
    // DECOMPRESS MODEL
    // =========================================================

    private IEnumerator Decompress()
    {
        string modelFolderName =
            Path.GetFileNameWithoutExtension(
                ModelPath
            );


        string persistentModelPath =
            Path.Combine(
                Application.persistentDataPath,
                modelFolderName
            );


        // -----------------------------------------------------
        // MODEL ALREADY DECOMPRESSED
        // -----------------------------------------------------

        if (!Path.HasExtension(
                ModelPath) ||
            Directory.Exists(
                persistentModelPath))
        {
            OnStatusUpdated?.Invoke(
                "Using existing decompressed model."
            );


            _decompressedModelPath =
                persistentModelPath;


            Debug.Log(
                "Using Vosk model: " +
                _decompressedModelPath
            );


            yield break;
        }


        // -----------------------------------------------------
        // DECOMPRESS NEW MODEL
        // -----------------------------------------------------

        OnStatusUpdated?.Invoke(
            "Decompressing model..."
        );


        _isDecompressing =
            false;


        string dataPath =
            Path.Combine(
                Application.streamingAssetsPath,
                ModelPath
            );


        Stream dataStream =
            null;


        // -----------------------------------------------------
        // ANDROID / URL STREAM
        // -----------------------------------------------------

        if (dataPath.Contains(
                "://"))
        {
            using (
                UnityWebRequest www =
                    UnityWebRequest.Get(
                        dataPath
                    )
            )
            {
                yield return
                    www.SendWebRequest();


                if (www.result !=
                    UnityWebRequest.Result.Success)
                {
                    Debug.LogError(
                        "Could not load Vosk model: " +
                        www.error
                    );


                    OnStatusUpdated?.Invoke(
                        "Could not load Vosk model."
                    );


                    yield break;
                }


                dataStream =
                    new MemoryStream(
                        www.downloadHandler.data
                    );
            }
        }

        // -----------------------------------------------------
        // WINDOWS / NORMAL FILE
        // -----------------------------------------------------

        else
        {
            if (!File.Exists(
                    dataPath))
            {
                Debug.LogError(
                    "Vosk model file does not exist: " +
                    dataPath
                );


                OnStatusUpdated?.Invoke(
                    "Vosk model file not found."
                );


                yield break;
            }


            dataStream =
                File.OpenRead(
                    dataPath
                );
        }


        // -----------------------------------------------------
        // EXTRACT ZIP
        // -----------------------------------------------------

        using (dataStream)
        using (
            ZipFile zipFile =
                ZipFile.Read(
                    dataStream
                )
        )
        {
            zipFile.ExtractProgress +=
                ZipFileOnExtractProgress;


            OnStatusUpdated?.Invoke(
                "Reading Zip file"
            );


            zipFile.ExtractAll(
                Application.persistentDataPath
            );


            while (!_isDecompressing)
            {
                yield return null;
            }


            zipFile.ExtractProgress -=
                ZipFileOnExtractProgress;
        }


        // -----------------------------------------------------
        // FINAL MODEL PATH
        // -----------------------------------------------------

        _decompressedModelPath =
            persistentModelPath;


        OnStatusUpdated?.Invoke(
            "Decompressing complete!"
        );


        Debug.Log(
            "Vosk model decompressed to: " +
            _decompressedModelPath
        );


        yield return
            new WaitForSeconds(
                0.25f
            );
    }


    // =========================================================
    // ZIP CALLBACK
    // =========================================================

    private void ZipFileOnExtractProgress(
        object sender,
        ExtractProgressEventArgs e)
    {
        if (e.EventType ==
            ZipProgressEventType
                .Extracting_AfterExtractAll)
        {
            _isDecompressing =
                true;
        }
    }


    // =========================================================
    // WAIT FOR MICROPHONE
    // =========================================================

    private IEnumerator WaitForMicrophoneInput()
    {
        while (
            Microphone.devices == null ||
            Microphone.devices.Length <= 0)
        {
            yield return null;
        }
    }


    // =========================================================
    // START RECORDING
    // =========================================================

    public void StartRecording()
    {
        if (!_didInit)
        {
            Debug.Log(
                "Vosk is not initialized yet. " +
                "Starting initialization with microphone."
            );


            StartVoskStt(
                null,
                default,
                true,
                MaxAlternatives
            );


            return;
        }


        if (VoiceProcessor == null)
        {
            Debug.LogError(
                "VoiceProcessor is missing."
            );

            return;
        }


        if (VoiceProcessor.IsRecording)
        {
            Debug.Log(
                "Vosk microphone is already recording."
            );

            return;
        }


        ClearAudioQueue();


        _running =
            true;


        VoiceProcessor.StartRecording();


        StartRecognitionThreadIfNeeded();


        Debug.Log(
            "Vosk microphone recording started."
        );
    }


    // =========================================================
    // STOP RECORDING
    // =========================================================

    public void StopRecording()
    {
        if (VoiceProcessor == null)
        {
            return;
        }


        _running =
            false;


        if (VoiceProcessor.IsRecording)
        {
            VoiceProcessor.StopRecording();


            Debug.Log(
                "Vosk microphone recording stopped."
            );
        }
    }


    // =========================================================
    // TOGGLE RECORDING
    //
    // Kept for compatibility with existing buttons/scripts.
    // =========================================================

    public void ToggleRecording()
    {
        if (VoiceProcessor == null)
        {
            Debug.LogError(
                "VoiceProcessor is missing."
            );

            return;
        }


        if (VoiceProcessor.IsRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }


    // =========================================================
    // START RECOGNITION WORKER
    // =========================================================

    private void StartRecognitionThreadIfNeeded()
    {
        if (_recognitionTask != null &&
            !_recognitionTask.IsCompleted)
        {
            return;
        }


        _recognitionTask =
            Task.Run(
                ThreadedWork
            );
    }


    // =========================================================
    // VOICE PROCESSOR CALLBACK
    // =========================================================

    private void VoiceProcessorOnOnFrameCaptured(
        short[] samples)
    {
        if (!_running ||
            samples == null ||
            samples.Length == 0)
        {
            return;
        }


        _threadedBufferQueue.Enqueue(
            samples
        );
    }


    // =========================================================
    // RECORDING STOP CALLBACK
    // =========================================================

    private void VoiceProcessorOnOnRecordingStop()
    {
        Debug.Log(
            "VoiceProcessor recording stopped."
        );
    }


    // =========================================================
    // RECOGNITION THREAD
    // =========================================================

    private async Task ThreadedWork()
    {
        try
        {
            // -------------------------------------------------
            // CREATE RECOGNIZER ONCE
            // -------------------------------------------------

            voskRecognizerCreateMarker.Begin();


            if (!_recognizerReady)
            {
                UpdateGrammar();


                if (string.IsNullOrEmpty(
                        _grammar))
                {
                    _recognizer =
                        new VoskRecognizer(
                            _model,
                            16000.0f
                        );
                }
                else
                {
                    _recognizer =
                        new VoskRecognizer(
                            _model,
                            16000.0f,
                            _grammar
                        );
                }


                _recognizer.SetMaxAlternatives(
                    MaxAlternatives
                );


                _recognizerReady =
                    true;


                Debug.Log(
                    "Vosk recognizer ready."
                );
            }


            voskRecognizerCreateMarker.End();


            // -------------------------------------------------
            // PROCESS AUDIO
            // -------------------------------------------------

            voskRecognizerReadMarker.Begin();


            while (_running)
            {
                if (_threadedBufferQueue.TryDequeue(
                        out short[] voiceResult))
                {
                    if (_recognizer != null &&
                        _recognizer.AcceptWaveform(
                            voiceResult,
                            voiceResult.Length
                        ))
                    {
                        string result =
                            _recognizer.Result();


                        if (!string.IsNullOrEmpty(
                                result))
                        {
                            _threadedResultQueue.Enqueue(
                                result
                            );
                        }
                    }
                }
                else
                {
                    await Task.Delay(
                        50
                    );
                }
            }


            voskRecognizerReadMarker.End();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Vosk recognition thread error: " +
                exception.Message
            );


            _running =
                false;
        }
    }


    // =========================================================
    // CLEAR OLD AUDIO
    // =========================================================

    private void ClearAudioQueue()
    {
        while (
            _threadedBufferQueue.TryDequeue(
                out _))
        {
        }
    }
}
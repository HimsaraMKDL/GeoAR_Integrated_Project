using System;
using UnityEngine;
using Unity.Sentis;

public class SentisInferenceManager : MonoBehaviour
{
    [Header("Temporary Test")]
    [SerializeField] private SketchDrawingPad testDrawingPad;

    [Header("Sentis Model")]
    [SerializeField] private ModelAsset modelAsset;

    [Header("Model Settings")]
    [SerializeField] private BackendType backendType = BackendType.CPU;

    [Header("Recognition Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumConfidence = 0.30f;

    private Model runtimeModel;
    private Worker worker;

    private readonly string[] classLabels =
    {
        "apple",
        "cat",
        "fish",
        "sun",
        "tree"
    };

    public bool IsReady => worker != null;

    private void Start()
    {
        InitializeModel();
    }

    private void InitializeModel()
    {
        if (modelAsset == null)
        {
            Debug.LogError(
                "SentisInferenceManager: Model Asset is not assigned."
            );

            return;
        }

        runtimeModel =
            ModelLoader.Load(modelAsset);

        worker =
            new Worker(
                runtimeModel,
                backendType
            );

        Debug.Log(
            "Sentis model loaded successfully."
        );

        Debug.Log(
            "On-device AI is ready."
        );
    }

    public void RecognizeSketch(
        Texture2D sketchTexture,
        Action<string, float> onComplete)
    {
        if (worker == null)
        {
            Debug.LogError(
                "SentisInferenceManager: Worker is not initialized."
            );

            onComplete?.Invoke(
                string.Empty,
                0f
            );

            return;
        }

        if (sketchTexture == null)
        {
            Debug.LogError(
                "SentisInferenceManager: Sketch texture is null."
            );

            onComplete?.Invoke(
                string.Empty,
                0f
            );

            return;
        }

        float[] processedData =
            SketchImagePreprocessor.Preprocess(
                sketchTexture
            );

        if (processedData == null ||
            processedData.Length != 4096)
        {
            Debug.LogError(
                "SentisInferenceManager: Sketch preprocessing failed."
            );

            onComplete?.Invoke(
                string.Empty,
                0f
            );

            return;
        }

        TensorShape inputShape =
            new TensorShape(
                1,
                64,
                64,
                1
            );

        using Tensor<float> inputTensor =
            new Tensor<float>(
                inputShape,
                processedData
            );

        worker.Schedule(inputTensor);

        Tensor<float> outputTensor =
            worker.PeekOutput()
            as Tensor<float>;

        if (outputTensor == null)
        {
            Debug.LogError(
                "SentisInferenceManager: Output tensor is null."
            );

            onComplete?.Invoke(
                string.Empty,
                0f
            );

            return;
        }

        using Tensor<float> cpuOutput =
            outputTensor.ReadbackAndClone();

        int bestIndex = 0;
        float bestConfidence =
            cpuOutput[0];

        Debug.Log(
            "----- ON-DEVICE AI RESULTS -----"
        );

        for (
            int i = 0;
            i < classLabels.Length;
            i++
        )
        {
            float confidence =
                cpuOutput[i];

            Debug.Log(
                classLabels[i] +
                " = " +
                confidence.ToString("F4")
            );

            if (confidence >
                bestConfidence)
            {
                bestConfidence =
                    confidence;

                bestIndex = i;
            }
        }

        string predictedLabel =
            classLabels[bestIndex];

        Debug.Log(
            "ON-DEVICE PREDICTION: " +
            predictedLabel +
            " | Confidence: " +
            bestConfidence.ToString("F4")
        );

        if (bestConfidence <
            minimumConfidence)
        {
            Debug.LogWarning(
                "On-device prediction rejected. " +
                "Confidence too low: " +
                bestConfidence.ToString("F4")
            );

            onComplete?.Invoke(
                string.Empty,
                bestConfidence
            );

            return;
        }

        onComplete?.Invoke(
            predictedLabel,
            bestConfidence
        );
    }

    private void OnDestroy()
    {
        if (worker != null)
        {
            worker.Dispose();
            worker = null;
        }
    }
    public void TestCurrentDrawing()
    {
        if (testDrawingPad == null)
        {
            Debug.LogError(
                "Test Drawing Pad is not assigned."
            );

            return;
        }

        if (!testDrawingPad.HasDrawing)
        {
            Debug.LogWarning(
                "Draw something before testing."
            );

            return;
        }

        RecognizeSketch(
            testDrawingPad.DrawingTexture,
            (label, confidence) =>
            {
                Debug.Log(
                    "TEST RESULT => Label: " +
                    label +
                    " | Confidence: " +
                    confidence.ToString("F4")
                );
            }
        );
    }
}
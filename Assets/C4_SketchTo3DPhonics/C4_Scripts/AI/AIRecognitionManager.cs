using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AIRecognitionManager : MonoBehaviour
{
    [Header("AI API")]
    [SerializeField]
    private string apiUrl =
        "http://127.0.0.1:8000/predict";

    [Header("Testing")]
    [SerializeField]
    private bool useRealAI = true;

    [SerializeField]
    private string fallbackLabel =
        "apple";

    [Header("Confidence")]
    [Range(0f, 1f)]
    [SerializeField]
    private float minimumConfidence =
        0.60f;

    // =========================================================
    // RECOGNIZE SKETCH
    // =========================================================

    public IEnumerator RecognizeSketch(
        string imagePath,
        Action<string, float>
            onComplete)
    {
        // -----------------------------------------------------
        // FALLBACK MODE
        // -----------------------------------------------------

        if (!useRealAI)
        {
            Debug.Log(
                "Real AI disabled. " +
                "Using fallback label."
            );

            yield return
                new WaitForSeconds(1f);

            onComplete?.Invoke(
                fallbackLabel,
                1f
            );

            yield break;
        }

        // -----------------------------------------------------
        // CHECK IMAGE
        // -----------------------------------------------------

        if (!File.Exists(imagePath))
        {
            Debug.LogError(
                "Sketch image does not exist: " +
                imagePath
            );

            onComplete?.Invoke(
                "",
                0f
            );

            yield break;
        }

        byte[] imageBytes =
            File.ReadAllBytes(
                imagePath
            );

        // -----------------------------------------------------
        // CREATE FORM
        // -----------------------------------------------------

        WWWForm form =
            new WWWForm();

        form.AddBinaryData(
            "file",
            imageBytes,
            "sketch.png",
            "image/png"
        );

        // -----------------------------------------------------
        // SEND REQUEST
        // -----------------------------------------------------

        using UnityWebRequest request =
            UnityWebRequest.Post(
                apiUrl,
                form
            );

        Debug.Log(
            "Sending sketch to AI..."
        );

        yield return
            request.SendWebRequest();

        // -----------------------------------------------------
        // REQUEST ERROR
        // -----------------------------------------------------

        if (
            request.result !=
            UnityWebRequest.Result.Success
        )
        {
            Debug.LogError(
                "AI API Error: " +
                request.error
            );

            onComplete?.Invoke(
                "",
                0f
            );

            yield break;
        }

        string json =
            request.downloadHandler.text;

        Debug.Log(
            "AI response: " +
            json
        );

        // -----------------------------------------------------
        // JSON
        // -----------------------------------------------------

        PredictionResponse prediction =
            JsonUtility.FromJson<
                PredictionResponse>(
                    json
                );

        if (
            prediction == null ||
            string.IsNullOrWhiteSpace(
                prediction.label
            )
        )
        {
            Debug.LogError(
                "Invalid AI response."
            );

            onComplete?.Invoke(
                "",
                0f
            );

            yield break;
        }

        string label =
            prediction.label
                .Trim()
                .ToLower();

        float confidence =
            prediction.confidence;

        Debug.Log(
            "AI predicted: " +
            label
        );

        Debug.Log(
            "AI confidence: " +
            confidence.ToString("F2")
        );

        // -----------------------------------------------------
        // CONFIDENCE CHECK
        // -----------------------------------------------------

        bool confidenceAccepted =
            confidence >=
            minimumConfidence;

        bool serverAccepted =
            prediction.accepted;

        if (
            !confidenceAccepted ||
            !serverAccepted
        )
        {
            Debug.LogWarning(
                "Prediction rejected because " +
                "confidence is too low."
            );

            onComplete?.Invoke(
                "",
                confidence
            );

            yield break;
        }

        // -----------------------------------------------------
        // SUCCESS
        // -----------------------------------------------------

        onComplete?.Invoke(
            label,
            confidence
        );
    }

    // =========================================================
    // RESPONSE FORMAT
    // =========================================================

    [Serializable]
    private class PredictionResponse
    {
        public string label;
        public float confidence;
        public bool accepted;
    }
}
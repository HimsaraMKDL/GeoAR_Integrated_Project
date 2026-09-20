using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CubeCorrectionGuidanceAnimator : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private CubeCorrectionManager correctionManager;

    [SerializeField]
    private Text correctionMessageText;

    [SerializeField]
    private Button tryAgainButton;


    // ==================================================
    // ANIMATION SETTINGS
    // ==================================================

    [Header("Finger Animation")]

    [SerializeField]
    private float moveDuration = 1.0f;

    [SerializeField]
    private float holdDuration = 1.2f;


    [Header("Wrong Face Pulse")]

    [SerializeField]
    private float pulseScale = 1.15f;

    [SerializeField]
    private float pulseDuration = 0.35f;

    [SerializeField]
    private int pulseCount = 3;


    [Header("Messages")]

    [SerializeField]
    private string wrongFaceMessage =
        "Look at this square!";

    [SerializeField]
    private string explanationMessage =
        "It may stop your cube from folding.";

    [SerializeField]
    private string suggestionMessage =
        "Try moving it here.";


    // ==================================================
    // RUNTIME
    // ==================================================

    private Coroutine guidanceRoutine;


    // ==================================================
    // START
    // ==================================================

    private void Awake()
    {
        if (tryAgainButton != null)
        {
            tryAgainButton.interactable = false;
        }
    }


    // ==================================================
    // START GUIDANCE
    // ==================================================

    public void PlayGuidance()
    {
        if (guidanceRoutine != null)
        {
            StopCoroutine(guidanceRoutine);
        }

        guidanceRoutine =
            StartCoroutine(
                GuidanceSequence()
            );
    }


    // ==================================================
    // MAIN SEQUENCE
    // ==================================================

    private IEnumerator GuidanceSequence()
    {
        if (correctionManager == null)
        {
            Debug.LogError(
                "CubeCorrectionGuidanceAnimator: " +
                "Correction Manager is not assigned."
            );

            yield break;
        }

        RectTransform wrongFace =
            correctionManager.GetWrongFaceRect();

        RectTransform suggestedFace =
            correctionManager.GetSuggestedFaceRect();

        CubeCorrectionData correction =
            correctionManager.GetCurrentCorrection();


        if (wrongFace == null)
        {
            Debug.LogError(
                "CubeCorrectionGuidanceAnimator: " +
                "Wrong face RectTransform not found."
            );

            yield break;
        }


        // ----------------------------------------------
        // RESET UI
        // ----------------------------------------------

        if (tryAgainButton != null)
        {
            tryAgainButton.interactable = false;
        }

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                "";
        }


        // ----------------------------------------------
        // STEP 1 — WRONG FACE MESSAGE
        // ----------------------------------------------

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                wrongFaceMessage;
        }


        // ----------------------------------------------
        // STEP 2 — PULSE WRONG FACE
        // ----------------------------------------------

        yield return StartCoroutine(
            PulseFace(
                wrongFace
            )
        );


        yield return new WaitForSeconds(
            holdDuration
        );


        // ----------------------------------------------
        // STEP 3 — EXPLANATION
        // ----------------------------------------------

        if (correctionMessageText != null)
        {
            if (correction != null &&
                !string.IsNullOrEmpty(
                    correction.message2))
            {
                correctionMessageText.text =
                    correction.message2;
            }
            else
            {
                correctionMessageText.text =
                    explanationMessage;
            }
        }


        yield return new WaitForSeconds(
            holdDuration
        );


        // ----------------------------------------------
        // STEP 4 — SUGGESTED FACE
        // ----------------------------------------------

        if (correction != null &&
            correction.hasSuggestion &&
            suggestedFace != null)
        {
            if (correctionMessageText != null)
            {
                correctionMessageText.text =
                    suggestionMessage;
            }


            yield return StartCoroutine(
                PulseFace(
                    suggestedFace
                )
            );


            yield return new WaitForSeconds(
                holdDuration
            );
        }
        else
        {
            /*
             * Fallback case:
             * no single-cell correction was found.
             */
            if (correctionMessageText != null)
            {
                correctionMessageText.text =
                    "Try changing this square and check the net again.";
            }


            yield return new WaitForSeconds(
                holdDuration
            );
        }


        // ----------------------------------------------
        // STEP 5 — FINISH
        // ----------------------------------------------

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                "Now try fixing your net.";
        }


        if (tryAgainButton != null)
        {
            tryAgainButton.interactable =
                true;
        }


        guidanceRoutine =
            null;


        Debug.Log(
            "Cube correction guidance completed."
        );
    }


    // ==================================================
    // PULSE FACE
    // ==================================================

    private IEnumerator PulseFace(
        RectTransform face)
    {
        if (face == null)
            yield break;


        Vector3 originalScale =
            face.localScale;


        Vector3 largerScale =
            originalScale *
            pulseScale;


        for (int count = 0;
             count < pulseCount;
             count++)
        {
            float timer =
                0f;


            // ------------------------------------------
            // SCALE UP
            // ------------------------------------------

            while (timer < pulseDuration)
            {
                timer +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        timer /
                        pulseDuration
                    );


                face.localScale =
                    Vector3.Lerp(
                        originalScale,
                        largerScale,
                        t
                    );


                yield return null;
            }


            timer =
                0f;


            // ------------------------------------------
            // SCALE DOWN
            // ------------------------------------------

            while (timer < pulseDuration)
            {
                timer +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        timer /
                        pulseDuration
                    );


                face.localScale =
                    Vector3.Lerp(
                        largerScale,
                        originalScale,
                        t
                    );


                yield return null;
            }


            face.localScale =
                originalScale;
        }
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetGuidance()
    {
        if (guidanceRoutine != null)
        {
            StopCoroutine(
                guidanceRoutine
            );

            guidanceRoutine =
                null;
        }


        if (tryAgainButton != null)
        {
            tryAgainButton.interactable =
                false;
        }


        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                "";
        }
    }
}
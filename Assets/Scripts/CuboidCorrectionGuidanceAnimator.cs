using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CuboidCorrectionGuidanceAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CuboidCorrectionManagerV2 correctionManager;

    [SerializeField]
    private Text correctionMessageText;

    [SerializeField]
    private Button tryAgainButton;

    [Header("Animation Settings")]
    [SerializeField]
    private float messageHoldDuration = 1.2f;

    [SerializeField]
    private float pulseDuration = 0.35f;

    [SerializeField]
    private int pulseCount = 3;

    [SerializeField]
    private float pulseScale = 1.12f;

    private Coroutine guidanceRoutine;


    public void PlayGuidance()
    {
        if (guidanceRoutine != null)
        {
            StopCoroutine(guidanceRoutine);
        }

        guidanceRoutine =
            StartCoroutine(GuidanceSequence());
    }


    private IEnumerator GuidanceSequence()
    {
        if (correctionManager == null)
        {
            Debug.LogError(
                "CuboidCorrectionGuidanceAnimator: " +
                "CuboidCorrectionManagerV2 is not assigned."
            );

            yield break;
        }

        RectTransform wrongFace =
            correctionManager.GetWrongFaceRect();

        RectTransform suggestedFace =
            correctionManager.GetSuggestedFaceRect();

        CuboidCorrectionData correction =
            correctionManager.GetCurrentCorrection();

        if (wrongFace == null)
        {
            Debug.LogError(
                "Cuboid correction wrong face visual was not found."
            );

            yield break;
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.interactable = false;
        }


        // ==================================================
        // STEP 1 - RED FACE
        // ==================================================

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                "Look at the red rectangle!";
        }

        yield return StartCoroutine(
            PulseRect(wrongFace)
        );

        yield return new WaitForSeconds(
            messageHoldDuration
        );


        // ==================================================
        // STEP 2 - EXPLAIN PROBLEM
        // ==================================================

        if (correctionMessageText != null)
        {
            if (correction != null &&
                !string.IsNullOrEmpty(correction.message2))
            {
                correctionMessageText.text =
                    correction.message2;
            }
            else
            {
                correctionMessageText.text =
                    "This face may stop the cuboid from folding correctly.";
            }
        }

        yield return new WaitForSeconds(
            messageHoldDuration
        );


        // ==================================================
        // STEP 3 - GREEN GUIDE
        // ==================================================

        if (correction != null &&
            correction.hasSuggestion &&
            suggestedFace != null)
        {
            if (correctionMessageText != null)
            {
                correctionMessageText.text =
                    GetSuggestionMessage(correction);
            }

            yield return StartCoroutine(
                PulseRect(suggestedFace)
            );

            yield return new WaitForSeconds(
                messageHoldDuration
            );
        }
        else
        {
            if (correctionMessageText != null)
            {
                correctionMessageText.text =
                    "This net needs more than one small change.";
            }

            yield return new WaitForSeconds(
                messageHoldDuration
            );
        }


        // ==================================================
        // FINISH
        // ==================================================

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                "Now try fixing your cuboid net.";
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.interactable = true;
        }

        guidanceRoutine = null;

        Debug.Log(
            "Cuboid correction guidance completed."
        );
    }


    private string GetSuggestionMessage(
        CuboidCorrectionData correction)
    {
        if (correction == null ||
            correction.wrongFace == null)
        {
            return
                "Use the green guide to fix the rectangle.";
        }

        bool positionChanged =
            correction.wrongFace.bottomLeft !=
            correction.suggestedBottomLeft;

        bool directSameSize =
            correction.wrongFace.gridWidth ==
                correction.suggestedWidth
            &&
            correction.wrongFace.gridHeight ==
                correction.suggestedHeight;

        bool rotatedSameSize =
            correction.wrongFace.gridWidth ==
                correction.suggestedHeight
            &&
            correction.wrongFace.gridHeight ==
                correction.suggestedWidth;


        // SIZE CHANGE
        if (!directSameSize &&
            !rotatedSameSize)
        {
            return
                "Use the green rectangle as the correct size guide.";
        }


        // ROTATE + MOVE
        if (!directSameSize &&
            rotatedSameSize &&
            positionChanged)
        {
            return
                "Turn this rectangle and move it to the green area.";
        }


        // ROTATE ONLY
        if (!directSameSize &&
            rotatedSameSize)
        {
            return
                "Turn this rectangle to match the green guide.";
        }


        // MOVE
        if (positionChanged)
        {
            return
                "Move this rectangle to the green area.";
        }


        return
            "Use the green guide to correct this rectangle.";
    }


    private IEnumerator PulseRect(
        RectTransform target)
    {
        if (target == null)
            yield break;

        Vector3 originalScale =
            target.localScale;

        Vector3 enlargedScale =
            originalScale * pulseScale;

        for (int i = 0;
             i < pulseCount;
             i++)
        {
            float timer = 0f;

            // SCALE UP
            while (timer < pulseDuration)
            {
                timer += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        timer / pulseDuration
                    );

                target.localScale =
                    Vector3.Lerp(
                        originalScale,
                        enlargedScale,
                        t
                    );

                yield return null;
            }

            timer = 0f;

            // SCALE DOWN
            while (timer < pulseDuration)
            {
                timer += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        timer / pulseDuration
                    );

                target.localScale =
                    Vector3.Lerp(
                        enlargedScale,
                        originalScale,
                        t
                    );

                yield return null;
            }

            target.localScale =
                originalScale;
        }
    }


    public void ResetGuidance()
    {
        if (guidanceRoutine != null)
        {
            StopCoroutine(guidanceRoutine);
            guidanceRoutine = null;
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.interactable = false;
        }

        if (correctionMessageText != null)
        {
            correctionMessageText.text = "";
        }
    }
}
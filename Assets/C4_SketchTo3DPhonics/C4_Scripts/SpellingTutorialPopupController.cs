using System.Collections;
using UnityEngine;

public class SpellingTutorialPopupController : MonoBehaviour
{
    [Header("Spelling Tutorial")]
    [SerializeField] private GameObject spellingTutorialOverlay;

    [Header("Popup Delay")]
    [SerializeField] private float popupDelay = 1.0f;

    private Coroutine popupCoroutine;


    // Called when the Spelling button is pressed
    public void ShowPopup()
    {
        // Stop an older delayed popup if one is already waiting
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        // Make sure popup is hidden during the delay
        if (spellingTutorialOverlay != null)
        {
            spellingTutorialOverlay.SetActive(false);
        }

        popupCoroutine = StartCoroutine(ShowPopupAfterDelay());
    }


    private IEnumerator ShowPopupAfterDelay()
    {
        // Wait before showing the tutorial
        yield return new WaitForSeconds(popupDelay);

        if (spellingTutorialOverlay != null)
        {
            spellingTutorialOverlay.SetActive(true);
        }

        popupCoroutine = null;
    }


    // Called when "Got it!" is pressed
    public void ClosePopup()
    {
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (spellingTutorialOverlay != null)
        {
            spellingTutorialOverlay.SetActive(false);
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PronunciationTutorialPopupController : MonoBehaviour
{
    [Header("Pronunciation Tutorial")]
    [SerializeField] private GameObject pronunciationTutorialOverlay;

    [Header("Pronunciation Button")]
    [SerializeField] private Button pronunciationButton;

    [Header("Popup Delay")]
    [SerializeField] private float popupDelay = 1.0f;

    private Coroutine popupCoroutine;


    private void Start()
    {
        // Popup should not be visible at the beginning
        if (pronunciationTutorialOverlay != null)
        {
            pronunciationTutorialOverlay.SetActive(false);
        }

        // Add this tutorial listener without changing
        // the existing pronunciation functionality.
        if (pronunciationButton != null)
        {
            pronunciationButton.onClick.AddListener(ShowPopup);
        }
    }


    public void ShowPopup()
    {
        // Stop an older delayed popup if one exists
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        // Keep popup hidden during the delay
        if (pronunciationTutorialOverlay != null)
        {
            pronunciationTutorialOverlay.SetActive(false);
        }

        popupCoroutine = StartCoroutine(ShowPopupAfterDelay());
    }


    private IEnumerator ShowPopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDelay);

        if (pronunciationTutorialOverlay != null)
        {
            pronunciationTutorialOverlay.SetActive(true);
        }

        popupCoroutine = null;
    }


    public void ClosePopup()
    {
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (pronunciationTutorialOverlay != null)
        {
            pronunciationTutorialOverlay.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        if (pronunciationButton != null)
        {
            pronunciationButton.onClick.RemoveListener(ShowPopup);
        }
    }
}
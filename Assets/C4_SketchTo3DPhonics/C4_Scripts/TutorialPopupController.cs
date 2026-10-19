using System.Collections;
using UnityEngine;

public class TutorialPopupController : MonoBehaviour
{
    [Header("Tutorial Popup")]
    [SerializeField] private GameObject tutorialOverlay;

    [Header("Popup Delay")]
    [SerializeField] private float popupDelay = 1.5f;

    private Coroutine showPopupCoroutine;



    public void ShowPopup()
    {
        if (showPopupCoroutine != null)
        {
            StopCoroutine(showPopupCoroutine);
        }

        showPopupCoroutine = StartCoroutine(ShowPopupAfterDelay());
    }



    private IEnumerator ShowPopupAfterDelay()
    {
        if (tutorialOverlay != null)
        {
            tutorialOverlay.SetActive(false);
        }

        yield return new WaitForSeconds(popupDelay);

        if (tutorialOverlay != null)
        {
            tutorialOverlay.SetActive(true);
        }

        showPopupCoroutine = null;
    }



    public void ClosePopup()
    {
        if (showPopupCoroutine != null)
        {
            StopCoroutine(showPopupCoroutine);
            showPopupCoroutine = null;
        }

        if (tutorialOverlay != null)
        {
            tutorialOverlay.SetActive(false);
        }
    }
}
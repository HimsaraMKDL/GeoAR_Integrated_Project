using UnityEngine;
using UnityEngine.SceneManagement;

public class UIScreenManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject introCanvas;
    [SerializeField] private GameObject sketchInputCanvas;
    [SerializeField] private GameObject wordValidationCanvas;

    private void Start()
    {
        ShowIntroScreen();
    }

    public void ShowIntroScreen()
    {
        if (introCanvas != null)
            introCanvas.SetActive(true);

        if (sketchInputCanvas != null)
            sketchInputCanvas.SetActive(false);

        if (wordValidationCanvas != null)
            wordValidationCanvas.SetActive(false);
    }

    public void ShowSketchScreen()
    {
        if (introCanvas != null)
            introCanvas.SetActive(false);

        if (sketchInputCanvas != null)
            sketchInputCanvas.SetActive(true);

        if (wordValidationCanvas != null)
            wordValidationCanvas.SetActive(false);

        FindObjectOfType<TutorialPopupController>()?.ShowPopup();
    }

    public void ShowValidationScreen()
    {
        if (introCanvas != null)
            introCanvas.SetActive(false);

        if (sketchInputCanvas != null)
            sketchInputCanvas.SetActive(false);

        if (wordValidationCanvas != null)
            wordValidationCanvas.SetActive(true);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
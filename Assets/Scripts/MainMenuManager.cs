using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void OpenGeometry()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OpenSlicing()
    {
        SceneManager.LoadScene("C2_Main_Test");
    }

    public void OpenSketchLearning()
    {
        SceneManager.LoadScene("C4_Main_AR_Learning_Scene");
    }
}
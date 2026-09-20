using UnityEngine;

public class GeometryMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject cubeWorkspacePanel;

    [Header("Cuboid")]
    public GameObject cuboidWorkspacePanel;

    [Header("Cylinder Workspaces")]
    [SerializeField] private GameObject cylinderModeSelectionPanel;
    [SerializeField] private GameObject cylinderGuidedWorkspacePanel;
    [SerializeField] private GameObject cylinderWorkspacePanel;

    [Header("Prism Workspaces")]
    [SerializeField] private GameObject prismModeSelectionPanel;
    public GameObject prismWorkspacePanel;
    [SerializeField] private GameObject prismGuidedWorkspacePanel;

    [SerializeField] private PrismGuidedSetupManager prismGuidedSetupManager;

    private void Start()
    {
        HideAllPanels();
    }

    public void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (cubeWorkspacePanel != null) cubeWorkspacePanel.SetActive(false);
        if (cuboidWorkspacePanel != null) cuboidWorkspacePanel.SetActive(false);
        if (cylinderWorkspacePanel != null) cylinderWorkspacePanel.SetActive(false);

        // Cylinder Panels Hide කිරීම
        if (cylinderModeSelectionPanel != null) cylinderModeSelectionPanel.SetActive(false);
        if (cylinderGuidedWorkspacePanel != null) cylinderGuidedWorkspacePanel.SetActive(false);

        // Prism Panels Hide කිරීම
        if (prismModeSelectionPanel != null) prismModeSelectionPanel.SetActive(false);
        if (prismWorkspacePanel != null) prismWorkspacePanel.SetActive(false);
        if (prismGuidedWorkspacePanel != null) prismGuidedWorkspacePanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        // ----------------------------------------------
        // RESET PRISM GUIDED SESSION
        // ----------------------------------------------
        if (prismGuidedSetupManager != null)
        {
            prismGuidedSetupManager.ResetGuidedActivity();
            Debug.Log("Prism Guided state reset before returning Home.");
        }

        // ----------------------------------------------
        // HIDE COMPONENT PANELS
        // ----------------------------------------------
        HideAllPanels();

        // ----------------------------------------------
        // SHOW HOME
        // ----------------------------------------------
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void OpenCubeWorkspace()
    {
        HideAllPanels();
        if (cubeWorkspacePanel != null) cubeWorkspacePanel.SetActive(true);
    }

    public void OpenCuboidWorkspace()
    {
        HideAllPanels();
        if (cuboidWorkspacePanel != null) cuboidWorkspacePanel.SetActive(true);
    }

    public void OpenPrismWorkspace()
    {
        HideAllPanels();
        if (prismWorkspacePanel != null) prismWorkspacePanel.SetActive(true);
    }

    // --- Prism Methods ---

    public void OpenPrismModeSelection()
    {
        Debug.Log("Opening Prism Mode Selection");
        HideAllPanels();

        if (prismModeSelectionPanel != null)
        {
            prismModeSelectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Prism Mode Selection Panel is not assigned.");
        }
    }

    public void OpenPrismFreeDrawWorkspace()
    {
        Debug.Log("Opening Prism Free Draw Workspace");
        HideAllPanels();

        if (prismWorkspacePanel != null)
        {
            prismWorkspacePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Prism Workspace Panel is not assigned.");
        }
    }

    public void OpenPrismGuidedWorkspace()
    {
        Debug.Log("Opening Prism Guided Workspace");
        HideAllPanels();

        if (prismGuidedWorkspacePanel == null)
        {
            Debug.LogError("Prism Guided Workspace Panel is not assigned.");
            return;
        }

        prismGuidedWorkspacePanel.SetActive(true);

        if (prismGuidedSetupManager != null)
        {
            prismGuidedSetupManager.ResetPrismForHome();
        }
        else
        {
            Debug.LogError("PrismGuidedSetupManager is not assigned.");
        }
    }

    // අලුතින් යෙදූ ReturnHomeFromPrismGuided Method එක
    public void ReturnHomeFromPrismGuided()
    {
        Debug.Log("Leaving Prism and returning Home.");

        // ----------------------------------------------
        // COMPLETE PRISM RESET FIRST
        // ----------------------------------------------

        if (prismGuidedSetupManager != null)
        {
            prismGuidedSetupManager.ResetPrismForHome();
        }
        else
        {
            Debug.LogError("PrismGuidedSetupManager is not assigned.");
        }

        // ----------------------------------------------
        // THEN CHANGE SCREEN
        // ----------------------------------------------

        HideAllPanels();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    // --- Cylinder Methods ---

    public void OpenCylinderModeSelection()
    {
        Debug.Log("Opening Cylinder Mode Selection");
        HideAllPanels();

        if (cylinderModeSelectionPanel != null)
        {
            cylinderModeSelectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Cylinder Mode Selection Panel is not assigned.");
        }
    }

    public void OpenCylinderGuidedWorkspace()
    {
        Debug.Log("Opening Cylinder Guided Workspace");
        HideAllPanels();

        if (cylinderGuidedWorkspacePanel != null)
        {
            cylinderGuidedWorkspacePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Cylinder Guided Workspace Panel is not assigned.");
        }
    }

    public void OpenCylinderFreeDrawWorkspace()
    {
        Debug.Log("Opening Cylinder Free Draw Workspace");
        HideAllPanels();

        if (cylinderWorkspacePanel != null)
        {
            cylinderWorkspacePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Cylinder Free Draw Workspace is not created yet.");
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Runtime-only guidance/polish layer for the C1 Geometry component.
///
/// This script intentionally creates its own UI at runtime so the integrated
/// SampleScene does not need to be edited or re-wired in the Inspector.
/// It provides:
///  - first-time student onboarding
///  - shape-specific coach tips and replayable Help button
///  - a Draw -> Validate -> Fold -> AR progress indicator
///  - bundled UI sound effects with runtime-generated fallbacks
///  - contextual toast messages for validation, folding and AR placement
///  - V4 animated blue/cyan/purple button glow for the learner's next action
///  - gentle press-and-release bounce feedback on guided action buttons
///  - V8 image-like onboarding illustrations for primary learners
///
/// The system runs only in SampleScene and does not change C2/C3/C4 scenes.
/// </summary>
public class C1UserGuidanceSystem : MonoBehaviour
{
    private const string GeometrySceneName = "SampleScene";
    private const string SoundPreferenceKey = "C1_Guidance_Sound_Enabled";

    private static C1UserGuidanceSystem instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("C1UserGuidanceSystem");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<C1UserGuidanceSystem>();
    }

    private Canvas guidanceCanvas;
    private GameObject guideOverlay;
    private Text guideTitleText;
    private Text guideBodyText;
    private Text guidePageText;
    private Button guideLeftButton;
    private Text guideLeftButtonText;
    private Button guideRightButton;
    private Text guideRightButtonText;

    private GameObject progressRoot;
    private Text progressText;
    private Button helpButton;
    private Button soundButton;
    private Text soundButtonText;

    private GameObject toastRoot;
    private Text toastText;
    private Coroutine toastRoutine;

    private AudioSource audioSource;
    private AudioClip clickClip;
    private AudioClip tutorialClip;
    private AudioClip successClip;
    private AudioClip errorClip;
    private AudioClip foldClip;
    private AudioClip arStartClip;
    private AudioClip placedClip;
    private bool soundEnabled = true;

    private Sprite roundedSprite;
    private readonly Dictionary<Button, C1AnimatedButtonGlow> buttonGlowEffects = new Dictionary<Button, C1AnimatedButtonGlow>();
    private Text guideBadgeText;
    private Text guideIconText;
    // V8: small code-generated visual illustrations for the four onboarding steps.
    // These are built from Unity UI primitives, so no extra image assets or
    // Inspector wiring are required in the integrated project.
    private GameObject onboardingIllustrationRoot;
    private GameObject[] onboardingIllustrations = new GameObject[4];
    private readonly List<Text> legacyStatusTexts = new List<Text>();
    private Coroutine validationFeedbackRoutine;

    private GameObject studentEntryPanel;
    private GameObject mainMenuPanel;
    private GameObject cubeWorkspacePanel;
    private GameObject cuboidWorkspacePanel;
    private GameObject tetrahedronWorkspacePanel;
    private GameObject prismModeSelectionPanel;
    private GameObject prismGuidedWorkspacePanel;
    private GameObject cylinderModeSelectionPanel;
    private GameObject cylinderGuidedWorkspacePanel;
    private GameObject arOverlayCanvas;

    private readonly List<Button> validateButtons = new List<Button>();
    private readonly List<Button> create3DButtons = new List<Button>();
    private readonly List<Button> goToARButtons = new List<Button>();
    private readonly List<GameObject> invalidPopupPanels = new List<GameObject>();

    private ARPlacedModelManager arPlacedModelManager;

    private int boundSceneHandle = -1;
    private bool isGeometryScene;
    private string currentStudentID = string.Empty;
    private string currentShape = string.Empty;
    private int currentStage = 1;
    private bool previousInvalidVisible;
    private bool previousCreate3DInteractable;
    private bool previousGoToARVisible;
    private Transform observedARModel;
    private bool placementCelebrated;

    private enum GuideMode
    {
        None,
        Onboarding,
        ShapeCoach
    }

    private GuideMode guideMode = GuideMode.None;
    private int guideIndex;
    private string coachShape = string.Empty;

    private readonly string[] onboardingTitles =
    {
        "Welcome to Geometry Learning",
        "Build Your 2D Net",
        "Check Your Net",
        "Fold It and Explore in AR"
    };

    private readonly string[] onboardingBodies =
    {
        "Choose a shape and discover how a flat 2D net can become a real 3D solid.",
        "Draw on the grid or arrange the given faces. Keep them connected. Use Undo or Clear whenever you want to try again.",
        "Tap Validate when you are ready. If the net is correct, you can continue. If not, use the feedback and try again.",
        "Tap Create 3D to watch the net fold. Then tap Go To AR, move the phone slowly, and tap a detected surface to place your shape."
    };

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        soundEnabled = PlayerPrefs.GetInt(SoundPreferenceKey, 1) == 1;

        BuildAudio();
        BuildGuidanceUI();

        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(BindAfterFrame());
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(BindAfterFrame());
    }

    private IEnumerator BindAfterFrame()
    {
        yield return null;
        BindScene();
    }

    private void BindScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        boundSceneHandle = scene.handle;
        isGeometryScene = scene.name == GeometrySceneName;

        if (guidanceCanvas != null)
            guidanceCanvas.gameObject.SetActive(isGeometryScene);

        validateButtons.Clear();
        create3DButtons.Clear();
        goToARButtons.Clear();
        invalidPopupPanels.Clear();
        legacyStatusTexts.Clear();
        buttonGlowEffects.Clear();

        studentEntryPanel = null;
        mainMenuPanel = null;
        cubeWorkspacePanel = null;
        cuboidWorkspacePanel = null;
        tetrahedronWorkspacePanel = null;
        prismModeSelectionPanel = null;
        prismGuidedWorkspacePanel = null;
        cylinderModeSelectionPanel = null;
        cylinderGuidedWorkspacePanel = null;
        arOverlayCanvas = null;
        arPlacedModelManager = null;

        currentShape = string.Empty;
        currentStage = 1;
        previousInvalidVisible = false;
        previousCreate3DInteractable = false;
        previousGoToARVisible = false;
        observedARModel = null;
        placementCelebrated = false;
        HideGuide();
        HideToastImmediate();
        SetFloatingUIVisible(false);

        if (!isGeometryScene)
            return;

        studentEntryPanel = FindSceneObject("StudentEntryPanel");
        mainMenuPanel = FindSceneObject("MainMenuPanel");
        cubeWorkspacePanel = FindSceneObject("CubeWorkspacePanel");
        cuboidWorkspacePanel = FindSceneObject("CuboidWorkspacePanel");
        tetrahedronWorkspacePanel = FindSceneObject("TetrahedronWorkspacePanel");
        prismModeSelectionPanel = FindSceneObject("PrismModeSelectionPanel");
        prismGuidedWorkspacePanel = FindSceneObject("PrismGuidedWorkspacePanel");
        cylinderModeSelectionPanel = FindSceneObject("CylinderModeSelectionPanel");
        cylinderGuidedWorkspacePanel = FindSceneObject("CylinderGuidedWorkspacePanel");
        arOverlayCanvas = FindSceneObject("AROverlayCanvas");

        GameObject arManagerObject = FindSceneObject("ARPlacedModelManager");
        if (arManagerObject != null)
            arPlacedModelManager = arManagerObject.GetComponent<ARPlacedModelManager>();

        Button[] allButtons = FindSceneComponents<Button>();
        foreach (Button button in allButtons)
        {
            if (button == null)
                continue;

            string objectName = button.gameObject.name;
            bool guidedActionButton = false;

            if (objectName.IndexOf("Validate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("CheckPuzzle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                validateButtons.Add(button);
                button.onClick.RemoveListener(OnValidateClicked);
                button.onClick.AddListener(OnValidateClicked);
                guidedActionButton = true;
            }

            if (objectName.IndexOf("Create3D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Create 3D", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                create3DButtons.Add(button);
                button.onClick.RemoveListener(OnCreate3DClicked);
                button.onClick.AddListener(OnCreate3DClicked);
                guidedActionButton = true;
            }

            if (objectName.IndexOf("GoToAR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Go To AR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                goToARButtons.Add(button);
                button.onClick.RemoveListener(OnGoToARClicked);
                button.onClick.AddListener(OnGoToARClicked);
                guidedActionButton = true;
            }

            if (guidedActionButton)
                RegisterAnimatedGlow(button);
        }

        AddPopupIfFound("CubeInvalidPopupPanel");
        AddPopupIfFound("CuboidInvalidPopupPanel");
        BindLegacyStatusTexts();

        RefreshSoundButtonLabel();
    }

    private void AddPopupIfFound(string objectName)
    {
        GameObject panel = FindSceneObject(objectName);
        if (panel != null)
            invalidPopupPanels.Add(panel);
    }

    private void BindLegacyStatusTexts()
    {
        Text[] allTexts = FindSceneComponents<Text>();
        foreach (Text textItem in allTexts)
        {
            if (textItem == null || !IsLegacyStatusTextName(textItem.gameObject.name))
                continue;

            legacyStatusTexts.Add(textItem);

            CanvasGroup group = textItem.GetComponent<CanvasGroup>();
            if (group == null)
                group = textItem.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            textItem.raycastTarget = false;
        }
    }

    private bool IsLegacyStatusTextName(string objectName)
    {
        return objectName == "StatusText" ||
               objectName == "CuboidStatusText" ||
               objectName == "TetrahedronStatusText" ||
               objectName == "GuidedStatusText" ||
               objectName == "CylinderGuidedStatusText" ||
               objectName == "ARSurfaceStatusText";
    }

    private void KeepLegacyStatusTextsHidden()
    {
        foreach (Text textItem in legacyStatusTexts)
        {
            if (textItem == null)
                continue;

            CanvasGroup group = textItem.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            textItem.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (!isGeometryScene)
            return;

        KeepLegacyStatusTextsHidden();

        if (SceneManager.GetActiveScene().handle != boundSceneHandle)
        {
            BindScene();
            return;
        }

        string studentID = StudentProfileManager.CurrentStudentID ?? string.Empty;
        if (studentID != currentStudentID)
        {
            currentStudentID = studentID;
            currentShape = string.Empty;
            currentStage = 1;
        }

        bool studentEntryVisible = IsVisible(studentEntryPanel);
        bool mainMenuVisible = IsVisible(mainMenuPanel);
        bool arVisible = IsVisible(arOverlayCanvas);

        if (studentEntryVisible || string.IsNullOrEmpty(currentStudentID))
        {
            SetAllActionGlows(false);
            if (guideMode == GuideMode.None)
                SetFloatingUIVisible(false);
            return;
        }

        if (mainMenuVisible && !arVisible)
        {
            currentShape = string.Empty;
            currentStage = 1;
            SetAllActionGlows(false);
            SetProgressVisible(false);
            SetHelpAndSoundVisible(guideMode == GuideMode.None);

            if (guideMode == GuideMode.None && !HasSeenOnboarding())
                ShowOnboarding();

            return;
        }

        string detectedShape = DetectActiveShape();
        bool workspaceVisible = IsAnyWorkspaceVisible();

        if (!string.IsNullOrEmpty(detectedShape) && detectedShape != currentShape)
        {
            currentShape = detectedShape;
            currentStage = 1;
            previousCreate3DInteractable = IsCurrentCreate3DInteractable();
            previousGoToARVisible = IsAnyGoToARVisible();
            placementCelebrated = false;

            if (guideMode == GuideMode.None && workspaceVisible && !HasSeenCoach(currentShape))
                ShowShapeCoach(currentShape);
        }

        if (arVisible)
            currentStage = 4;

        bool goToARVisible = IsAnyGoToARVisible();
        if (goToARVisible && !previousGoToARVisible)
        {
            currentStage = 4;
            PlaySound(foldClip);
            ShowToast("Great! Your 3D shape is ready. Tap Go To AR to explore it in the real world.", 3.0f);
        }
        previousGoToARVisible = goToARVisible;

        bool createInteractable = IsCurrentCreate3DInteractable();
        if (createInteractable && !previousCreate3DInteractable && !arVisible)
        {
            currentStage = 3;
            PlaySound(successClip);
            ShowToast("Correct! Your net is valid. Tap Create 3D to watch it fold.", 2.8f);
        }
        previousCreate3DInteractable = createInteractable;

        bool invalidVisible = IsAnyInvalidPopupVisible();
        if (invalidVisible && !previousInvalidVisible)
        {
            currentStage = 2;
            PlaySound(errorClip);
            ShowToast("Almost there! Check the hint, fix the net, and try again.", 2.8f);
        }
        previousInvalidVisible = invalidVisible;

        CheckARPlacementCelebration(arVisible);

        bool keepProgressDuringFold = !mainMenuVisible && !studentEntryVisible && !string.IsNullOrEmpty(currentShape);
        SetProgressVisible((workspaceVisible || keepProgressDuringFold || arVisible) && guideMode == GuideMode.None);
        SetHelpAndSoundVisible(guideMode == GuideMode.None && !studentEntryVisible);
        UpdateProgressText();
        UpdateActionButtonHighlights(workspaceVisible, arVisible, createInteractable, goToARVisible);
    }

    private void OnValidateClicked()
    {
        if (string.IsNullOrEmpty(currentShape))
            currentShape = DetectActiveShape();

        currentStage = 2;
        PlaySound(clickClip);
        ShowToast("Checking your net...", 1.0f);
        UpdateProgressText();

        if (validationFeedbackRoutine != null)
            StopCoroutine(validationFeedbackRoutine);

        validationFeedbackRoutine = StartCoroutine(ShowValidationFeedbackAfterDelay());
    }

    private IEnumerator ShowValidationFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(0.45f);

        bool valid = IsCurrentCreate3DInteractable();
        if (!valid && !IsAnyInvalidPopupVisible())
        {
            PlaySound(errorClip);
            ShowToast(GetTryAgainText(currentShape), 2.8f);
        }

        validationFeedbackRoutine = null;
    }

    private string GetTryAgainText(string shape)
    {
        switch (shape)
        {
            case "Cube":
                return "Not quite yet. Check that you have 6 connected square faces, then try again.";
            case "Cuboid":
                return "Not quite yet. Check the 6 connected rectangular faces and shared edges, then try again.";
            case "Tetrahedron":
                return "Not quite yet. Check that 4 triangles are connected correctly, then try again.";
            case "Triangular Prism":
                return "Not quite yet. Check the 3 rectangles and 2 triangular ends, then try again.";
            case "Cylinder":
                return "Not quite yet. Check the rectangle and the 2 equal circular ends, then try again.";
            default:
                return "Not quite yet. Check the face connections and try again.";
        }
    }

    private void OnCreate3DClicked()
    {
        currentStage = 3;
        PlaySound(foldClip);
        ShowToast("Watch closely: your 2D net is folding into a 3D shape!", 2.5f);
        UpdateProgressText();
    }

    private void OnGoToARClicked()
    {
        currentStage = 4;
        PlaySound(arStartClip);
        ShowToast("Move your phone slowly to find a flat surface, then tap to place the shape.", 3.2f);
        UpdateProgressText();
    }

    private void CheckARPlacementCelebration(bool arVisible)
    {
        if (!arVisible || arPlacedModelManager == null)
            return;

        Transform model = arPlacedModelManager.modelToPlace;
        if (model == null)
            return;

        if (observedARModel != model)
        {
            observedARModel = model;
            placementCelebrated = false;
        }

        if (!placementCelebrated && model.gameObject.activeInHierarchy)
        {
            placementCelebrated = true;
            PlaySound(placedClip);
            ShowToast("Awesome! Your shape is now placed in AR. Move around and explore it!", 3.5f);
        }
    }

    private string DetectActiveShape()
    {
        if (IsVisible(cubeWorkspacePanel))
            return "Cube";

        if (IsVisible(cuboidWorkspacePanel))
            return "Cuboid";

        if (IsVisible(tetrahedronWorkspacePanel))
            return "Tetrahedron";

        if (IsVisible(prismGuidedWorkspacePanel) || IsVisible(prismModeSelectionPanel))
            return "Triangular Prism";

        if (IsVisible(cylinderGuidedWorkspacePanel) || IsVisible(cylinderModeSelectionPanel))
            return "Cylinder";

        return currentShape;
    }

    private bool IsAnyWorkspaceVisible()
    {
        return IsVisible(cubeWorkspacePanel) ||
               IsVisible(cuboidWorkspacePanel) ||
               IsVisible(tetrahedronWorkspacePanel) ||
               IsVisible(prismGuidedWorkspacePanel) ||
               IsVisible(cylinderGuidedWorkspacePanel);
    }

    private bool IsCurrentCreate3DInteractable()
    {
        foreach (Button button in create3DButtons)
        {
            if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                return true;
        }

        return false;
    }

    private bool IsAnyGoToARVisible()
    {
        foreach (Button button in goToARButtons)
        {
            if (button != null && button.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void RegisterAnimatedGlow(Button button)
    {
        if (button == null)
            return;

        C1AnimatedButtonGlow glow = button.GetComponent<C1AnimatedButtonGlow>();
        if (glow == null)
            glow = button.gameObject.AddComponent<C1AnimatedButtonGlow>();

        glow.Initialize(button);
        buttonGlowEffects[button] = glow;
    }

    private void UpdateActionButtonHighlights(bool workspaceVisible, bool arVisible, bool createInteractable, bool goToARVisible)
    {
        SetAllActionGlows(false);

        if (guideMode != GuideMode.None || arVisible)
            return;

        // Only the learner's next meaningful action receives the animated glow.
        // This keeps the colourful GeoAR UI clean while still guiding first-time users.
        if ((currentStage == 1 || currentStage == 2) && workspaceVisible)
        {
            SetGroupGlow(validateButtons, true);
            return;
        }

        if (currentStage == 3 && createInteractable)
        {
            SetGroupGlow(create3DButtons, true);
            return;
        }

        if (currentStage == 4 && goToARVisible)
            SetGroupGlow(goToARButtons, true);
    }

    private void SetGroupGlow(List<Button> buttons, bool highlighted)
    {
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            C1AnimatedButtonGlow glow;
            if (!buttonGlowEffects.TryGetValue(button, out glow) || glow == null)
                continue;

            bool usable = button.gameObject.activeInHierarchy && button.interactable;
            glow.SetHighlighted(highlighted && usable);
        }
    }

    private void SetAllActionGlows(bool highlighted)
    {
        foreach (KeyValuePair<Button, C1AnimatedButtonGlow> pair in buttonGlowEffects)
        {
            if (pair.Value != null)
                pair.Value.SetHighlighted(highlighted);
        }
    }

    private bool IsAnyInvalidPopupVisible()
    {
        foreach (GameObject panel in invalidPopupPanels)
        {
            if (IsVisible(panel))
                return true;
        }

        return false;
    }

    private bool IsVisible(GameObject target)
    {
        return target != null && target.activeInHierarchy;
    }

    private void SetFloatingUIVisible(bool visible)
    {
        SetProgressVisible(visible);
        SetHelpAndSoundVisible(visible);
    }

    private void SetProgressVisible(bool visible)
    {
        if (progressRoot != null)
            progressRoot.SetActive(visible);
    }

    private void SetHelpAndSoundVisible(bool visible)
    {
        if (helpButton != null)
            helpButton.gameObject.SetActive(visible);

        if (soundButton != null)
            soundButton.gameObject.SetActive(visible);
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        string[] labels = { "BUILD", "VALIDATE", "FOLD", "AR" };
        string result = string.Empty;

        for (int i = 0; i < labels.Length; i++)
        {
            int step = i + 1;
            bool active = step == Mathf.Clamp(currentStage, 1, 4);
            bool done = step < currentStage;

            string marker;
            string label;

            if (active)
            {
                marker = "<color=#2D7FF9><b>●</b></color>";
                label = "<color=#123B7A><b>" + labels[i] + "</b></color>";
            }
            else if (done)
            {
                marker = "<color=#35A86B>●</color>";
                label = "<color=#3F5D67>" + labels[i] + "</color>";
            }
            else
            {
                marker = "<color=#B5C1D1>○</color>";
                label = "<color=#697A90>" + labels[i] + "</color>";
            }

            result += marker + " " + label;
            if (i < labels.Length - 1)
                result += "   <color=#C9D4E3>›</color>   ";
        }

        progressText.text = result;
    }

    private bool HasSeenOnboarding()
    {
        if (string.IsNullOrEmpty(currentStudentID))
            return true;

        return PlayerPrefs.GetInt("C1_Onboarding_" + currentStudentID, 0) == 1;
    }

    private void MarkOnboardingSeen()
    {
        if (string.IsNullOrEmpty(currentStudentID))
            return;

        PlayerPrefs.SetInt("C1_Onboarding_" + currentStudentID, 1);
        PlayerPrefs.Save();
    }

    private bool HasSeenCoach(string shape)
    {
        if (string.IsNullOrEmpty(currentStudentID) || string.IsNullOrEmpty(shape))
            return true;

        return PlayerPrefs.GetInt("C1_Coach_" + currentStudentID + "_" + shape.Replace(" ", "_"), 0) == 1;
    }

    private void MarkCoachSeen(string shape)
    {
        if (string.IsNullOrEmpty(currentStudentID) || string.IsNullOrEmpty(shape))
            return;

        PlayerPrefs.SetInt("C1_Coach_" + currentStudentID + "_" + shape.Replace(" ", "_"), 1);
        PlayerPrefs.Save();
    }

    private void ShowOnboarding()
    {
        guideMode = GuideMode.Onboarding;
        SetAllActionGlows(false);
        guideIndex = 0;
        coachShape = string.Empty;
        SetFloatingUIVisible(false);
        guideOverlay.SetActive(true);
        guideOverlay.transform.SetAsLastSibling();
        RefreshGuideContent();
        PlaySound(tutorialClip);
    }

    private void ShowShapeCoach(string shape)
    {
        if (string.IsNullOrEmpty(shape))
            return;

        guideMode = GuideMode.ShapeCoach;
        SetAllActionGlows(false);
        guideIndex = 0;
        coachShape = shape;
        SetFloatingUIVisible(false);
        guideOverlay.SetActive(true);
        guideOverlay.transform.SetAsLastSibling();
        RefreshGuideContent();
        PlaySound(clickClip);
    }

    private void HideGuide()
    {
        if (guideOverlay != null)
            guideOverlay.SetActive(false);

        guideMode = GuideMode.None;
    }

    private void OnGuideLeftPressed()
    {
        PlaySound(clickClip);

        if (guideMode == GuideMode.Onboarding)
        {
            MarkOnboardingSeen();
            HideGuide();
            ShowToast("Choose a shape to begin. You can tap HELP anytime.", 2.8f);
        }
        else if (guideMode == GuideMode.ShapeCoach)
        {
            MarkCoachSeen(coachShape);
            HideGuide();
        }
    }

    private void OnGuideRightPressed()
    {
        PlaySound(clickClip);

        if (guideMode == GuideMode.Onboarding)
        {
            guideIndex++;

            if (guideIndex >= onboardingTitles.Length)
            {
                MarkOnboardingSeen();
                HideGuide();
                ShowToast("You're ready! Choose a shape and start building.", 2.8f);
                return;
            }

            RefreshGuideContent();
            return;
        }

        if (guideMode == GuideMode.ShapeCoach)
        {
            MarkCoachSeen(coachShape);
            HideGuide();
            ShowToast("Tip saved. Tap HELP if you want to see it again.", 2.0f);
        }
    }

    private void RefreshGuideContent()
    {
        if (guideMode == GuideMode.Onboarding)
        {
            guideBadgeText.text = "GEOMETRY GUIDE";
            guideIconText.text = GetOnboardingIcon(guideIndex);
            guideTitleText.text = onboardingTitles[guideIndex];
            guideBodyText.text = onboardingBodies[guideIndex];
            guideBodyText.fontSize = 30;
            SetRect(guideBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -132f), new Vector2(720f, 182f));
            guidePageText.text = BuildPageDots(guideIndex, onboardingTitles.Length);
            SetOnboardingIllustration(guideIndex, true);
            guideLeftButton.gameObject.SetActive(true);
            guideLeftButtonText.text = "SKIP";

            // V11: restore the normal right-side position for the onboarding
            // NEXT / LET'S START button. Shape Quick Help temporarily centres
            // this same button, so each mode explicitly owns its layout.
            SetRect(guideRightButton.GetComponent<RectTransform>(),
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-240f, 82f), new Vector2(320f, 96f));
            guideRightButtonText.text = guideIndex == onboardingTitles.Length - 1 ? "LET'S START" : "NEXT";
            return;
        }

        if (guideMode == GuideMode.ShapeCoach)
        {
            guideBadgeText.text = "QUICK HELP";
            guideIconText.text = GetShapeIcon(coachShape);
            SetOnboardingIllustration(0, false);
            guideTitleText.text = coachShape + " Guide";
            guideBodyText.text = GetCoachText(coachShape);
            guideBodyText.fontSize = 34;
            SetRect(guideBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(720f, 340f));
            guidePageText.text = "Tap HELP anytime to view this again";
            guideLeftButton.gameObject.SetActive(false);

            // V11: centre the single GOT IT button on every shape Quick Help
            // card so the action is visually balanced for primary learners.
            SetRect(guideRightButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 82f), new Vector2(320f, 96f));
            guideRightButtonText.text = "GOT IT";
        }
    }

    private string BuildPageDots(int activeIndex, int count)
    {
        string dots = string.Empty;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                dots += "   ";

            dots += i == activeIndex
                ? "<color=#2D7FF9><b>●</b></color>"
                : "<color=#CCD7E7>●</color>";
        }

        return dots + "\n<color=#72839B>STEP " + (activeIndex + 1) + " OF " + count + "</color>";
    }

    private string GetOnboardingIcon(int index)
    {
        switch (index)
        {
            case 0: return "2D→3D";
            case 1: return "2D";
            case 2: return "✓";
            case 3: return "AR";
            default: return "?";
        }
    }


    // V8 onboarding illustrations -------------------------------------------------
    // Built entirely from UI primitives so this patch remains self-contained.
    private void BuildOnboardingIllustrations(Transform parent)
    {
        onboardingIllustrationRoot = CreatePanel("OnboardingVisualRoot", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 10f), new Vector2(680f, 170f),
            new Color(0.94f, 0.97f, 1f, 1f));
        ApplyRounded(onboardingIllustrationRoot.GetComponent<Image>());
        onboardingIllustrationRoot.GetComponent<Image>().raycastTarget = false;

        // Step 1: choose a shape -> 2D to 3D discovery.
        GameObject step1 = CreateVisualStepRoot("Step1Visual", onboardingIllustrationRoot.transform);
        CreateMiniTile(step1.transform, new Vector2(-205f, 0f), "□", "CUBE", new Color(0.22f, 0.53f, 0.96f, 1f));
        CreateMiniTile(step1.transform, new Vector2(0f, 0f), "△", "PRISM", new Color(0.40f, 0.42f, 0.96f, 1f));
        CreateMiniTile(step1.transform, new Vector2(205f, 0f), "○", "CYLINDER", new Color(0.16f, 0.68f, 0.78f, 1f));
        onboardingIllustrations[0] = step1;

        // Step 2: a simple connected six-square net with a finger/path cue.
        GameObject step2 = CreateVisualStepRoot("Step2Visual", onboardingIllustrationRoot.transform);
        Vector2[] netCells =
        {
            new Vector2(-52f, 0f), new Vector2(0f, 0f), new Vector2(52f, 0f), new Vector2(104f, 0f),
            new Vector2(0f, 52f), new Vector2(0f, -52f)
        };
        for (int i = 0; i < netCells.Length; i++)
            CreateVisualSquare(step2.transform, netCells[i] + new Vector2(-70f, 0f), 44f, new Color(0.82f, 0.90f, 1f, 1f));
        Text drawArrow = CreateText("DrawCue", step2.transform, 34, TextAnchor.MiddleCenter, new Color(0.18f, 0.45f, 0.86f, 1f));
        drawArrow.text = "DRAW  →";
        drawArrow.fontStyle = FontStyle.Bold;
        SetRect(drawArrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(215f, 0f), new Vector2(220f, 70f));
        onboardingIllustrations[1] = step2;

        // Step 3: net + large validation check.
        GameObject step3 = CreateVisualStepRoot("Step3Visual", onboardingIllustrationRoot.transform);
        for (int i = 0; i < netCells.Length; i++)
            CreateVisualSquare(step3.transform, netCells[i] + new Vector2(-125f, 0f), 39f, new Color(0.86f, 0.92f, 1f, 1f));
        GameObject checkBadge = CreatePanel("CheckBadge", step3.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, 0f), new Vector2(150f, 92f),
            new Color(0.20f, 0.70f, 0.43f, 1f));
        ApplyRounded(checkBadge.GetComponent<Image>());
        checkBadge.GetComponent<Image>().raycastTarget = false;
        Text checkText = CreateText("CheckText", checkBadge.transform, 36, TextAnchor.MiddleCenter, Color.white);
        checkText.text = "✓  VALID";
        checkText.fontStyle = FontStyle.Bold;
        SetStretch(checkText.rectTransform, 8f, 6f, 8f, 6f);
        onboardingIllustrations[2] = step3;

        // Step 4: 2D net -> 3D -> AR phone. This is intentionally pictorial
        // rather than text-heavy so younger students can understand the flow.
        GameObject step4 = CreateVisualStepRoot("Step4Visual", onboardingIllustrationRoot.transform);
        GameObject netBadge = CreatePanel("NetBadge", step4.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, 0f), new Vector2(126f, 92f),
            new Color(0.83f, 0.90f, 1f, 1f));
        ApplyRounded(netBadge.GetComponent<Image>());
        netBadge.GetComponent<Image>().raycastTarget = false;
        Text netText = CreateText("NetText", netBadge.transform, 27, TextAnchor.MiddleCenter, new Color(0.12f, 0.36f, 0.72f, 1f));
        netText.text = "2D NET";
        netText.fontStyle = FontStyle.Bold;
        SetStretch(netText.rectTransform, 6f, 6f, 6f, 6f);

        CreateFlowArrow(step4.transform, new Vector2(-115f, 0f));

        GameObject solidBadge = CreatePanel("SolidBadge", step4.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(126f, 92f),
            new Color(0.44f, 0.43f, 0.96f, 1f));
        ApplyRounded(solidBadge.GetComponent<Image>());
        solidBadge.GetComponent<Image>().raycastTarget = false;
        Text solidText = CreateText("SolidText", solidBadge.transform, 29, TextAnchor.MiddleCenter, Color.white);
        solidText.text = "3D";
        solidText.fontStyle = FontStyle.Bold;
        SetStretch(solidText.rectTransform, 6f, 6f, 6f, 6f);

        CreateFlowArrow(step4.transform, new Vector2(115f, 0f));

        GameObject phone = CreatePanel("Phone", step4.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(225f, 0f), new Vector2(112f, 130f),
            new Color(0.10f, 0.18f, 0.31f, 1f));
        ApplyRounded(phone.GetComponent<Image>());
        phone.GetComponent<Image>().raycastTarget = false;
        GameObject phoneScreen = CreatePanel("PhoneScreen", phone.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(82f, 92f),
            new Color(0.74f, 0.91f, 1f, 1f));
        ApplyRounded(phoneScreen.GetComponent<Image>());
        phoneScreen.GetComponent<Image>().raycastTarget = false;
        Text arText = CreateText("ARText", phoneScreen.transform, 27, TextAnchor.MiddleCenter, new Color(0.10f, 0.42f, 0.73f, 1f));
        arText.text = "AR";
        arText.fontStyle = FontStyle.Bold;
        SetStretch(arText.rectTransform, 4f, 4f, 4f, 4f);
        onboardingIllustrations[3] = step4;

        SetOnboardingIllustration(0, false);
    }

    private GameObject CreateVisualStepRoot(string name, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.layer = 5;
        root.transform.SetParent(parent, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        SetStretch(rect, 8f, 8f, 8f, 8f);
        root.SetActive(false);
        return root;
    }

    private void CreateMiniTile(Transform parent, Vector2 position, string symbol, string label, Color accentColor)
    {
        GameObject tile = CreatePanel("ShapeTile_" + label, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(170f, 122f),
            Color.white);
        ApplyRounded(tile.GetComponent<Image>());
        tile.GetComponent<Image>().raycastTarget = false;

        Text symbolText = CreateText("Symbol", tile.transform, 38, TextAnchor.MiddleCenter, accentColor);
        symbolText.text = symbol;
        symbolText.fontStyle = FontStyle.Bold;
        SetRect(symbolText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(110f, 58f));

        Text labelText = CreateText("Caption", tile.transform, 16, TextAnchor.MiddleCenter, new Color(0.24f, 0.34f, 0.49f, 1f));
        labelText.text = label;
        labelText.fontStyle = FontStyle.Bold;
        SetRect(labelText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 23f), new Vector2(145f, 35f));
    }

    private void CreateVisualSquare(Transform parent, Vector2 position, float size, Color fill)
    {
        GameObject square = CreatePanel("NetCell", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size), fill);
        ApplyRounded(square.GetComponent<Image>());
        square.GetComponent<Image>().raycastTarget = false;
    }

    private void CreateFlowArrow(Transform parent, Vector2 position)
    {
        Text arrow = CreateText("FlowArrow", parent, 36, TextAnchor.MiddleCenter, new Color(0.25f, 0.49f, 0.88f, 1f));
        arrow.text = "→";
        arrow.fontStyle = FontStyle.Bold;
        SetRect(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(70f, 60f));
    }

    private void SetOnboardingIllustration(int activeIndex, bool visible)
    {
        if (onboardingIllustrationRoot == null)
            return;

        onboardingIllustrationRoot.SetActive(visible);
        for (int i = 0; i < onboardingIllustrations.Length; i++)
        {
            if (onboardingIllustrations[i] != null)
                onboardingIllustrations[i].SetActive(visible && i == activeIndex);
        }
    }

    private string GetShapeIcon(string shape)
    {
        switch (shape)
        {
            case "Cube": return "CUBE";
            case "Cuboid": return "BOX";
            case "Tetrahedron": return "△";
            case "Triangular Prism": return "PRISM";
            case "Cylinder": return "CYL";
            default: return "?";
        }
    }

    private string GetCoachText(string shape)
    {
        switch (shape)
        {
            case "Cube":
                return "Draw a connected cube net on the grid. A cube needs 6 square faces. Use Undo or Clear if needed, then tap Validate. If it is correct, tap Create 3D.";

            case "Cuboid":
                return "Draw 6 connected rectangular faces on the grid. Keep shared edges connected and make the rectangles match the cuboid dimensions. Validate before creating the 3D model.";

            case "Tetrahedron":
                return "Use the triangular grid to build 4 connected triangular faces. You can keep your finger down and drag from one neighbouring grid point to the next to draw connected edges continuously.";

            case "Triangular Prism":
                return "Use Guided Mode to build the prism net. Arrange 3 rectangular side faces and 2 matching triangular end faces so their edges connect correctly, then validate the net.";

            case "Cylinder":
                return "Use Guided Mode to build the cylinder net. Arrange 1 rectangular lateral face and 2 equal circular ends, then tap Validate. A correct net can be folded into 3D.";

            default:
                return "Build the 2D net, tap Validate, watch the valid net fold into 3D, and then explore it in AR.";
        }
    }

    private void OnHelpPressed()
    {
        PlaySound(clickClip);

        if (string.IsNullOrEmpty(currentShape))
            ShowOnboarding();
        else
            ShowShapeCoach(currentShape);
    }

    private void OnSoundPressed()
    {
        soundEnabled = !soundEnabled;
        PlayerPrefs.SetInt(SoundPreferenceKey, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshSoundButtonLabel();

        if (soundEnabled)
            PlaySound(successClip);
    }

    private void RefreshSoundButtonLabel()
    {
        if (soundButtonText != null)
            soundButtonText.text = soundEnabled ? "SOUND\nON" : "SOUND\nOFF";
    }

    private void ShowToast(string message, float duration)
    {
        if (toastRoot == null || toastText == null)
            return;

        if (toastRoutine != null)
            StopCoroutine(toastRoutine);

        toastRoutine = StartCoroutine(ToastRoutine(message, duration));
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        toastText.text = message;
        toastRoot.SetActive(true);
        toastRoot.transform.SetAsLastSibling();

        yield return new WaitForSeconds(duration);

        toastRoot.SetActive(false);
        toastRoutine = null;
    }

    private void HideToastImmediate()
    {
        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
            toastRoutine = null;
        }

        if (toastRoot != null)
            toastRoot.SetActive(false);
    }

    private void BuildGuidanceUI()
    {
        roundedSprite = CreateRoundedSprite();

        GameObject canvasObject = new GameObject("C1GuidanceCanvas");
        canvasObject.transform.SetParent(transform, false);

        guidanceCanvas = canvasObject.AddComponent<Canvas>();
        guidanceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        guidanceCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // V3: place the progress card clearly below the SOUND / HELP controls.
        // Keeping it centred and slightly narrower prevents any overlap on
        // portrait phones while still leaving the current step easy to read.
        progressRoot = CreatePanel("C1Progress", canvasObject.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -235f), new Vector2(760f, 82f),
            new Color(1f, 1f, 1f, 0.95f));
        ApplyRounded(progressRoot.GetComponent<Image>());
        AddSoftShadow(progressRoot, new Color(0.10f, 0.22f, 0.42f, 0.15f), new Vector2(0f, -5f));

        progressText = CreateText("ProgressText", progressRoot.transform, 26, TextAnchor.MiddleCenter,
            new Color(0.20f, 0.28f, 0.40f, 1f));
        SetStretch(progressText.rectTransform, 24f, 10f, 24f, 10f);
        progressText.supportRichText = true;
        progressRoot.SetActive(false);

        helpButton = CreateButton("C1HelpButton", canvasObject.transform, "?",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-70f, -85f), new Vector2(88f, 88f),
            new Color(0.18f, 0.49f, 0.95f, 0.98f), 48);
        helpButton.onClick.AddListener(OnHelpPressed);
        AddSoftShadow(helpButton.gameObject, new Color(0.08f, 0.18f, 0.35f, 0.18f), new Vector2(0f, -4f));
        helpButton.gameObject.SetActive(false);

        soundButton = CreateButton("C1SoundButton", canvasObject.transform, "SOUND\nON",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-178f, -85f), new Vector2(104f, 88f),
            new Color(0.20f, 0.34f, 0.57f, 0.97f), 18);
        soundButton.onClick.AddListener(OnSoundPressed);
        soundButtonText = soundButton.GetComponentInChildren<Text>();
        AddSoftShadow(soundButton.gameObject, new Color(0.08f, 0.18f, 0.35f, 0.16f), new Vector2(0f, -4f));
        soundButton.gameObject.SetActive(false);

        toastRoot = CreatePanel("C1Toast", canvasObject.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 150f), new Vector2(880f, 132f),
            new Color(0.08f, 0.10f, 0.22f, 0.96f));
        ApplyRounded(toastRoot.GetComponent<Image>());
        AddSoftShadow(toastRoot, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -7f));
        toastText = CreateText("ToastText", toastRoot.transform, 30, TextAnchor.MiddleCenter, Color.white);
        SetStretch(toastText.rectTransform, 30f, 14f, 30f, 14f);
        toastRoot.SetActive(false);

        // V3: stronger focus backdrop for onboarding / quick-help cards.
        // A translucent navy overlay keeps the existing UI visible as context
        // but darkens it enough that the guide card becomes the clear focus.
        guideOverlay = CreatePanel("C1GuideOverlay", canvasObject.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.025f, 0.055f, 0.11f, 0.72f));
        RectTransform overlayRect = guideOverlay.GetComponent<RectTransform>();
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject card = CreatePanel("GuideCard", guideOverlay.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(880f, 920f),
            new Color(0.985f, 0.992f, 1f, 1f));
        ApplyRounded(card.GetComponent<Image>());
        AddSoftShadow(card, new Color(0.03f, 0.12f, 0.28f, 0.28f), new Vector2(0f, -12f));

        GameObject accent = CreatePanel("GuideTopAccent", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(760f, 14f),
            new Color(0.20f, 0.48f, 0.95f, 1f));
        ApplyRounded(accent.GetComponent<Image>());

        GameObject badge = CreatePanel("GuideBadge", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -82f), new Vector2(300f, 62f),
            new Color(0.90f, 0.95f, 1f, 1f));
        ApplyRounded(badge.GetComponent<Image>());
        guideBadgeText = CreateText("GuideBadgeText", badge.transform, 22, TextAnchor.MiddleCenter,
            new Color(0.14f, 0.36f, 0.72f, 1f));
        guideBadgeText.fontStyle = FontStyle.Bold;
        SetStretch(guideBadgeText.rectTransform, 12f, 6f, 12f, 6f);

        GameObject iconCircle = CreatePanel("GuideIcon", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -182f), new Vector2(136f, 112f),
            new Color(0.18f, 0.49f, 0.95f, 1f));
        ApplyRounded(iconCircle.GetComponent<Image>());
        AddSoftShadow(iconCircle, new Color(0.10f, 0.28f, 0.55f, 0.18f), new Vector2(0f, -4f));
        guideIconText = CreateText("GuideIconText", iconCircle.transform, 28, TextAnchor.MiddleCenter, Color.white);
        guideIconText.fontStyle = FontStyle.Bold;
        SetStretch(guideIconText.rectTransform, 8f, 8f, 8f, 8f);

        guideTitleText = CreateText("GuideTitle", card.transform, 43, TextAnchor.MiddleCenter,
            new Color(0.07f, 0.24f, 0.48f, 1f));
        SetRect(guideTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -300f), new Vector2(760f, 110f));
        guideTitleText.fontStyle = FontStyle.Bold;

        // V8: image-like visual panel for primary learners. The illustration
        // sits between the title and the instruction text and changes for each
        // of the four onboarding steps.
        BuildOnboardingIllustrations(card.transform);

        guideBodyText = CreateText("GuideBody", card.transform, 30, TextAnchor.MiddleCenter,
            new Color(0.22f, 0.28f, 0.39f, 1f));
        SetRect(guideBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -132f), new Vector2(720f, 182f));
        guideBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        guideBodyText.verticalOverflow = VerticalWrapMode.Truncate;
        guideBodyText.lineSpacing = 1.16f;

        guidePageText = CreateText("GuidePage", card.transform, 22, TextAnchor.MiddleCenter,
            new Color(0.44f, 0.52f, 0.64f, 1f));
        SetRect(guidePageText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 185f), new Vector2(520f, 88f));
        guidePageText.supportRichText = true;
        guidePageText.lineSpacing = 1.15f;

        guideLeftButton = CreateButton("GuideLeftButton", card.transform, "SKIP",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(240f, 82f), new Vector2(280f, 96f),
            new Color(0.88f, 0.91f, 0.96f, 1f), 28);
        guideLeftButton.onClick.AddListener(OnGuideLeftPressed);
        guideLeftButtonText = guideLeftButton.GetComponentInChildren<Text>();
        guideLeftButtonText.color = new Color(0.36f, 0.45f, 0.59f, 1f);

        guideRightButton = CreateButton("GuideRightButton", card.transform, "NEXT",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-240f, 82f), new Vector2(320f, 96f),
            new Color(0.18f, 0.49f, 0.95f, 1f), 28);
        guideRightButton.onClick.AddListener(OnGuideRightPressed);
        guideRightButtonText = guideRightButton.GetComponentInChildren<Text>();
        AddSoftShadow(guideRightButton.gameObject, new Color(0.10f, 0.28f, 0.55f, 0.17f), new Vector2(0f, -4f));

        guideOverlay.SetActive(false);
        guidanceCanvas.gameObject.SetActive(false);
    }

    private GameObject CreatePanel(string objectName, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition,
        Vector2 sizeDelta, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.layer = 5;
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        return panel;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.font = GetRuntimeFont();
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.raycastTarget = false;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        return textComponent;
    }

    private Button CreateButton(string objectName, Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition,
        Vector2 sizeDelta, Color backgroundColor, int fontSize)
    {
        GameObject buttonObject = CreatePanel(objectName, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta, backgroundColor);
        ApplyRounded(buttonObject.GetComponent<Image>());

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.98f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.90f, 1f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        Text labelText = CreateText("Label", buttonObject.transform, fontSize, TextAnchor.MiddleCenter, Color.white);
        labelText.text = label;
        labelText.fontStyle = FontStyle.Bold;
        SetStretch(labelText.rectTransform, 8f, 5f, 8f, 5f);

        return button;
    }

    private void ApplyRounded(Image image)
    {
        if (image == null || roundedSprite == null)
            return;

        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
    }

    private Sprite CreateRoundedSprite()
    {
        const int size = 96;
        const float radius = 24f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "C1RoundedRectTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;
        float inner = half - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs((x + 0.5f) - half);
                float py = Mathf.Abs((y + 0.5f) - half);
                float dx = Mathf.Max(px - inner, 0f);
                float dy = Mathf.Max(py - inner, 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.8f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Vector4 border = new Vector4(26f, 26f, 26f, 26f);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, border);
        sprite.name = "C1RoundedRectSprite";
        return sprite;
    }

    private void AddSoftShadow(GameObject target, Color color, Vector2 distance)
    {
        if (target == null)
            return;

        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private Font GetRuntimeFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void SetStretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.90f;
        audioSource.ignoreListenerPause = true;

        clickClip = LoadGuidanceClip("c1_tap", CreateToneSequence("GuideClick", new[] { 760f }, 0.070f, 0.28f));
        tutorialClip = LoadGuidanceClip("c1_open", CreateToneSequence("GuideOpen", new[] { 520f, 680f, 840f }, 0.090f, 0.25f));
        successClip = LoadGuidanceClip("c1_success", CreateToneSequence("GuideSuccess", new[] { 620f, 800f, 1040f }, 0.105f, 0.30f));
        errorClip = LoadGuidanceClip("c1_try_again", CreateToneSequence("GuideTryAgain", new[] { 390f, 315f }, 0.125f, 0.25f));
        foldClip = LoadGuidanceClip("c1_fold", CreateToneSequence("GuideFold", new[] { 430f, 560f, 720f, 900f }, 0.085f, 0.24f));
        arStartClip = LoadGuidanceClip("c1_ar_start", CreateToneSequence("GuideARStart", new[] { 500f, 700f, 930f }, 0.115f, 0.27f));
        placedClip = LoadGuidanceClip("c1_place", CreateToneSequence("GuidePlaced", new[] { 660f, 825f, 990f, 1180f }, 0.115f, 0.31f));
    }

    private AudioClip LoadGuidanceClip(string resourceName, AudioClip fallback)
    {
        AudioClip clip = Resources.Load<AudioClip>("C1GuidanceAudio/" + resourceName);
        return clip != null ? clip : fallback;
    }

    private AudioClip CreateToneSequence(string clipName, float[] frequencies, float secondsPerTone, float amplitude)
    {
        const int sampleRate = 44100;
        int samplesPerTone = Mathf.Max(1, Mathf.RoundToInt(sampleRate * secondsPerTone));
        int totalSamples = samplesPerTone * frequencies.Length;
        float[] samples = new float[totalSamples];

        for (int tone = 0; tone < frequencies.Length; tone++)
        {
            float frequency = frequencies[tone];
            int start = tone * samplesPerTone;

            for (int i = 0; i < samplesPerTone; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / Mathf.Max(1, samplesPerTone - 1));
                float fundamental = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float harmonic = 0.28f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * t);
                samples[start + i] = (fundamental + harmonic) * amplitude * envelope;
            }
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlaySound(AudioClip clip)
    {
        if (!soundEnabled || audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, 1f);
    }

    private GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in transforms)
            {
                if (item != null && item.gameObject.name == objectName)
                    return item.gameObject;
            }
        }

        return null;
    }

    private T[] FindSceneComponents<T>() where T : Component
    {
        List<T> result = new List<T>();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return result.ToArray();

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
            result.AddRange(root.GetComponentsInChildren<T>(true));

        return result.ToArray();
    }
}

/// <summary>
/// V4 visual polish for the key C1 action buttons.
/// Adds a soft animated blue/cyan/purple outline to the current next-action
/// button and a small press bounce. It works with the existing PNG button
/// artwork, so no scene or sprite replacement is required.
/// </summary>
public class C1AnimatedButtonGlow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Button ownerButton;
    private Graphic targetGraphic;
    private Outline innerOutline;
    private Outline outerOutline;
    private bool highlighted;
    private bool pointerDown;
    private Vector3 restingScale = Vector3.one;
    private Coroutine releaseRoutine;
    private float phaseOffset;

    private static readonly Color GlowBlue = new Color(0.12f, 0.48f, 1.00f, 1f);
    private static readonly Color GlowCyan = new Color(0.10f, 0.88f, 1.00f, 1f);
    private static readonly Color GlowPurple = new Color(0.66f, 0.28f, 1.00f, 1f);

    public void Initialize(Button button)
    {
        ownerButton = button;
        if (ownerButton == null)
            return;

        targetGraphic = ownerButton.targetGraphic;
        if (targetGraphic == null)
            targetGraphic = ownerButton.GetComponent<Graphic>();

        restingScale = transform.localScale;
        phaseOffset = Mathf.Abs(gameObject.GetInstanceID() % 97) / 97f;

        if (targetGraphic != null && innerOutline == null)
        {
            innerOutline = targetGraphic.gameObject.AddComponent<Outline>();
            innerOutline.effectDistance = new Vector2(3.2f, 3.2f);
            innerOutline.useGraphicAlpha = true;
            innerOutline.effectColor = new Color(GlowCyan.r, GlowCyan.g, GlowCyan.b, 0f);

            outerOutline = targetGraphic.gameObject.AddComponent<Outline>();
            outerOutline.effectDistance = new Vector2(7.0f, 7.0f);
            outerOutline.useGraphicAlpha = true;
            outerOutline.effectColor = new Color(GlowPurple.r, GlowPurple.g, GlowPurple.b, 0f);
        }
    }

    public void SetHighlighted(bool value)
    {
        highlighted = value;
        if (!highlighted)
            SetOutlineAlpha(0f, 0f);
    }

    private void OnEnable()
    {
        restingScale = transform.localScale;
    }

    private void OnDisable()
    {
        pointerDown = false;
        if (releaseRoutine != null)
        {
            StopCoroutine(releaseRoutine);
            releaseRoutine = null;
        }

        transform.localScale = restingScale;
        SetOutlineAlpha(0f, 0f);
    }

    private void Update()
    {
        if (innerOutline == null || outerOutline == null || ownerButton == null)
            return;

        bool canGlow = highlighted && ownerButton.interactable && ownerButton.gameObject.activeInHierarchy;
        if (!canGlow)
        {
            SetOutlineAlpha(0f, 0f);
            return;
        }

        float time = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin((time * 3.15f) + phaseOffset * Mathf.PI * 2f);
        Color inner = GradientColor(time * 0.28f + phaseOffset);
        Color outer = GradientColor(time * 0.28f + phaseOffset + 0.34f);

        inner.a = Mathf.Lerp(0.48f, 0.88f, pulse);
        outer.a = Mathf.Lerp(0.16f, 0.40f, 1f - pulse * 0.45f);

        innerOutline.effectColor = inner;
        outerOutline.effectColor = outer;

        // A tiny breathing change in outline distance makes the halo feel alive
        // without moving the actual button or disturbing the existing layout.
        float spread = Mathf.Lerp(0.0f, 1.6f, pulse);
        innerOutline.effectDistance = new Vector2(3.0f + spread * 0.35f, 3.0f + spread * 0.35f);
        outerOutline.effectDistance = new Vector2(6.3f + spread, 6.3f + spread);
    }

    private Color GradientColor(float cycle)
    {
        float position = Mathf.Repeat(cycle, 1f) * 3f;
        if (position < 1f)
            return Color.Lerp(GlowBlue, GlowCyan, position);
        if (position < 2f)
            return Color.Lerp(GlowCyan, GlowPurple, position - 1f);
        return Color.Lerp(GlowPurple, GlowBlue, position - 2f);
    }

    private void SetOutlineAlpha(float innerAlpha, float outerAlpha)
    {
        if (innerOutline != null)
        {
            Color c = innerOutline.effectColor;
            c.a = innerAlpha;
            innerOutline.effectColor = c;
        }

        if (outerOutline != null)
        {
            Color c = outerOutline.effectColor;
            c.a = outerAlpha;
            outerOutline.effectColor = c;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ownerButton == null || !ownerButton.interactable)
            return;

        if (releaseRoutine != null)
        {
            StopCoroutine(releaseRoutine);
            releaseRoutine = null;
        }

        pointerDown = true;
        restingScale = transform.localScale;
        transform.localScale = restingScale * 0.94f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerDown)
            return;

        pointerDown = false;
        StartReleaseBounce();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!pointerDown)
            return;

        pointerDown = false;
        StartReleaseBounce();
    }

    private void StartReleaseBounce()
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.localScale = restingScale;
            return;
        }

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(ReleaseBounceRoutine());
    }

    private IEnumerator ReleaseBounceRoutine()
    {
        Vector3 start = transform.localScale;
        Vector3 overshoot = restingScale * 1.025f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.075f;
            transform.localScale = Vector3.Lerp(start, overshoot, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.09f;
            transform.localScale = Vector3.Lerp(overshoot, restingScale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }

        transform.localScale = restingScale;
        releaseRoutine = null;
    }
}


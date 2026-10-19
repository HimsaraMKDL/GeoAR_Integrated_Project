using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// V6 additive polish layer for the C1 Geometry component.
///
/// This file is intentionally separate from the working V4 guidance system.
/// It adds extra presentation / UX features at runtime without editing the
/// integrated SampleScene or changing any validator, folding, AR or logger logic.
///
/// V5 + V6 features:
///  1. soft screen transitions between menu/workspace/fold/AR states
///  2. fold-stage solid camera background + subtle gradient backdrop (all five shapes)
///  3. fold-complete celebration card
///  4. animated AR placement reticle with plane-ready state
///  5. AR placement confirmation animation
///  6. shape facts card
///  7. inactivity "TAP" hint on the next action
///  8. per-student completion badges on shape menu buttons
///  9. invalid-attempt drawing-area warning pulse
/// 10. small accessibility polish / expanded hit areas for undersized key buttons
///
/// The system only runs in SampleScene.
/// </summary>
public class C1AdvancedPolishV5 : MonoBehaviour
{
    private const string GeometrySceneName = "SampleScene";
    private const float IdleHintDelay = 9f;

    private static C1AdvancedPolishV5 instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("C1AdvancedPolishV5");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<C1AdvancedPolishV5>();
    }

    private Canvas polishCanvas;
    private RectTransform canvasRect;
    private Sprite roundedSprite;
    private Sprite circleSprite;
    private Sprite foldGradientSprite;

    private Image transitionImage;
    private Coroutine transitionRoutine;

    private GameObject foldBackdrop;
    private CanvasGroup foldBackdropGroup;

    // V6: the plain base color is rendered by the scene camera so it sits
    // behind the 3D model. The existing semi-transparent UI gradient remains
    // above it, giving a clean solid + gradient fold-stage background.
    private Camera foldStageCamera;
    private bool foldCameraStateCaptured;
    private bool foldCameraBackgroundApplied;
    private CameraClearFlags originalCameraClearFlags;
    private Color originalCameraBackgroundColor;

    private GameObject celebrationRoot;
    private CanvasGroup celebrationGroup;
    private Text celebrationTitle;
    private Text celebrationBody;
    private Coroutine celebrationRoutine;

    private GameObject factsRoot;
    private CanvasGroup factsGroup;
    private Text factsTitle;
    private Text factsText;
    private Coroutine factsRoutine;

    private GameObject reticleRoot;
    private RectTransform reticleSpinner;
    private readonly List<Image> reticleMarks = new List<Image>();
    private Text reticleText;
    private bool reticleSurfaceReady;

    private GameObject placementRoot;
    private CanvasGroup placementGroup;
    private Text placementTitle;
    private Coroutine placementRoutine;

    private GameObject idleHintRoot;
    private CanvasGroup idleHintGroup;
    private RectTransform idleHintRect;
    private Text idleHintText;
    private Button currentHintButton;

    private GameObject invalidFrameRoot;
    private CanvasGroup invalidFrameGroup;
    private readonly List<Image> invalidFrameBars = new List<Image>();
    private Coroutine invalidPulseRoutine;

    private GameObject mainMenuPanel;
    private GameObject studentEntryPanel;
    private GameObject cubeWorkspacePanel;
    private GameObject cuboidWorkspacePanel;
    private GameObject tetrahedronWorkspacePanel;
    private GameObject prismModeSelectionPanel;
    private GameObject prismGuidedWorkspacePanel;
    private GameObject cylinderModeSelectionPanel;
    private GameObject cylinderGuidedWorkspacePanel;
    private GameObject arOverlayCanvas;

    private ARPlacedModelManager arPlacedModelManager;

    private readonly List<Button> validateButtons = new List<Button>();
    private readonly List<Button> createButtons = new List<Button>();
    private readonly List<Button> goToARButtons = new List<Button>();
    private readonly Dictionary<string, Button> shapeButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, GameObject> completionBadges = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, RectTransform> drawingTargets = new Dictionary<string, RectTransform>();

    private readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();

    private int boundSceneHandle = -1;
    private bool isGeometryScene;
    private string currentStudentID = string.Empty;
    private string currentShape = string.Empty;
    private string previousViewKey = string.Empty;
    private bool previousARVisible;
    private bool previousGoToARVisible;
    private Transform observedPlacedModel;
    private bool placementHandled;
    private bool foldPreviewActive;
    private bool foldCelebrated;

    private float lastInteractionTime;
    private bool idleHintShownThisCycle;
    private Button previousHintTarget;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildPolishUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(BindAfterFrame());
    }

    private void OnDestroy()
    {
        RestoreFoldStageCameraBackground();

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
        yield return null;
        BindScene();
    }

    private void BindScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        boundSceneHandle = scene.handle;
        isGeometryScene = scene.name == GeometrySceneName;

        if (polishCanvas != null)
            polishCanvas.gameObject.SetActive(isGeometryScene);

        validateButtons.Clear();
        createButtons.Clear();
        goToARButtons.Clear();
        shapeButtons.Clear();
        completionBadges.Clear();
        drawingTargets.Clear();

        mainMenuPanel = null;
        studentEntryPanel = null;
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
        previousViewKey = string.Empty;
        previousARVisible = false;
        previousGoToARVisible = false;
        observedPlacedModel = null;
        placementHandled = false;
        foldPreviewActive = false;
        foldCelebrated = false;
        RestoreFoldStageCameraBackground();
        foldStageCamera = null;
        foldCameraStateCaptured = false;
        foldCameraBackgroundApplied = false;
        lastInteractionTime = Time.unscaledTime;
        idleHintShownThisCycle = false;
        previousHintTarget = null;

        HideAllV5OverlaysImmediate();

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

        BindDrawingTarget("Cube", "DrawingArea", cubeWorkspacePanel);
        BindDrawingTarget("Cuboid", "CuboidDrawingArea", cuboidWorkspacePanel);
        BindDrawingTarget("Tetrahedron", "TetrahedronDrawingArea", tetrahedronWorkspacePanel);
        BindDrawingTarget("Triangular Prism", "PrismGuidedDrawingPanel", prismGuidedWorkspacePanel);
        BindDrawingTarget("Cylinder", "CylinderPuzzleBoard", cylinderGuidedWorkspacePanel);

        Button[] allButtons = FindSceneComponents<Button>();
        foreach (Button button in allButtons)
        {
            if (button == null)
                continue;

            string name = button.gameObject.name.Trim();

            if (ContainsIgnoreCase(name, "Validate") || ContainsIgnoreCase(name, "CheckPuzzle"))
            {
                validateButtons.Add(button);
                button.onClick.RemoveListener(OnValidatePressed);
                button.onClick.AddListener(OnValidatePressed);
                ApplyTouchTargetPolish(button);
            }

            if ((ContainsIgnoreCase(name, "Create") && ContainsIgnoreCase(name, "3D")) || ContainsIgnoreCase(name, "Create 3D"))
            {
                createButtons.Add(button);
                button.onClick.RemoveListener(OnCreate3DPressed);
                button.onClick.AddListener(OnCreate3DPressed);
                ApplyTouchTargetPolish(button);
            }

            if (ContainsIgnoreCase(name, "GoToAR") || ContainsIgnoreCase(name, "Go To AR"))
            {
                goToARButtons.Add(button);
                button.onClick.RemoveListener(OnGoToARPressed);
                button.onClick.AddListener(OnGoToARPressed);
                ApplyTouchTargetPolish(button);
            }

            string shape = ShapeForMenuButton(name);
            if (!string.IsNullOrEmpty(shape))
            {
                shapeButtons[shape] = button;
                CreateCompletionBadge(shape, button);
                ApplyTouchTargetPolish(button);
            }
        }

        // Also polish the V4 runtime SOUND / HELP controls when available.
        StartCoroutine(PolishRuntimeGuidanceButtons());

        currentStudentID = StudentProfileManager.CurrentStudentID ?? string.Empty;
        RefreshCompletionBadges();
    }

    private IEnumerator PolishRuntimeGuidanceButtons()
    {
        yield return null;
        Button[] runtimeButtons = FindObjectsOfType<Button>();
        foreach (Button button in runtimeButtons)
        {
            if (button == null)
                continue;

            string n = button.gameObject.name;
            if (n == "C1HelpButton" || n == "C1SoundButton" || n == "GuideLeftButton" || n == "GuideRightButton")
                ApplyTouchTargetPolish(button);
        }
    }

    private void Update()
    {
        if (!isGeometryScene)
            return;

        if (SceneManager.GetActiveScene().handle != boundSceneHandle)
        {
            BindScene();
            return;
        }

        string studentID = StudentProfileManager.CurrentStudentID ?? string.Empty;
        if (studentID != currentStudentID)
        {
            currentStudentID = studentID;
            RefreshCompletionBadges();
            lastInteractionTime = Time.unscaledTime;
        }

        DetectUserActivity();

        bool studentEntryVisible = IsVisible(studentEntryPanel);
        bool menuVisible = IsVisible(mainMenuPanel);
        bool arVisible = IsVisible(arOverlayCanvas);
        string detectedShape = DetectActiveShape();

        if (!string.IsNullOrEmpty(detectedShape))
            currentShape = detectedShape;

        string viewKey = GetViewKey(studentEntryVisible, menuVisible, arVisible);
        if (!string.IsNullOrEmpty(previousViewKey) && viewKey != previousViewKey)
        {
            // Keep transitions short so they feel polished, not like a delay.
            if (!IsV4GuideVisible())
                PlayScreenTransition(arVisible ? new Color(0.04f, 0.10f, 0.20f, 1f) : new Color(0.86f, 0.94f, 1f, 1f), 0.28f);

            lastInteractionTime = Time.unscaledTime;
            idleHintShownThisCycle = false;
        }
        previousViewKey = viewKey;

        bool goToARVisible = IsAnyGoToARVisible();
        if (goToARVisible && !previousGoToARVisible)
        {
            foldPreviewActive = true;
            if (!foldCelebrated)
            {
                foldCelebrated = true;
                PlayFoldCelebration(currentShape);
            }
        }
        previousGoToARVisible = goToARVisible;

        if (arVisible && !previousARVisible)
        {
            foldPreviewActive = false;
            SetFoldBackdropVisible(false, true);
            PlayScreenTransition(new Color(0.03f, 0.08f, 0.16f, 1f), 0.32f);
        }
        previousARVisible = arVisible;

        UpdateFoldBackdrop(arVisible);
        UpdateARReticle(arVisible);
        UpdatePlacementState(arVisible);
        UpdateIdleHint(studentEntryVisible, menuVisible, arVisible);
        UpdateCompletionBadgesOnMenu(menuVisible);
    }

    private void DetectUserActivity()
    {
        bool activity = Input.touchCount > 0 || Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.anyKeyDown;
        if (!activity)
            return;

        lastInteractionTime = Time.unscaledTime;
        idleHintShownThisCycle = false;
        HideIdleHint();
    }

    private void OnValidatePressed()
    {
        lastInteractionTime = Time.unscaledTime;
        idleHintShownThisCycle = false;
        HideIdleHint();
        StartCoroutine(CheckValidationResultAfterDelay());
    }

    private IEnumerator CheckValidationResultAfterDelay()
    {
        yield return new WaitForSeconds(0.62f);

        if (!isGeometryScene || IsVisible(arOverlayCanvas))
            yield break;

        if (IsAnyCreateButtonReady())
            yield break;

        RectTransform target = GetCurrentDrawingTarget();
        if (target != null)
            PlayInvalidDrawingAreaPulse(target);
    }

    private void OnCreate3DPressed()
    {
        lastInteractionTime = Time.unscaledTime;
        idleHintShownThisCycle = false;
        HideIdleHint();

        foldPreviewActive = true;
        foldCelebrated = false;
        SetFoldBackdropVisible(true, false);
        PlayScreenTransition(new Color(0.52f, 0.84f, 1f, 1f), 0.22f);

        StartCoroutine(FallbackFoldCelebration());
    }

    private IEnumerator FallbackFoldCelebration()
    {
        // Manual Go-To-AR shapes trigger on button appearance. Auto-AR shapes do
        // not expose that event, so this delayed fallback still gives them a
        // short completion moment without changing their timing logic.
        yield return new WaitForSeconds(3.2f);

        if (!foldCelebrated && foldPreviewActive && !IsVisible(arOverlayCanvas))
        {
            foldCelebrated = true;
            PlayFoldCelebration(currentShape);
        }
    }

    private void OnGoToARPressed()
    {
        lastInteractionTime = Time.unscaledTime;
        idleHintShownThisCycle = false;
        HideIdleHint();
        PlayScreenTransition(new Color(0.03f, 0.08f, 0.16f, 1f), 0.30f);
    }

    private void UpdateFoldBackdrop(bool arVisible)
    {
        bool shouldShow = foldPreviewActive && !arVisible && !IsVisible(mainMenuPanel) && !IsVisible(studentEntryPanel);
        SetFoldBackdropVisible(shouldShow, false);
    }

    private void SetFoldBackdropVisible(bool visible, bool immediate)
    {
        if (foldBackdrop == null || foldBackdropGroup == null)
            return;

        if (visible)
        {
            ApplyFoldStageCameraBackground(currentShape);
            foldBackdrop.SetActive(true);
            foldBackdropGroup.alpha = Mathf.MoveTowards(foldBackdropGroup.alpha, 1f, Time.unscaledDeltaTime * 3.5f);
        }
        else
        {
            RestoreFoldStageCameraBackground();

            if (immediate)
            {
                foldBackdropGroup.alpha = 0f;
                foldBackdrop.SetActive(false);
            }
            else if (foldBackdrop.activeSelf)
            {
                foldBackdropGroup.alpha = Mathf.MoveTowards(foldBackdropGroup.alpha, 0f, Time.unscaledDeltaTime * 4f);
                if (foldBackdropGroup.alpha <= 0.01f)
                    foldBackdrop.SetActive(false);
            }
        }
    }

    // V6 fold-stage base background. Using the camera instead of an opaque
    // ScreenSpaceOverlay panel keeps the 3D shape fully visible and clear.
    private void ApplyFoldStageCameraBackground(string shape)
    {
        if (!isGeometryScene)
            return;

        if (foldStageCamera == null)
        {
            foldStageCamera = Camera.main;
            if (foldStageCamera == null)
            {
                GameObject mainCameraObject = FindSceneObject("Main Camera");
                if (mainCameraObject != null)
                    foldStageCamera = mainCameraObject.GetComponent<Camera>();
            }
        }

        if (foldStageCamera == null)
            return;

        if (!foldCameraStateCaptured)
        {
            originalCameraClearFlags = foldStageCamera.clearFlags;
            originalCameraBackgroundColor = foldStageCamera.backgroundColor;
            foldCameraStateCaptured = true;
        }

        foldStageCamera.clearFlags = CameraClearFlags.SolidColor;
        foldStageCamera.backgroundColor = GetFoldStageBaseColor(shape);
        foldCameraBackgroundApplied = true;
    }

    private void RestoreFoldStageCameraBackground()
    {
        if (!foldCameraBackgroundApplied)
            return;

        if (foldStageCamera != null && foldCameraStateCaptured)
        {
            foldStageCamera.clearFlags = originalCameraClearFlags;
            foldStageCamera.backgroundColor = originalCameraBackgroundColor;
        }

        foldCameraBackgroundApplied = false;
    }

    private Color GetFoldStageBaseColor(string shape)
    {
        // Consistent dark-pastel family, with a tiny per-shape shift so every
        // solid stays visually distinct while preserving the same C1 theme.
        if (ContainsIgnoreCase(shape, "Cube") && !ContainsIgnoreCase(shape, "Cuboid"))
            return new Color(0.055f, 0.145f, 0.255f, 1f);
        if (ContainsIgnoreCase(shape, "Cuboid"))
            return new Color(0.045f, 0.175f, 0.220f, 1f);
        if (ContainsIgnoreCase(shape, "Tetra"))
            return new Color(0.115f, 0.105f, 0.265f, 1f);
        if (ContainsIgnoreCase(shape, "Prism"))
            return new Color(0.060f, 0.155f, 0.280f, 1f);
        if (ContainsIgnoreCase(shape, "Cylinder"))
            return new Color(0.085f, 0.120f, 0.245f, 1f);

        return new Color(0.055f, 0.145f, 0.255f, 1f);
    }

    private void PlayFoldCelebration(string shape)
    {
        if (string.IsNullOrEmpty(shape))
            shape = "3D Shape";

        ShowCelebration("GREAT WORK!", "Your " + shape + " is ready.\n" + GetShapeFact(shape), 2.8f);
        ShowFactsCard(shape, 4.8f);
    }

    private void UpdateARReticle(bool arVisible)
    {
        if (!arVisible || arPlacedModelManager == null || arPlacedModelManager.modelToPlace == null)
        {
            SetReticleVisible(false);
            return;
        }

        Transform model = arPlacedModelManager.modelToPlace;
        bool waitingForPlacement = !model.gameObject.activeInHierarchy;
        SetReticleVisible(waitingForPlacement);

        if (!waitingForPlacement)
            return;

        bool surfaceReady = false;
        if (arPlacedModelManager.raycastManager != null)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            surfaceReady = arPlacedModelManager.raycastManager.Raycast(center, arHits, TrackableType.PlaneWithinPolygon);
        }

        reticleSurfaceReady = surfaceReady;
        Color c = surfaceReady
            ? new Color(0.18f, 0.85f, 0.48f, 0.96f)
            : new Color(0.20f, 0.58f, 1f, 0.92f);

        foreach (Image mark in reticleMarks)
        {
            if (mark != null)
                mark.color = c;
        }

        if (reticleText != null)
            reticleText.text = surfaceReady ? "Surface found • Tap to place" : "Move slowly to scan for a surface";

        if (reticleSpinner != null)
        {
            float speed = surfaceReady ? 18f : 42f;
            reticleSpinner.localEulerAngles = new Vector3(0f, 0f, -Time.unscaledTime * speed);
            float pulse = 1f + 0.055f * Mathf.Sin(Time.unscaledTime * 4.4f);
            reticleSpinner.localScale = Vector3.one * pulse;
        }
    }

    private void SetReticleVisible(bool visible)
    {
        if (reticleRoot != null && reticleRoot.activeSelf != visible)
            reticleRoot.SetActive(visible);
    }

    private void UpdatePlacementState(bool arVisible)
    {
        if (!arVisible || arPlacedModelManager == null || arPlacedModelManager.modelToPlace == null)
            return;

        Transform model = arPlacedModelManager.modelToPlace;
        if (observedPlacedModel != model)
        {
            observedPlacedModel = model;
            placementHandled = false;
        }

        if (placementHandled || !model.gameObject.activeInHierarchy)
            return;

        placementHandled = true;
        foldPreviewActive = false;
        SetFoldBackdropVisible(false, true);
        SetReticleVisible(false);

        MarkShapeCompleted(currentShape);
        RefreshCompletionBadges();

        ShowPlacementConfirmation(currentShape);
        ShowFactsCard(currentShape, 5.5f);
    }

    private void ShowPlacementConfirmation(string shape)
    {
        if (placementRoutine != null)
            StopCoroutine(placementRoutine);

        placementRoutine = StartCoroutine(PlacementConfirmationRoutine(shape));
    }

    private IEnumerator PlacementConfirmationRoutine(string shape)
    {
        placementRoot.SetActive(true);
        placementRoot.transform.SetAsLastSibling();
        placementGroup.alpha = 0f;
        placementRoot.transform.localScale = Vector3.one * 0.82f;
        placementTitle.text = "✓  PLACED!";

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.24f;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            placementGroup.alpha = s;
            placementRoot.transform.localScale = Vector3.Lerp(Vector3.one * 0.82f, Vector3.one * 1.03f, s);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.12f;
            placementRoot.transform.localScale = Vector3.Lerp(Vector3.one * 1.03f, Vector3.one, Mathf.Clamp01(t));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.55f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.22f;
            placementGroup.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }

        placementRoot.SetActive(false);
        placementRoutine = null;
    }

    private void ShowCelebration(string title, string body, float duration)
    {
        if (celebrationRoutine != null)
            StopCoroutine(celebrationRoutine);

        celebrationRoutine = StartCoroutine(CelebrationRoutine(title, body, duration));
    }

    private IEnumerator CelebrationRoutine(string title, string body, float duration)
    {
        celebrationTitle.text = title;
        celebrationBody.text = body;
        celebrationRoot.SetActive(true);
        celebrationRoot.transform.SetAsLastSibling();
        celebrationGroup.alpha = 0f;
        celebrationRoot.transform.localScale = Vector3.one * 0.84f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            celebrationGroup.alpha = eased;
            celebrationRoot.transform.localScale = Vector3.Lerp(Vector3.one * 0.84f, Vector3.one * 1.025f, eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.12f;
            celebrationRoot.transform.localScale = Vector3.Lerp(Vector3.one * 1.025f, Vector3.one, Mathf.Clamp01(t));
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float breathe = 1f + 0.012f * Mathf.Sin(Time.unscaledTime * 3.8f);
            celebrationRoot.transform.localScale = Vector3.one * breathe;
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.22f;
            celebrationGroup.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }

        celebrationRoot.SetActive(false);
        celebrationRoot.transform.localScale = Vector3.one;
        celebrationRoutine = null;
    }

    private void ShowFactsCard(string shape, float duration)
    {
        if (factsRoutine != null)
            StopCoroutine(factsRoutine);

        factsRoutine = StartCoroutine(FactsRoutine(shape, duration));
    }

    private IEnumerator FactsRoutine(string shape, float duration)
    {
        if (string.IsNullOrEmpty(shape))
            yield break;

        factsTitle.text = shape + " FACT";
        factsText.text = GetShapeFact(shape);
        factsRoot.SetActive(true);
        factsGroup.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.22f;
            factsGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(duration);

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            factsGroup.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }

        factsRoot.SetActive(false);
        factsRoutine = null;
    }

    private string GetShapeFact(string shape)
    {
        switch (shape)
        {
            case "Cube":
                return "6 square faces • 12 edges • 8 vertices";
            case "Cuboid":
                return "6 rectangular faces • 12 edges • 8 vertices";
            case "Tetrahedron":
                return "4 triangular faces • 6 edges • 4 vertices";
            case "Triangular Prism":
                return "5 faces • 9 edges • 6 vertices";
            case "Cylinder":
                return "2 circular faces • 1 curved surface • 0 vertices";
            default:
                return "A correct 2D net can fold into a 3D solid.";
        }
    }

    private void UpdateIdleHint(bool studentEntryVisible, bool menuVisible, bool arVisible)
    {
        if (studentEntryVisible || menuVisible || arVisible || IsV4GuideVisible() || string.IsNullOrEmpty(currentShape))
        {
            HideIdleHint();
            return;
        }

        Button target = GetNextActionButton();
        if (target != previousHintTarget)
        {
            previousHintTarget = target;
            currentHintButton = target;
            lastInteractionTime = Time.unscaledTime;
            idleHintShownThisCycle = false;
            HideIdleHint();
        }

        if (target == null || !target.gameObject.activeInHierarchy || !target.interactable)
        {
            HideIdleHint();
            return;
        }

        if (!idleHintShownThisCycle && Time.unscaledTime - lastInteractionTime >= IdleHintDelay)
        {
            idleHintShownThisCycle = true;
            ShowIdleHint(target);
        }

        if (!idleHintRoot.activeSelf)
            return;

        PositionOverlayAtButton(idleHintRect, target, new Vector2(0f, 92f));
        float pulse = 1f + 0.055f * Mathf.Sin(Time.unscaledTime * 5.2f);
        idleHintRoot.transform.localScale = Vector3.one * pulse;
        idleHintGroup.alpha = 0.82f + 0.18f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.1f));
    }

    private void ShowIdleHint(Button target)
    {
        currentHintButton = target;
        idleHintText.text = "TAP\n↓";
        idleHintRoot.SetActive(true);
        PositionOverlayAtButton(idleHintRect, target, new Vector2(0f, 92f));
    }

    private void HideIdleHint()
    {
        if (idleHintRoot != null)
            idleHintRoot.SetActive(false);
    }

    private Button GetNextActionButton()
    {
        Button go = FirstActiveInteractable(goToARButtons);
        if (go != null)
            return go;

        Button create = FirstActiveInteractable(createButtons);
        if (create != null)
            return create;

        return FirstActiveInteractable(validateButtons);
    }

    private Button FirstActiveInteractable(List<Button> buttons)
    {
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                return button;
        }

        return null;
    }

    private void PlayInvalidDrawingAreaPulse(RectTransform target)
    {
        if (target == null)
            return;

        FitInvalidFrameToTarget(target);

        if (invalidPulseRoutine != null)
            StopCoroutine(invalidPulseRoutine);

        invalidPulseRoutine = StartCoroutine(InvalidPulseRoutine());
    }

    private IEnumerator InvalidPulseRoutine()
    {
        invalidFrameRoot.SetActive(true);
        invalidFrameGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < 1.8f)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 13f);
            invalidFrameGroup.alpha = Mathf.Lerp(0.28f, 0.92f, pulse);

            Color c = Color.Lerp(new Color(1f, 0.72f, 0.15f, 1f), new Color(1f, 0.30f, 0.20f, 1f), pulse);
            foreach (Image bar in invalidFrameBars)
            {
                if (bar != null)
                    bar.color = c;
            }

            yield return null;
        }

        invalidFrameGroup.alpha = 0f;
        invalidFrameRoot.SetActive(false);
        invalidPulseRoutine = null;
    }

    private void FitInvalidFrameToTarget(RectTransform target)
    {
        if (canvasRect == null || invalidFrameRoot == null)
            return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Canvas sourceCanvas = target.GetComponentInParent<Canvas>();
        Camera cam = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? sourceCanvas.worldCamera : null;

        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        Vector2 bottomLeftLocal;
        Vector2 topRightLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, bottomLeftScreen, null, out bottomLeftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, topRightScreen, null, out topRightLocal);

        RectTransform rect = invalidFrameRoot.GetComponent<RectTransform>();
        Vector2 center = (bottomLeftLocal + topRightLocal) * 0.5f;
        Vector2 size = new Vector2(Mathf.Abs(topRightLocal.x - bottomLeftLocal.x), Mathf.Abs(topRightLocal.y - bottomLeftLocal.y));
        size += new Vector2(24f, 24f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;

        LayoutInvalidFrameBars(rect.sizeDelta);
    }

    private void LayoutInvalidFrameBars(Vector2 size)
    {
        if (invalidFrameBars.Count != 4)
            return;

        float thickness = 10f;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        SetRect(invalidFrameBars[0].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, halfH), new Vector2(size.x, thickness));
        SetRect(invalidFrameBars[1].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -halfH), new Vector2(size.x, thickness));
        SetRect(invalidFrameBars[2].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-halfW, 0f), new Vector2(thickness, size.y));
        SetRect(invalidFrameBars[3].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(halfW, 0f), new Vector2(thickness, size.y));
    }

    private void MarkShapeCompleted(string shape)
    {
        if (string.IsNullOrEmpty(currentStudentID) || string.IsNullOrEmpty(shape))
            return;

        PlayerPrefs.SetInt(CompletionKey(currentStudentID, shape), 1);
        PlayerPrefs.Save();
    }

    private string CompletionKey(string studentID, string shape)
    {
        return "C1_V5_Complete_" + studentID + "_" + shape.Replace(" ", "_");
    }

    private void CreateCompletionBadge(string shape, Button button)
    {
        if (button == null || completionBadges.ContainsKey(shape))
            return;

        GameObject badge = CreatePanel("C1CompletedBadge_" + shape.Replace(" ", "_"), button.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(58f, 58f),
            new Color(0.16f, 0.78f, 0.42f, 0.98f), circleSprite);
        RectTransform rect = badge.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = badge.GetComponent<Image>();
        image.raycastTarget = false;
        AddShadow(badge, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -3f));

        Text check = CreateText("Check", badge.transform, 34, TextAnchor.MiddleCenter, Color.white);
        check.text = "✓";
        check.fontStyle = FontStyle.Bold;
        check.raycastTarget = false;

        completionBadges[shape] = badge;
        badge.SetActive(false);
    }

    private void RefreshCompletionBadges()
    {
        foreach (KeyValuePair<string, GameObject> pair in completionBadges)
        {
            bool complete = !string.IsNullOrEmpty(currentStudentID) && PlayerPrefs.GetInt(CompletionKey(currentStudentID, pair.Key), 0) == 1;
            if (pair.Value != null)
                pair.Value.SetActive(complete);
        }
    }

    private void UpdateCompletionBadgesOnMenu(bool menuVisible)
    {
        if (!menuVisible)
            return;

        RefreshCompletionBadges();
    }

    private void ApplyTouchTargetPolish(Button button)
    {
        if (button == null)
            return;

        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
            return;

        Vector2 size = rect.rect.size;
        if (size.x >= 150f && size.y >= 82f)
            return;

        if (button.transform.Find("C1V5HitArea") != null)
            return;

        GameObject hit = new GameObject("C1V5HitArea");
        hit.layer = 5;
        hit.transform.SetParent(button.transform, false);
        RectTransform hitRect = hit.AddComponent<RectTransform>();
        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.offsetMin = new Vector2(-18f, -14f);
        hitRect.offsetMax = new Vector2(18f, 14f);
        hitRect.SetAsFirstSibling();

        Image hitImage = hit.AddComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0.001f);
        hitImage.raycastTarget = true;
    }

    private void PlayScreenTransition(Color tint, float duration)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(ScreenTransitionRoutine(tint, duration));
    }

    private IEnumerator ScreenTransitionRoutine(Color tint, float duration)
    {
        transitionImage.gameObject.SetActive(true);
        tint.a = 0.24f;
        transitionImage.color = tint;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.05f, duration);
            Color c = tint;
            c.a = Mathf.Lerp(0.24f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            transitionImage.color = c;
            yield return null;
        }

        transitionImage.gameObject.SetActive(false);
        transitionRoutine = null;
    }

    private bool IsV4GuideVisible()
    {
        GameObject guide = GameObject.Find("C1GuideOverlay");
        return guide != null && guide.activeInHierarchy;
    }

    private bool IsAnyCreateButtonReady()
    {
        return FirstActiveInteractable(createButtons) != null;
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

    private RectTransform GetCurrentDrawingTarget()
    {
        RectTransform result;
        if (!string.IsNullOrEmpty(currentShape) && drawingTargets.TryGetValue(currentShape, out result) && result != null)
            return result;

        return null;
    }

    private void BindDrawingTarget(string shape, string preferredObjectName, GameObject fallback)
    {
        GameObject target = FindSceneObject(preferredObjectName);
        if (target == null)
            target = fallback;

        if (target != null)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect != null)
                drawingTargets[shape] = rect;
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

    private string GetViewKey(bool studentEntryVisible, bool menuVisible, bool arVisible)
    {
        if (studentEntryVisible)
            return "ENTRY";
        if (arVisible)
            return "AR";
        if (menuVisible)
            return "MENU";
        if (IsVisible(cubeWorkspacePanel))
            return "CUBE";
        if (IsVisible(cuboidWorkspacePanel))
            return "CUBOID";
        if (IsVisible(tetrahedronWorkspacePanel))
            return "TETRA";
        if (IsVisible(prismModeSelectionPanel))
            return "PRISM_MODE";
        if (IsVisible(prismGuidedWorkspacePanel))
            return "PRISM_WORK";
        if (IsVisible(cylinderModeSelectionPanel))
            return "CYL_MODE";
        if (IsVisible(cylinderGuidedWorkspacePanel))
            return "CYL_WORK";
        if (foldPreviewActive)
            return "FOLD_" + currentShape;
        return "OTHER";
    }

    private bool IsVisible(GameObject obj)
    {
        return obj != null && obj.activeInHierarchy;
    }

    private string ShapeForMenuButton(string name)
    {
        switch (name)
        {
            case "CubeButton": return "Cube";
            case "CuboidButton": return "Cuboid";
            case "TetrahedronButton": return "Tetrahedron";
            case "PrismButton": return "Triangular Prism";
            case "CylinderButton": return "Cylinder";
            default: return string.Empty;
        }
    }

    private bool ContainsIgnoreCase(string source, string value)
    {
        return source != null && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void PositionOverlayAtButton(RectTransform overlay, Button button, Vector2 offset)
    {
        if (overlay == null || button == null || canvasRect == null)
            return;

        RectTransform target = button.transform as RectTransform;
        if (target == null)
            return;

        Canvas sourceCanvas = target.GetComponentInParent<Canvas>();
        Camera cam = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? sourceCanvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.TransformPoint(target.rect.center));
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint))
            overlay.anchoredPosition = localPoint + offset;
    }

    private void BuildPolishUI()
    {
        roundedSprite = CreateRoundedSprite();
        circleSprite = CreateCircleSprite();
        foldGradientSprite = CreateVerticalGradientSprite();

        GameObject canvasObject = new GameObject("C1V5PolishCanvas");
        canvasObject.transform.SetParent(transform, false);
        polishCanvas = canvasObject.AddComponent<Canvas>();
        polishCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        polishCanvas.sortingOrder = 450;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasRect = canvasObject.GetComponent<RectTransform>();

        // Fold-stage gradient. It is intentionally low-opacity so the actual
        // folding model remains the focus and existing scene UI stays readable.
        foldBackdrop = CreatePanel("C1V5FoldBackdrop", canvasObject.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, foldGradientSprite);
        RectTransform foldRect = foldBackdrop.GetComponent<RectTransform>();
        foldRect.offsetMin = Vector2.zero;
        foldRect.offsetMax = Vector2.zero;
        foldBackdrop.GetComponent<Image>().raycastTarget = false;
        foldBackdropGroup = foldBackdrop.AddComponent<CanvasGroup>();
        foldBackdropGroup.alpha = 0f;
        foldBackdropGroup.blocksRaycasts = false;
        foldBackdrop.SetActive(false);

        transitionImage = CreatePanel("C1V5Transition", canvasObject.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), null).GetComponent<Image>();
        RectTransform transitionRect = transitionImage.rectTransform;
        transitionRect.offsetMin = Vector2.zero;
        transitionRect.offsetMax = Vector2.zero;
        transitionImage.raycastTarget = false;
        transitionImage.gameObject.SetActive(false);

        BuildCelebrationUI(canvasObject.transform);
        BuildFactsUI(canvasObject.transform);
        BuildReticleUI(canvasObject.transform);
        BuildPlacementUI(canvasObject.transform);
        BuildIdleHintUI(canvasObject.transform);
        BuildInvalidFrameUI(canvasObject.transform);

        canvasObject.SetActive(false);
    }

    private void BuildCelebrationUI(Transform parent)
    {
        celebrationRoot = CreatePanel("C1V5Celebration", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(760f, 300f),
            new Color(0.975f, 0.992f, 1f, 0.985f), roundedSprite);
        AddShadow(celebrationRoot, new Color(0.02f, 0.08f, 0.20f, 0.28f), new Vector2(0f, -10f));
        celebrationRoot.GetComponent<Image>().raycastTarget = false;
        celebrationGroup = celebrationRoot.AddComponent<CanvasGroup>();
        celebrationGroup.blocksRaycasts = false;

        GameObject accent = CreatePanel("Accent", celebrationRoot.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -9f), new Vector2(650f, 14f),
            new Color(0.20f, 0.55f, 1f, 1f), roundedSprite);
        accent.GetComponent<Image>().raycastTarget = false;

        celebrationTitle = CreateText("Title", celebrationRoot.transform, 40, TextAnchor.MiddleCenter, new Color(0.06f, 0.27f, 0.55f, 1f));
        celebrationTitle.fontStyle = FontStyle.Bold;
        SetRect(celebrationTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(680f, 90f));

        celebrationBody = CreateText("Body", celebrationRoot.transform, 27, TextAnchor.MiddleCenter, new Color(0.20f, 0.29f, 0.42f, 1f));
        SetRect(celebrationBody.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(680f, 130f));

        // Small decorative dots; simple Images avoid external assets and keep
        // the patch safe for the integrated project.
        Vector2[] dotPositions =
        {
            new Vector2(-330f, 105f), new Vector2(330f, 100f), new Vector2(-350f, -85f),
            new Vector2(350f, -78f), new Vector2(-265f, 138f), new Vector2(272f, 142f)
        };
        for (int i = 0; i < dotPositions.Length; i++)
        {
            GameObject dot = CreatePanel("Spark" + i, celebrationRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dotPositions[i], new Vector2(18f + (i % 2) * 8f, 18f + (i % 2) * 8f),
                i % 2 == 0 ? new Color(0.24f, 0.63f, 1f, 0.95f) : new Color(0.68f, 0.34f, 1f, 0.92f), circleSprite);
            dot.GetComponent<Image>().raycastTarget = false;
        }

        celebrationRoot.SetActive(false);
    }

    private void BuildFactsUI(Transform parent)
    {
        factsRoot = CreatePanel("C1V5FactsCard", parent,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(760f, 130f),
            new Color(0.06f, 0.12f, 0.25f, 0.94f), roundedSprite);
        factsRoot.GetComponent<Image>().raycastTarget = false;
        AddShadow(factsRoot, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -5f));
        factsGroup = factsRoot.AddComponent<CanvasGroup>();
        factsGroup.blocksRaycasts = false;

        factsTitle = CreateText("FactTitle", factsRoot.transform, 19, TextAnchor.MiddleLeft, new Color(0.42f, 0.78f, 1f, 1f));
        factsTitle.fontStyle = FontStyle.Bold;
        SetRect(factsTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 30f), new Vector2(185f, 46f));

        factsText = CreateText("FactText", factsRoot.transform, 25, TextAnchor.MiddleLeft, Color.white);
        SetRect(factsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(445f, -22f), new Vector2(650f, 70f));
        factsRoot.SetActive(false);
    }

    private void BuildReticleUI(Transform parent)
    {
        reticleRoot = new GameObject("C1V5ARReticle");
        reticleRoot.layer = 5;
        reticleRoot.transform.SetParent(parent, false);
        RectTransform rootRect = reticleRoot.AddComponent<RectTransform>();
        SetRect(rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 260f));

        GameObject spinner = new GameObject("Spinner");
        spinner.layer = 5;
        spinner.transform.SetParent(reticleRoot.transform, false);
        reticleSpinner = spinner.AddComponent<RectTransform>();
        SetRect(reticleSpinner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(170f, 170f));

        CreateReticleMark(spinner.transform, new Vector2(0f, 72f), new Vector2(58f, 10f));
        CreateReticleMark(spinner.transform, new Vector2(0f, -72f), new Vector2(58f, 10f));
        CreateReticleMark(spinner.transform, new Vector2(-72f, 0f), new Vector2(10f, 58f));
        CreateReticleMark(spinner.transform, new Vector2(72f, 0f), new Vector2(10f, 58f));

        GameObject centerDot = CreatePanel("CenterDot", reticleRoot.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(22f, 22f),
            new Color(0.20f, 0.58f, 1f, 0.95f), circleSprite);
        centerDot.GetComponent<Image>().raycastTarget = false;
        reticleMarks.Add(centerDot.GetComponent<Image>());

        reticleText = CreateText("ReticleText", reticleRoot.transform, 25, TextAnchor.MiddleCenter, Color.white);
        SetRect(reticleText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(460f, 72f));
        Shadow textShadow = reticleText.gameObject.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        textShadow.effectDistance = new Vector2(1f, -2f);

        reticleRoot.SetActive(false);
    }

    private void CreateReticleMark(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject mark = CreatePanel("Mark", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size,
            new Color(0.20f, 0.58f, 1f, 0.92f), roundedSprite);
        Image image = mark.GetComponent<Image>();
        image.raycastTarget = false;
        reticleMarks.Add(image);
    }

    private void BuildPlacementUI(Transform parent)
    {
        placementRoot = CreatePanel("C1V5PlacementConfirmation", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(500f, 160f),
            new Color(0.06f, 0.16f, 0.27f, 0.94f), roundedSprite);
        placementRoot.GetComponent<Image>().raycastTarget = false;
        AddShadow(placementRoot, new Color(0f, 0f, 0f, 0.30f), new Vector2(0f, -7f));
        placementGroup = placementRoot.AddComponent<CanvasGroup>();
        placementGroup.blocksRaycasts = false;

        placementTitle = CreateText("PlacedText", placementRoot.transform, 42, TextAnchor.MiddleCenter, new Color(0.36f, 1f, 0.65f, 1f));
        placementTitle.fontStyle = FontStyle.Bold;
        placementTitle.text = "✓  PLACED!";
        placementRoot.SetActive(false);
    }

    private void BuildIdleHintUI(Transform parent)
    {
        idleHintRoot = CreatePanel("C1V5IdleHint", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 112f),
            new Color(0.12f, 0.34f, 0.70f, 0.97f), roundedSprite);
        idleHintRoot.GetComponent<Image>().raycastTarget = false;
        AddShadow(idleHintRoot, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -5f));
        idleHintGroup = idleHintRoot.AddComponent<CanvasGroup>();
        idleHintGroup.blocksRaycasts = false;
        idleHintRect = idleHintRoot.GetComponent<RectTransform>();

        idleHintText = CreateText("HintText", idleHintRoot.transform, 27, TextAnchor.MiddleCenter, Color.white);
        idleHintText.fontStyle = FontStyle.Bold;
        idleHintText.text = "TAP\n↓";
        idleHintRoot.SetActive(false);
    }

    private void BuildInvalidFrameUI(Transform parent)
    {
        invalidFrameRoot = new GameObject("C1V5InvalidFrame");
        invalidFrameRoot.layer = 5;
        invalidFrameRoot.transform.SetParent(parent, false);
        RectTransform rect = invalidFrameRoot.AddComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));
        invalidFrameGroup = invalidFrameRoot.AddComponent<CanvasGroup>();
        invalidFrameGroup.blocksRaycasts = false;

        for (int i = 0; i < 4; i++)
        {
            GameObject bar = CreatePanel("Border" + i, invalidFrameRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 10f),
                new Color(1f, 0.55f, 0.15f, 1f), roundedSprite);
            Image image = bar.GetComponent<Image>();
            image.raycastTarget = false;
            invalidFrameBars.Add(image);
        }

        invalidFrameRoot.SetActive(false);
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        SetRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = sprite == roundedSprite ? Image.Type.Sliced : Image.Type.Simple;
        }

        return go;
    }

    private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private Sprite CreateRoundedSprite()
    {
        const int size = 96;
        const float radius = 24f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "C1V5Rounded";
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
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "C1V5Circle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius + 1.2f - d);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateVerticalGradientSprite()
    {
        const int width = 8;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "C1V5FoldGradient";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        // V6: brighter translucent gradient on top of the solid camera base.
        // Alpha stays low enough that the folding model remains crisp.
        Color top = new Color(0.24f, 0.70f, 1f, 0.24f);
        Color middle = new Color(0.58f, 0.86f, 1f, 0.10f);
        Color bottom = new Color(0.64f, 0.34f, 0.94f, 0.22f);

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color c = t < 0.5f ? Color.Lerp(bottom, middle, t * 2f) : Color.Lerp(middle, top, (t - 0.5f) * 2f);
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, c);
        }

        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void HideAllV5OverlaysImmediate()
    {
        RestoreFoldStageCameraBackground();
        if (transitionImage != null)
            transitionImage.gameObject.SetActive(false);
        if (foldBackdrop != null)
            foldBackdrop.SetActive(false);
        if (celebrationRoot != null)
            celebrationRoot.SetActive(false);
        if (factsRoot != null)
            factsRoot.SetActive(false);
        if (reticleRoot != null)
            reticleRoot.SetActive(false);
        if (placementRoot != null)
            placementRoot.SetActive(false);
        if (idleHintRoot != null)
            idleHintRoot.SetActive(false);
        if (invalidFrameRoot != null)
            invalidFrameRoot.SetActive(false);
    }

    private GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t != null && t.gameObject.name == objectName)
                    return t.gameObject;
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

        foreach (GameObject root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<T>(true));

        return result.ToArray();
    }
}

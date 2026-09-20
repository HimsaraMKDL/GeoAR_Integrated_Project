using UnityEngine;
using UnityEngine.UI;

public class CylinderGuidedFaceGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RectTransform faceTray;

    [SerializeField]
    private CylinderGuidedSetupManager setupManager;

    [SerializeField]
    private CylinderEdgeSnapManager snapManager;

    // ??????? ???? ?? ????
    [SerializeField]
    private CylinderGuidedInteractionManager interactionManager;

    [Header("Drag References")]
    [SerializeField]
    private Canvas rootCanvas;

    [SerializeField]
    private RectTransform puzzleBoard;

    [Header("Display Scaling")]
    [SerializeField]
    private float circleDiameterPixels = 120f;

    [SerializeField]
    private float rectangleWidthPixels = 300f;

    [SerializeField]
    private float minimumRectangleHeight = 70f;

    [SerializeField]
    private float heightPixelsPerUnit = 35f;

    [Header("Colors")]
    [SerializeField]
    private Color circleColor = new Color(0.15f, 0.55f, 0.95f, 1f);

    [SerializeField]
    private Color rectangleColor = new Color(0.25f, 0.72f, 0.38f, 1f);

    [Header("Tray Layout")]
    [SerializeField]
    private float circleLeftX = -250f;

    [SerializeField]
    private float rectangleX = 0f;

    [SerializeField]
    private float circleRightX = 250f;

    [SerializeField]
    private float pieceY = 0f;

    private const string GeneratedPrefix = "GeneratedCylinderFace_";

    // ==================================================
    // PUBLIC GENERATION
    // ==================================================

    public void GenerateFaces()
    {
        if (setupManager == null)
        {
            Debug.LogError("CylinderGuidedFaceGenerator: Setup Manager is not assigned.");
            return;
        }

        CylinderGuidedConfig config = setupManager.GetConfig();

        if (config == null || !config.IsReady)
        {
            Debug.LogError("Cylinder face generation stopped: configuration is not ready.");
            return;
        }

        if (faceTray == null)
        {
            Debug.LogError("CylinderGuidedFaceGenerator: Face Tray is not assigned.");
            return;
        }

        ClearGeneratedFaces();

        CreateCircleFace(
            1,
            config.radius,
            new Vector2(circleLeftX, pieceY)
        );

        CreateRectangleFace(
            2,
            config.Circumference,
            config.height,
            new Vector2(rectangleX, pieceY)
        );

        CreateCircleFace(
            3,
            config.radius,
            new Vector2(circleRightX, pieceY)
        );

        Debug.Log($"Cylinder faces generated. Radius = {config.radius}, Circumference = {config.Circumference:F2}, Height = {config.height}");
    }

    // ==================================================
    // CIRCLE FACE
    // ==================================================

    private void CreateCircleFace(
        int faceId,
        float radius,
        Vector2 anchoredPosition)
    {
        GameObject circleObject = new GameObject(
            GeneratedPrefix + faceId + "_Circle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(CylinderGeneratedFace),
            typeof(CylinderDynamicFaceDrag)
        );

        circleObject.transform.SetParent(faceTray, false);

        RectTransform rect = circleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(circleDiameterPixels, circleDiameterPixels);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        Image image = circleObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        // ==================================================
        // CIRCLE VISUAL
        // ==================================================

        GameObject visualObject = new GameObject(
            "CircleVisual",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(CylinderCircleGraphic)
        );

        visualObject.transform.SetParent(circleObject.transform, false);

        RectTransform visualRect = visualObject.GetComponent<RectTransform>();
        visualRect.anchorMin = Vector2.zero;
        visualRect.anchorMax = Vector2.one;
        visualRect.offsetMin = Vector2.zero;
        visualRect.offsetMax = Vector2.zero;
        visualRect.localScale = Vector3.one;

        CylinderCircleGraphic circleGraphic = visualObject.GetComponent<CylinderCircleGraphic>();
        circleGraphic.color = circleColor;
        circleGraphic.raycastTarget = false;

        CylinderGeneratedFace data = circleObject.GetComponent<CylinderGeneratedFace>();
        data.faceId = faceId;
        data.faceType = CylinderGeneratedFaceType.Circle;
        data.radius = radius;

        AddFaceLabel(circleObject.transform, faceId.ToString());

        CylinderDynamicFaceDrag drag = circleObject.GetComponent<CylinderDynamicFaceDrag>();

        if (drag != null)
        {
            // ?????????? ?? ????
            drag.Initialize(
                puzzleBoard,
                rootCanvas,
                snapManager,
                interactionManager
            );
        }
    }

    // ==================================================
    // RECTANGLE FACE
    // ==================================================

    private void CreateRectangleFace(
        int faceId,
        float circumference,
        float cylinderHeight,
        Vector2 anchoredPosition)
    {
        GameObject rectangleObject = new GameObject(
            GeneratedPrefix + faceId + "_Rectangle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(CylinderGeneratedFace),
            typeof(CylinderDynamicFaceDrag)
        );

        rectangleObject.transform.SetParent(faceTray, false);

        RectTransform rect = rectangleObject.GetComponent<RectTransform>();

        /*
         * Important:
         *
         * Internal geometry keeps the exact circumference.
         * UI display width is intentionally compressed to remain child-friendly.
         */

        float displayHeight = Mathf.Max(
            minimumRectangleHeight,
            cylinderHeight * heightPixelsPerUnit
        );

        rect.sizeDelta = new Vector2(rectangleWidthPixels, displayHeight);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        Image image = rectangleObject.GetComponent<Image>();
        image.color = rectangleColor;
        image.raycastTarget = true;

        CylinderGeneratedFace data = rectangleObject.GetComponent<CylinderGeneratedFace>();
        data.faceId = faceId;
        data.faceType = CylinderGeneratedFaceType.Rectangle;
        data.circumference = circumference;
        data.cylinderHeight = cylinderHeight;

        Outline outline = rectangleObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2f, -2f);

        AddFaceLabel(rectangleObject.transform, faceId.ToString());

        CylinderDynamicFaceDrag drag = rectangleObject.GetComponent<CylinderDynamicFaceDrag>();

        if (drag != null)
        {
            // ?????????? ?? ????
            drag.Initialize(
                puzzleBoard,
                rootCanvas,
                snapManager,
                interactionManager
            );
        }
    }

    // ==================================================
    // LABEL
    // ==================================================

    private void AddFaceLabel(
        Transform parent,
        string label)
    {
        GameObject labelObject = new GameObject(
            "FaceNumber",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)
        );

        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = labelObject.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        labelObject.transform.SetAsLastSibling();
    }

    // ==================================================
    // CLEAR
    // ==================================================

    public void ClearGeneratedFaces()
    {
        if (faceTray == null)
            return;

        for (int index = faceTray.childCount - 1; index >= 0; index--)
        {
            Transform child = faceTray.GetChild(index);

            if (!child.name.StartsWith(GeneratedPrefix))
            {
                continue;
            }

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
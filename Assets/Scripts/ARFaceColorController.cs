using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ARFaceColorController : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private LayerMask raycastLayers = ~0;

    [SerializeField]
    private float maxRayDistance = 20f;


    [Header("Tap Detection")]
    [SerializeField]
    private float maximumTapMovement = 30f;

    [SerializeField]
    private float maximumTapDuration = 0.35f;


    [Header("Face Colors")]
    [SerializeField]
    private Color[] faceColors =
    {
        new Color(0.20f, 0.60f, 1.00f, 1f),
        new Color(1.00f, 0.45f, 0.25f, 1f),
        new Color(0.30f, 0.80f, 0.45f, 1f),
        new Color(1.00f, 0.80f, 0.20f, 1f),
        new Color(0.65f, 0.40f, 1.00f, 1f),
        new Color(1.00f, 0.35f, 0.65f, 1f)
    };


    [Header("Face Number Labels")]
    [SerializeField]
    private bool showFaceNumbers = true;

    [SerializeField]
    private float numberSurfaceOffset = 0.008f;

    [SerializeField]
    private float numberScaleFromFace = 0.18f;

    [SerializeField]
    private float minimumNumberWorldSize = 0.035f;

    [SerializeField]
    private float maximumNumberWorldSize = 0.09f;

    [SerializeField]
    private Color numberBackgroundColor =
        new Color(0.035f, 0.11f, 0.25f, 0.90f);

    [SerializeField]
    private Color numberTextColor = Color.white;


    private Transform activeModelRoot;

    private Vector2 touchStartPosition;
    private float touchStartTime;

    private bool trackingTap = false;

    private int nextColorIndex = 0;

    // V9: number each unique rendered face once. Re-tapping that face only
    // changes its color; its original number remains unchanged.
    private readonly Dictionary<Renderer, int> faceNumbers =
        new Dictionary<Renderer, int>();

    private readonly Dictionary<Renderer, RectTransform> faceNumberLabels =
        new Dictionary<Renderer, RectTransform>();

    private int nextFaceNumber = 1;


    // ==================================================
    // SET MODEL
    // ==================================================

    public void SetActiveModel(
        Transform modelRoot)
    {
        ClearFaceNumberLabels();

        activeModelRoot =
            modelRoot;

        nextColorIndex = 0;
        nextFaceNumber = 1;

        Debug.Log(
            activeModelRoot != null
                ? "AR face coloring + numbering enabled."
                : "AR face coloring disabled."
        );
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (activeModelRoot == null)
            return;

        if (Input.touchCount != 1)
        {
            trackingTap = false;
            return;
        }

        Touch touch =
            Input.GetTouch(0);


        // ----------------------------------------------
        // TOUCH START
        // ----------------------------------------------

        if (touch.phase ==
            TouchPhase.Began)
        {
            /*
             * Ignore UI buttons.
             */
            if (EventSystem.current != null &&
                EventSystem.current
                    .IsPointerOverGameObject(
                        touch.fingerId))
            {
                trackingTap = false;
                return;
            }

            touchStartPosition =
                touch.position;

            touchStartTime =
                Time.time;

            trackingTap = true;

            return;
        }


        // ----------------------------------------------
        // MOVEMENT CHECK
        // ----------------------------------------------

        if (touch.phase ==
            TouchPhase.Moved)
        {
            float movement =
                Vector2.Distance(
                    touchStartPosition,
                    touch.position
                );

            /*
             * This was a drag/rotation,
             * not a tap.
             */
            if (movement >
                maximumTapMovement)
            {
                trackingTap = false;
            }

            return;
        }


        // ----------------------------------------------
        // TOUCH END
        // ----------------------------------------------

        if (touch.phase ==
            TouchPhase.Ended)
        {
            if (!trackingTap)
                return;

            trackingTap = false;

            float duration =
                Time.time -
                touchStartTime;

            float movement =
                Vector2.Distance(
                    touchStartPosition,
                    touch.position
                );

            if (duration >
                    maximumTapDuration ||
                movement >
                    maximumTapMovement)
            {
                return;
            }

            TryColorFace(
                touch.position
            );
        }
    }


    // ==================================================
    // KEEP NUMBER LABELS FACING THE AR CAMERA
    // ==================================================

    private void LateUpdate()
    {
        if (faceNumberLabels.Count == 0)
            return;

        Camera cameraToUse =
            arCamera != null
                ? arCamera
                : Camera.main;

        if (cameraToUse == null)
            return;

        List<Renderer> missing = null;

        foreach (KeyValuePair<Renderer, RectTransform> pair
                 in faceNumberLabels)
        {
            if (pair.Key == null || pair.Value == null)
            {
                if (missing == null)
                    missing = new List<Renderer>();

                missing.Add(pair.Key);
                continue;
            }

            // Match the camera orientation so the number remains readable
            // while the learner rotates / explores the placed solid.
            pair.Value.rotation =
                Quaternion.LookRotation(
                    cameraToUse.transform.forward,
                    cameraToUse.transform.up
                );
        }

        if (missing != null)
        {
            foreach (Renderer renderer in missing)
                faceNumberLabels.Remove(renderer);
        }
    }


    // ==================================================
    // COLOR + NUMBER FACE
    // ==================================================

    private void TryColorFace(
        Vector2 screenPosition)
    {
        Camera cameraToUse =
            arCamera != null
                ? arCamera
                : Camera.main;

        if (cameraToUse == null)
        {
            Debug.LogError(
                "ARFaceColorController: Camera not found."
            );

            return;
        }


        Ray ray =
            cameraToUse.ScreenPointToRay(
                screenPosition
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxRayDistance,
                raycastLayers))
        {
            return;
        }


        /*
         * Only allow objects belonging
         * to the currently placed model.
         */
        if (!hit.transform.IsChildOf(
                activeModelRoot) &&
            hit.transform !=
                activeModelRoot)
        {
            return;
        }


        Renderer renderer =
            hit.collider
                .GetComponent<Renderer>();

        if (renderer == null)
        {
            renderer =
                hit.collider
                    .GetComponentInParent<
                        Renderer>();
        }

        if (renderer == null)
            return;


        // V9: assign the number only on the FIRST tap of this face.
        // Future taps still cycle the face color but keep the same number.
        EnsureFaceNumber(
            renderer,
            hit.point,
            hit.normal,
            cameraToUse
        );

        ApplyNextColor(
            renderer
        );


        Debug.Log(
            $"AR face touched: " +
            $"{hit.transform.name}" +
            (faceNumbers.TryGetValue(renderer, out int number)
                ? $" | Face #{number}"
                : string.Empty)
        );
    }


    // ==================================================
    // V9 FACE NUMBERING
    // ==================================================

    private void EnsureFaceNumber(
        Renderer renderer,
        Vector3 hitPoint,
        Vector3 hitNormal,
        Camera cameraToUse)
    {
        if (!showFaceNumbers ||
            renderer == null ||
            activeModelRoot == null)
        {
            return;
        }

        if (faceNumbers.ContainsKey(renderer))
            return;

        int assignedNumber =
            nextFaceNumber++;

        faceNumbers[renderer] =
            assignedNumber;

        RectTransform label =
            CreateFaceNumberLabel(
                renderer,
                assignedNumber,
                hitPoint,
                hitNormal,
                cameraToUse
            );

        if (label != null)
        {
            faceNumberLabels[renderer] =
                label;
        }
    }


    private RectTransform CreateFaceNumberLabel(
        Renderer renderer,
        int faceNumber,
        Vector3 hitPoint,
        Vector3 hitNormal,
        Camera cameraToUse)
    {
        GameObject labelRoot =
            new GameObject(
                $"C1_FaceNumber_{faceNumber}",
                typeof(RectTransform),
                typeof(Canvas)
            );

        labelRoot.transform.SetParent(
            activeModelRoot,
            true
        );

        RectTransform rootRect =
            labelRoot.GetComponent<RectTransform>();

        rootRect.sizeDelta =
            new Vector2(100f, 100f);

        Vector3 normal =
            hitNormal.sqrMagnitude > 0.0001f
                ? hitNormal.normalized
                : (cameraToUse != null
                    ? -cameraToUse.transform.forward
                    : Vector3.up);

        rootRect.position =
            hitPoint +
            normal * numberSurfaceOffset;

        float faceReferenceSize =
            Mathf.Max(
                renderer.bounds.size.x,
                renderer.bounds.size.y,
                renderer.bounds.size.z
            );

        float numberWorldSize =
            Mathf.Clamp(
                faceReferenceSize * numberScaleFromFace,
                minimumNumberWorldSize,
                maximumNumberWorldSize
            );

        float parentScale = 1f;
        Vector3 lossyScale =
            activeModelRoot.lossyScale;

        parentScale =
            Mathf.Max(
                0.0001f,
                Mathf.Max(
                    Mathf.Abs(lossyScale.x),
                    Mathf.Max(
                        Mathf.Abs(lossyScale.y),
                        Mathf.Abs(lossyScale.z)
                    )
                )
            );

        rootRect.localScale =
            Vector3.one *
            (numberWorldSize /
             (100f * parentScale));

        Canvas canvas =
            labelRoot.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.WorldSpace;

        canvas.worldCamera =
            cameraToUse;

        canvas.sortingOrder = 120;


        // ----------------------------------------------
        // LABEL BACKGROUND
        // ----------------------------------------------

        GameObject backgroundObject =
            new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        backgroundObject.transform.SetParent(
            labelRoot.transform,
            false
        );

        RectTransform backgroundRect =
            backgroundObject.GetComponent<RectTransform>();

        backgroundRect.anchorMin =
            Vector2.zero;
        backgroundRect.anchorMax =
            Vector2.one;
        backgroundRect.offsetMin =
            Vector2.zero;
        backgroundRect.offsetMax =
            Vector2.zero;

        Image background =
            backgroundObject.GetComponent<Image>();

        background.color =
            numberBackgroundColor;

        background.raycastTarget =
            false;

        Outline backgroundOutline =
            backgroundObject.AddComponent<Outline>();

        backgroundOutline.effectColor =
            new Color(0.35f, 0.78f, 1f, 0.95f);

        backgroundOutline.effectDistance =
            new Vector2(3f, -3f);


        // ----------------------------------------------
        // NUMBER TEXT
        // ----------------------------------------------

        GameObject textObject =
            new GameObject(
                "Number",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

        textObject.transform.SetParent(
            labelRoot.transform,
            false
        );

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin =
            Vector2.zero;
        textRect.anchorMax =
            Vector2.one;
        textRect.offsetMin =
            Vector2.zero;
        textRect.offsetMax =
            Vector2.zero;

        Text numberText =
            textObject.GetComponent<Text>();

        numberText.text =
            faceNumber.ToString();

        numberText.alignment =
            TextAnchor.MiddleCenter;

        numberText.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        numberText.fontSize = 64;
        numberText.fontStyle =
            FontStyle.Bold;

        numberText.color =
            numberTextColor;

        numberText.raycastTarget =
            false;

        Outline textOutline =
            textObject.AddComponent<Outline>();

        textOutline.effectColor =
            new Color(0f, 0f, 0f, 0.82f);

        textOutline.effectDistance =
            new Vector2(2f, -2f);


        if (cameraToUse != null)
        {
            rootRect.rotation =
                Quaternion.LookRotation(
                    cameraToUse.transform.forward,
                    cameraToUse.transform.up
                );
        }

        return rootRect;
    }


    private void ClearFaceNumberLabels()
    {
        foreach (KeyValuePair<Renderer, RectTransform> pair
                 in faceNumberLabels)
        {
            if (pair.Value != null)
            {
                Destroy(
                    pair.Value.gameObject
                );
            }
        }

        faceNumberLabels.Clear();
        faceNumbers.Clear();
        nextFaceNumber = 1;
    }


    // ==================================================
    // APPLY NEXT COLOR
    // ==================================================

    private void ApplyNextColor(
        Renderer renderer)
    {
        if (renderer == null ||
            faceColors == null ||
            faceColors.Length == 0)
        {
            return;
        }


        Color selectedColor =
            faceColors[
                nextColorIndex
            ];


        /*
         * renderer.material creates
         * an independent material instance,
         * so only the selected face changes.
         */
        Material material =
            renderer.material;

        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                selectedColor
            );
        }
        else if (material.HasProperty(
                     "_Color"))
        {
            material.SetColor(
                "_Color",
                selectedColor
            );
        }


        nextColorIndex++;

        if (nextColorIndex >=
            faceColors.Length)
        {
            nextColorIndex = 0;
        }
    }


    // ==================================================
    // HELPER FOR AR PLACEMENT
    // ==================================================

    public bool IsScreenPointOnActiveModel(
        Vector2 screenPosition)
    {
        if (activeModelRoot == null)
            return false;

        Camera cameraToUse =
            arCamera != null
                ? arCamera
                : Camera.main;

        if (cameraToUse == null)
            return false;


        Ray ray =
            cameraToUse.ScreenPointToRay(
                screenPosition
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxRayDistance,
                raycastLayers))
        {
            return false;
        }


        return
            hit.transform ==
                activeModelRoot ||
            hit.transform.IsChildOf(
                activeModelRoot
            );
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

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


    private Transform activeModelRoot;

    private Vector2 touchStartPosition;
    private float touchStartTime;

    private bool trackingTap = false;

    private int nextColorIndex = 0;


    // ==================================================
    // SET MODEL
    // ==================================================

    public void SetActiveModel(
        Transform modelRoot)
    {
        activeModelRoot =
            modelRoot;

        nextColorIndex = 0;

        Debug.Log(
            activeModelRoot != null
                ? "AR face coloring enabled."
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
    // COLOR FACE
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


        ApplyNextColor(
            renderer
        );


        Debug.Log(
            $"AR face touched: " +
            $"{hit.transform.name}"
        );
    }


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
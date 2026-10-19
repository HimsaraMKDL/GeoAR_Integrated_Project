using UnityEngine;

public class ModelRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float mouseRotationSpeed = 120f;
    [SerializeField] private float touchRotationSpeed = 0.25f;

    private bool isDragging;
    private Vector2 previousPointerPosition;

    private void Update()
    {
        HandleMouseRotation();
        HandleTouchRotation();
    }

    private void HandleMouseRotation()
    {
        if (Input.touchCount > 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;

            previousPointerPosition =
                Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (!isDragging)
        {
            return;
        }

        Vector2 currentPointerPosition =
            Input.mousePosition;

        Vector2 delta =
            currentPointerPosition -
            previousPointerPosition;

        float rotationY =
            -delta.x *
            mouseRotationSpeed *
            Time.deltaTime;

        float rotationX =
            delta.y *
            mouseRotationSpeed *
            Time.deltaTime;

        transform.Rotate(
            Vector3.up,
            rotationY,
            Space.World
        );

        transform.Rotate(
            Vector3.right,
            rotationX,
            Space.World
        );

        previousPointerPosition =
            currentPointerPosition;
    }

    private void HandleTouchRotation()
    {
        if (Input.touchCount != 1)
        {
            return;
        }

        Touch touch =
            Input.GetTouch(0);

        if (touch.phase != TouchPhase.Moved)
        {
            return;
        }

        float rotationY =
            -touch.deltaPosition.x *
            touchRotationSpeed;

        float rotationX =
            touch.deltaPosition.y *
            touchRotationSpeed;

        transform.Rotate(
            Vector3.up,
            rotationY,
            Space.World
        );

        transform.Rotate(
            Vector3.right,
            rotationX,
            Space.World
        );
    }
}
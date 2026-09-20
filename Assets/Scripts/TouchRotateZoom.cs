using UnityEngine;
using UnityEngine.EventSystems;

public class TouchRotateZoom : MonoBehaviour
{
    public Transform target;

    [Header("Rotate")]
    public float rotateSpeed = 0.10f; // Updated rotate speed

    [Header("Zoom")]
    public float zoomSpeed = 0.01f;
    public float minScale = 0.5f;
    public float maxScale = 2.5f;

    private float lastPinchDistance;

    private void Start()
    {
        if (target == null)
            target = transform;
    }

    private void Update()
    {
        if (target == null)
            return;

        HandleMouseEditor();
        HandleTouchMobile();
    }

    private void HandleMouseEditor()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0))
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            // Updated mouse multiplier to 60f
            target.Rotate(Vector3.up, -x * rotateSpeed * 60f, Space.World);
            target.Rotate(Vector3.right, y * rotateSpeed * 60f, Space.World);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            float scaleChange = scroll * 2f;
            ApplyZoom(scaleChange);
        }
    }

    private void HandleTouchMobile()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;

                target.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);
                target.Rotate(Vector3.right, delta.y * rotateSpeed, Space.World);
            }
        }

        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(t1.position, t2.position);

            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                lastPinchDistance = currentDistance;
            }
            else
            {
                float difference = currentDistance - lastPinchDistance;
                ApplyZoom(difference * zoomSpeed);
                lastPinchDistance = currentDistance;
            }
        }
    }

    private void ApplyZoom(float amount)
    {
        Vector3 newScale = target.localScale + Vector3.one * amount;
        float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
        target.localScale = Vector3.one * clamped;
    }
}
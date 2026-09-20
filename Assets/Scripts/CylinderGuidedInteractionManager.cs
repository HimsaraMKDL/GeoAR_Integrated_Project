using UnityEngine;
using UnityEngine.UI;

public class CylinderGuidedInteractionManager :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Button rotateButton;

    [SerializeField]
    private Text statusText;

    [Header("Rotation")]
    [SerializeField]
    private float rotationStep = 90f;

    private CylinderDynamicFaceDrag selectedFace;

    private void Start()
    {
        if (rotateButton != null)
        {
            rotateButton.interactable = false;

            rotateButton.onClick.RemoveListener(
                RotateSelectedFace
            );

            rotateButton.onClick.AddListener(
                RotateSelectedFace
            );
        }
    }

    public void SelectFace(
        CylinderDynamicFaceDrag face)
    {
        selectedFace = face;

        if (rotateButton != null)
            rotateButton.interactable =
                selectedFace != null;

        if (selectedFace != null &&
            selectedFace.FaceData != null &&
            statusText != null)
        {
            statusText.text =
                $"Face {selectedFace.FaceData.faceId} selected.";
        }
    }

    public void RotateSelectedFace()
    {
        if (selectedFace == null)
            return;

        RectTransform rect =
            selectedFace.GetComponent<RectTransform>();

        if (rect == null)
            return;

        rect.Rotate(
            0f,
            0f,
            -rotationStep
        );

        if (statusText != null)
        {
            statusText.text =
                $"Face {selectedFace.FaceData.faceId} rotated.";
        }
    }

    public void ClearSelection()
    {
        selectedFace = null;

        if (rotateButton != null)
            rotateButton.interactable = false;
    }
}
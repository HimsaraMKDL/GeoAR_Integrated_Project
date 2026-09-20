using UnityEngine;
using UnityEngine.UI;

public class PrismGuidedInteractionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Button rotateButton;

    [SerializeField]
    private Button flipButton;

    [SerializeField]
    private Text statusText;

    private PrismDynamicFaceDrag selectedFace;

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

        if (flipButton != null)
        {
            flipButton.onClick.RemoveAllListeners();

            flipButton.onClick.AddListener(
                FlipSelectedFace
            );

            flipButton.interactable = false;
        }
    }

    public void SelectFace(
        PrismDynamicFaceDrag face)
    {
        if (face == null)
            return;

        selectedFace = face;

        if (rotateButton != null)
        {
            rotateButton.interactable = true;
        }

        PrismGeneratedFace faceData =
            face.GetComponent<PrismGeneratedFace>();

        if (statusText != null)
        {
            if (faceData != null)
            {
                string typeName =
                    faceData.IsTriangle
                        ? "triangle"
                        : "rectangle";

                statusText.text =
                    $"Face {faceData.faceId} selected. Rotate it if needed.";
            }
            else
            {
                statusText.text =
                    "Face selected.";
            }
        }

        if (flipButton != null)
        {
            flipButton.interactable =
                faceData != null &&
                faceData.IsTriangle;
        }
    }

    public void RotateSelectedFace()
    {
        if (selectedFace == null)
            return;

        selectedFace.RotateClockwise();

        if (statusText != null)
        {
            statusText.text =
                $"Rotated to {selectedFace.CurrentRotation:0}°.";
        }
    }

    public void FlipSelectedFace()
    {
        if (selectedFace == null)
        {
            Debug.LogWarning(
                "Flip clicked, but no face is selected."
            );
            return;
        }

        PrismGeneratedFace faceData =
            selectedFace.GetComponent<
                PrismGeneratedFace>();

        if (faceData == null ||
            !faceData.IsTriangle)
        {
            return;
        }

        selectedFace.FlipHorizontal();

        if (statusText != null)
        {
            statusText.text =
                selectedFace.IsFlipped
                    ? "Triangle flipped."
                    : "Triangle returned to its original side.";
        }
    }

    public void ClearSelection()
    {
        selectedFace = null;

        if (rotateButton != null)
        {
            rotateButton.interactable = false;
        }

        if (flipButton != null)
        {
            flipButton.interactable = false;
        }
    }
}
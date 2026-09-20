using UnityEngine;
using UnityEngine.UI;

public class PrismPuzzleManager : MonoBehaviour
{
    [Header("Puzzle References")]
    [SerializeField]
    private PrismPuzzleFace[] puzzleFaces;

    [SerializeField]
    private PrismPuzzleSlot[] puzzleSlots;

    [Header("UI")]
    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text progressText;

    [SerializeField]
    private Button checkPuzzleButton;

    [SerializeField]
    private Button create3DButton;

    [Header("Messages")]
    [SerializeField]
    private string startMessage =
        "Drag each face to its correct place.";

    private int placedFaceCount = 0;
    private bool puzzleComplete = false;

    private void Start()
    {
        ResetPuzzleState();
    }

    public void NotifyCorrectPlacement(
        PrismPuzzleFace face,
        PrismPuzzleSlot slot)
    {
        placedFaceCount++;

        UpdateProgress();

        if (statusText != null)
        {
            statusText.text =
                "Correct! Place the next face.";
        }

        if (placedFaceCount >= 5)
        {
            puzzleComplete = true;

            if (checkPuzzleButton != null)
                checkPuzzleButton.interactable =
                    true;

            if (statusText != null)
            {
                statusText.text =
                    "All 5 faces are placed. Check your prism net!";
            }
        }
    }

    public void NotifyWrongPlacement(
        PrismPuzzleFace face,
        PrismPuzzleSlot slot)
    {
        if (statusText != null)
        {
            statusText.text =
                "Try another position.";
        }
    }

    public void CheckPuzzle()
    {
        if (!puzzleComplete)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Place all 5 faces first.";
            }

            return;
        }

        foreach (PrismPuzzleSlot slot
                 in puzzleSlots)
        {
            if (slot == null ||
                !slot.IsOccupied)
            {
                if (statusText != null)
                {
                    statusText.text =
                        "The prism net is not complete yet.";
                }

                return;
            }
        }

        if (statusText != null)
        {
            statusText.text =
                "Excellent! You built a valid triangular prism net.";
        }

        if (create3DButton != null)
        {
            create3DButton.interactable =
                true;
        }

        Debug.Log(
            "Guided prism puzzle validated successfully."
        );
    }

    public void ResetPuzzle()
    {
        foreach (PrismPuzzleFace face
                 in puzzleFaces)
        {
            if (face != null)
                face.UnlockAndReturn();
        }

        foreach (PrismPuzzleSlot slot
                 in puzzleSlots)
        {
            if (slot != null)
                slot.ResetSlot();
        }

        ResetPuzzleState();
    }

    private void ResetPuzzleState()
    {
        placedFaceCount = 0;
        puzzleComplete = false;

        if (checkPuzzleButton != null)
        {
            checkPuzzleButton.interactable =
                false;
        }

        if (create3DButton != null)
        {
            create3DButton.interactable =
                false;
        }

        if (statusText != null)
        {
            statusText.text =
                startMessage;
        }

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text =
                $"Faces placed: {placedFaceCount} / 5";
        }
    }
}
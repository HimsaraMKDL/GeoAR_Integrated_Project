using UnityEngine;

public class CuboidCorrectionData
{
    public CuboidFace wrongFace;

    public Vector2Int suggestedBottomLeft;

    public int suggestedWidth;
    public int suggestedHeight;

    public bool hasSuggestion;

    public string message1;
    public string message2;

    public CuboidCorrectionData(
        CuboidFace wrongFace,
        Vector2Int suggestedBottomLeft,
        int suggestedWidth,
        int suggestedHeight,
        bool hasSuggestion,
        string message1,
        string message2)
    {
        this.wrongFace = wrongFace;

        this.suggestedBottomLeft =
            suggestedBottomLeft;

        this.suggestedWidth =
            suggestedWidth;

        this.suggestedHeight =
            suggestedHeight;

        this.hasSuggestion =
            hasSuggestion;

        this.message1 =
            message1;

        this.message2 =
            message2;
    }
}
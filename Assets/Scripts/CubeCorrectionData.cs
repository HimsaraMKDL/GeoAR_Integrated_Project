using UnityEngine;

public class CubeCorrectionData
{
    public Vector2Int wrongCell;
    public Vector2Int suggestedCell;

    public string message1;
    public string message2;

    public bool hasSuggestion;

    public CubeCorrectionData(
        Vector2Int wrongCell,
        Vector2Int suggestedCell,
        bool hasSuggestion,
        string message1,
        string message2)
    {
        this.wrongCell = wrongCell;
        this.suggestedCell = suggestedCell;
        this.hasSuggestion = hasSuggestion;
        this.message1 = message1;
        this.message2 = message2;
    }
}
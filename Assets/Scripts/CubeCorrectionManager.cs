using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeCorrectionManager : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]

    [SerializeField]
    private GridDrawManager cubeDrawManager;

    [SerializeField]
    private RectTransform correctionDrawingArea;

    [SerializeField]
    private Text correctionMessageText;


    // ==================================================
    // VISUALS
    // ==================================================

    [Header("Visuals")]

    [SerializeField]
    private Color normalFaceColor =
        new Color(
            1f,
            1f,
            1f,
            0.85f
        );

    [SerializeField]
    private Color wrongFaceColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            0.85f
        );

    [SerializeField]
    private Color suggestedFaceColor =
        new Color(
            1f,
            0.82f,
            0.15f,
            0.40f
        );


    // ==================================================
    // RUNTIME DATA
    // ==================================================

    private readonly List<GameObject>
        createdFaces =
            new List<GameObject>();

    private GameObject suggestedFaceObject;

    private HashSet<Vector2Int>
        currentFaces;

    private CubeCorrectionData
        currentCorrection;


    // ==================================================
    // PREPARE CORRECTION
    // ==================================================

    public void PrepareCorrection()
    {
        ClearCorrectionVisuals();


        if (cubeDrawManager == null ||
            correctionDrawingArea == null)
        {
            Debug.LogError(
                "CubeCorrectionManager references are missing."
            );

            return;
        }


        currentFaces =
            cubeDrawManager
                .GetDetectedFacesCopy();


        if (currentFaces == null ||
            currentFaces.Count == 0)
        {
            Debug.LogWarning(
                "No Cube faces are available for correction."
            );

            return;
        }


        /*
         * First try the intelligent correction:
         *
         * Remove one existing face
         * and search for a new position that
         * produces a valid Cube net.
         */
        currentCorrection =
            FindNearestOneMoveCorrection(
                currentFaces
            );


        /*
         * If the student's pattern needs more
         * than one movement, use a safe fallback.
         */
        if (currentCorrection == null)
        {
            currentCorrection =
                BuildFallbackCorrection(
                    currentFaces
                );
        }


        DrawStudentNet();


        if (currentCorrection != null)
        {
            HighlightWrongFace();


            if (currentCorrection.hasSuggestion)
            {
                ShowSuggestedFace();
            }


            if (correctionMessageText != null)
            {
                correctionMessageText.text =
                    currentCorrection.message1;
            }


            Debug.Log(
                "Cube correction prepared. " +
                $"Wrong = {currentCorrection.wrongCell}, " +
                $"Suggested = {currentCorrection.suggestedCell}, " +
                $"Has suggestion = {currentCorrection.hasSuggestion}"
            );
        }
    }


    // ==================================================
    // INTELLIGENT ONE-MOVE CORRECTION
    // ==================================================

    private CubeCorrectionData
        FindNearestOneMoveCorrection(
            HashSet<Vector2Int> faces)
    {
        /*
         * Correction feature is currently designed
         * for an invalid Cube arrangement containing
         * exactly six detected square faces.
         */
        if (faces == null ||
            faces.Count != 6)
        {
            return null;
        }


        CubeCorrectionData bestCorrection =
            null;


        int bestMoveDistance =
            int.MaxValue;


        /*
         * Try every existing face as the face
         * that may have been placed incorrectly.
         */
        foreach (Vector2Int possibleWrongCell
                 in faces)
        {
            HashSet<Vector2Int> remainingFaces =
                new HashSet<Vector2Int>(
                    faces
                );


            remainingFaces.Remove(
                possibleWrongCell
            );


            /*
             * We only need sensible candidate cells:
             * empty grid cells neighbouring at least
             * one of the remaining five faces.
             */
            HashSet<Vector2Int> candidateCells =
                GetCandidateCells(
                    remainingFaces
                );


            foreach (Vector2Int candidate
                     in candidateCells)
            {
                /*
                 * Do not suggest the exact same place.
                 */
                if (candidate ==
                    possibleWrongCell)
                {
                    continue;
                }


                /*
                 * Keep suggestion inside the student's
                 * actual Cube grid.
                 */
                if (!IsInsideGrid(
                        candidate))
                {
                    continue;
                }


                /*
                 * It must be an empty location.
                 */
                if (faces.Contains(
                        candidate))
                {
                    continue;
                }


                HashSet<Vector2Int> testNet =
                    new HashSet<Vector2Int>(
                        remainingFaces
                    );


                testNet.Add(
                    candidate
                );


                bool valid =
                    CubeNetValidator
                        .ValidateCubeNet(
                            testNet,
                            out string validationMessage
                        );


                if (!valid)
                {
                    continue;
                }


                /*
                 * If several possible fixes exist,
                 * choose the smallest movement.
                 *
                 * This is easier for the child:
                 * move one square to the nearest
                 * position that makes a valid net.
                 */
                int moveDistance =
                    Mathf.Abs(
                        candidate.x -
                        possibleWrongCell.x
                    )
                    +
                    Mathf.Abs(
                        candidate.y -
                        possibleWrongCell.y
                    );


                if (moveDistance >=
                    bestMoveDistance)
                {
                    continue;
                }


                bestMoveDistance =
                    moveDistance;


                bestCorrection =
                    new CubeCorrectionData(
                        possibleWrongCell,
                        candidate,
                        true,

                        "Look at this square!",

                        "Try moving it to the highlighted place."
                    );


                Debug.Log(
                    "Possible Cube correction found: " +
                    $"{possibleWrongCell} -> {candidate}. " +
                    $"Move distance = {moveDistance}. " +
                    validationMessage
                );
            }
        }


        return bestCorrection;
    }


    // ==================================================
    // CANDIDATE POSITIONS
    // ==================================================

    private HashSet<Vector2Int>
        GetCandidateCells(
            HashSet<Vector2Int> remainingFaces)
    {
        HashSet<Vector2Int> candidates =
            new HashSet<Vector2Int>();


        Vector2Int[] directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };


        foreach (Vector2Int face
                 in remainingFaces)
        {
            foreach (Vector2Int direction
                     in directions)
            {
                Vector2Int candidate =
                    face +
                    direction;


                if (remainingFaces.Contains(
                        candidate))
                {
                    continue;
                }


                if (!IsInsideGrid(
                        candidate))
                {
                    continue;
                }


                candidates.Add(
                    candidate
                );
            }
        }


        return candidates;
    }


    // ==================================================
    // GRID BOUNDS
    // ==================================================

    private bool IsInsideGrid(
        Vector2Int cell)
    {
        if (cubeDrawManager == null)
            return false;


        return
            cell.x >= 0 &&
            cell.y >= 0 &&
            cell.x <
                cubeDrawManager.GetGridWidth() &&
            cell.y <
                cubeDrawManager.GetGridHeight();
    }


    // ==================================================
    // FALLBACK
    // ==================================================

    private CubeCorrectionData
        BuildFallbackCorrection(
            HashSet<Vector2Int> faces)
    {
        /*
         * Some invalid drawings may require more
         * than one square movement.
         *
         * In that situation we still provide
         * useful guidance instead of failing.
         */

        Vector2Int fallbackCell =
            Vector2Int.zero;


        bool found =
            false;


        int smallestNeighbourCount =
            int.MaxValue;


        foreach (Vector2Int cell
                 in faces)
        {
            int neighbourCount =
                CountNeighbours(
                    cell,
                    faces
                );


            if (neighbourCount <
                smallestNeighbourCount)
            {
                smallestNeighbourCount =
                    neighbourCount;

                fallbackCell =
                    cell;

                found =
                    true;
            }
        }


        if (!found)
        {
            return null;
        }


        return new CubeCorrectionData(
            fallbackCell,
            Vector2Int.zero,
            false,

            "Look at this square!",

            "This net may need more than one small change."
        );
    }


    // ==================================================
    // NEIGHBOUR COUNT
    // ==================================================

    private int CountNeighbours(
        Vector2Int cell,
        HashSet<Vector2Int> faces)
    {
        int count =
            0;


        Vector2Int[] directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };


        foreach (Vector2Int direction
                 in directions)
        {
            if (faces.Contains(
                    cell +
                    direction))
            {
                count++;
            }
        }


        return count;
    }


    // ==================================================
    // DRAW STUDENT NET
    // ==================================================

    private void DrawStudentNet()
    {
        float cellWidth =
            GetCorrectionCellWidth();


        float cellHeight =
            GetCorrectionCellHeight();


        foreach (Vector2Int cell
                 in currentFaces)
        {
            GameObject face =
                CreateFaceVisual(
                    cell,
                    normalFaceColor,
                    "CorrectionFace"
                );


            createdFaces.Add(
                face
            );
        }
    }


    // ==================================================
    // CREATE FACE VISUAL
    // ==================================================

    private GameObject CreateFaceVisual(
        Vector2Int cell,
        Color color,
        string objectPrefix)
    {
        float cellWidth =
            GetCorrectionCellWidth();


        float cellHeight =
            GetCorrectionCellHeight();


        GameObject face =
            new GameObject(
                $"{objectPrefix}_{cell.x}_{cell.y}",
                typeof(Image)
            );


        face.transform.SetParent(
            correctionDrawingArea,
            false
        );


        Image image =
            face.GetComponent<Image>();


        image.color =
            color;


        image.raycastTarget =
            false;


        RectTransform rect =
            face.GetComponent<RectTransform>();


        rect.sizeDelta =
            new Vector2(
                cellWidth,
                cellHeight
            );


        rect.anchoredPosition =
            CellToCorrectionPosition(
                cell
            );


        return face;
    }


    // ==================================================
    // CELL POSITION
    // ==================================================

    private Vector2 CellToCorrectionPosition(
        Vector2Int cell)
    {
        float cellWidth =
            GetCorrectionCellWidth();


        float cellHeight =
            GetCorrectionCellHeight();


        float x =
            cell.x *
            cellWidth
            -
            correctionDrawingArea.rect.width /
            2f
            +
            cellWidth /
            2f;


        float y =
            cell.y *
            cellHeight
            -
            correctionDrawingArea.rect.height /
            2f
            +
            cellHeight /
            2f;


        return new Vector2(
            x,
            y
        );
    }


    // ==================================================
    // CELL SIZE
    // ==================================================

    private float GetCorrectionCellWidth()
    {
        return
            correctionDrawingArea.rect.width /
            cubeDrawManager.GetGridWidth();
    }


    private float GetCorrectionCellHeight()
    {
        return
            correctionDrawingArea.rect.height /
            cubeDrawManager.GetGridHeight();
    }


    // ==================================================
    // HIGHLIGHT WRONG FACE
    // ==================================================

    private void HighlightWrongFace()
    {
        if (currentCorrection == null)
            return;


        string targetName =
            $"CorrectionFace_" +
            $"{currentCorrection.wrongCell.x}_" +
            $"{currentCorrection.wrongCell.y}";


        foreach (GameObject face
                 in createdFaces)
        {
            if (face == null ||
                face.name != targetName)
            {
                continue;
            }


            Image image =
                face.GetComponent<Image>();


            if (image != null)
            {
                image.color =
                    wrongFaceColor;
            }


            return;
        }
    }


    // ==================================================
    // GHOST / SUGGESTED FACE
    // ==================================================

    private void ShowSuggestedFace()
    {
        if (currentCorrection == null ||
            !currentCorrection.hasSuggestion)
        {
            return;
        }


        suggestedFaceObject =
            CreateFaceVisual(
                currentCorrection
                    .suggestedCell,

                suggestedFaceColor,

                "SuggestedFace"
            );


        /*
         * Put the ghost suggestion above the
         * normal correction faces.
         */
        suggestedFaceObject
            .transform
            .SetAsLastSibling();
    }


    // ==================================================
    // WRONG FACE RECT
    // ==================================================

    public RectTransform GetWrongFaceRect()
    {
        if (currentCorrection == null)
            return null;


        string targetName =
            $"CorrectionFace_" +
            $"{currentCorrection.wrongCell.x}_" +
            $"{currentCorrection.wrongCell.y}";


        foreach (GameObject face
                 in createdFaces)
        {
            if (face != null &&
                face.name ==
                targetName)
            {
                return face.GetComponent<
                    RectTransform>();
            }
        }


        return null;
    }


    // ==================================================
    // SUGGESTED FACE RECT
    // ==================================================

    public RectTransform
        GetSuggestedFaceRect()
    {
        if (suggestedFaceObject ==
            null)
        {
            return null;
        }


        return
            suggestedFaceObject
                .GetComponent<
                    RectTransform>();
    }


    // ==================================================
    // GET CORRECTION DATA
    // ==================================================

    public CubeCorrectionData
        GetCurrentCorrection()
    {
        return currentCorrection;
    }


    // ==================================================
    // CLEAR
    // ==================================================

    public void ClearCorrectionVisuals()
    {
        foreach (GameObject obj
                 in createdFaces)
        {
            if (obj != null)
            {
                Destroy(
                    obj
                );
            }
        }


        createdFaces.Clear();


        if (suggestedFaceObject != null)
        {
            Destroy(
                suggestedFaceObject
            );


            suggestedFaceObject =
                null;
        }


        currentCorrection =
            null;
    }
}
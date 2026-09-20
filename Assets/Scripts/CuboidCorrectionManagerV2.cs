using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CuboidCorrectionManagerV2 : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField] private CuboidDrawManager cuboidDrawManager;
    [SerializeField] private RectTransform correctionDrawingArea;
    [SerializeField] private Text correctionMessageText;


    // ==================================================
    // VISUAL SETTINGS
    // ==================================================

    [Header("Visuals")]
    [SerializeField]
    private Color normalFaceColor =
        new Color(1f, 1f, 1f, 0.85f);

    [SerializeField]
    private Color wrongFaceColor =
        new Color(1f, 0.25f, 0.25f, 0.80f);

    [SerializeField]
    private Color suggestedFaceColor =
        new Color(0.25f, 0.90f, 0.35f, 0.35f);


    // ==================================================
    // RUNTIME
    // ==================================================

    private readonly List<GameObject> createdFaces =
        new List<GameObject>();

    private GameObject suggestedFaceObject;

    private List<CuboidFace> currentFaces;

    private CuboidCorrectionData currentCorrection;


    // ==================================================
    // PREPARE CORRECTION
    // ==================================================

    public void PrepareCorrection()
    {
        ClearCorrectionVisuals();

        if (cuboidDrawManager == null)
        {
            Debug.LogError(
                "CuboidCorrectionManagerV2: " +
                "CuboidDrawManager is not assigned."
            );
            return;
        }

        if (correctionDrawingArea == null)
        {
            Debug.LogError(
                "CuboidCorrectionManagerV2: " +
                "CorrectionDrawingArea is not assigned."
            );
            return;
        }

        IReadOnlyList<CuboidFace> detectedFaces =
            cuboidDrawManager.DetectedFaces;

        if (detectedFaces == null ||
            detectedFaces.Count == 0)
        {
            Debug.LogWarning(
                "No Cuboid faces available for correction."
            );
            return;
        }

        currentFaces =
            new List<CuboidFace>();

        foreach (CuboidFace face in detectedFaces)
        {
            if (face == null)
                continue;

            currentFaces.Add(
                CloneFace(face)
            );
        }

        currentCorrection =
            FindBestCorrection(
                currentFaces
            );

        if (currentCorrection == null)
        {
            currentCorrection =
                BuildFallbackCorrection(
                    currentFaces
                );
        }

        if (currentCorrection == null)
        {
            Debug.LogWarning(
                "Could not create Cuboid correction guidance."
            );
            return;
        }

        DrawStudentNet();

        HighlightWrongFace();

        DrawSuggestedFace();

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                currentCorrection.message1 +
                "\n" +
                currentCorrection.message2;
        }

        Debug.Log(
            "Cuboid correction prepared. " +
            $"Wrong face: {currentCorrection.wrongFace.bottomLeft} " +
            $"[{currentCorrection.wrongFace.gridWidth}x" +
            $"{currentCorrection.wrongFace.gridHeight}] | " +
            $"Suggested: {currentCorrection.suggestedBottomLeft} " +
            $"[{currentCorrection.suggestedWidth}x" +
            $"{currentCorrection.suggestedHeight}] | " +
            $"Has suggestion: {currentCorrection.hasSuggestion}"
        );
    }


    // ==================================================
    // FIND BEST VALID CORRECTION
    // ==================================================

    private CuboidCorrectionData FindBestCorrection(
        List<CuboidFace> faces)
    {
        if (faces == null ||
            faces.Count != 6)
        {
            return null;
        }

        CuboidCorrectionData bestCorrection =
            null;

        float bestScore =
            float.MaxValue;

        foreach (CuboidFace possibleWrongFace
                 in faces)
        {
            List<CuboidFace> remainingFaces =
                BuildRemainingFaceList(
                    faces,
                    possibleWrongFace
                );

            List<Vector2Int> candidateSizes =
                BuildCandidateSizes(
                    remainingFaces,
                    possibleWrongFace
                );

            foreach (Vector2Int size
                     in candidateSizes)
            {
                int width =
                    size.x;

                int height =
                    size.y;

                if (width <= 0 ||
                    height <= 0)
                {
                    continue;
                }

                HashSet<Vector2Int> candidatePositions =
                    BuildCandidatePositions(
                        remainingFaces,
                        width,
                        height
                    );

                /*
                 * Test same position too.
                 * Useful for wrong-size / rotated faces.
                 */
                candidatePositions.Add(
                    possibleWrongFace.bottomLeft
                );

                foreach (Vector2Int candidatePosition
                         in candidatePositions)
                {
                    bool identical =
                        candidatePosition ==
                            possibleWrongFace.bottomLeft
                        &&
                        width ==
                            possibleWrongFace.gridWidth
                        &&
                        height ==
                            possibleWrongFace.gridHeight;

                    if (identical)
                        continue;

                    if (!IsCandidateFreeAgainstRemaining(
                            candidatePosition,
                            width,
                            height,
                            remainingFaces))
                    {
                        continue;
                    }

                    List<CuboidFace> testFaces =
                        CloneFaceList(
                            remainingFaces
                        );

                    testFaces.Add(
                        new CuboidFace(
                            candidatePosition,
                            width,
                            height,
                            possibleWrongFace.faceType
                        )
                    );

                    bool valid =
                        CuboidNetValidator
                            .ValidateCandidateFaces(
                                testFaces,
                                out string validationMessage
                            );

                    if (!valid)
                        continue;

                    float score =
                        CalculateCorrectionScore(
                            possibleWrongFace,
                            candidatePosition,
                            width,
                            height
                        );

                    if (score >= bestScore)
                        continue;

                    bestScore =
                        score;


                    // ----------------------------------
                    // DETERMINE CORRECTION TYPE
                    // ----------------------------------

                    bool sameDirectSize =
                        possibleWrongFace.gridWidth ==
                            width
                        &&
                        possibleWrongFace.gridHeight ==
                            height;

                    bool sameRotatedSize =
                        possibleWrongFace.gridWidth ==
                            height
                        &&
                        possibleWrongFace.gridHeight ==
                            width;

                    bool actualSizeChanged =
                        !sameDirectSize &&
                        !sameRotatedSize;

                    bool orientationChanged =
                        !sameDirectSize &&
                        sameRotatedSize;

                    bool positionChanged =
                        candidatePosition !=
                            possibleWrongFace.bottomLeft;


                    string secondMessage;

                    if (actualSizeChanged)
                    {
                        secondMessage =
                            "Its size does not match the cuboid. " +
                            "Use the green rectangle as the correct size guide.";
                    }
                    else if (orientationChanged &&
                             positionChanged)
                    {
                        secondMessage =
                            "Try turning and moving it to the green area.";
                    }
                    else if (orientationChanged)
                    {
                        secondMessage =
                            "Try turning this rectangle to match the green guide.";
                    }
                    else if (positionChanged)
                    {
                        secondMessage =
                            "Try moving it to the green area.";
                    }
                    else
                    {
                        secondMessage =
                            "Use the green guide to correct this rectangle.";
                    }


                    bestCorrection =
                        new CuboidCorrectionData(
                            possibleWrongFace,
                            candidatePosition,
                            width,
                            height,
                            true,
                            "Look at the red rectangle!",
                            secondMessage
                        );


                    Debug.Log(
                        "VALID CUBOID CORRECTION FOUND: " +
                        $"{possibleWrongFace.bottomLeft} " +
                        $"[{possibleWrongFace.gridWidth}x" +
                        $"{possibleWrongFace.gridHeight}] " +
                        $"-> {candidatePosition} " +
                        $"[{width}x{height}] | " +
                        $"Score = {score:0.00} | " +
                        validationMessage
                    );
                }
            }
        }

        return bestCorrection;
    }


    // ==================================================
    // BUILD REMAINING FACE LIST
    // ==================================================

    private List<CuboidFace> BuildRemainingFaceList(
        List<CuboidFace> source,
        CuboidFace excludedFace)
    {
        List<CuboidFace> result =
            new List<CuboidFace>();

        foreach (CuboidFace face in source)
        {
            if (face == excludedFace)
                continue;

            result.Add(
                CloneFace(face)
            );
        }

        return result;
    }


    // ==================================================
    // BUILD POSSIBLE SIZES
    // ==================================================

    private List<Vector2Int> BuildCandidateSizes(
        List<CuboidFace> remainingFaces,
        CuboidFace originalFace)
    {
        HashSet<int> lengths =
            new HashSet<int>();

        foreach (CuboidFace face
                 in remainingFaces)
        {
            if (face == null)
                continue;

            lengths.Add(
                face.gridWidth
            );

            lengths.Add(
                face.gridHeight
            );
        }

        lengths.Add(
            originalFace.gridWidth
        );

        lengths.Add(
            originalFace.gridHeight
        );

        List<int> orderedLengths =
            new List<int>(lengths);

        orderedLengths.Sort();

        List<Vector2Int> sizes =
            new List<Vector2Int>();

        AddUniqueSize(
            sizes,
            originalFace.gridWidth,
            originalFace.gridHeight
        );

        AddUniqueSize(
            sizes,
            originalFace.gridHeight,
            originalFace.gridWidth
        );

        for (int first = 0;
             first < orderedLengths.Count;
             first++)
        {
            for (int second = first;
                 second < orderedLengths.Count;
                 second++)
            {
                int a =
                    orderedLengths[first];

                int b =
                    orderedLengths[second];

                AddUniqueSize(
                    sizes,
                    a,
                    b
                );

                AddUniqueSize(
                    sizes,
                    b,
                    a
                );
            }
        }

        return sizes;
    }


    private void AddUniqueSize(
        List<Vector2Int> list,
        int width,
        int height)
    {
        if (width <= 0 ||
            height <= 0)
        {
            return;
        }

        Vector2Int candidate =
            new Vector2Int(
                width,
                height
            );

        if (!list.Contains(candidate))
        {
            list.Add(candidate);
        }
    }


    // ==================================================
    // BUILD POSSIBLE POSITIONS
    // ==================================================

    private HashSet<Vector2Int> BuildCandidatePositions(
        List<CuboidFace> remainingFaces,
        int movingWidth,
        int movingHeight)
    {
        HashSet<Vector2Int> result =
            new HashSet<Vector2Int>();

        foreach (CuboidFace anchor
                 in remainingFaces)
        {
            if (anchor == null)
                continue;

            // ------------------------------------------
            // LEFT / RIGHT
            // ------------------------------------------

            if (movingHeight ==
                anchor.gridHeight)
            {
                // Right.
                result.Add(
                    new Vector2Int(
                        anchor.bottomLeft.x +
                            anchor.gridWidth,
                        anchor.bottomLeft.y
                    )
                );

                // Left.
                result.Add(
                    new Vector2Int(
                        anchor.bottomLeft.x -
                            movingWidth,
                        anchor.bottomLeft.y
                    )
                );
            }

            // ------------------------------------------
            // TOP / BOTTOM
            // ------------------------------------------

            if (movingWidth ==
                anchor.gridWidth)
            {
                // Top.
                result.Add(
                    new Vector2Int(
                        anchor.bottomLeft.x,
                        anchor.bottomLeft.y +
                            anchor.gridHeight
                    )
                );

                // Bottom.
                result.Add(
                    new Vector2Int(
                        anchor.bottomLeft.x,
                        anchor.bottomLeft.y -
                            movingHeight
                    )
                );
            }
        }

        return result;
    }


    // ==================================================
    // FREE POSITION CHECK
    // ==================================================

    private bool IsCandidateFreeAgainstRemaining(
        Vector2Int bottomLeft,
        int width,
        int height,
        List<CuboidFace> remainingFaces)
    {
        if (bottomLeft.x < 0 ||
            bottomLeft.y < 0)
        {
            return false;
        }

        if (bottomLeft.x + width >
            cuboidDrawManager.GetGridWidth())
        {
            return false;
        }

        if (bottomLeft.y + height >
            cuboidDrawManager.GetGridHeight())
        {
            return false;
        }

        foreach (CuboidFace face
                 in remainingFaces)
        {
            if (RectanglesOverlap(
                    bottomLeft,
                    width,
                    height,
                    face))
            {
                return false;
            }
        }

        return true;
    }


    // ==================================================
    // OVERLAP TEST
    // ==================================================

    private bool RectanglesOverlap(
        Vector2Int candidateBottomLeft,
        int candidateWidth,
        int candidateHeight,
        CuboidFace existing)
    {
        int candidateLeft =
            candidateBottomLeft.x;

        int candidateRight =
            candidateBottomLeft.x +
            candidateWidth;

        int candidateBottom =
            candidateBottomLeft.y;

        int candidateTop =
            candidateBottomLeft.y +
            candidateHeight;

        int existingLeft =
            existing.bottomLeft.x;

        int existingRight =
            existing.bottomLeft.x +
            existing.gridWidth;

        int existingBottom =
            existing.bottomLeft.y;

        int existingTop =
            existing.bottomLeft.y +
            existing.gridHeight;

        bool separated =
            candidateRight <=
                existingLeft
            ||
            candidateLeft >=
                existingRight
            ||
            candidateTop <=
                existingBottom
            ||
            candidateBottom >=
                existingTop;

        return !separated;
    }


    // ==================================================
    // CORRECTION SCORE
    // ==================================================

    private float CalculateCorrectionScore(
        CuboidFace original,
        Vector2Int candidatePosition,
        int candidateWidth,
        int candidateHeight)
    {
        Vector2 originalCenter =
            original.Center;

        Vector2 candidateCenter =
            new Vector2(
                candidatePosition.x +
                    candidateWidth * 0.5f,
                candidatePosition.y +
                    candidateHeight * 0.5f
            );

        float moveDistance =
            Vector2.Distance(
                originalCenter,
                candidateCenter
            );

        bool sameDirect =
            original.gridWidth ==
                candidateWidth
            &&
            original.gridHeight ==
                candidateHeight;

        bool sameRotated =
            original.gridWidth ==
                candidateHeight
            &&
            original.gridHeight ==
                candidateWidth;

        float changePenalty;

        if (sameDirect)
        {
            changePenalty =
                0f;
        }
        else if (sameRotated)
        {
            changePenalty =
                0.35f;
        }
        else
        {
            changePenalty =
                1.5f
                +
                Mathf.Abs(
                    original.gridWidth -
                    candidateWidth
                )
                +
                Mathf.Abs(
                    original.gridHeight -
                    candidateHeight
                );
        }

        return
            moveDistance +
            changePenalty;
    }


    // ==================================================
    // FALLBACK
    // ==================================================

    private CuboidCorrectionData BuildFallbackCorrection(
        List<CuboidFace> faces)
    {
        if (faces == null ||
            faces.Count == 0)
        {
            return null;
        }

        CuboidFace selectedFace =
            null;

        int minimumNeighbours =
            int.MaxValue;

        foreach (CuboidFace face
                 in faces)
        {
            int count =
                CountNeighbours(
                    face,
                    faces
                );

            if (count <
                minimumNeighbours)
            {
                minimumNeighbours =
                    count;

                selectedFace =
                    face;
            }
        }

        if (selectedFace == null)
            return null;

        return new CuboidCorrectionData(
            selectedFace,
            selectedFace.bottomLeft,
            selectedFace.gridWidth,
            selectedFace.gridHeight,
            false,
            "Look at the red rectangle!",
            "This net needs more than one change. " +
            "Try checking the rectangle sizes and connections."
        );
    }


    // ==================================================
    // NEIGHBOURS
    // ==================================================

    private int CountNeighbours(
        CuboidFace target,
        List<CuboidFace> faces)
    {
        int count =
            0;

        foreach (CuboidFace other
                 in faces)
        {
            if (other == target)
                continue;

            if (AreFacesAdjacent(
                    target,
                    other))
            {
                count++;
            }
        }

        return count;
    }


    private bool AreFacesAdjacent(
        CuboidFace first,
        CuboidFace second)
    {
        int firstLeft =
            first.bottomLeft.x;

        int firstRight =
            first.bottomLeft.x +
            first.gridWidth;

        int firstBottom =
            first.bottomLeft.y;

        int firstTop =
            first.bottomLeft.y +
            first.gridHeight;

        int secondLeft =
            second.bottomLeft.x;

        int secondRight =
            second.bottomLeft.x +
            second.gridWidth;

        int secondBottom =
            second.bottomLeft.y;

        int secondTop =
            second.bottomLeft.y +
            second.gridHeight;


        bool verticalTouch =
            firstRight ==
                secondLeft
            ||
            secondRight ==
                firstLeft;

        if (verticalTouch)
        {
            int overlap =
                Mathf.Min(
                    firstTop,
                    secondTop
                )
                -
                Mathf.Max(
                    firstBottom,
                    secondBottom
                );

            if (overlap > 0)
                return true;
        }


        bool horizontalTouch =
            firstTop ==
                secondBottom
            ||
            secondTop ==
                firstBottom;

        if (horizontalTouch)
        {
            int overlap =
                Mathf.Min(
                    firstRight,
                    secondRight
                )
                -
                Mathf.Max(
                    firstLeft,
                    secondLeft
                );

            if (overlap > 0)
                return true;
        }

        return false;
    }


    // ==================================================
    // DRAW ORIGINAL STUDENT NET
    // ==================================================

    private void DrawStudentNet()
    {
        foreach (CuboidFace face
                 in currentFaces)
        {
            GameObject obj =
                CreateFaceVisual(
                    face.bottomLeft,
                    face.gridWidth,
                    face.gridHeight,
                    normalFaceColor,
                    GetFaceObjectName(face)
                );

            createdFaces.Add(
                obj
            );
        }
    }


    // ==================================================
    // RED WRONG FACE
    // ==================================================

    private void HighlightWrongFace()
    {
        if (currentCorrection == null ||
            currentCorrection.wrongFace == null)
        {
            return;
        }

        string targetName =
            GetFaceObjectName(
                currentCorrection.wrongFace
            );

        foreach (GameObject obj
                 in createdFaces)
        {
            if (obj == null ||
                obj.name != targetName)
            {
                continue;
            }

            Image image =
                obj.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    wrongFaceColor;
            }

            break;
        }
    }


    // ==================================================
    // GREEN GHOST
    // ==================================================

    private void DrawSuggestedFace()
    {
        if (currentCorrection == null ||
            !currentCorrection.hasSuggestion)
        {
            return;
        }

        suggestedFaceObject =
            CreateFaceVisual(
                currentCorrection
                    .suggestedBottomLeft,
                currentCorrection
                    .suggestedWidth,
                currentCorrection
                    .suggestedHeight,
                suggestedFaceColor,
                "CuboidSuggestedGhostFace"
            );

        if (suggestedFaceObject != null)
        {
            suggestedFaceObject
                .transform
                .SetAsFirstSibling();
        }
    }


    // ==================================================
    // CREATE RECTANGLE UI
    // ==================================================

    private GameObject CreateFaceVisual(
        Vector2Int bottomLeft,
        int faceGridWidth,
        int faceGridHeight,
        Color color,
        string objectName)
    {
        float cellWidth =
            correctionDrawingArea.rect.width /
            cuboidDrawManager.GetGridWidth();

        float cellHeight =
            correctionDrawingArea.rect.height /
            cuboidDrawManager.GetGridHeight();

        GameObject faceObject =
            new GameObject(
                objectName,
                typeof(Image)
            );

        faceObject.transform.SetParent(
            correctionDrawingArea,
            false
        );

        Image image =
            faceObject.GetComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;

        RectTransform rect =
            faceObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                faceGridWidth *
                    cellWidth,
                faceGridHeight *
                    cellHeight
            );

        float x =
            bottomLeft.x *
                cellWidth
            -
            correctionDrawingArea.rect.width /
                2f
            +
            rect.sizeDelta.x /
                2f;

        float y =
            bottomLeft.y *
                cellHeight
            -
            correctionDrawingArea.rect.height /
                2f
            +
            rect.sizeDelta.y /
                2f;

        rect.anchoredPosition =
            new Vector2(
                x,
                y
            );

        return faceObject;
    }


    // ==================================================
    // HELPERS
    // ==================================================

    private CuboidFace CloneFace(
        CuboidFace face)
    {
        return new CuboidFace(
            face.bottomLeft,
            face.gridWidth,
            face.gridHeight,
            face.faceType
        );
    }


    private List<CuboidFace> CloneFaceList(
        List<CuboidFace> source)
    {
        List<CuboidFace> result =
            new List<CuboidFace>();

        foreach (CuboidFace face
                 in source)
        {
            result.Add(
                CloneFace(face)
            );
        }

        return result;
    }


    private string GetFaceObjectName(
        CuboidFace face)
    {
        return
            $"CuboidCorrectionFace_" +
            $"{face.bottomLeft.x}_" +
            $"{face.bottomLeft.y}_" +
            $"{face.gridWidth}_" +
            $"{face.gridHeight}";
    }


    // ==================================================
    // PUBLIC ACCESS FOR ANIMATOR
    // ==================================================

    public RectTransform GetWrongFaceRect()
    {
        if (currentCorrection == null ||
            currentCorrection.wrongFace == null)
        {
            return null;
        }

        string targetName =
            GetFaceObjectName(
                currentCorrection.wrongFace
            );

        foreach (GameObject obj
                 in createdFaces)
        {
            if (obj != null &&
                obj.name == targetName)
            {
                return obj.GetComponent<
                    RectTransform>();
            }
        }

        return null;
    }


    public RectTransform GetSuggestedFaceRect()
    {
        if (suggestedFaceObject == null)
            return null;

        return suggestedFaceObject
            .GetComponent<RectTransform>();
    }


    public CuboidCorrectionData GetCurrentCorrection()
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
                Destroy(obj);
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

        currentFaces =
            null;

        currentCorrection =
            null;

        if (correctionMessageText != null)
        {
            correctionMessageText.text =
                "";
        }
    }
}
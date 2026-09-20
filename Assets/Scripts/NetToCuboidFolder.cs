using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetToCuboidFolder : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material faceMaterial;

    [Header("Animation")]
    [SerializeField] private float holdOnCanvasTime = 0.7f;
    [SerializeField] private float foldDuration = 2.5f;
    [SerializeField] private float faceThickness = 0.01f;

    private GameObject currentModel;

    private readonly List<Transform> createdFaces =
        new List<Transform>();

    private int finishedFaceCount;
    private bool interactionEnabled;

    private struct FaceBasis
    {
        public Vector3 right;
        public Vector3 up;
        public Vector3 normal;

        public FaceBasis(
            Vector3 right,
            Vector3 up,
            Vector3 normal)
        {
            this.right = right;
            this.up = up;
            this.normal = normal;
        }
    }

    public void CreateAndFoldFromCanvas(
        IReadOnlyList<CuboidFace> faces,
        RectTransform drawingArea,
        int gridWidth,
        int gridHeight,
        Vector3Int inferredDimensions,
        float distanceFromCamera)
    {
        ClearModel();

        if (faces == null || faces.Count != 6)
        {
            Debug.LogError(
                "NetToCuboidFolder: Exactly 6 faces are required."
            );
            return;
        }

        if (drawingArea == null)
        {
            Debug.LogError(
                "NetToCuboidFolder: Drawing Area is missing."
            );
            return;
        }

        Camera cameraReference = Camera.main;

        if (cameraReference == null)
        {
            Debug.LogError(
                "NetToCuboidFolder: Main Camera was not found."
            );
            return;
        }

        finishedFaceCount = 0;
        createdFaces.Clear();
        interactionEnabled = false;

        currentModel =
            new GameObject("Exact_Canvas_Net_To_3D_Cuboid");

        currentModel.transform.rotation =
            cameraReference.transform.rotation;

        Dictionary<int, FaceBasis> finalBases =
            CalculateFinalCuboidBases(faces);

        if (finalBases.Count != 6)
        {
            Debug.LogError(
                "NetToCuboidFolder: Could not calculate all face orientations."
            );

            ClearModel();
            return;
        }

        float cellCanvasWidth =
            drawingArea.rect.width / gridWidth;

        float cellCanvasHeight =
            drawingArea.rect.height / gridHeight;

        float cellWorldWidth =
            GetWorldWidthFromCanvasCell(
                cameraReference,
                drawingArea,
                cellCanvasWidth,
                distanceFromCamera
            );

        float cellWorldHeight =
            GetWorldHeightFromCanvasCell(
                cameraReference,
                drawingArea,
                cellCanvasHeight,
                distanceFromCamera
            );

        Vector3 cuboidCenterWorld =
            cameraReference.transform.position +
            cameraReference.transform.forward *
            distanceFromCamera;

        for (int faceIndex = 0;
             faceIndex < faces.Count;
             faceIndex++)
        {
            CuboidFace cuboidFace = faces[faceIndex];

            Vector3 startWorldPosition =
                GetCanvasFaceWorldPosition(
                    cameraReference,
                    drawingArea,
                    cuboidFace,
                    cellCanvasWidth,
                    cellCanvasHeight,
                    distanceFromCamera
                );

            float faceWorldWidth =
                cuboidFace.gridWidth * cellWorldWidth;

            float faceWorldHeight =
                cuboidFace.gridHeight * cellWorldHeight;

            GameObject faceObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            faceObject.name =
                $"ConvertedCuboidFace_{faceIndex + 1}";

            createdFaces.Add(faceObject.transform);

            faceObject.transform.position =
                startWorldPosition;

            faceObject.transform.rotation =
                cameraReference.transform.rotation;

            faceObject.transform.localScale =
                new Vector3(
                    faceWorldWidth,
                    faceWorldHeight,
                    faceThickness
                );

            if (faceMaterial != null)
            {
                Renderer faceRenderer =
                    faceObject.GetComponent<Renderer>();

                if (faceRenderer != null)
                    faceRenderer.material = faceMaterial;
            }

            FaceBasis basis = finalBases[faceIndex];

            int missingDimension =
                FindMissingDimension(
                    cuboidFace.gridWidth,
                    cuboidFace.gridHeight,
                    inferredDimensions
                );

            float normalWorldSize =
                GetNormalWorldSize(
                    cuboidFace,
                    missingDimension,
                    cellWorldWidth,
                    cellWorldHeight
                );

            Vector3 localTargetOffset =
                basis.normal * (normalWorldSize * 0.5f);

            Vector3 targetWorldPosition =
                cuboidCenterWorld +
                cameraReference.transform.rotation *
                localTargetOffset;

            Quaternion localTargetRotation =
                Quaternion.LookRotation(
                    basis.normal,
                    basis.up
                );

            Quaternion targetWorldRotation =
                cameraReference.transform.rotation *
                localTargetRotation;

            StartCoroutine(
                AnimateFace(
                    faceObject.transform,
                    startWorldPosition,
                    cameraReference.transform.rotation,
                    targetWorldPosition,
                    targetWorldRotation
                )
            );
        }

        Debug.Log(
            $"Cuboid generation started. Inferred dimensions: " +
            $"{inferredDimensions.x} × " +
            $"{inferredDimensions.y} × " +
            $"{inferredDimensions.z}"
        );
    }

    private Vector3 GetCanvasFaceWorldPosition(
        Camera cameraReference,
        RectTransform drawingArea,
        CuboidFace face,
        float cellWidth,
        float cellHeight,
        float distanceFromCamera)
    {
        float faceCenterX =
            face.bottomLeft.x +
            face.gridWidth * 0.5f;

        float faceCenterY =
            face.bottomLeft.y +
            face.gridHeight * 0.5f;

        float localX =
            faceCenterX * cellWidth -
            drawingArea.rect.width * 0.5f;

        float localY =
            faceCenterY * cellHeight -
            drawingArea.rect.height * 0.5f;

        Vector3 worldUIPosition =
            drawingArea.TransformPoint(
                new Vector3(localX, localY, 0f)
            );

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                null,
                worldUIPosition
            );

        return cameraReference.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceFromCamera
            )
        );
    }

    private float GetWorldWidthFromCanvasCell(
        Camera cameraReference,
        RectTransform drawingArea,
        float cellWidth,
        float distanceFromCamera)
    {
        Vector3 firstCanvasPoint =
            drawingArea.TransformPoint(Vector3.zero);

        Vector3 secondCanvasPoint =
            drawingArea.TransformPoint(
                new Vector3(cellWidth, 0f, 0f)
            );

        Vector2 firstScreenPoint =
            RectTransformUtility.WorldToScreenPoint(
                null,
                firstCanvasPoint
            );

        Vector2 secondScreenPoint =
            RectTransformUtility.WorldToScreenPoint(
                null,
                secondCanvasPoint
            );

        Vector3 firstWorldPoint =
            cameraReference.ScreenToWorldPoint(
                new Vector3(
                    firstScreenPoint.x,
                    firstScreenPoint.y,
                    distanceFromCamera
                )
            );

        Vector3 secondWorldPoint =
            cameraReference.ScreenToWorldPoint(
                new Vector3(
                    secondScreenPoint.x,
                    secondScreenPoint.y,
                    distanceFromCamera
                )
            );

        return Vector3.Distance(
            firstWorldPoint,
            secondWorldPoint
        );
    }

    private float GetWorldHeightFromCanvasCell(
        Camera cameraReference,
        RectTransform drawingArea,
        float cellHeight,
        float distanceFromCamera)
    {
        Vector3 firstCanvasPoint =
            drawingArea.TransformPoint(Vector3.zero);

        Vector3 secondCanvasPoint =
            drawingArea.TransformPoint(
                new Vector3(0f, cellHeight, 0f)
            );

        Vector2 firstScreenPoint =
            RectTransformUtility.WorldToScreenPoint(
                null,
                firstCanvasPoint
            );

        Vector2 secondScreenPoint =
            RectTransformUtility.WorldToScreenPoint(
                null,
                secondCanvasPoint
            );

        Vector3 firstWorldPoint =
            cameraReference.ScreenToWorldPoint(
                new Vector3(
                    firstScreenPoint.x,
                    firstScreenPoint.y,
                    distanceFromCamera
                )
            );

        Vector3 secondWorldPoint =
            cameraReference.ScreenToWorldPoint(
                new Vector3(
                    secondScreenPoint.x,
                    secondScreenPoint.y,
                    distanceFromCamera
                )
            );

        return Vector3.Distance(
            firstWorldPoint,
            secondWorldPoint
        );
    }

    private Dictionary<int, FaceBasis>
        CalculateFinalCuboidBases(
            IReadOnlyList<CuboidFace> faces)
    {
        Dictionary<int, FaceBasis> result =
            new Dictionary<int, FaceBasis>();

        Queue<int> queue = new Queue<int>();

        result[0] = new FaceBasis(
            Vector3.right,
            Vector3.up,
            Vector3.forward
        );

        queue.Enqueue(0);

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();

            FaceBasis currentBasis =
                result[currentIndex];

            for (int neighbourIndex = 0;
                 neighbourIndex < faces.Count;
                 neighbourIndex++)
            {
                if (currentIndex == neighbourIndex ||
                    result.ContainsKey(neighbourIndex))
                {
                    continue;
                }

                if (!TryGetConnectionDirection(
                        faces[currentIndex],
                        faces[neighbourIndex],
                        out Vector2Int direction))
                {
                    continue;
                }

                result[neighbourIndex] =
                    FoldBasis(
                        currentBasis,
                        direction
                    );

                queue.Enqueue(neighbourIndex);
            }
        }

        return result;
    }

    private bool TryGetConnectionDirection(
        CuboidFace current,
        CuboidFace neighbour,
        out Vector2Int direction)
    {
        direction = Vector2Int.zero;

        int currentLeft = current.bottomLeft.x;
        int currentRight =
            current.bottomLeft.x +
            current.gridWidth;

        int currentBottom = current.bottomLeft.y;
        int currentTop =
            current.bottomLeft.y +
            current.gridHeight;

        int neighbourLeft = neighbour.bottomLeft.x;
        int neighbourRight =
            neighbour.bottomLeft.x +
            neighbour.gridWidth;

        int neighbourBottom = neighbour.bottomLeft.y;
        int neighbourTop =
            neighbour.bottomLeft.y +
            neighbour.gridHeight;

        if (currentRight == neighbourLeft &&
            currentBottom == neighbourBottom &&
            currentTop == neighbourTop)
        {
            direction = Vector2Int.right;
            return true;
        }

        if (currentLeft == neighbourRight &&
            currentBottom == neighbourBottom &&
            currentTop == neighbourTop)
        {
            direction = Vector2Int.left;
            return true;
        }

        if (currentTop == neighbourBottom &&
            currentLeft == neighbourLeft &&
            currentRight == neighbourRight)
        {
            direction = Vector2Int.up;
            return true;
        }

        if (currentBottom == neighbourTop &&
            currentLeft == neighbourLeft &&
            currentRight == neighbourRight)
        {
            direction = Vector2Int.down;
            return true;
        }

        return false;
    }

    private FaceBasis FoldBasis(
        FaceBasis basis,
        Vector2Int direction)
    {
        if (direction == Vector2Int.right)
        {
            return new FaceBasis(
                -basis.normal,
                basis.up,
                basis.right
            );
        }

        if (direction == Vector2Int.left)
        {
            return new FaceBasis(
                basis.normal,
                basis.up,
                -basis.right
            );
        }

        if (direction == Vector2Int.up)
        {
            return new FaceBasis(
                basis.right,
                -basis.normal,
                basis.up
            );
        }

        if (direction == Vector2Int.down)
        {
            return new FaceBasis(
                basis.right,
                basis.normal,
                -basis.up
            );
        }

        return basis;
    }

    private int FindMissingDimension(
        int faceWidth,
        int faceHeight,
        Vector3Int dimensions)
    {
        List<int> dimensionValues =
            new List<int>
            {
                dimensions.x,
                dimensions.y,
                dimensions.z
            };

        if (!RemoveFirstMatchingValue(
                dimensionValues,
                faceWidth))
        {
            Debug.LogWarning(
                $"Could not match face width {faceWidth}."
            );
        }

        if (!RemoveFirstMatchingValue(
                dimensionValues,
                faceHeight))
        {
            Debug.LogWarning(
                $"Could not match face height {faceHeight}."
            );
        }

        if (dimensionValues.Count > 0)
            return dimensionValues[0];

        return 1;
    }

    private bool RemoveFirstMatchingValue(
        List<int> values,
        int targetValue)
    {
        for (int index = 0;
             index < values.Count;
             index++)
        {
            if (values[index] != targetValue)
                continue;

            values.RemoveAt(index);
            return true;
        }

        return false;
    }

    private float GetNormalWorldSize(
        CuboidFace face,
        int missingDimension,
        float cellWorldWidth,
        float cellWorldHeight)
    {
        float averageCellWorldSize =
            (cellWorldWidth + cellWorldHeight) * 0.5f;

        return missingDimension *
               averageCellWorldSize;
    }

    private IEnumerator AnimateFace(
        Transform face,
        Vector3 startPosition,
        Quaternion startRotation,
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        yield return new WaitForSeconds(
            holdOnCanvasTime
        );

        float timer = 0f;

        while (timer < foldDuration)
        {
            if (face == null)
                yield break;

            timer += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    timer / foldDuration
                );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            face.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothTime
                );

            face.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothTime
                );

            yield return null;
        }

        if (face != null)
        {
            face.position = targetPosition;
            face.rotation = targetRotation;
        }

        finishedFaceCount++;

        if (finishedFaceCount >= 6 &&
            !interactionEnabled)
        {
            interactionEnabled = true;
            SetupFinalPivotAndInteraction();
        }
    }

    private void SetupFinalPivotAndInteraction()
    {
        if (currentModel == null ||
            createdFaces.Count == 0)
        {
            return;
        }

        Vector3 center = Vector3.zero;
        int validFaceCount = 0;

        foreach (Transform face in createdFaces)
        {
            if (face == null)
                continue;

            center += face.position;
            validFaceCount++;
        }

        if (validFaceCount == 0)
            return;

        center /= validFaceCount;

        currentModel.transform.position = center;

        foreach (Transform face in createdFaces)
        {
            if (face != null)
            {
                face.SetParent(
                    currentModel.transform,
                    true
                );
            }
        }

        TouchRotateZoom rotateZoom =
            currentModel.GetComponent<TouchRotateZoom>();

        if (rotateZoom == null)
        {
            rotateZoom =
                currentModel.AddComponent<TouchRotateZoom>();
        }

        rotateZoom.target =
            currentModel.transform;

        Debug.Log(
            "Cuboid folding completed and interaction enabled."
        );
    }

    public void ClearModel()
    {
        StopAllCoroutines();

        finishedFaceCount = 0;
        interactionEnabled = false;

        createdFaces.Clear();

        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
    }

    public Transform GetCurrentModelRoot()
    {
        if (currentModel == null)
            return null;

        return currentModel.transform;
    }
}
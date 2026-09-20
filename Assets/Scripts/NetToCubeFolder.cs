using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetToCubeFolder : MonoBehaviour
{
    [Header("Materials")]
    public Material faceMaterial;

    [Header("Animation")]
    public float holdOnCanvasTime = 0.7f;
    public float foldDuration = 2.5f;
    public float faceThickness = 0.01f;

    private GameObject currentModel;
    private int finishedFaceCount = 0;
    private List<Transform> createdFaces = new List<Transform>();
    private bool interactionEnabled = false;

    private struct FaceBasis
    {
        public Vector3 right;
        public Vector3 up;
        public Vector3 normal;

        public FaceBasis(Vector3 right, Vector3 up, Vector3 normal)
        {
            this.right = right;
            this.up = up;
            this.normal = normal;
        }
    }

    
    public void CreateAndFoldFromCanvas(
        HashSet<Vector2Int> cells,
        RectTransform drawingArea,
        int gridWidth,
        int gridHeight,
        float distanceFromCamera
    )
    {
        ClearModel();

        if (cells == null || cells.Count != 6)
        {
            Debug.LogError("Cannot create model. Face count is not 6.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found.");
            return;
        }

        finishedFaceCount = 0;
        createdFaces.Clear();
        interactionEnabled = false;

        currentModel = new GameObject("Exact_Canvas_Net_To_3D_Cube");

        Dictionary<Vector2Int, FaceBasis> finalBases = CalculateFinalCubeBases(cells);

        float cellWidth = drawingArea.rect.width / gridWidth;
        float cellHeight = drawingArea.rect.height / gridHeight;

        
        float faceWorldSize = GetWorldSizeFromCanvasCell(cam, drawingArea, cellWidth, distanceFromCamera);

        Vector3 cubeCenterWorld = cam.transform.position + cam.transform.forward * distanceFromCamera;

        foreach (Vector2Int cell in cells)
        {
            
            Vector3 startWorldPos = GetCanvasCellWorldPosition(
                cam,
                drawingArea,
                cell,
                cellWidth,
                cellHeight,
                distanceFromCamera
            );

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "ConvertedFace_" + cell.x + "_" + cell.y;
            createdFaces.Add(face.transform);

            face.transform.position = startWorldPos;
            face.transform.rotation = cam.transform.rotation;
            face.transform.localScale = new Vector3(faceWorldSize, faceWorldSize, faceThickness);

            if (faceMaterial != null)
                face.GetComponent<Renderer>().material = faceMaterial;

            FaceBasis basis = finalBases[cell];

            
            Vector3 targetWorldPos =
                cubeCenterWorld +
                currentModel.transform.rotation * (basis.normal * (faceWorldSize / 2f));

            Quaternion targetWorldRot =
                Quaternion.LookRotation(
                    currentModel.transform.rotation * basis.normal,
                    currentModel.transform.rotation * basis.up
                );

            StartCoroutine(AnimateFace(
                face.transform,
                startWorldPos,
                cam.transform.rotation,
                targetWorldPos,
                targetWorldRot
            ));
        }

        Debug.Log("Canvas net converted with exact sizing.");
    }

    private Vector3 GetCanvasCellWorldPosition(
        Camera cam,
        RectTransform drawingArea,
        Vector2Int cell,
        float cellWidth,
        float cellHeight,
        float distanceFromCamera
    )
    {
        float localX = (cell.x + 0.5f) * cellWidth - drawingArea.rect.width / 2f;
        float localY = (cell.y + 0.5f) * cellHeight - drawingArea.rect.height / 2f;

        Vector3 worldUIPos = drawingArea.TransformPoint(new Vector3(localX, localY, 0f));
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldUIPos);

        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));
    }

    private float GetWorldSizeFromCanvasCell(
        Camera cam,
        RectTransform drawingArea,
        float cellWidth,
        float distanceFromCamera
    )
    {
        Vector3 p1 = drawingArea.TransformPoint(new Vector3(0f, 0f, 0f));
        Vector3 p2 = drawingArea.TransformPoint(new Vector3(cellWidth, 0f, 0f));

        Vector2 s1 = RectTransformUtility.WorldToScreenPoint(null, p1);
        Vector2 s2 = RectTransformUtility.WorldToScreenPoint(null, p2);

        Vector3 w1 = cam.ScreenToWorldPoint(new Vector3(s1.x, s1.y, distanceFromCamera));
        Vector3 w2 = cam.ScreenToWorldPoint(new Vector3(s2.x, s2.y, distanceFromCamera));

        return Vector3.Distance(w1, w2);
    }

    private IEnumerator AnimateFace(
        Transform face,
        Vector3 startPos,
        Quaternion startRot,
        Vector3 targetPos,
        Quaternion targetRot
    )
    {
        yield return new WaitForSeconds(holdOnCanvasTime);

        float timer = 0f;
        while (timer < foldDuration)
        {
            if (face == null) yield break;

            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / foldDuration));

            face.position = Vector3.Lerp(startPos, targetPos, t);
            face.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        if (face != null)
        {
            face.position = targetPos;
            face.rotation = targetRot;
        }

        finishedFaceCount++;
        if (finishedFaceCount >= 6 && !interactionEnabled)
        {
            interactionEnabled = true;
            SetupFinalPivotAndInteraction();
        }
    }

    private void SetupFinalPivotAndInteraction()
    {
        if (currentModel == null || createdFaces.Count == 0) return;

        Vector3 center = Vector3.zero;
        int validCount = 0;
        foreach (Transform face in createdFaces)
        {
            if (face != null)
            {
                center += face.position;
                validCount++;
            }
        }

        if (validCount == 0) return;
        center /= validCount;

        currentModel.transform.position = center;
        foreach (Transform face in createdFaces)
        {
            if (face != null) face.SetParent(currentModel.transform, true);
        }

        
        if (!currentModel.TryGetComponent<TouchRotateZoom>(out var rotateZoom))
            rotateZoom = currentModel.AddComponent<TouchRotateZoom>();

        rotateZoom.target = currentModel.transform;
    }

    private Dictionary<Vector2Int, FaceBasis> CalculateFinalCubeBases(HashSet<Vector2Int> cells)
    {
        Dictionary<Vector2Int, FaceBasis> result = new Dictionary<Vector2Int, FaceBasis>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        Vector2Int start = Vector2Int.zero;
        foreach (Vector2Int c in cells) { start = c; break; }

        result[start] = new FaceBasis(Vector3.right, Vector3.up, Vector3.forward);
        queue.Enqueue(start);

        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            FaceBasis currentBasis = result[current];

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;
                if (cells.Contains(next) && !result.ContainsKey(next))
                {
                    result[next] = FoldBasis(currentBasis, dir);
                    queue.Enqueue(next);
                }
            }
        }
        return result;
    }

    private FaceBasis FoldBasis(FaceBasis b, Vector2Int dir)
    {
        if (dir == Vector2Int.right) return new FaceBasis(-b.normal, b.up, b.right);
        if (dir == Vector2Int.left) return new FaceBasis(b.normal, b.up, -b.right);
        if (dir == Vector2Int.up) return new FaceBasis(b.right, -b.normal, b.up);
        if (dir == Vector2Int.down) return new FaceBasis(b.right, b.normal, -b.up);
        return b;
    }

    public void ClearModel()
    {
        StopAllCoroutines();
        finishedFaceCount = 0;
        createdFaces.Clear();
        interactionEnabled = false;
        if (currentModel != null) Destroy(currentModel);
    }

    // --- Getter Addition ---


    public Transform GetCurrentModelRoot()
    {
        if (currentModel == null)
            return null;

        return currentModel.transform;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetToCylinderFolder : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField]
    private Material cylinderMaterial;

    [Header("Animation")]
    [SerializeField]
    private float flatHoldTime = 1.0f;

    [SerializeField]
    private float wrapDuration = 2.5f;

    [SerializeField]
    private float capFoldDuration = 1.2f;

    [SerializeField]
    private float delayBetweenStages = 0.25f;

    [Header("Geometry Normalization")]
    [SerializeField]
    private float normalizationDuration = 0.8f;

    [SerializeField]
    private float normalizationHoldTime = 0.3f;

    [Header("3D Settings")]
    [SerializeField]
    private int cylinderSegments = 48;

    [Header("Preview")]
    [SerializeField]
    private float previewTiltX = -15f;

    [SerializeField]
    private float previewTurnY = 25f;

    private GameObject currentModel;

    private GameObject wallObject;
    private GameObject topCircleObject;
    private GameObject bottomCircleObject;

    private Mesh wallMesh;

    private float radiusWorld;
    private float heightWorld;

    private float sourceWallWidth;
    private float sourceWallHeight;

    private float sourceTopCircleRadius;
    private float sourceBottomCircleRadius;

    // --- New Fields ---
    private RectTransform activePuzzleBoard;

    private CylinderGeneratedFace sourceRectangle;
    private CylinderGeneratedFace sourceTopCircle;
    private CylinderGeneratedFace sourceBottomCircle;

    // ==================================================
    // MAIN ENTRY
    // ==================================================

    public void CreateCylinder(
        CylinderGuidedConfig config,
        List<CylinderGeneratedFace> sourceFaces,
        RectTransform puzzleBoard,
        float distanceFromCamera)
    {
        ClearModel();

        if (config == null ||
            !config.IsReady)
        {
            Debug.LogError(
                "Cannot create cylinder. Configuration is invalid."
            );

            return;
        }

        if (sourceFaces == null ||
            sourceFaces.Count != 3)
        {
            Debug.LogError(
                "Cylinder requires exactly 3 source faces."
            );

            return;
        }

        if (puzzleBoard == null)
        {
            Debug.LogError(
                "Cylinder PuzzleBoard is missing."
            );

            return;
        }

        Camera cam =
            Camera.main;

        if (cam == null)
        {
            Debug.LogError(
                "Main Camera not found."
            );

            return;
        }

        activePuzzleBoard =
            puzzleBoard;

        sourceRectangle = null;

        List<CylinderGeneratedFace> circles =
            new List<CylinderGeneratedFace>();

        foreach (CylinderGeneratedFace face
                 in sourceFaces)
        {
            if (face == null)
                continue;

            if (face.IsRectangle)
            {
                sourceRectangle =
                    face;
            }

            if (face.IsCircle)
            {
                circles.Add(
                    face
                );
            }
        }

        if (sourceRectangle == null ||
            circles.Count != 2)
        {
            Debug.LogError(
                "Could not identify cylinder source faces."
            );

            return;
        }

        /*
         * Determine which actual student circle
         * is above and which is below Rectangle.
         */
        float rectangleY =
            GetBoardLocalCenter(
                sourceRectangle
            ).y;

        float circle0Y = GetBoardLocalCenter(circles[0]).y;
        float circle1Y = GetBoardLocalCenter(circles[1]).y;

        if (circle0Y > circle1Y)
        {
            sourceTopCircle = circles[0];
            sourceBottomCircle = circles[1];
        }
        else
        {
            sourceTopCircle = circles[1];
            sourceBottomCircle = circles[0];
        }

        // Exact visual size from student's UI net
        Vector2 rectangleWorldSize =
            GetUIFaceWorldSize(
                sourceRectangle,
                cam,
                distanceFromCamera
            );

        Vector2 topCircleWorldSize =
            GetUIFaceWorldSize(
                sourceTopCircle,
                cam,
                distanceFromCamera
            );

        Vector2 bottomCircleWorldSize =
            GetUIFaceWorldSize(
                sourceBottomCircle,
                cam,
                distanceFromCamera
            );

        sourceWallWidth =
            rectangleWorldSize.x;

        sourceWallHeight =
            rectangleWorldSize.y;

        sourceTopCircleRadius =
            topCircleWorldSize.x * 0.5f;

        sourceBottomCircleRadius =
            bottomCircleWorldSize.x * 0.5f;

        /*
         * Final mathematically correct geometry.
         *
         * We use the student's visible circle size
         * as the final cylinder radius.
         */
        radiusWorld =
            (
                sourceTopCircleRadius +
                sourceBottomCircleRadius
            ) * 0.5f;

        heightWorld =
            sourceWallHeight;

        Debug.Log(
            $"Cylinder source capture: " +
            $"Wall = {sourceWallWidth:F3} x {sourceWallHeight:F3}, " +
            $"Circle radius = {radiusWorld:F3}, " +
            $"Required circumference = " +
            $"{2f * Mathf.PI * radiusWorld:F3}"
        );

        currentModel =
            new GameObject(
                "Guided_Cylinder_3D"
            );

        currentModel.transform.position =
            cam.transform.position +
            cam.transform.forward *
            distanceFromCamera;

        currentModel.transform.rotation =
            cam.transform.rotation;

        // Use the new student-based net generator
        CreateFlatCylinderNetFromStudent(
            cam,
            distanceFromCamera
        );

        StartCoroutine(
            FoldingSequence()
        );
    }

    // ==================================================
    // CREATE FLAT NET FROM STUDENT
    // ==================================================

    private void CreateFlatCylinderNetFromStudent(
        Camera cam,
        float distanceFromCamera)
    {
        CreateFlatWall();

        CreateStudentCircle(
            sourceTopCircle,
            true,
            cam,
            distanceFromCamera
        );

        CreateStudentCircle(
            sourceBottomCircle,
            false,
            cam,
            distanceFromCamera
        );

        Debug.Log(
            "Exact student cylinder net recreated in 3D."
        );
    }

    // ==================================================
    // CREATE FLAT NET (Fallback/Original)
    // ==================================================

    private void CreateFlatCylinderNet()
    {
        CreateFlatWall();
        CreateFlatCircle(
            true
        );

        CreateFlatCircle(
            false
        );

        Debug.Log(
            "Flat cylinder net created."
        );
    }

    // ==================================================
    // FLAT WALL
    // ==================================================

    private void CreateFlatWall()
    {
        wallObject =
            new GameObject(
                "Cylinder_Wall"
            );

        wallObject.transform.SetParent(
            currentModel.transform,
            false
        );

        MeshFilter filter =
            wallObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            wallObject.AddComponent<MeshRenderer>();

        MeshCollider wallCollider =
            wallObject.AddComponent<MeshCollider>();

        if (cylinderMaterial != null)
        {
            renderer.material =
                cylinderMaterial;
        }

        wallMesh =
            new Mesh();

        filter.mesh =
            wallMesh;

        BuildFlatWallMesh(
            sourceWallWidth
        );
    }

    // ==================================================
    // FLAT WALL MESH
    // ==================================================

    private void BuildFlatWallMesh(
        float wallWidth)
    {
        int segmentCount =
            Mathf.Max(
                8,
                cylinderSegments
            );

        int vertexCount =
            (segmentCount + 1) * 2;

        Vector3[] vertices =
            new Vector3[vertexCount];

        Vector2[] uvs =
            new Vector2[vertexCount];

        int[] triangles =
            new int[segmentCount * 6];

        for (int i = 0;
             i <= segmentCount;
             i++)
        {
            float t =
                (float)i /
                segmentCount;

            float x =
                (
                    t - 0.5f
                ) *
                wallWidth;

            int bottomIndex =
                i * 2;

            int topIndex =
                bottomIndex + 1;

            vertices[bottomIndex] =
                new Vector3(
                    x,
                    -heightWorld * 0.5f,
                    0f
                );

            vertices[topIndex] =
                new Vector3(
                    x,
                    heightWorld * 0.5f,
                    0f
                );

            uvs[bottomIndex] =
                new Vector2(
                    t,
                    0f
                );

            uvs[topIndex] =
                new Vector2(
                    t,
                    1f
                );
        }

        int triangleIndex = 0;

        for (int i = 0;
             i < segmentCount;
             i++)
        {
            int bottomLeft =
                i * 2;

            int topLeft =
                bottomLeft + 1;

            int bottomRight =
                bottomLeft + 2;

            int topRight =
                bottomLeft + 3;

            triangles[triangleIndex++] =
                bottomLeft;

            triangles[triangleIndex++] =
                topLeft;

            triangles[triangleIndex++] =
                topRight;

            triangles[triangleIndex++] =
                bottomLeft;

            triangles[triangleIndex++] =
                topRight;

            triangles[triangleIndex++] =
                bottomRight;
        }

        wallMesh.Clear();

        wallMesh.vertices =
            vertices;

        wallMesh.uv =
            uvs;

        wallMesh.triangles =
            triangles;

        wallMesh.RecalculateNormals();
        wallMesh.RecalculateBounds();
    }


    // ==================================================
    // WALL MESH
    // ==================================================

    private void BuildWallMesh(
        float wrapAmount)
    {
        int segmentCount =
            Mathf.Max(
                8,
                cylinderSegments
            );

        int vertexCount =
            (segmentCount + 1) * 2;

        Vector3[] vertices =
            new Vector3[
                vertexCount
            ];

        Vector2[] uvs =
            new Vector2[
                vertexCount
            ];

        int[] triangles =
            new int[
                segmentCount * 6
            ];

        float circumference =
            2f *
            Mathf.PI *
            radiusWorld;

        for (int i = 0;
             i <= segmentCount;
             i++)
        {
            float t =
                (float)i /
                segmentCount;

            float flatX =
                (
                    t - 0.5f
                ) *
                circumference;

            float angle =
                (
                    t - 0.5f
                ) *
                Mathf.PI *
                2f;

            float curvedX =
                Mathf.Sin(
                    angle
                ) *
                radiusWorld;

            float curvedZ =
                Mathf.Cos(
                    angle
                ) *
                radiusWorld -
                radiusWorld;

            float x =
                Mathf.Lerp(
                    flatX,
                    curvedX,
                    wrapAmount
                );

            float z =
                Mathf.Lerp(
                    0f,
                    curvedZ,
                    wrapAmount
                );

            int bottomIndex =
                i * 2;

            int topIndex =
                bottomIndex + 1;

            vertices[bottomIndex] =
                new Vector3(
                    x,
                    -heightWorld * 0.5f,
                    z
                );

            vertices[topIndex] =
                new Vector3(
                    x,
                    heightWorld * 0.5f,
                    z
                );

            uvs[bottomIndex] =
                new Vector2(
                    t,
                    0f
                );

            uvs[topIndex] =
                new Vector2(
                    t,
                    1f
                );
        }

        int triangleIndex = 0;

        for (int i = 0;
             i < segmentCount;
             i++)
        {
            int bottomLeft =
                i * 2;

            int topLeft =
                bottomLeft + 1;

            int bottomRight =
                bottomLeft + 2;

            int topRight =
                bottomLeft + 3;

            triangles[triangleIndex++] =
                bottomLeft;

            triangles[triangleIndex++] =
                topLeft;

            triangles[triangleIndex++] =
                topRight;

            triangles[triangleIndex++] =
                bottomLeft;

            triangles[triangleIndex++] =
                topRight;

            triangles[triangleIndex++] =
                bottomRight;
        }

        wallMesh.Clear();

        wallMesh.vertices =
            vertices;

        wallMesh.uv =
            uvs;

        wallMesh.triangles =
            triangles;

        wallMesh.RecalculateNormals();
        wallMesh.RecalculateBounds();
    }

    // ==================================================
    // STUDENT CIRCLE CREATION (UPDATED)
    // ==================================================

    private void CreateStudentCircle(
        CylinderGeneratedFace sourceFace,
        bool top,
        Camera cam,
        float distanceFromCamera)
    {
        if (sourceFace == null)
            return;

        GameObject circleObject =
            new GameObject(
                top
                    ? "Cylinder_Top_Cap"
                    : "Cylinder_Bottom_Cap"
            );

        MeshFilter filter =
            circleObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            circleObject.AddComponent<MeshRenderer>();

        MeshCollider circleCollider =
            circleObject.AddComponent<MeshCollider>();

        if (cylinderMaterial != null)
        {
            renderer.material =
                cylinderMaterial;
        }

        Mesh circleMesh =
            CreateCircleMesh();

        filter.mesh =
            circleMesh;

        circleCollider.sharedMesh =
            circleMesh;

        // -----------------------------------------
        // DISPLAY SIZE FROM STUDENT UI
        // -----------------------------------------

        Vector2 displaySize =
            GetUIFaceWorldSize(
                sourceFace,
                cam,
                distanceFromCamera
            );

        float requiredRadius =
            displaySize.x * 0.5f;

        float baseRadius =
            Mathf.Max(
                0.0001f,
                radiusWorld
            );

        float scaleFactor =
            requiredRadius /
            baseRadius;

        // -----------------------------------------
        // ATTACH TO MODEL ROOT
        // -----------------------------------------

        circleObject.transform.SetParent(
            currentModel.transform,
            false
        );

        circleObject.transform.localScale =
            Vector3.one *
            scaleFactor;

        // -----------------------------------------
        // EXACT FLAT-NET POSITION
        // -----------------------------------------

        float circleCenterY;

        if (top)
        {
            circleCenterY =
                heightWorld * 0.5f +
                requiredRadius;
        }
        else
        {
            circleCenterY =
                -heightWorld * 0.5f -
                requiredRadius;
        }

        circleObject.transform.localPosition =
            new Vector3(
                0f,
                circleCenterY,
                0f
            );

        circleObject.transform.localRotation =
            Quaternion.identity;

        // -----------------------------------------
        // STORE REFERENCE
        // -----------------------------------------

        if (top)
        {
            topCircleObject =
                circleObject;
        }
        else
        {
            bottomCircleObject =
                circleObject;
        }
    }

    // ==================================================
    // CIRCLE CREATION (Original)
    // ==================================================

    private void CreateFlatCircle(
        bool top)
    {
        GameObject circleObject =
            new GameObject(
                top
                    ? "Cylinder_Top_Cap"
                    : "Cylinder_Bottom_Cap"
            );

        circleObject.transform.SetParent(
            currentModel.transform,
            false
        );

        MeshFilter filter =
            circleObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            circleObject.AddComponent<MeshRenderer>();

        MeshCollider circleCollider =
            circleObject.AddComponent<MeshCollider>();

        if (cylinderMaterial != null)
        {
            renderer.material =
                cylinderMaterial;
        }

        Mesh circleMesh =
            CreateCircleMesh();

        filter.mesh =
            circleMesh;

        circleCollider.sharedMesh =
            circleMesh;

        float y =
            top
                ? heightWorld * 0.5f +
                  radiusWorld
                : -heightWorld * 0.5f -
                  radiusWorld;

        circleObject.transform.localPosition =
            new Vector3(
                0f,
                y,
                0f
            );

        circleObject.transform.localRotation =
            Quaternion.identity;

        if (top)
        {
            topCircleObject =
                circleObject;
        }
        else
        {
            bottomCircleObject =
                circleObject;
        }
    }

    // ==================================================
    // UPDATED DOUBLE-SIDED CIRCLE MESH
    // ==================================================
    private Mesh CreateCircleMesh()
    {
        int segmentCount =
            Mathf.Max(
                16,
                cylinderSegments
            );

        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
            new Vector3[
                segmentCount + 1
            ];

        Vector2[] uvs =
            new Vector2[
                segmentCount + 1
            ];

        /*
         * Double sided:
         *
         * Front  = segmentCount * 3
         * Back   = segmentCount * 3
         */
        int[] triangles =
            new int[
                segmentCount * 6
            ];

        // -----------------------------------------
        // CENTER
        // -----------------------------------------

        vertices[0] =
            Vector3.zero;

        uvs[0] =
            new Vector2(
                0.5f,
                0.5f
            );

        // -----------------------------------------
        // OUTER POINTS
        // -----------------------------------------

        for (int i = 0;
             i < segmentCount;
             i++)
        {
            float angle =
                Mathf.PI *
                2f *
                i /
                segmentCount;

            float x =
                Mathf.Cos(angle) *
                radiusWorld;

            float y =
                Mathf.Sin(angle) *
                radiusWorld;

            vertices[i + 1] =
                new Vector3(
                    x,
                    y,
                    0f
                );

            uvs[i + 1] =
                new Vector2(
                    0.5f +
                    Mathf.Cos(angle) *
                    0.5f,

                    0.5f +
                    Mathf.Sin(angle) *
                    0.5f
                );
        }

        // -----------------------------------------
        // TRIANGLES (FRONT & BACK FACES)
        // -----------------------------------------

        for (int i = 0; i < segmentCount; i++)
        {
            int next = (i + 1) % segmentCount;

            // Front face (Clockwise)
            triangles[i * 6] = 0;
            triangles[i * 6 + 1] = i + 1;
            triangles[i * 6 + 2] = next + 1;

            // Back face (Counter-Clockwise)
            triangles[i * 6 + 3] = 0;
            triangles[i * 6 + 4] = next + 1;
            triangles[i * 6 + 5] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // ==================================================
    // UI TO WORLD HELPERS
    // ==================================================

    private Vector3 GetUIFaceWorldPosition(
        CylinderGeneratedFace sourceFace,
        Camera cam,
        float distanceFromCamera)
    {
        RectTransform rect =
            sourceFace.GetComponent<
                RectTransform>();

        if (rect == null)
        {
            return
                cam.transform.position +
                cam.transform.forward *
                distanceFromCamera;
        }

        Vector3 worldCenter =
            rect.TransformPoint(
                rect.rect.center
            );

        Vector2 screenPoint =
            RectTransformUtility
                .WorldToScreenPoint(
                    null,
                    worldCenter
                );

        return cam.ScreenToWorldPoint(
            new Vector3(
                screenPoint.x,
                screenPoint.y,
                distanceFromCamera
            )
        );
    }

    private Quaternion GetUIFaceWorldRotation(
        CylinderGeneratedFace sourceFace,
        Camera cam)
    {
        RectTransform rect =
            sourceFace.GetComponent<
                RectTransform>();

        if (rect == null)
            return cam.transform.rotation;

        float zRotation =
            rect.eulerAngles.z;

        return
            Quaternion.AngleAxis(
                zRotation,
                cam.transform.forward
            )
            *
            cam.transform.rotation;
    }

    private Vector2 GetUIFaceWorldSize(
        CylinderGeneratedFace sourceFace,
        Camera cam,
        float distanceFromCamera)
    {
        RectTransform rect =
            sourceFace.GetComponent<
                RectTransform>();

        if (rect == null)
            return Vector2.zero;

        Vector3 left =
            rect.TransformPoint(
                new Vector3(
                    rect.rect.xMin,
                    0f,
                    0f
                )
            );

        Vector3 right =
            rect.TransformPoint(
                new Vector3(
                    rect.rect.xMax,
                    0f,
                    0f
                )
            );

        Vector3 bottom =
            rect.TransformPoint(
                new Vector3(
                    0f,
                    rect.rect.yMin,
                    0f
                )
            );

        Vector3 top =
            rect.TransformPoint(
                new Vector3(
                    0f,
                    rect.rect.yMax,
                    0f
                )
            );

        Vector2 screenLeft =
            RectTransformUtility.WorldToScreenPoint(
                null,
                left
            );

        Vector2 screenRight =
            RectTransformUtility.WorldToScreenPoint(
                null,
                right
            );

        Vector2 screenBottom =
            RectTransformUtility.WorldToScreenPoint(
                null,
                bottom
            );

        Vector2 screenTop =
            RectTransformUtility.WorldToScreenPoint(
                null,
                top
            );

        Vector3 worldLeft =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenLeft.x,
                    screenLeft.y,
                    distanceFromCamera
                )
            );

        Vector3 worldRight =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenRight.x,
                    screenRight.y,
                    distanceFromCamera
                )
            );

        Vector3 worldBottom =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenBottom.x,
                    screenBottom.y,
                    distanceFromCamera
                )
            );

        Vector3 worldTop =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenTop.x,
                    screenTop.y,
                    distanceFromCamera
                )
            );

        return new Vector2(
            Vector3.Distance(worldLeft, worldRight),
            Vector3.Distance(worldBottom, worldTop)
        );
    }

    private Vector2 GetBoardLocalCenter(
        CylinderGeneratedFace face)
    {
        RectTransform rect =
            face.GetComponent<
                RectTransform>();

        if (rect == null ||
            activePuzzleBoard == null)
        {
            return Vector2.zero;
        }

        Vector3 worldCenter =
            rect.TransformPoint(
                rect.rect.center
            );

        Vector3 local =
            activePuzzleBoard
                .InverseTransformPoint(
                    worldCenter
                );

        return new Vector2(
            local.x,
            local.y
        );
    }

    // ==================================================
    // FULL FOLDING SEQUENCE
    // ==================================================

    private IEnumerator FoldingSequence()
    {
        /*
         * STEP 1:
         * Student's exact flat net stays visible.
         */
        yield return
            new WaitForSeconds(
                flatHoldTime
            );

        /*
         * STEP 2:
         * Smoothly normalize screen-friendly
         * display geometry into mathematically
         * foldable cylinder geometry.
         */
        Debug.Log(
            "Cylinder net normalization started."
        );

        yield return
            StartCoroutine(
                NormalizeFlatNet()
            );

        yield return
            new WaitForSeconds(
                normalizationHoldTime
            );

        /*
         * STEP 3:
         * Wrap the normalized rectangle.
         */
        Debug.Log(
            "Cylinder wall wrapping started."
        );

        yield return
            StartCoroutine(
                WrapWall()
            );

        Debug.Log(
            "Cylinder wall wrapping completed."
        );

        yield return
            new WaitForSeconds(
                delayBetweenStages
            );

        Debug.Log(
            "Top cylinder cap folding started."
        );

        yield return
            StartCoroutine(
                FoldCap(
                    topCircleObject,
                    true
                )
            );

        yield return
            new WaitForSeconds(
                delayBetweenStages
            );

        Debug.Log(
            "Bottom cylinder cap folding started."
        );

        yield return
            StartCoroutine(
                FoldCap(
                    bottomCircleObject,
                    false
                )
            );

        Debug.Log(
            "Cylinder folding completed."
        );

        SetupFinalModel();
    }

    // ==================================================
    // NORMALIZE FLAT NET
    // ==================================================

    private IEnumerator NormalizeFlatNet()
    {
        float finalCircumference =
            2f *
            Mathf.PI *
            radiusWorld;

        Vector3 topStartScale =
            topCircleObject != null
                ? topCircleObject.transform.localScale
                : Vector3.one;

        Vector3 bottomStartScale =
            bottomCircleObject != null
                ? bottomCircleObject.transform.localScale
                : Vector3.one;

        /*
         * Final circle scale is one because
         * CreateCircleMesh() already uses radiusWorld.
         */
        Vector3 finalCircleScale =
            Vector3.one;

        float timer = 0f;

        while (timer <
               normalizationDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    normalizationDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            float currentWidth =
                Mathf.Lerp(
                    sourceWallWidth,
                    finalCircumference,
                    smooth
                );

            BuildFlatWallMesh(
                currentWidth
            );

            if (topCircleObject != null)
            {
                topCircleObject
                    .transform
                    .localScale =
                    Vector3.Lerp(
                        topStartScale,
                        finalCircleScale,
                        smooth
                    );
            }

            if (bottomCircleObject != null)
            {
                bottomCircleObject
                    .transform
                    .localScale =
                    Vector3.Lerp(
                        bottomStartScale,
                        finalCircleScale,
                        smooth
                    );
            }

            yield return null;
        }

        BuildFlatWallMesh(
            finalCircumference
        );

        if (topCircleObject != null)
        {
            topCircleObject
                .transform
                .localScale =
                finalCircleScale;
        }

        if (bottomCircleObject != null)
        {
            bottomCircleObject
                .transform
                .localScale =
                finalCircleScale;
        }
    }

    // ==================================================
    // WRAP WALL
    // ==================================================

    private IEnumerator WrapWall()
    {
        float timer =
            0f;

        while (timer <
               wrapDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    wrapDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            BuildWallMesh(
                smooth
            );

            yield return null;
        }

        BuildWallMesh(
            1f
        );
    }

    // ==================================================
    // FOLD CAP
    // ==================================================

    private IEnumerator FoldCap(
        GameObject circleObject,
        bool top)
    {
        if (circleObject == null)
            yield break;

        Transform cap =
            circleObject.transform;

        Vector3 startPosition =
            cap.localPosition;

        Quaternion startRotation =
            cap.localRotation;

        Vector3 startScale =
            cap.localScale;

        Vector3 targetScale =
            Vector3.one;

        Vector3 targetPosition =
            new Vector3(
                0f,
                top
                    ? heightWorld * 0.5f
                    : -heightWorld * 0.5f,
                -radiusWorld
            );

        /*
         * Circle starts in XY plane.
         * Final cap must lie in XZ plane.
         */
        Quaternion targetRotation =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );

        float timer =
            0f;

        while (timer <
               capFoldDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    capFoldDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            cap.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smooth
                );

            cap.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smooth
                );

            cap.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    smooth
                );

            yield return null;
        }

        cap.localPosition =
            targetPosition;

        cap.localRotation =
            targetRotation;

        cap.localScale =
            targetScale;
    }

    // ==================================================
    // FINAL MODEL
    // ==================================================

    private void SetupFinalModel()
    {
        if (currentModel == null)
            return;

        // ----------------------------------------------
        // FINAL WALL COLLIDER
        // ----------------------------------------------
        if (wallObject != null)
        {
            MeshCollider wallCollider =
                wallObject.GetComponent<MeshCollider>();

            if (wallCollider != null &&
                wallMesh != null)
            {
                wallCollider.sharedMesh = null;
                wallCollider.sharedMesh = wallMesh;
            }
        }

        currentModel.transform.Rotate(
            previewTiltX,
            previewTurnY,
            0f,
            Space.Self
        );

        TouchRotateZoom rotateZoom;

        if (!currentModel
            .TryGetComponent<
                TouchRotateZoom>(
                    out rotateZoom))
        {
            rotateZoom =
                currentModel.AddComponent<
                    TouchRotateZoom>();
        }

        rotateZoom.target =
            currentModel.transform;

        Debug.Log(
            "Cylinder interaction enabled."
        );
    }

    // ==================================================
    // CLEAR
    // ==================================================

    public void ClearModel()
    {
        StopAllCoroutines();

        if (currentModel != null)
        {
            Destroy(
                currentModel
            );

            currentModel =
                null;
        }

        wallObject = null;
        topCircleObject = null;
        bottomCircleObject = null;
        wallMesh = null;
    }

    public Transform GetCurrentModelRoot()
    {
        if (currentModel == null)
            return null;

        return currentModel.transform;
    }
}
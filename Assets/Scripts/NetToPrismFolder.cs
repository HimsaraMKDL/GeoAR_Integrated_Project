using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetToPrismFolder : MonoBehaviour
{
    public event Action FoldingCompleted;

    [Header("Materials")]
    public Material faceMaterial;

    [Header("Preview")]
    public float flatNetHoldTime = 1.2f;

    [Header("Normalization")]
    public float normalizationHoldTime = 0.35f;

    [Header("Hinge Folding")]
    public float hingeFoldDuration = 1.0f;
    public float delayBetweenFolds = 0.25f;

    [Header("Face")]
    public float faceThickness = 0.015f;

    [Header("3D Size")]
    [Tooltip("Fallback world-space size for one geometry unit.")]
    public float worldUnitsPerGeometryUnit = 0.22f;

    [Tooltip("Makes the final prism longer along its depth direction.")]
    public float prismLengthMultiplier = 2.5f;

    [Header("Final 3D Preview")]
    public float previewTiltX = -20f;
    public float previewTurnY = 30f;
    public float previewRollZ = 0f;

    private GameObject currentModel;
    private RectTransform activePuzzleBoard;

    private readonly List<RuntimePrismFace> runtimeFaces =
        new List<RuntimePrismFace>();

    private readonly List<Transform> createdFaces =
        new List<Transform>();

    private bool interactionEnabled;

    // =====================================================
    // RUNTIME DATA
    // =====================================================

    private class RuntimePrismFace
    {
        public PrismGeneratedFace sourceFace;
        public Transform transform;
        public bool isTriangle;

        public RuntimePrismFace(
            PrismGeneratedFace sourceFace,
            Transform transform,
            bool isTriangle)
        {
            this.sourceFace = sourceFace;
            this.transform = transform;
            this.isTriangle = isTriangle;
        }
    }

    private class TriangleAttachment
    {
        public PrismGeneratedFace triangle;
        public PrismGeneratedFace rectangle;

        public PrismCompatibleEdge triangleEdge;
        public PrismCompatibleEdge rectangleEdge;

        public RuntimePrismFace triangleRuntime;
        public RuntimePrismFace rectangleRuntime;

        public int triangleVertexA;
        public int triangleVertexB;
        public int triangleVertexThird;

        public bool rectangleTopEdge;
        public bool reversed;
    }

    // =====================================================
    // MAIN ENTRY
    // =====================================================

    public void CreateAndFoldFromGuidedNet(
        PrismGuidedConfig config,
        List<PrismGeneratedFace> sourceFaces,
        RectTransform puzzleBoard,
        float distanceFromCamera)
    {
        ClearModel();

        activePuzzleBoard = puzzleBoard;

        if (config == null || !config.IsReady)
        {
            Debug.LogError(
                "Cannot create prism. Guided configuration is incomplete."
            );

            return;
        }

        if (sourceFaces == null ||
            sourceFaces.Count != 5)
        {
            Debug.LogError(
                "Exactly 5 prism faces are required."
            );

            return;
        }

        if (puzzleBoard == null)
        {
            Debug.LogError(
                "PuzzleBoard is missing."
            );

            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError(
                "Main Camera was not found."
            );

            return;
        }

        List<PrismGeneratedFace> triangles =
            new List<PrismGeneratedFace>();

        List<PrismGeneratedFace> rectangles =
            new List<PrismGeneratedFace>();

        foreach (PrismGeneratedFace face in sourceFaces)
        {
            if (face == null)
                continue;

            if (face.IsTriangle)
                triangles.Add(face);

            if (face.IsRectangle)
                rectangles.Add(face);
        }

        if (triangles.Count != 2 ||
            rectangles.Count != 3)
        {
            Debug.LogError(
                "Expected 2 triangles and 3 rectangles."
            );

            return;
        }

        // -------------------------------------------------
        // MODEL ROOT
        // -------------------------------------------------

        currentModel =
            new GameObject(
                "Guided_Triangular_Prism_3D"
            );

        currentModel.transform.position =
            Vector3.zero;

        currentModel.transform.rotation =
            Quaternion.identity;

        currentModel.transform.localScale =
            Vector3.one;

        interactionEnabled = false;

        // -------------------------------------------------
        // FIRST: SHOW EXACT STUDENT UI NET IN 3D
        // -------------------------------------------------

        foreach (PrismGeneratedFace triangle in triangles)
        {
            CreateDisplayTriangle(
                triangle,
                cam,
                distanceFromCamera
            );
        }

        foreach (PrismGeneratedFace rectangle in rectangles)
        {
            CreateDisplayRectangle(
                rectangle,
                cam,
                distanceFromCamera
            );
        }

        StartCoroutine(
            PrepareAndFold(
                config,
                triangles,
                rectangles,
                sourceFaces,
                cam,
                distanceFromCamera
            )
        );
    }

    // =====================================================
    // FULL SEQUENCE
    // =====================================================

    private IEnumerator PrepareAndFold(
        PrismGuidedConfig config,
        List<PrismGeneratedFace> triangles,
        List<PrismGeneratedFace> rectangles,
        List<PrismGeneratedFace> allFaces,
        Camera cam,
        float distanceFromCamera)
    {
        // -------------------------------------------------
        // 1. DISPLAY STUDENT'S ORIGINAL NET
        // -------------------------------------------------

        Debug.Log(
            "Student flat prism net displayed in 3D."
        );

        yield return new WaitForSeconds(
            flatNetHoldTime
        );

        // -------------------------------------------------
        // 2. IDENTIFY RECTANGLE STRIP
        // -------------------------------------------------

        PrismGeneratedFace centerRectangle =
            FindCenterRectangle(
                rectangles
            );

        if (centerRectangle == null)
        {
            Debug.LogError(
                "Could not determine the middle rectangle."
            );

            yield break;
        }

        List<PrismGeneratedFace> sideRectangles =
            new List<PrismGeneratedFace>();

        foreach (PrismGeneratedFace rectangle
                 in rectangles)
        {
            if (rectangle == centerRectangle)
                continue;

            if (AreSourceFacesConnected(
                    centerRectangle,
                    rectangle))
            {
                sideRectangles.Add(rectangle);
            }
        }

        if (sideRectangles.Count != 2)
        {
            Debug.LogError(
                $"Expected 2 side rectangles. " +
                $"Found {sideRectangles.Count}."
            );

            yield break;
        }

        // -------------------------------------------------
        // 3. TRIANGLE ATTACHMENTS
        // -------------------------------------------------

        TriangleAttachment attachment1 =
            FindTriangleAttachment(
                triangles[0],
                rectangles
            );

        TriangleAttachment attachment2 =
            FindTriangleAttachment(
                triangles[1],
                rectangles
            );

        if (attachment1 == null ||
            attachment2 == null)
        {
            Debug.LogError(
                "Could not determine triangle attachments."
            );

            yield break;
        }

        // -------------------------------------------------
        // 4. SCALE FROM DISPLAYED RECTANGLE WIDTH
        // -------------------------------------------------

        float geometryScale =
            CalculateWorldScaleFromSourceNet(
                allFaces,
                cam,
                distanceFromCamera
            );

        // -------------------------------------------------
        // 5. NORMALIZE INTO A TRUE FOLDABLE NET
        // -------------------------------------------------

        bool normalized =
            BuildNormalizedFoldableNet(
                config,
                centerRectangle,
                sideRectangles,
                rectangles,
                attachment1,
                attachment2,
                geometryScale
            );

        if (!normalized)
        {
            Debug.LogError(
                "Could not construct normalized foldable prism net."
            );

            yield break;
        }

        Debug.Log(
            "Flat net normalized to exact triangular-prism geometry."
        );

        yield return new WaitForSeconds(
            normalizationHoldTime
        );

        // -------------------------------------------------
        // 6. RECTANGLE FOLDING
        // -------------------------------------------------

        RuntimePrismFace centerRuntime =
            FindRuntimeFace(
                centerRectangle
            );

        RuntimePrismFace side1Runtime =
            FindRuntimeFace(
                sideRectangles[0]
            );

        RuntimePrismFace side2Runtime =
            FindRuntimeFace(
                sideRectangles[1]
            );

        if (centerRuntime == null ||
            side1Runtime == null ||
            side2Runtime == null)
        {
            Debug.LogError(
                "Normalized rectangle runtime objects are missing."
            );

            yield break;
        }

        Vector2 pointA =
            config.trianglePointA;

        Vector2 pointB =
            config.trianglePointB;

        Vector2 pointC =
            config.trianglePointC;

        float side1FoldMagnitude =
            GetFoldAngleForRectanglePair(
                centerRectangle,
                sideRectangles[0],
                rectangles,
                pointA,
                pointB,
                pointC
            );

        float side2FoldMagnitude =
            GetFoldAngleForRectanglePair(
                centerRectangle,
                sideRectangles[1],
                rectangles,
                pointA,
                pointB,
                pointC
            );

        // Determine which one is left and which is right.
        Vector3 stripRight =
            centerRuntime.transform.right;

        float side1Position =
            Vector3.Dot(
                side1Runtime.transform.position -
                centerRuntime.transform.position,
                stripRight
            );

        float side2Position =
            Vector3.Dot(
                side2Runtime.transform.position -
                centerRuntime.transform.position,
                stripRight
            );

        float side1SignedAngle =
            side1Position < 0f
                ? side1FoldMagnitude
                : -side1FoldMagnitude;

        float side2SignedAngle =
            side2Position < 0f
                ? side2FoldMagnitude
                : -side2FoldMagnitude;

        List<Transform> side1Followers =
            GetTriangleFollowers(
                sideRectangles[0],
                attachment1,
                attachment2
            );

        List<Transform> side2Followers =
            GetTriangleFollowers(
                sideRectangles[1],
                attachment1,
                attachment2
            );

        Debug.Log(
            $"Folding rectangle Face {sideRectangles[0].faceId}."
        );

        yield return StartCoroutine(
            FoldRectangleAroundClosestDepthEdge(
                centerRuntime.transform,
                side1Runtime.transform,
                side1SignedAngle,
                side1Followers
            )
        );

        yield return new WaitForSeconds(
            delayBetweenFolds
        );

        Debug.Log(
            $"Folding rectangle Face {sideRectangles[1].faceId}."
        );

        yield return StartCoroutine(
            FoldRectangleAroundClosestDepthEdge(
                centerRuntime.transform,
                side2Runtime.transform,
                side2SignedAngle,
                side2Followers
            )
        );

        yield return new WaitForSeconds(
            delayBetweenFolds
        );

        Debug.Log(
            "Rectangle folding completed."
        );

        // -------------------------------------------------
        // 7. TRIANGLE END CAP 1
        // -------------------------------------------------

        yield return StartCoroutine(
            FoldTriangleCap(
                attachment1,
                rectangles
            )
        );

        yield return new WaitForSeconds(
            delayBetweenFolds
        );

        // -------------------------------------------------
        // 8. TRIANGLE END CAP 2
        // -------------------------------------------------

        yield return StartCoroutine(
            FoldTriangleCap(
                attachment2,
                rectangles
            )
        );

        yield return new WaitForSeconds(
            delayBetweenFolds
        );

        Debug.Log(
            "Triangle cap folding completed."
        );

        // -------------------------------------------------
        // 9. FINALIZE
        // -------------------------------------------------

        SetupFinalPivotAndInteraction();
    }

    // =====================================================
    // DISPLAY PREVIEW
    // =====================================================

    private void CreateDisplayRectangle(
        PrismGeneratedFace sourceFace,
        Camera cam,
        float distanceFromCamera)
    {
        GameObject rectangle =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        rectangle.name =
            "Display_Rectangle_" +
            sourceFace.faceId;

        Renderer renderer =
            rectangle.GetComponent<Renderer>();

        if (renderer != null &&
            faceMaterial != null)
        {
            renderer.material =
                faceMaterial;
        }

        rectangle.transform.position =
            GetUIFaceWorldPosition(
                sourceFace,
                cam,
                distanceFromCamera
            );

        rectangle.transform.rotation =
            GetUIFaceWorldRotation(
                sourceFace,
                cam
            );

        Vector2 displayedSize =
            GetDisplayedFaceWorldSize(
                sourceFace,
                cam,
                distanceFromCamera
            );

        rectangle.transform.localScale =
            new Vector3(
                displayedSize.x,
                displayedSize.y,
                faceThickness
            );

        RegisterRuntimeFace(
            sourceFace,
            rectangle.transform,
            false
        );
    }

    private void CreateDisplayTriangle(
        PrismGeneratedFace sourceFace,
        Camera cam,
        float distanceFromCamera)
    {
        RectTransform sourceRect =
            sourceFace.GetComponent<
                RectTransform>();

        if (sourceRect == null)
            return;

        Vector2 localA =
            ConvertTrianglePointToSourceRect(
                sourceFace,
                sourceFace.trianglePointA
            );

        Vector2 localB =
            ConvertTrianglePointToSourceRect(
                sourceFace,
                sourceFace.trianglePointB
            );

        Vector2 localC =
            ConvertTrianglePointToSourceRect(
                sourceFace,
                sourceFace.trianglePointC
            );

        Vector3 uiWorldA =
            sourceRect.TransformPoint(
                localA
            );

        Vector3 uiWorldB =
            sourceRect.TransformPoint(
                localB
            );

        Vector3 uiWorldC =
            sourceRect.TransformPoint(
                localC
            );

        Vector2 screenA =
            RectTransformUtility.WorldToScreenPoint(
                null,
                uiWorldA
            );

        Vector2 screenB =
            RectTransformUtility.WorldToScreenPoint(
                null,
                uiWorldB
            );

        Vector2 screenC =
            RectTransformUtility.WorldToScreenPoint(
                null,
                uiWorldC
            );

        Vector3 worldA =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenA.x,
                    screenA.y,
                    distanceFromCamera
                )
            );

        Vector3 worldB =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenB.x,
                    screenB.y,
                    distanceFromCamera
                )
            );

        Vector3 worldC =
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenC.x,
                    screenC.y,
                    distanceFromCamera
                )
            );

        GameObject triangle =
            CreateTriangleObject(
                worldA,
                worldB,
                worldC,
                "Display_Triangle_" +
                sourceFace.faceId
            );

        RegisterRuntimeFace(
            sourceFace,
            triangle.transform,
            true
        );
    }

    // =====================================================
    // NORMALIZATION
    // =====================================================

    private bool BuildNormalizedFoldableNet(
        PrismGuidedConfig config,
        PrismGeneratedFace centerRectangle,
        List<PrismGeneratedFace> sideRectangles,
        List<PrismGeneratedFace> allRectangles,
        TriangleAttachment attachment1,
        TriangleAttachment attachment2,
        float scale)
    {
        RuntimePrismFace centerRuntime =
            FindRuntimeFace(
                centerRectangle
            );

        RuntimePrismFace side1Runtime =
            FindRuntimeFace(
                sideRectangles[0]
            );

        RuntimePrismFace side2Runtime =
            FindRuntimeFace(
                sideRectangles[1]
            );

        if (centerRuntime == null ||
            side1Runtime == null ||
            side2Runtime == null)
        {
            return false;
        }

        Vector3 centerPosition =
            centerRuntime.transform.position;

        Quaternion flatRotation =
            centerRuntime.transform.rotation;

        Vector3 right =
            flatRotation *
            Vector3.right;

        Vector3 up =
            flatRotation *
            Vector3.up;

        float depth =
            config.depth *
            scale *
            prismLengthMultiplier;

        float centerWidth =
            centerRectangle.edgeLength *
            scale;

        float side1Width =
            sideRectangles[0].edgeLength *
            scale;

        float side2Width =
            sideRectangles[1].edgeLength *
            scale;

        // Which source rectangle appeared left/right?
        float sourceSide1 =
            Vector3.Dot(
                side1Runtime.transform.position -
                centerRuntime.transform.position,
                right
            );

        float sourceSide2 =
            Vector3.Dot(
                side2Runtime.transform.position -
                centerRuntime.transform.position,
                right
            );

        // CENTER
        ConfigureNormalizedRectangle(
            centerRuntime.transform,
            centerPosition,
            flatRotation,
            centerWidth,
            depth
        );

        // SIDE 1
        float side1Direction =
            sourceSide1 < sourceSide2
                ? -1f
                : 1f;

        Vector3 side1Position =
            centerPosition +
            right *
            side1Direction *
            (
                centerWidth * 0.5f +
                side1Width * 0.5f
            );

        ConfigureNormalizedRectangle(
            side1Runtime.transform,
            side1Position,
            flatRotation,
            side1Width,
            depth
        );

        // SIDE 2
        float side2Direction =
            -side1Direction;

        Vector3 side2Position =
            centerPosition +
            right *
            side2Direction *
            (
                centerWidth * 0.5f +
                side2Width * 0.5f
            );

        ConfigureNormalizedRectangle(
            side2Runtime.transform,
            side2Position,
            flatRotation,
            side2Width,
            depth
        );

        // Build mathematically correct triangles directly
        // against their normalized rectangle edges.
        if (!NormalizeTriangleAttachment(
                config,
                attachment1,
                scale,
                up))
        {
            return false;
        }

        if (!NormalizeTriangleAttachment(
                config,
                attachment2,
                scale,
                up))
        {
            return false;
        }

        return true;
    }

    private void ConfigureNormalizedRectangle(
        Transform rectangle,
        Vector3 position,
        Quaternion rotation,
        float width,
        float depth)
    {
        rectangle.position =
            position;

        rectangle.rotation =
            rotation;

        rectangle.localScale =
            new Vector3(
                width,
                depth,
                faceThickness
            );
    }

    // =====================================================
    // NORMALIZE TRIANGLES
    // =====================================================

    private bool NormalizeTriangleAttachment(
        PrismGuidedConfig config,
        TriangleAttachment attachment,
        float scale,
        Vector3 flatUp)
    {
        if (attachment == null ||
            attachment.triangleRuntime == null ||
            attachment.rectangleRuntime == null)
        {
            return false;
        }

        Transform triangleTransform =
            attachment.triangleRuntime.transform;

        Transform rectangleTransform =
            attachment.rectangleRuntime.transform;

        Vector3 edgeA;
        Vector3 edgeB;

        if (!GetRectangleLengthEdgeWorld(
                rectangleTransform,
                attachment.rectangleTopEdge,
                out edgeA,
                out edgeB))
        {
            return false;
        }

        Vector2[] mathPoints =
        {
            config.trianglePointA,
            config.trianglePointB,
            config.trianglePointC
        };

        int indexA =
            attachment.triangleVertexA;

        int indexB =
            attachment.triangleVertexB;

        int indexThird =
            attachment.triangleVertexThird;

        float distanceA =
            Vector2.Distance(
                mathPoints[indexA],
                mathPoints[indexThird]
            ) * scale;

        float distanceB =
            Vector2.Distance(
                mathPoints[indexB],
                mathPoints[indexThird]
            ) * scale;

        if (attachment.reversed)
        {
            Vector3 temp =
                edgeA;

            edgeA =
                edgeB;

            edgeB =
                temp;
        }

        float baseLength =
            Vector3.Distance(
                edgeA,
                edgeB
            );

        if (baseLength <= 0.0001f)
            return false;

        float x =
            (
                distanceA * distanceA -
                distanceB * distanceB +
                baseLength * baseLength
            )
            /
            (
                2f *
                baseLength
            );

        float heightSquared =
            distanceA * distanceA -
            x * x;

        float triangleHeight =
            Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    heightSquared
                )
            );

        Vector3 edgeDirection =
            (edgeB - edgeA)
            .normalized;

        /*
         * Triangle sits OUTSIDE the rectangle
         * while the net is flat.
         */
        Vector3 outsideDirection =
            attachment.rectangleTopEdge
                ? rectangleTransform.up
                : -rectangleTransform.up;

        Vector3 thirdPoint =
            edgeA +
            edgeDirection *
            x +
            outsideDirection *
            triangleHeight;

        Vector3[] worldVertices =
            new Vector3[3];

        worldVertices[indexA] =
            edgeA;

        worldVertices[indexB] =
            edgeB;

        worldVertices[indexThird] =
            thirdPoint;

        SetTriangleWorldGeometry(
            triangleTransform,
            worldVertices[0],
            worldVertices[1],
            worldVertices[2]
        );

        return true;
    }

    // =====================================================
    // TRIANGLE ATTACHMENT DETECTION
    // =====================================================

    private TriangleAttachment FindTriangleAttachment(
        PrismGeneratedFace triangle,
        List<PrismGeneratedFace> rectangles)
    {
        if (triangle == null ||
            activePuzzleBoard == null)
        {
            return null;
        }

        Vector2 triA =
            ConvertTrianglePointToSourceRect(
                triangle,
                triangle.trianglePointA
            );

        Vector2 triB =
            ConvertTrianglePointToSourceRect(
                triangle,
                triangle.trianglePointB
            );

        Vector2 triC =
            ConvertTrianglePointToSourceRect(
                triangle,
                triangle.trianglePointC
            );

        foreach (PrismGeneratedFace rectangle
                 in rectangles)
        {
            List<PrismCompatibleEdge> triangleEdges =
                triangle.GetCompatibleEdges();

            List<PrismCompatibleEdge> rectangleEdges =
                rectangle.GetCompatibleEdges();

            foreach (PrismCompatibleEdge triangleEdge
                     in triangleEdges)
            {
                if (triangleEdge.edgeType !=
                    PrismEdgeType.TriangleSide)
                {
                    continue;
                }

                foreach (PrismCompatibleEdge rectangleEdge
                         in rectangleEdges)
                {
                    if (rectangleEdge.edgeType !=
                        PrismEdgeType.RectangleLength)
                    {
                        continue;
                    }

                    if (Mathf.Abs(
                            triangleEdge.geometricLength -
                            rectangleEdge.geometricLength)
                        > 0.05f)
                    {
                        continue;
                    }

                    Vector2 ta =
                        triangleEdge.GetBoardPointA(
                            activePuzzleBoard
                        );

                    Vector2 tb =
                        triangleEdge.GetBoardPointB(
                            activePuzzleBoard
                        );

                    Vector2 ra =
                        rectangleEdge.GetBoardPointA(
                            activePuzzleBoard
                        );

                    Vector2 rb =
                        rectangleEdge.GetBoardPointB(
                            activePuzzleBoard
                        );

                    const float tolerance =
                        8f;

                    bool same =
                        Vector2.Distance(
                            ta,
                            ra
                        ) <= tolerance
                        &&
                        Vector2.Distance(
                            tb,
                            rb
                        ) <= tolerance;

                    bool reversed =
                        Vector2.Distance(
                            ta,
                            rb
                        ) <= tolerance
                        &&
                        Vector2.Distance(
                            tb,
                            ra
                        ) <= tolerance;

                    if (!same &&
                        !reversed)
                    {
                        continue;
                    }

                    int vertexA =
                        FindClosestTriangleVertexIndex(
                            triangleEdge.localPointA,
                            triA,
                            triB,
                            triC
                        );

                    int vertexB =
                        FindClosestTriangleVertexIndex(
                            triangleEdge.localPointB,
                            triA,
                            triB,
                            triC
                        );

                    if (vertexA == vertexB)
                        continue;

                    int vertexThird =
                        3 -
                        vertexA -
                        vertexB;

                    RectTransform rectangleRect =
                        rectangle.GetComponent<
                            RectTransform>();

                    bool topEdge =
                        Mathf.Abs(
                            rectangleEdge.localPointA.y -
                            rectangleRect.rect.yMax
                        )
                        <
                        Mathf.Abs(
                            rectangleEdge.localPointA.y -
                            rectangleRect.rect.yMin
                        );

                    return new TriangleAttachment
                    {
                        triangle =
                            triangle,

                        rectangle =
                            rectangle,

                        triangleEdge =
                            triangleEdge,

                        rectangleEdge =
                            rectangleEdge,

                        triangleRuntime =
                            FindRuntimeFace(
                                triangle
                            ),

                        rectangleRuntime =
                            FindRuntimeFace(
                                rectangle
                            ),

                        triangleVertexA =
                            vertexA,

                        triangleVertexB =
                            vertexB,

                        triangleVertexThird =
                            vertexThird,

                        rectangleTopEdge =
                            topEdge,

                        reversed =
                            reversed
                    };
                }
            }
        }

        return null;
    }

    // =====================================================
    // RECTANGLE FOLDING
    // =====================================================

    private IEnumerator FoldRectangleAroundClosestDepthEdge(
        Transform stationary,
        Transform moving,
        float angle,
        List<Transform> followers)
    {
        if (!TryGetClosestRectangleDepthHinge(
                stationary,
                moving,
                out Vector3 hingePoint,
                out Vector3 hingeAxis))
        {
            Debug.LogError(
                "Could not find normalized rectangle hinge."
            );

            yield break;
        }

        float timer = 0f;
        float previousAngle = 0f;

        while (timer <
               hingeFoldDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    hingeFoldDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            float currentAngle =
                Mathf.Lerp(
                    0f,
                    angle,
                    smooth
                );

            float delta =
                currentAngle -
                previousAngle;

            moving.RotateAround(
                hingePoint,
                hingeAxis,
                delta
            );

            if (followers != null)
            {
                foreach (Transform follower
                         in followers)
                {
                    if (follower == null)
                        continue;

                    follower.RotateAround(
                        hingePoint,
                        hingeAxis,
                        delta
                    );
                }
            }

            previousAngle =
                currentAngle;

            yield return null;
        }
    }

    private bool TryGetClosestRectangleDepthHinge(
        Transform rectangleA,
        Transform rectangleB,
        out Vector3 hingePoint,
        out Vector3 hingeAxis)
    {
        hingePoint =
            Vector3.zero;

        hingeAxis =
            Vector3.up;

        Vector3[,] edgesA =
            GetRectangleDepthEdges(
                rectangleA
            );

        Vector3[,] edgesB =
            GetRectangleDepthEdges(
                rectangleB
            );

        float best =
            float.MaxValue;

        Vector3 best1 =
            Vector3.zero;

        Vector3 best2 =
            Vector3.zero;

        for (int a = 0; a < 2; a++)
        {
            for (int b = 0; b < 2; b++)
            {
                float same =
                    Vector3.Distance(
                        edgesA[a, 0],
                        edgesB[b, 0]
                    )
                    +
                    Vector3.Distance(
                        edgesA[a, 1],
                        edgesB[b, 1]
                    );

                float reversed =
                    Vector3.Distance(
                        edgesA[a, 0],
                        edgesB[b, 1]
                    )
                    +
                    Vector3.Distance(
                        edgesA[a, 1],
                        edgesB[b, 0]
                    );

                float value =
                    Mathf.Min(
                        same,
                        reversed
                    );

                if (value < best)
                {
                    best =
                        value;

                    best1 =
                        edgesA[a, 0];

                    best2 =
                        edgesA[a, 1];
                }
            }
        }

        hingePoint =
            (
                best1 +
                best2
            ) * 0.5f;

        hingeAxis =
            (
                best2 -
                best1
            ).normalized;

        return
            hingeAxis.sqrMagnitude >
            0.001f;
    }

    private Vector3[,] GetRectangleDepthEdges(
        Transform rectangle)
    {
        Vector3[,] edges =
            new Vector3[2, 2];

        edges[0, 0] =
            rectangle.TransformPoint(
                new Vector3(
                    -0.5f,
                    -0.5f,
                    0f
                )
            );

        edges[0, 1] =
            rectangle.TransformPoint(
                new Vector3(
                    -0.5f,
                    0.5f,
                    0f
                )
            );

        edges[1, 0] =
            rectangle.TransformPoint(
                new Vector3(
                    0.5f,
                    -0.5f,
                    0f
                )
            );

        edges[1, 1] =
            rectangle.TransformPoint(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );

        return edges;
    }

    // =====================================================
    // TRIANGLE CAP FOLDING
    // =====================================================

    private IEnumerator FoldTriangleCap(
        TriangleAttachment attachment,
        List<PrismGeneratedFace> rectangles)
    {
        if (attachment == null ||
            attachment.triangleRuntime == null ||
            attachment.rectangleRuntime == null)
        {
            yield break;
        }

        Transform triangle =
            attachment.triangleRuntime.transform;

        Transform rectangle =
            attachment.rectangleRuntime.transform;

        if (!GetRectangleLengthEdgeWorld(
                rectangle,
                attachment.rectangleTopEdge,
                out Vector3 hingeA,
                out Vector3 hingeB))
        {
            yield break;
        }

        Vector3 hingePoint =
            (
                hingeA +
                hingeB
            ) * 0.5f;

        Vector3 hingeAxis =
            (
                hingeB -
                hingeA
            ).normalized;

        Vector3 expectedCapCenter =
            CalculateExpectedCapCenter(
                attachment.rectangleTopEdge,
                rectangles
            );

        float foldAngle =
            ChooseCapFoldDirection(
                triangle,
                hingePoint,
                hingeAxis,
                expectedCapCenter
            );

        Debug.Log(
            $"Folding Triangle {attachment.triangle.faceId} " +
            $"around Face {attachment.rectangle.faceId}. " +
            $"Angle = {foldAngle}°"
        );

        float timer = 0f;
        float previousAngle = 0f;

        while (timer <
               hingeFoldDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    hingeFoldDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            float currentAngle =
                Mathf.Lerp(
                    0f,
                    foldAngle,
                    smooth
                );

            float delta =
                currentAngle -
                previousAngle;

            triangle.RotateAround(
                hingePoint,
                hingeAxis,
                delta
            );

            previousAngle =
                currentAngle;

            yield return null;
        }

        Debug.Log(
            $"Triangle {attachment.triangle.faceId} cap fold completed."
        );
    }

    private float ChooseCapFoldDirection(
        Transform triangle,
        Vector3 hingePoint,
        Vector3 hingeAxis,
        Vector3 expectedCenter)
    {
        Vector3 offset =
            triangle.position -
            hingePoint;

        Vector3 plus =
            hingePoint +
            Quaternion.AngleAxis(
                90f,
                hingeAxis
            ) * offset;

        Vector3 minus =
            hingePoint +
            Quaternion.AngleAxis(
                -90f,
                hingeAxis
            ) * offset;

        float plusDistance =
            Vector3.Distance(
                plus,
                expectedCenter
            );

        float minusDistance =
            Vector3.Distance(
                minus,
                expectedCenter
            );

        return
            plusDistance <= minusDistance
                ? 90f
                : -90f;
    }

    private Vector3 CalculateExpectedCapCenter(
        bool top,
        List<PrismGeneratedFace> rectangles)
    {
        Vector3 total =
            Vector3.zero;

        int count =
            0;

        foreach (PrismGeneratedFace rectangleSource
                 in rectangles)
        {
            RuntimePrismFace runtime =
                FindRuntimeFace(
                    rectangleSource
                );

            if (runtime == null)
                continue;

            if (GetRectangleLengthEdgeWorld(
                    runtime.transform,
                    top,
                    out Vector3 a,
                    out Vector3 b))
            {
                total +=
                    (
                        a +
                        b
                    ) * 0.5f;

                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        return
            total /
            count;
    }

    // =====================================================
    // RECTANGLE LENGTH EDGE
    // =====================================================

    private bool GetRectangleLengthEdgeWorld(
        Transform rectangle,
        bool top,
        out Vector3 edgeA,
        out Vector3 edgeB)
    {
        edgeA =
            Vector3.zero;

        edgeB =
            Vector3.zero;

        if (rectangle == null)
            return false;

        float y =
            top
                ? 0.5f
                : -0.5f;

        edgeA =
            rectangle.TransformPoint(
                new Vector3(
                    -0.5f,
                    y,
                    0f
                )
            );

        edgeB =
            rectangle.TransformPoint(
                new Vector3(
                    0.5f,
                    y,
                    0f
                )
            );

        return true;
    }

    // =====================================================
    // TRIANGLE MESH
    // =====================================================

    private GameObject CreateTriangleObject(
        Vector3 worldA,
        Vector3 worldB,
        Vector3 worldC,
        string objectName)
    {
        GameObject triangle =
            new GameObject(
                objectName
            );

        MeshFilter meshFilter =
            triangle.AddComponent<
                MeshFilter>();

        MeshRenderer meshRenderer =
            triangle.AddComponent<
                MeshRenderer>();

        if (faceMaterial != null)
        {
            meshRenderer.material =
                faceMaterial;
        }

        SetTriangleWorldGeometry(
            triangle.transform,
            worldA,
            worldB,
            worldC
        );

        return triangle;
    }

    private void SetTriangleWorldGeometry(
        Transform triangle,
        Vector3 worldA,
        Vector3 worldB,
        Vector3 worldC)
    {
        Vector3 center =
            (
                worldA +
                worldB +
                worldC
            ) / 3f;

        triangle.position =
            center;

        triangle.rotation =
            Quaternion.identity;

        triangle.localScale =
            Vector3.one;

        Vector3 localA =
            worldA -
            center;

        Vector3 localB =
            worldB -
            center;

        Vector3 localC =
            worldC -
            center;

        MeshFilter meshFilter =
            triangle.GetComponent<
                MeshFilter>();

        if (meshFilter == null)
        {
            meshFilter =
                triangle.gameObject
                    .AddComponent<
                        MeshFilter>();
        }

        Mesh mesh =
            new Mesh();

        mesh.name =
            "PrismTriangleMesh";

        mesh.vertices =
            new Vector3[]
            {
                localA,
                localB,
                localC
            };

        mesh.triangles =
            new int[]
            {
                0, 1, 2,
                2, 1, 0
            };

        mesh.uv =
            new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f)
            };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (meshFilter.sharedMesh != null)
        {
            Destroy(
                meshFilter.sharedMesh
            );
        }

        meshFilter.sharedMesh =
            mesh;
    }

    // =====================================================
    // RECTANGLE TOPOLOGY
    // =====================================================

    private PrismGeneratedFace FindCenterRectangle(
        List<PrismGeneratedFace> rectangles)
    {
        foreach (PrismGeneratedFace candidate
                 in rectangles)
        {
            int count = 0;

            foreach (PrismGeneratedFace other
                     in rectangles)
            {
                if (candidate == other)
                    continue;

                if (AreSourceFacesConnected(
                        candidate,
                        other))
                {
                    count++;
                }
            }

            if (count == 2)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool AreSourceFacesConnected(
        PrismGeneratedFace first,
        PrismGeneratedFace second)
    {
        if (first == null ||
            second == null ||
            activePuzzleBoard == null)
        {
            return false;
        }

        foreach (PrismCompatibleEdge firstEdge
                 in first.GetCompatibleEdges())
        {
            if (firstEdge.edgeType !=
                PrismEdgeType.RectangleDepth)
            {
                continue;
            }

            foreach (PrismCompatibleEdge secondEdge
                     in second.GetCompatibleEdges())
            {
                if (secondEdge.edgeType !=
                    PrismEdgeType.RectangleDepth)
                {
                    continue;
                }

                Vector2 a1 =
                    firstEdge.GetBoardPointA(
                        activePuzzleBoard
                    );

                Vector2 a2 =
                    firstEdge.GetBoardPointB(
                        activePuzzleBoard
                    );

                Vector2 b1 =
                    secondEdge.GetBoardPointA(
                        activePuzzleBoard
                    );

                Vector2 b2 =
                    secondEdge.GetBoardPointB(
                        activePuzzleBoard
                    );

                const float tolerance =
                    8f;

                bool same =
                    Vector2.Distance(
                        a1,
                        b1
                    ) <= tolerance
                    &&
                    Vector2.Distance(
                        a2,
                        b2
                    ) <= tolerance;

                bool reverse =
                    Vector2.Distance(
                        a1,
                        b2
                    ) <= tolerance
                    &&
                    Vector2.Distance(
                        a2,
                        b1
                    ) <= tolerance;

                if (same || reverse)
                    return true;
            }
        }

        return false;
    }

    // =====================================================
    // RECTANGLE FOLD ANGLES
    // =====================================================

    private float GetFoldAngleForRectanglePair(
        PrismGeneratedFace first,
        PrismGeneratedFace second,
        List<PrismGeneratedFace> rectangles,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        float lengthAB =
            Vector2.Distance(
                a,
                b
            );

        float lengthBC =
            Vector2.Distance(
                b,
                c
            );

        float lengthCA =
            Vector2.Distance(
                c,
                a
            );

        float firstLength =
            first.edgeLength;

        float secondLength =
            second.edgeLength;

        if (LengthsMatchPair(
                firstLength,
                secondLength,
                lengthAB,
                lengthBC))
        {
            return
                180f -
                CalculateTriangleAngle(
                    a,
                    b,
                    c
                );
        }

        if (LengthsMatchPair(
                firstLength,
                secondLength,
                lengthBC,
                lengthCA))
        {
            return
                180f -
                CalculateTriangleAngle(
                    b,
                    c,
                    a
                );
        }

        if (LengthsMatchPair(
                firstLength,
                secondLength,
                lengthCA,
                lengthAB))
        {
            return
                180f -
                CalculateTriangleAngle(
                    b,
                    a,
                    c
                );
        }

        return 90f;
    }

    private bool LengthsMatchPair(
        float first,
        float second,
        float expected1,
        float expected2)
    {
        const float tolerance =
            0.05f;

        return
            (
                Mathf.Abs(
                    first -
                    expected1
                ) <= tolerance
                &&
                Mathf.Abs(
                    second -
                    expected2
                ) <= tolerance
            )
            ||
            (
                Mathf.Abs(
                    first -
                    expected2
                ) <= tolerance
                &&
                Mathf.Abs(
                    second -
                    expected1
                ) <= tolerance
            );
    }

    private float CalculateTriangleAngle(
        Vector2 first,
        Vector2 vertex,
        Vector2 second)
    {
        Vector2 d1 =
            (
                first -
                vertex
            ).normalized;

        Vector2 d2 =
            (
                second -
                vertex
            ).normalized;

        return
            Vector2.Angle(
                d1,
                d2
            );
    }

    // =====================================================
    // FOLLOW TRIANGLES DURING RECTANGLE FOLD
    // =====================================================

    private List<Transform> GetTriangleFollowers(
        PrismGeneratedFace rectangle,
        TriangleAttachment attachment1,
        TriangleAttachment attachment2)
    {
        List<Transform> result =
            new List<Transform>();

        if (attachment1 != null &&
            attachment1.rectangle ==
            rectangle &&
            attachment1.triangleRuntime != null)
        {
            result.Add(
                attachment1
                    .triangleRuntime
                    .transform
            );
        }

        if (attachment2 != null &&
            attachment2.rectangle ==
            rectangle &&
            attachment2.triangleRuntime != null)
        {
            Transform second =
                attachment2
                    .triangleRuntime
                    .transform;

            if (!result.Contains(
                    second))
            {
                result.Add(
                    second
                );
            }
        }

        return result;
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private RuntimePrismFace FindRuntimeFace(
        PrismGeneratedFace source)
    {
        foreach (RuntimePrismFace runtime
                 in runtimeFaces)
        {
            if (runtime.sourceFace ==
                source)
            {
                return runtime;
            }
        }

        return null;
    }

    private void RegisterRuntimeFace(
        PrismGeneratedFace source,
        Transform runtimeTransform,
        bool triangle)
    {
        runtimeTransform.SetParent(
            currentModel.transform,
            true
        );

        createdFaces.Add(
            runtimeTransform
        );

        runtimeFaces.Add(
            new RuntimePrismFace(
                source,
                runtimeTransform,
                triangle
            )
        );
    }

    private PrismGeneratedFace FindRectangleByLength(
        List<PrismGeneratedFace> rectangles,
        float required,
        params PrismGeneratedFace[] excluded)
    {
        foreach (PrismGeneratedFace rectangle
                 in rectangles)
        {
            if (rectangle == null)
                continue;

            bool skip = false;

            foreach (PrismGeneratedFace item
                     in excluded)
            {
                if (rectangle == item)
                {
                    skip = true;
                    break;
                }
            }

            if (skip)
                continue;

            if (Mathf.Abs(
                    rectangle.edgeLength -
                    required)
                <= 0.05f)
            {
                return rectangle;
            }
        }

        return null;
    }

    private int FindClosestTriangleVertexIndex(
        Vector2 target,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        float da =
            Vector2.Distance(
                target,
                a
            );

        float db =
            Vector2.Distance(
                target,
                b
            );

        float dc =
            Vector2.Distance(
                target,
                c
            );

        if (da <= db &&
            da <= dc)
        {
            return 0;
        }

        if (db <= da &&
            db <= dc)
        {
            return 1;
        }

        return 2;
    }

    // =====================================================
    // UI -> WORLD
    // =====================================================

    private Vector2 ConvertTrianglePointToSourceRect(
        PrismGeneratedFace sourceFace,
        Vector2 sourcePoint)
    {
        RectTransform rect =
            sourceFace.GetComponent<
                RectTransform>();

        Vector2 a =
            sourceFace.trianglePointA;

        Vector2 b =
            sourceFace.trianglePointB;

        Vector2 c =
            sourceFace.trianglePointC;

        float minX =
            Mathf.Min(
                a.x,
                Mathf.Min(
                    b.x,
                    c.x
                )
            );

        float maxX =
            Mathf.Max(
                a.x,
                Mathf.Max(
                    b.x,
                    c.x
                )
            );

        float minY =
            Mathf.Min(
                a.y,
                Mathf.Min(
                    b.y,
                    c.y
                )
            );

        float maxY =
            Mathf.Max(
                a.y,
                Mathf.Max(
                    b.y,
                    c.y
                )
            );

        float width =
            Mathf.Max(
                0.001f,
                maxX -
                minX
            );

        float height =
            Mathf.Max(
                0.001f,
                maxY -
                minY
            );

        float nx =
            (
                sourcePoint.x -
                minX
            ) /
            width;

        float ny =
            (
                sourcePoint.y -
                minY
            ) /
            height;

        return new Vector2(
            Mathf.Lerp(
                rect.rect.xMin,
                rect.rect.xMax,
                nx
            ),
            Mathf.Lerp(
                rect.rect.yMin,
                rect.rect.yMax,
                ny
            )
        );
    }

    private Vector3 GetUIFaceWorldPosition(
        PrismGeneratedFace source,
        Camera cam,
        float distance)
    {
        RectTransform rect =
            source.GetComponent<
                RectTransform>();

        Vector3 uiCenter =
            rect.TransformPoint(
                rect.rect.center
            );

        Vector2 screen =
            RectTransformUtility.WorldToScreenPoint(
                null,
                uiCenter
            );

        return
            cam.ScreenToWorldPoint(
                new Vector3(
                    screen.x,
                    screen.y,
                    distance
                )
            );
    }

    private Quaternion GetUIFaceWorldRotation(
        PrismGeneratedFace source,
        Camera cam)
    {
        RectTransform rect =
            source.GetComponent<
                RectTransform>();

        float angle =
            rect.eulerAngles.z;

        return
            Quaternion.AngleAxis(
                angle,
                cam.transform.forward
            )
            *
            cam.transform.rotation;
    }

    private Vector2 GetDisplayedFaceWorldSize(
        PrismGeneratedFace source,
        Camera cam,
        float distance)
    {
        RectTransform rect =
            source.GetComponent<
                RectTransform>();

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

        Vector2 sl =
            RectTransformUtility.WorldToScreenPoint(
                null,
                left
            );

        Vector2 sr =
            RectTransformUtility.WorldToScreenPoint(
                null,
                right
            );

        Vector2 sb =
            RectTransformUtility.WorldToScreenPoint(
                null,
                bottom
            );

        Vector2 st =
            RectTransformUtility.WorldToScreenPoint(
                null,
                top
            );

        Vector3 wl =
            cam.ScreenToWorldPoint(
                new Vector3(
                    sl.x,
                    sl.y,
                    distance
                )
            );

        Vector3 wr =
            cam.ScreenToWorldPoint(
                new Vector3(
                    sr.x,
                    sr.y,
                    distance
                )
            );

        Vector3 wb =
            cam.ScreenToWorldPoint(
                new Vector3(
                    sb.x,
                    sb.y,
                    distance
                )
            );

        Vector3 wt =
            cam.ScreenToWorldPoint(
                new Vector3(
                    st.x,
                    st.y,
                    distance
                )
            );

        return new Vector2(
            Vector3.Distance(
                wl,
                wr
            ),
            Vector3.Distance(
                wb,
                wt
            )
        );
    }

    private float CalculateWorldScaleFromSourceNet(
        List<PrismGeneratedFace> sourceFaces,
        Camera cam,
        float distance)
    {
        foreach (PrismGeneratedFace face
                 in sourceFaces)
        {
            if (face == null ||
                !face.IsRectangle ||
                face.edgeLength <=
                0f)
            {
                continue;
            }

            Vector2 size =
                GetDisplayedFaceWorldSize(
                    face,
                    cam,
                    distance
                );

            if (size.x >
                0.0001f)
            {
                float result =
                    size.x /
                    face.edgeLength;

                Debug.Log(
                    $"Prism geometry scale = {result:0.0000}"
                );

                return result;
            }
        }

        return
            worldUnitsPerGeometryUnit;
    }

    // =====================================================
    // FINAL MODEL
    // =====================================================

    private void SetupFinalPivotAndInteraction()
    {
        if (currentModel == null ||
            createdFaces.Count == 0)
        {
            return;
        }

        Vector3 center =
            Vector3.zero;

        int count =
            0;

        foreach (Transform face
                 in createdFaces)
        {
            if (face == null)
                continue;

            center +=
                face.position;

            count++;
        }

        if (count == 0)
            return;

        center /=
            count;

        /*
         * Move root to new pivot without moving faces.
         */
        foreach (Transform face
                 in createdFaces)
        {
            if (face != null)
            {
                face.SetParent(
                    null,
                    true
                );
            }
        }

        currentModel.transform.position =
            center;

        currentModel.transform.rotation =
            Quaternion.identity;

        currentModel.transform.localScale =
            Vector3.one;

        foreach (Transform face
                 in createdFaces)
        {
            if (face != null)
            {
                face.SetParent(
                    currentModel.transform,
                    true
                );
            }
        }

        currentModel.transform.Rotate(
            previewTiltX,
            previewTurnY,
            previewRollZ,
            Space.Self
        );

        if (!currentModel.TryGetComponent<
                TouchRotateZoom>(
                out TouchRotateZoom rotateZoom))
        {
            rotateZoom =
                currentModel.AddComponent<
                    TouchRotateZoom>();
        }

        rotateZoom.target =
            currentModel.transform;

        interactionEnabled =
            true;

        Debug.Log(
            "Triangular prism geometry closed successfully."
        );

        FoldingCompleted?.Invoke();
    }

    // =====================================================
    // PUBLIC
    // =====================================================

    public void ClearModel()
    {
        StopAllCoroutines();

        interactionEnabled =
            false;

        activePuzzleBoard =
            null;

        runtimeFaces.Clear();
        createdFaces.Clear();

        if (currentModel != null)
        {
            Destroy(
                currentModel
            );

            currentModel =
                null;
        }
    }

    public Transform GetCurrentModelRoot()
    {
        if (currentModel == null)
            return null;

        return
            currentModel.transform;
    }
}
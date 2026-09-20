using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetToTetrahedronFolder : MonoBehaviour
{
    // ==================================================
    // MATERIAL
    // ==================================================

    [Header("Materials")]
    [SerializeField]
    private Material faceMaterial;


    // ==================================================
    // ANIMATION
    // ==================================================

    [Header("Animation")]

    [SerializeField]
    private float flatHoldTime = 1.0f;

    [SerializeField]
    private float foldDuration = 1.5f;

    [SerializeField]
    private float delayBetweenFaces = 0.15f;

    /*
     * Regular tetrahedron:
     *
     * Internal dihedral angle ? 70.5288°
     *
     * Fold rotation from a flat net:
     *
     * 180° - 70.5288°
     * = 109.4712°
     */
    [SerializeField]
    private float tetrahedronFoldAngle = 109.47122f;


    // ==================================================
    // FOLD DIRECTION
    // ==================================================

    [Header("Fold Direction")]

    /*
     * true:
     * Faces initially fold toward the camera side
     * of the flat net.
     *
     * If the complete tetrahedron folds in the
     * opposite visual direction on your scene,
     * uncheck this in Inspector.
     */
    [SerializeField]
    private bool foldTowardCamera = true;


    // ==================================================
    // PREVIEW
    // ==================================================

    [Header("Preview")]

    [SerializeField]
    private float previewTiltX = -15f;

    [SerializeField]
    private float previewTurnY = 25f;


    // ==================================================
    // RUNTIME
    // ==================================================

    private GameObject currentModel;

    private readonly List<FaceRuntime>
        runtimeFaces =
            new List<FaceRuntime>();

    private readonly List<HingeRuntime>
        runtimeHinges =
            new List<HingeRuntime>();

    private Transform rootFaceTransform;


    // ==================================================
    // RUNTIME CLASSES
    // ==================================================

    private class FaceRuntime
    {
        public int index;

        public TetrahedronTriangleFace data;

        public Transform transform;

        public Vector2Int[] points;

        public readonly List<int>
            neighbours =
                new List<int>();
    }


    private class HingeRuntime
    {
        public Transform hinge;

        public int parentFaceIndex;

        public int childFaceIndex;

        public Vector2Int sharedPointA;

        public Vector2Int sharedPointB;

        public Quaternion startLocalRotation;

        public Quaternion targetLocalRotation;
    }


    // ==================================================
    // MAIN ENTRY
    // ==================================================

    public void CreateAndFold(
        List<TetrahedronTriangleFace> faces,
        TetrahedronGridDrawManager drawManager,
        float distanceFromCamera)
    {
        ClearModel();

        if (faces == null ||
            faces.Count != 4)
        {
            Debug.LogError(
                "Tetrahedron requires exactly 4 detected triangle faces."
            );

            return;
        }


        if (drawManager == null)
        {
            Debug.LogError(
                "TetrahedronGridDrawManager is missing."
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


        RectTransform drawingArea =
            drawManager.GetDrawingArea();


        if (drawingArea == null)
        {
            Debug.LogError(
                "Tetrahedron drawing area not found."
            );

            return;
        }


        runtimeFaces.Clear();

        runtimeHinges.Clear();

        rootFaceTransform = null;


        /*
         * Keep the model root neutral during
         * flat-net creation and folding.
         *
         * The four actual faces are created
         * directly at their corresponding
         * Canvas/world positions.
         */
        currentModel =
            new GameObject(
                "Tetrahedron_3D"
            );

        currentModel.transform.position =
            Vector3.zero;

        currentModel.transform.rotation =
            Quaternion.identity;


        float sideWorld =
            GetWorldTriangleSide(
                drawManager,
                cam,
                distanceFromCamera
            );


        if (sideWorld <= 0f)
        {
            Debug.LogError(
                "Could not calculate tetrahedron world size."
            );

            ClearModel();

            return;
        }


        // ----------------------------------------------
        // CREATE THE FOUR FLAT FACES
        // ----------------------------------------------

        for (int index = 0;
             index < faces.Count;
             index++)
        {
            FaceRuntime runtimeFace =
                CreateFace(
                    faces[index],
                    index,
                    drawManager,
                    cam,
                    distanceFromCamera,
                    sideWorld
                );


            if (runtimeFace == null)
            {
                Debug.LogError(
                    $"Failed to create tetrahedron face {index + 1}."
                );

                ClearModel();

                return;
            }


            runtimeFaces.Add(
                runtimeFace
            );
        }


        // ----------------------------------------------
        // FIND FACE CONNECTIONS
        // ----------------------------------------------

        BuildFaceGraph();


        if (!IsFaceGraphConnected())
        {
            Debug.LogError(
                "Tetrahedron folding stopped: " +
                "the four faces do not form one connected net."
            );

            ClearModel();

            return;
        }


        StartCoroutine(
            FoldingSequence(
                drawManager,
                cam,
                distanceFromCamera
            )
        );


        Debug.Log(
            "Tetrahedron hinge-folding sequence started."
        );
    }


    // ==================================================
    // CREATE A FLAT FACE
    // ==================================================

    private FaceRuntime CreateFace(
        TetrahedronTriangleFace faceData,
        int index,
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera,
        float sideWorld)
    {
        if (faceData == null)
            return null;


        GameObject faceObject =
            new GameObject(
                $"TetrahedronFace_{index + 1}"
            );


        MeshFilter filter =
            faceObject.AddComponent<
                MeshFilter>();


        MeshRenderer renderer =
            faceObject.AddComponent<
                MeshRenderer>();


        MeshCollider collider =
            faceObject.AddComponent<
                MeshCollider>();


        if (faceMaterial != null)
        {
            renderer.material =
                faceMaterial;
        }


        // ----------------------------------------------
        // READ ORIGINAL TRIANGLE POINTS
        // ----------------------------------------------

        Vector2 localA =
            drawManager.GridPointToLocal(
                faceData.PointA
            );

        Vector2 localB =
            drawManager.GridPointToLocal(
                faceData.PointB
            );

        Vector2 localC =
            drawManager.GridPointToLocal(
                faceData.PointC
            );


        /*
         * Force a common counter-clockwise
         * vertex winding for every flat face.
         *
         * This ensures all initial face normals
         * point in the same local direction.
         */
        EnsureCounterClockwise(
            ref localA,
            ref localB,
            ref localC
        );


        Vector2 centroid =
            (
                localA +
                localB +
                localC
            ) / 3f;


        localA -= centroid;

        localB -= centroid;

        localC -= centroid;


        float pixelSide =
            drawManager.GetTriangleSide();


        float scale =
            sideWorld /
            Mathf.Max(
                0.001f,
                pixelSide
            );


        Vector2 scaledA =
            localA * scale;

        Vector2 scaledB =
            localB * scale;

        Vector2 scaledC =
            localC * scale;


        Mesh mesh =
            CreateTriangleMesh(
                scaledA,
                scaledB,
                scaledC
            );


        filter.mesh =
            mesh;


        collider.sharedMesh =
            mesh;


        // ----------------------------------------------
        // EXACT FLAT-NET WORLD POSITION
        // ----------------------------------------------

        Vector3 startPosition =
            GetTriangleWorldCenter(
                faceData,
                drawManager,
                cam,
                distanceFromCamera
            );


        faceObject.transform.position =
            startPosition;


        /*
         * All four flat triangles start parallel
         * to the Canvas/camera plane.
         */
        faceObject.transform.rotation =
            cam.transform.rotation;


        FaceRuntime runtimeFace =
            new FaceRuntime();


        runtimeFace.index =
            index;


        runtimeFace.data =
            faceData;


        runtimeFace.transform =
            faceObject.transform;


        runtimeFace.points =
            new[]
            {
                faceData.PointA,
                faceData.PointB,
                faceData.PointC
            };


        return runtimeFace;
    }


    // ==================================================
    // FACE GRAPH
    // ==================================================

    private void BuildFaceGraph()
    {
        foreach (FaceRuntime face
                 in runtimeFaces)
        {
            face.neighbours.Clear();
        }


        for (int first = 0;
             first < runtimeFaces.Count - 1;
             first++)
        {
            for (int second =
                     first + 1;
                 second < runtimeFaces.Count;
                 second++)
            {
                Vector2Int sharedA;
                Vector2Int sharedB;


                if (!TryGetSharedEdge(
                        runtimeFaces[first],
                        runtimeFaces[second],
                        out sharedA,
                        out sharedB))
                {
                    continue;
                }


                runtimeFaces[first]
                    .neighbours
                    .Add(second);


                runtimeFaces[second]
                    .neighbours
                    .Add(first);
            }
        }
    }


    private bool IsFaceGraphConnected()
    {
        if (runtimeFaces.Count != 4)
            return false;


        HashSet<int> visited =
            new HashSet<int>();


        Queue<int> queue =
            new Queue<int>();


        queue.Enqueue(0);

        visited.Add(0);


        while (queue.Count > 0)
        {
            int current =
                queue.Dequeue();


            foreach (int neighbour
                     in runtimeFaces[current]
                         .neighbours)
            {
                if (visited.Contains(
                        neighbour))
                {
                    continue;
                }


                visited.Add(
                    neighbour
                );


                queue.Enqueue(
                    neighbour
                );
            }
        }


        return
            visited.Count ==
            runtimeFaces.Count;
    }


    // ==================================================
    // FOLDING SEQUENCE
    // ==================================================

    private IEnumerator FoldingSequence(
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera)
    {
        /*
         * STEP 1:
         * Keep the student's original 2D net
         * visible briefly in world space.
         */
        yield return
            new WaitForSeconds(
                flatHoldTime
            );


        // ----------------------------------------------
        // CHOOSE BEST BASE FACE
        // ----------------------------------------------

        int rootIndex =
            FindBestRootFace();


        rootFaceTransform =
            runtimeFaces[rootIndex]
                .transform;


        /*
         * Base face becomes the top-level face
         * under the final model root.
         */
        rootFaceTransform.SetParent(
            currentModel.transform,
            true
        );


        // ----------------------------------------------
        // BUILD HINGE TREE
        // ----------------------------------------------

        bool hierarchyCreated =
            BuildHingeHierarchy(
                rootIndex,
                drawManager,
                cam,
                distanceFromCamera
            );


        if (!hierarchyCreated ||
            runtimeHinges.Count != 3)
        {
            Debug.LogError(
                $"Tetrahedron requires exactly 3 folding hinges. " +
                $"Created: {runtimeHinges.Count}."
            );

            yield break;
        }


        Debug.Log(
            $"Tetrahedron base face = {rootIndex + 1}. " +
            $"Folding 3 faces at {tetrahedronFoldAngle:F2}°."
        );


        // ----------------------------------------------
        // FOLD ONE HINGE AT A TIME
        // ----------------------------------------------

        foreach (HingeRuntime hinge
                 in runtimeHinges)
        {
            yield return
                StartCoroutine(
                    AnimateHinge(
                        hinge
                    )
                );


            if (delayBetweenFaces > 0f)
            {
                yield return
                    new WaitForSeconds(
                        delayBetweenFaces
                    );
            }
        }


        // ----------------------------------------------
        // FINISHED
        // ----------------------------------------------

        SetupFinalModel();
    }


    // ==================================================
    // SELECT ROOT FACE
    // ==================================================

    private int FindBestRootFace()
    {
        /*
         * Prefer the face with the highest
         * number of direct neighbours.
         *
         * For the common tetrahedron net:
         *
         *        ?
         *      ? ? ?
         *
         * the centre triangle becomes the base.
         */

        int bestIndex = 0;

        int highestDegree = -1;


        for (int index = 0;
             index < runtimeFaces.Count;
             index++)
        {
            int degree =
                runtimeFaces[index]
                    .neighbours.Count;


            if (degree >
                highestDegree)
            {
                highestDegree =
                    degree;

                bestIndex =
                    index;
            }
        }


        return bestIndex;
    }


    // ==================================================
    // BUILD HINGE HIERARCHY
    // ==================================================

    private bool BuildHingeHierarchy(
        int rootIndex,
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera)
    {
        runtimeHinges.Clear();


        HashSet<int> visited =
            new HashSet<int>();


        Queue<int> queue =
            new Queue<int>();


        visited.Add(
            rootIndex
        );


        queue.Enqueue(
            rootIndex
        );


        while (queue.Count > 0)
        {
            int parentIndex =
                queue.Dequeue();


            FaceRuntime parentFace =
                runtimeFaces[
                    parentIndex
                ];


            foreach (int childIndex
                     in parentFace.neighbours)
            {
                if (visited.Contains(
                        childIndex))
                {
                    continue;
                }


                FaceRuntime childFace =
                    runtimeFaces[
                        childIndex
                    ];


                Vector2Int sharedPointA;
                Vector2Int sharedPointB;


                if (!TryGetSharedEdge(
                        parentFace,
                        childFace,
                        out sharedPointA,
                        out sharedPointB))
                {
                    Debug.LogError(
                        $"Could not find shared edge between " +
                        $"faces {parentIndex + 1} and " +
                        $"{childIndex + 1}."
                    );

                    return false;
                }


                HingeRuntime hinge =
                    CreateHinge(
                        parentFace,
                        childFace,
                        sharedPointA,
                        sharedPointB,
                        drawManager,
                        cam,
                        distanceFromCamera
                    );


                if (hinge == null)
                {
                    return false;
                }


                runtimeHinges.Add(
                    hinge
                );


                visited.Add(
                    childIndex
                );


                queue.Enqueue(
                    childIndex
                );
            }
        }


        return
            visited.Count ==
            runtimeFaces.Count;
    }


    // ==================================================
    // CREATE HINGE
    // ==================================================

    private HingeRuntime CreateHinge(
        FaceRuntime parentFace,
        FaceRuntime childFace,
        Vector2Int sharedPointA,
        Vector2Int sharedPointB,
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera)
    {
        Vector3 edgeWorldA =
            GetGridPointWorldPosition(
                sharedPointA,
                drawManager,
                cam,
                distanceFromCamera
            );


        Vector3 edgeWorldB =
            GetGridPointWorldPosition(
                sharedPointB,
                drawManager,
                cam,
                distanceFromCamera
            );


        Vector3 edgeDirection =
            (
                edgeWorldB -
                edgeWorldA
            ).normalized;


        if (edgeDirection.sqrMagnitude <
            0.001f)
        {
            Debug.LogError(
                "Invalid tetrahedron hinge edge."
            );

            return null;
        }


        Vector3 hingeWorldPosition =
            (
                edgeWorldA +
                edgeWorldB
            ) * 0.5f;


        GameObject hingeObject =
            new GameObject(
                $"TetraHinge_" +
                $"{parentFace.index + 1}_" +
                $"{childFace.index + 1}"
            );


        Transform hingeTransform =
            hingeObject.transform;


        hingeTransform.position =
            hingeWorldPosition;


        /*
         * Keep world rotation neutral first.
         * SetParent(true) preserves that
         * world transform.
         */
        hingeTransform.rotation =
            Quaternion.identity;


        hingeTransform.SetParent(
            parentFace.transform,
            true
        );


        /*
         * Child triangle becomes a child
         * of its hinge.
         *
         * Its world position/rotation does not
         * change yet.
         */
        childFace.transform.SetParent(
            hingeTransform,
            true
        );


        // ----------------------------------------------
        // DETERMINE CORRECT ±109.47° DIRECTION
        // ----------------------------------------------

        float signedFoldAngle =
            DetermineSignedFoldAngle(
                parentFace,
                childFace,
                hingeWorldPosition,
                edgeDirection
            );


        /*
         * Hinge's local rotation is relative
         * to its parent triangle.
         *
         * Convert the world shared-edge axis
         * into parent-face local coordinates.
         */
        Vector3 axisInParent =
            parentFace.transform
                .InverseTransformDirection(
                    edgeDirection
                )
                .normalized;


        Quaternion startLocalRotation =
            hingeTransform.localRotation;


        Quaternion targetLocalRotation =
            Quaternion.AngleAxis(
                signedFoldAngle,
                axisInParent
            )
            *
            startLocalRotation;


        HingeRuntime result =
            new HingeRuntime();


        result.hinge =
            hingeTransform;


        result.parentFaceIndex =
            parentFace.index;


        result.childFaceIndex =
            childFace.index;


        result.sharedPointA =
            sharedPointA;


        result.sharedPointB =
            sharedPointB;


        result.startLocalRotation =
            startLocalRotation;


        result.targetLocalRotation =
            targetLocalRotation;


        Debug.Log(
            $"Hinge created: " +
            $"Face {parentFace.index + 1} -> " +
            $"Face {childFace.index + 1}, " +
            $"Angle = {signedFoldAngle:F2}°"
        );


        return result;
    }


    // ==================================================
    // DETERMINE FOLD SIGN
    // ==================================================

    private float DetermineSignedFoldAngle(
        FaceRuntime parentFace,
        FaceRuntime childFace,
        Vector3 hingeWorldPosition,
        Vector3 edgeWorldDirection)
    {
        /*
         * Try both:
         *
         * +109.47°
         * -109.47°
         *
         * and choose the one that moves the
         * child triangle to the requested side
         * of its parent face.
         */

        Vector3 childVector =
            childFace.transform.position -
            hingeWorldPosition;


        Vector3 plusResult =
            Quaternion.AngleAxis(
                tetrahedronFoldAngle,
                edgeWorldDirection
            )
            *
            childVector;


        Vector3 minusResult =
            Quaternion.AngleAxis(
                -tetrahedronFoldAngle,
                edgeWorldDirection
            )
            *
            childVector;


        Vector3 parentNormal =
            parentFace.transform.forward;


        Vector3 desiredDirection =
            foldTowardCamera
                ? -parentNormal
                : parentNormal;


        float plusScore =
            Vector3.Dot(
                plusResult,
                desiredDirection
            );


        float minusScore =
            Vector3.Dot(
                minusResult,
                desiredDirection
            );


        if (plusScore >=
            minusScore)
        {
            return
                tetrahedronFoldAngle;
        }


        return
            -tetrahedronFoldAngle;
    }


    // ==================================================
    // HINGE ANIMATION
    // ==================================================

    private IEnumerator AnimateHinge(
        HingeRuntime hinge)
    {
        if (hinge == null ||
            hinge.hinge == null)
        {
            yield break;
        }


        float timer =
            0f;


        while (timer <
               foldDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    foldDuration
                );


            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            hinge.hinge.localRotation =
                Quaternion.Slerp(
                    hinge.startLocalRotation,
                    hinge.targetLocalRotation,
                    smooth
                );


            yield return null;
        }


        hinge.hinge.localRotation =
            hinge.targetLocalRotation;


        Debug.Log(
            $"Fold completed: " +
            $"Face {hinge.childFaceIndex + 1} " +
            $"around Face {hinge.parentFaceIndex + 1} hinge."
        );
    }


    // ==================================================
    // FINAL MODEL
    // ==================================================

    private void SetupFinalModel()
    {
        if (currentModel == null ||
            rootFaceTransform == null)
        {
            return;
        }


        // ----------------------------------------------
        // RECENTER PIVOT
        // ----------------------------------------------

        Vector3 center =
            CalculateFinalFaceCenter();


        /*
         * Preserve the actual tetrahedron world
         * position while moving the model pivot
         * to the centre.
         */
        Vector3 rootWorldPosition =
            rootFaceTransform.position;


        Quaternion rootWorldRotation =
            rootFaceTransform.rotation;


        currentModel.transform.position =
            center;


        rootFaceTransform.position =
            rootWorldPosition;


        rootFaceTransform.rotation =
            rootWorldRotation;


        // ----------------------------------------------
        // PREVIEW ORIENTATION
        // ----------------------------------------------

        currentModel.transform.Rotate(
            previewTiltX,
            previewTurnY,
            0f,
            Space.Self
        );


        // ----------------------------------------------
        // TOUCH ROTATE / ZOOM
        // ----------------------------------------------

        TouchRotateZoom rotateZoom;


        if (!currentModel
            .TryGetComponent<
                TouchRotateZoom>(
                    out rotateZoom))
        {
            rotateZoom =
                currentModel
                    .AddComponent<
                        TouchRotateZoom>();
        }


        rotateZoom.target =
            currentModel.transform;


        Debug.Log(
            "Tetrahedron folding completed successfully " +
            $"using {tetrahedronFoldAngle:F2}° hinge rotations."
        );
    }


    private Vector3 CalculateFinalFaceCenter()
    {
        Vector3 center =
            Vector3.zero;


        int validCount =
            0;


        foreach (FaceRuntime face
                 in runtimeFaces)
        {
            if (face == null ||
                face.transform == null)
            {
                continue;
            }


            center +=
                face.transform.position;


            validCount++;
        }


        if (validCount == 0)
        {
            return
                Vector3.zero;
        }


        return
            center /
            validCount;
    }


    // ==================================================
    // SHARED EDGE
    // ==================================================

    private bool TryGetSharedEdge(
        FaceRuntime first,
        FaceRuntime second,
        out Vector2Int sharedA,
        out Vector2Int sharedB)
    {
        sharedA =
            Vector2Int.zero;


        sharedB =
            Vector2Int.zero;


        List<Vector2Int> sharedPoints =
            new List<Vector2Int>();


        foreach (Vector2Int firstPoint
                 in first.points)
        {
            foreach (Vector2Int secondPoint
                     in second.points)
            {
                if (firstPoint !=
                    secondPoint)
                {
                    continue;
                }


                if (!sharedPoints.Contains(
                        firstPoint))
                {
                    sharedPoints.Add(
                        firstPoint
                    );
                }
            }
        }


        if (sharedPoints.Count != 2)
        {
            return false;
        }


        sharedA =
            sharedPoints[0];


        sharedB =
            sharedPoints[1];


        return true;
    }


    // ==================================================
    // GRID POINT -> WORLD
    // ==================================================

    private Vector3 GetGridPointWorldPosition(
        Vector2Int gridPoint,
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera)
    {
        Vector2 localPoint =
            drawManager.GridPointToLocal(
                gridPoint
            );


        RectTransform drawingArea =
            drawManager.GetDrawingArea();


        Vector3 uiWorld =
            drawingArea.TransformPoint(
                localPoint
            );


        Vector2 screenPoint =
            RectTransformUtility
                .WorldToScreenPoint(
                    null,
                    uiWorld
                );


        return
            cam.ScreenToWorldPoint(
                new Vector3(
                    screenPoint.x,
                    screenPoint.y,
                    distanceFromCamera
                )
            );
    }


    // ==================================================
    // WORLD TRIANGLE SIDE
    // ==================================================

    private float GetWorldTriangleSide(
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera)
    {
        Vector3 worldA =
            GetGridPointWorldPosition(
                new Vector2Int(
                    0,
                    0
                ),
                drawManager,
                cam,
                distanceFromCamera
            );


        Vector3 worldB =
            GetGridPointWorldPosition(
                new Vector2Int(
                    1,
                    0
                ),
                drawManager,
                cam,
                distanceFromCamera
            );


        return
            Vector3.Distance(
                worldA,
                worldB
            );
    }


    // ==================================================
    // TRIANGLE WORLD CENTER
    // ==================================================

    private Vector3 GetTriangleWorldCenter(
        TetrahedronTriangleFace face,
        TetrahedronGridDrawManager drawManager,
        Camera cam,
        float distanceFromCamera)
    {
        Vector3 worldA =
            GetGridPointWorldPosition(
                face.PointA,
                drawManager,
                cam,
                distanceFromCamera
            );


        Vector3 worldB =
            GetGridPointWorldPosition(
                face.PointB,
                drawManager,
                cam,
                distanceFromCamera
            );


        Vector3 worldC =
            GetGridPointWorldPosition(
                face.PointC,
                drawManager,
                cam,
                distanceFromCamera
            );


        return
            (
                worldA +
                worldB +
                worldC
            ) / 3f;
    }


    // ==================================================
    // MESH
    // ==================================================

    private Mesh CreateTriangleMesh(
        Vector2 pointA,
        Vector2 pointB,
        Vector2 pointC)
    {
        Mesh mesh =
            new Mesh();


        Vector3[] vertices =
        {
            new Vector3(
                pointA.x,
                pointA.y,
                0f
            ),

            new Vector3(
                pointB.x,
                pointB.y,
                0f
            ),

            new Vector3(
                pointC.x,
                pointC.y,
                0f
            )
        };


        /*
         * Double-sided face.
         *
         * Useful during folding,
         * preview and AR interaction.
         */
        int[] triangles =
        {
            0, 1, 2,
            2, 1, 0
        };


        mesh.vertices =
            vertices;


        mesh.triangles =
            triangles;


        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        return mesh;
    }


    // ==================================================
    // WINDING
    // ==================================================

    private void EnsureCounterClockwise(
        ref Vector2 pointA,
        ref Vector2 pointB,
        ref Vector2 pointC)
    {
        float signedArea =
            (
                pointB.x -
                pointA.x
            )
            *
            (
                pointC.y -
                pointA.y
            )
            -
            (
                pointB.y -
                pointA.y
            )
            *
            (
                pointC.x -
                pointA.x
            );


        if (signedArea >= 0f)
            return;


        Vector2 temporary =
            pointB;


        pointB =
            pointC;


        pointC =
            temporary;
    }


    // ==================================================
    // CLEAR
    // ==================================================

    public void ClearModel()
    {
        StopAllCoroutines();


        runtimeFaces.Clear();

        runtimeHinges.Clear();

        rootFaceTransform =
            null;


        if (currentModel != null)
        {
            Destroy(
                currentModel
            );


            currentModel =
                null;
        }
    }


    // ==================================================
    // GET MODEL
    // ==================================================

    public Transform GetCurrentModelRoot()
    {
        if (currentModel == null)
        {
            return null;
        }


        return
            currentModel.transform;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class C2_ARPlacementManager : MonoBehaviour
{
    // =========================================================
    // SHAPES
    // =========================================================

    public enum C2_ShapeType
    {
        None,
        Cube,
        Cuboid,
        Prism,
        Tetrahedron,
        Cylinder
    }


    // =========================================================
    // AR REFERENCES
    // =========================================================

    [Header("AR References")]

    [SerializeField]
    private ARRaycastManager raycastManager;


    [SerializeField]
    private ARPlaneManager planeManager;


    // =========================================================
    // SHAPE PREFABS
    // =========================================================

    [Header("Shape Prefabs")]

    [SerializeField]
    private GameObject cubeExperiencePrefab;


    [SerializeField]
    private GameObject cuboidExperiencePrefab;


    [SerializeField]
    private GameObject prismExperiencePrefab;


    [SerializeField]
    private GameObject tetrahedronExperiencePrefab;


    [SerializeField]
    private GameObject cylinderExperiencePrefab;


    // =========================================================
    // SHARED PREFABS
    // =========================================================

    [Header("Shared Prefabs")]

    [SerializeField]
    private GameObject physicsFloorPrefab;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private C2_AppUIManager uiManager;


    // =========================================================
    // PLACEMENT
    // =========================================================

    [Header("Placement")]

    [SerializeField]
    private bool allowOnlyOnePlacement =
        true;


    [SerializeField]
    private C2_ShapeType selectedShape =
        C2_ShapeType.None;


    // =========================================================
    // RUNTIME OBJECTS
    // =========================================================

    private GameObject placedExperience;

    private GameObject placedPhysicsFloor;


    // =========================================================
    // ACTIVE SHAPE MANAGERS
    // =========================================================

    private C2_CubeSlicingManager
        activeCubeManager;


    private C2_CuboidSlicingManager
        activeCuboidManager;


    private C2_PrismSlicingManager
        activePrismManager;


    private C2_TetrahedronSlicingManager
        activeTetrahedronManager;


    private C2_CylinderSlicingManager
        activeCylinderManager;


    // =========================================================
    // RAYCAST
    // =========================================================

    private static readonly
        List<ARRaycastHit>
        raycastHits =
            new List<ARRaycastHit>();


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool ShapePlaced
        => placedExperience != null;


    // Compatibility with older Cube-only UI code.
    public bool CubePlaced
        => ShapePlaced;


    public C2_ShapeType SelectedShape
        => selectedShape;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (
            raycastManager == null ||
            uiManager == null
        )
        {
            return;
        }


        if (
            uiManager.CurrentState !=
            C2_AppUIManager.C2_UIState.Placement
        )
        {
            return;
        }


        if (
            selectedShape ==
            C2_ShapeType.None
        )
        {
            return;
        }


        if (
            allowOnlyOnePlacement &&
            ShapePlaced
        )
        {
            return;
        }


        HandlePlacementTouch();
    }


    // =========================================================
    // TOUCH PLACEMENT
    // =========================================================

    private void HandlePlacementTouch()
    {
        if (Input.touchCount <= 0)
        {
            return;
        }


        Touch touch =
            Input.GetTouch(0);


        if (
            touch.phase !=
            TouchPhase.Began
        )
        {
            return;
        }


        // Do not place a shape when the user
        // is touching a UI button.

        if (
            EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject(
                    touch.fingerId
                )
        )
        {
            return;
        }


        raycastHits.Clear();


        bool hitPlane =
            raycastManager.Raycast(
                touch.position,
                raycastHits,
                TrackableType.PlaneWithinPolygon
            );


        if (
            !hitPlane ||
            raycastHits.Count == 0
        )
        {
            return;
        }


        Pose hitPose =
            raycastHits[0].pose;


        PlaceSelectedShape(
            hitPose
        );
    }


    // =========================================================
    // SELECT CUBE
    // =========================================================

    public void SelectCubeShape()
    {
        if (ShapePlaced)
        {
            return;
        }


        selectedShape =
            C2_ShapeType.Cube;


        if (uiManager != null)
        {
            uiManager.SetShapeSelection(
                "Cube"
            );


            uiManager.ShowPlacementPanel();
        }


        Debug.Log(
            "C2 AR: Cube selected."
        );
    }


    // =========================================================
    // SELECT CUBOID
    // =========================================================

    public void SelectCuboidShape()
    {
        if (ShapePlaced)
        {
            return;
        }


        selectedShape =
            C2_ShapeType.Cuboid;


        if (uiManager != null)
        {
            uiManager.SetShapeSelection(
                "Cuboid"
            );


            uiManager.ShowPlacementPanel();
        }


        Debug.Log(
            "C2 AR: Cuboid selected."
        );
    }


    // =========================================================
    // SELECT PRISM
    // =========================================================

    public void SelectPrismShape()
    {
        if (ShapePlaced)
        {
            return;
        }


        selectedShape =
            C2_ShapeType.Prism;


        if (uiManager != null)
        {
            uiManager.SetShapeSelection(
                "Prism"
            );


            uiManager.ShowPlacementPanel();
        }


        Debug.Log(
            "C2 AR: Prism selected."
        );
    }


    // =========================================================
    // SELECT TETRAHEDRON
    // =========================================================

    public void SelectTetrahedronShape()
    {
        if (ShapePlaced)
        {
            return;
        }


        selectedShape =
            C2_ShapeType.Tetrahedron;


        if (uiManager != null)
        {
            uiManager.SetShapeSelection(
                "Tetrahedron"
            );


            uiManager.ShowPlacementPanel();
        }


        Debug.Log(
            "C2 AR: Tetrahedron selected."
        );
    }


    // =========================================================
    // SELECT CYLINDER
    // =========================================================

    public void SelectCylinderShape()
    {
        if (ShapePlaced)
        {
            return;
        }


        selectedShape =
            C2_ShapeType.Cylinder;


        if (uiManager != null)
        {
            uiManager.SetShapeSelection(
                "Cylinder"
            );


            uiManager.ShowPlacementPanel();
        }


        Debug.Log(
            "C2 AR: Cylinder selected."
        );
    }


    // =========================================================
    // RETURN TO SHAPE SELECTION
    // =========================================================

    public void ReturnToShapeSelection()
    {
        if (ShapePlaced)
        {
            return;
        }


        selectedShape =
            C2_ShapeType.None;


        if (uiManager != null)
        {
            uiManager.ClearShapeSelection();

            uiManager.ClearSliceSelection();

            uiManager.ShowShapeSelectionPanel();
        }


        Debug.Log(
            "C2 AR: Returned to Shape Selection."
        );
    }


    // =========================================================
    // PLACE SELECTED SHAPE
    // =========================================================

    private void PlaceSelectedShape(
        Pose hitPose
    )
    {
        if (ShapePlaced)
        {
            return;
        }


        GameObject selectedPrefab =
            GetSelectedShapePrefab();


        if (selectedPrefab == null)
        {
            Debug.LogError(
                "C2 AR: Selected shape prefab is missing."
            );

            return;
        }


        // =====================================================
        // CREATE SELECTED EXPERIENCE
        // =====================================================

        placedExperience =
            Instantiate(
                selectedPrefab,
                hitPose.position,
                hitPose.rotation
            );


        // =====================================================
        // CREATE SHARED PHYSICS FLOOR
        // =====================================================

        if (physicsFloorPrefab != null)
        {
            placedPhysicsFloor =
                Instantiate(
                    physicsFloorPrefab,
                    hitPose.position,
                    hitPose.rotation
                );
        }


        // =====================================================
        // FIND THE MANAGER BELONGING TO THE SPAWNED SHAPE
        // =====================================================

        activeCubeManager =
            placedExperience
                .GetComponent<
                    C2_CubeSlicingManager
                >();


        activeCuboidManager =
            placedExperience
                .GetComponent<
                    C2_CuboidSlicingManager
                >();


        activePrismManager =
            placedExperience
                .GetComponent<
                    C2_PrismSlicingManager
                >();


        activeTetrahedronManager =
            placedExperience
                .GetComponent<
                    C2_TetrahedronSlicingManager
                >();


        activeCylinderManager =
            placedExperience
                .GetComponent<
                    C2_CylinderSlicingManager
                >();


        // =====================================================
        // INJECT SHARED UI
        // =====================================================

        if (activeCubeManager != null)
        {
            activeCubeManager.SetUIManager(
                uiManager
            );
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SetUIManager(
                uiManager
            );
        }


        if (activePrismManager != null)
        {
            activePrismManager.SetUIManager(
                uiManager
            );
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SetUIManager(
                uiManager
            );
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SetUIManager(
                uiManager
            );
        }


        // =====================================================
        // HIDE AR PLANE VISUALS
        // =====================================================

        HideARPlanes();


        // =====================================================
        // OPEN SLICING PANEL
        // =====================================================

        if (uiManager != null)
        {
            uiManager.ClearSliceSelection();

            uiManager.ShowSlicingPanel();
        }


        Debug.Log(
            "C2 AR: Placed shape = " +
            selectedShape
        );
    }


    // =========================================================
    // GET SELECTED PREFAB
    // =========================================================

    private GameObject GetSelectedShapePrefab()
    {
        switch (selectedShape)
        {
            case C2_ShapeType.Cube:

                return
                    cubeExperiencePrefab;


            case C2_ShapeType.Cuboid:

                return
                    cuboidExperiencePrefab;


            case C2_ShapeType.Prism:

                return
                    prismExperiencePrefab;


            case C2_ShapeType.Tetrahedron:

                return
                    tetrahedronExperiencePrefab;


            case C2_ShapeType.Cylinder:

                return
                    cylinderExperiencePrefab;


            default:

                return null;
        }
    }


    // =========================================================
    // DIRECTION — VERTICAL
    // =========================================================

    public void SelectVerticalSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectVertical();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectVertical();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectVertical();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectVertical();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectVertical();
        }


        if (uiManager != null)
        {
            uiManager.SetDirectionSelection(
                "Vertical"
            );
        }
    }


    // =========================================================
    // DIRECTION — HORIZONTAL
    // =========================================================

    public void SelectHorizontalSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectHorizontal();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectHorizontal();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectHorizontal();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectHorizontal();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectHorizontal();
        }


        if (uiManager != null)
        {
            uiManager.SetDirectionSelection(
                "Horizontal"
            );
        }
    }


    // =========================================================
    // DIRECTION — ANGLED
    // =========================================================

    public void SelectAngledSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectAngled();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectAngled();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectAngled();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectAngled();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectAngled();
        }


        if (uiManager != null)
        {
            uiManager.SetDirectionSelection(
                "Angled"
            );
        }
    }


    // =========================================================
    // FRACTION — 1/2
    // =========================================================

    public void SelectHalfSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectHalf();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectHalf();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectHalf();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectHalf();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectHalf();
        }


        if (uiManager != null)
        {
            uiManager.SetFractionSelection(
                "1/2"
            );
        }
    }


    // =========================================================
    // FRACTION — 1/3
    // =========================================================

    public void SelectThirdSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectThird();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectThird();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectThird();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectThird();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectThird();
        }


        if (uiManager != null)
        {
            uiManager.SetFractionSelection(
                "1/3"
            );
        }
    }


    // =========================================================
    // FRACTION — 1/4
    // =========================================================

    public void SelectQuarterSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectQuarter();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectQuarter();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectQuarter();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectQuarter();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectQuarter();
        }


        if (uiManager != null)
        {
            uiManager.SetFractionSelection(
                "1/4"
            );
        }
    }


    // =========================================================
    // FRACTION — 3/4
    // =========================================================

    public void SelectThreeQuarterSlice()
    {
        if (!ShapePlaced)
        {
            return;
        }


        if (activeCubeManager != null)
        {
            activeCubeManager.SelectThreeQuarter();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.SelectThreeQuarter();
        }


        if (activePrismManager != null)
        {
            activePrismManager.SelectThreeQuarter();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.SelectThreeQuarter();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.SelectThreeQuarter();
        }


        if (uiManager != null)
        {
            uiManager.SetFractionSelection(
                "3/4"
            );
        }
    }


    // =========================================================
    // TRY SAME SLICE
    // =========================================================

    public void TryAgainCurrentSlice()
    {
        if (activeCubeManager != null)
        {
            activeCubeManager.TryAgainCurrentSlice();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.TryAgainCurrentSlice();
        }


        if (activePrismManager != null)
        {
            activePrismManager.TryAgainCurrentSlice();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.TryAgainCurrentSlice();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.TryAgainCurrentSlice();
        }


        if (uiManager != null)
        {
            uiManager.ShowSlicingPanel();
        }
    }


    // =========================================================
    // RESET CURRENT SHAPE / CLEAR SLICE CHOICES
    // =========================================================

    public void ResetCurrentShape()
    {
        if (activeCubeManager != null)
        {
            activeCubeManager.ResetCube();
        }


        if (activeCuboidManager != null)
        {
            activeCuboidManager.ResetCuboid();
        }


        if (activePrismManager != null)
        {
            activePrismManager.ResetPrism();
        }


        if (activeTetrahedronManager != null)
        {
            activeTetrahedronManager.ResetTetrahedron();
        }


        if (activeCylinderManager != null)
        {
            activeCylinderManager.ResetCylinder();
        }


        if (uiManager != null)
        {
            uiManager.ClearSliceSelection();

            uiManager.ShowSlicingPanel();
        }


        Debug.Log(
            "C2 AR: Current shape choices reset."
        );
    }


    // =========================================================
    // OLD COMPATIBILITY METHOD
    // =========================================================

    public void ResetCurrentCube()
    {
        ResetCurrentShape();
    }


    // =========================================================
    // FULL AR RESTART
    // =========================================================

    public void ResetPlacement()
    {
        if (placedExperience != null)
        {
            Destroy(
                placedExperience
            );
        }


        if (placedPhysicsFloor != null)
        {
            Destroy(
                placedPhysicsFloor
            );
        }


        placedExperience =
            null;


        placedPhysicsFloor =
            null;


        activeCubeManager =
            null;


        activeCuboidManager =
            null;


        activePrismManager =
            null;


        activeTetrahedronManager =
            null;


        activeCylinderManager =
            null;


        selectedShape =
            C2_ShapeType.None;


        ShowARPlanes();


        if (uiManager != null)
        {
            uiManager.ClearShapeSelection();

            uiManager.ClearSliceSelection();

            uiManager.ShowScanPanel();
        }


        Debug.Log(
            "C2 AR: Placement reset."
        );
    }


    // =========================================================
    // HIDE PLANES
    // =========================================================

    private void HideARPlanes()
    {
        if (planeManager == null)
        {
            return;
        }


        foreach (
            ARPlane plane in
            planeManager.trackables
        )
        {
            plane.gameObject.SetActive(
                false
            );
        }


        planeManager.enabled =
            false;
    }


    // =========================================================
    // SHOW PLANES
    // =========================================================

    private void ShowARPlanes()
    {
        if (planeManager == null)
        {
            return;
        }


        planeManager.enabled =
            true;


        foreach (
            ARPlane plane in
            planeManager.trackables
        )
        {
            plane.gameObject.SetActive(
                true
            );
        }
    }
}
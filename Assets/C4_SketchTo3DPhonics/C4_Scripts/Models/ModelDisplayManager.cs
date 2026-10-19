using UnityEngine;

public class ModelDisplayManager : MonoBehaviour
{
    [Header("Preview Position")]
    [SerializeField] private Transform modelPreviewPoint;

    // =========================================================
    // APPLE
    // =========================================================

    [Header("Apple Prefabs")]
    [SerializeField] private GameObject appleLowOpacityPrefab;
    [SerializeField] private GameObject appleMediumOpacityPrefab;
    [SerializeField] private GameObject appleFullPrefab;

    // =========================================================
    // CAT
    // =========================================================

    [Header("Cat Prefabs")]
    [SerializeField] private GameObject catLowOpacityPrefab;
    [SerializeField] private GameObject catMediumOpacityPrefab;
    [SerializeField] private GameObject catFullPrefab;

    // =========================================================
    // FISH
    // =========================================================

    [Header("Fish Prefabs")]
    [SerializeField] private GameObject fishLowOpacityPrefab;
    [SerializeField] private GameObject fishMediumOpacityPrefab;
    [SerializeField] private GameObject fishFullPrefab;

    // =========================================================
    // TREE
    // =========================================================

    [Header("Tree Prefabs")]
    [SerializeField] private GameObject treeLowOpacityPrefab;
    [SerializeField] private GameObject treeMediumOpacityPrefab;
    [SerializeField] private GameObject treeFullPrefab;

    // =========================================================
    // SUN
    // =========================================================

    [Header("Sun Prefabs")]
    [SerializeField] private GameObject sunLowOpacityPrefab;
    [SerializeField] private GameObject sunMediumOpacityPrefab;
    [SerializeField] private GameObject sunFullPrefab;

    // =========================================================
    // SCALE
    // =========================================================

    [Header("Sketch-Based Scaling")]
    [SerializeField] private float minimumModelScale = 0.3f;
    [SerializeField] private float maximumModelScale = 1.2f;
    [SerializeField] private float scaleMultiplier = 1f;

    [Header("Rotation")]
    [SerializeField]
    private Vector3 modelRotation =
        new Vector3(0f, 180f, 0f);

    private GameObject currentModel;

    private string currentRecognizedLabel = "";

    private float currentSketchSizeRatio =
        0.5f;

    public GameObject CurrentModel =>
        currentModel;

    // =========================================================
    // INITIAL MODEL
    // =========================================================

    public void DisplayInitialModel(
        string recognizedLabel,
        float sketchSizeRatio)
    {
        if (string.IsNullOrWhiteSpace(
                recognizedLabel))
        {
            Debug.LogError(
                "Recognized label is empty."
            );

            return;
        }

        currentRecognizedLabel =
            recognizedLabel
                .Trim()
                .ToLower();

        currentSketchSizeRatio =
            Mathf.Clamp01(
                sketchSizeRatio
            );

        SetProgressStage(0);
    }

    // =========================================================
    // OPACITY PROGRESS
    // =========================================================

    public void SetProgressStage(
        int completedTaskCount)
    {
        completedTaskCount =
            Mathf.Clamp(
                completedTaskCount,
                0,
                2
            );

        GameObject selectedPrefab =
            GetPrefabForStage(
                currentRecognizedLabel,
                completedTaskCount
            );

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "No model prefab found for label: " +
                currentRecognizedLabel +
                " at stage: " +
                completedTaskCount
            );

            return;
        }

        SpawnModel(
            selectedPrefab
        );

        Debug.Log(
            "Model opacity stage changed. " +
            "Object: " +
            currentRecognizedLabel +
            " | Tasks completed: " +
            completedTaskCount
        );
    }

    // =========================================================
    // SELECT MODEL
    // =========================================================

    private GameObject GetPrefabForStage(
        string label,
        int taskCount)
    {
        switch (label)
        {
            // -------------------------------------------------
            // APPLE
            // -------------------------------------------------

            case "apple":

                if (taskCount == 0)
                    return appleLowOpacityPrefab;

                if (taskCount == 1)
                    return appleMediumOpacityPrefab;

                return appleFullPrefab;

            // -------------------------------------------------
            // CAT
            // -------------------------------------------------

            case "cat":

                if (taskCount == 0)
                    return catLowOpacityPrefab;

                if (taskCount == 1)
                    return catMediumOpacityPrefab;

                return catFullPrefab;

            // -------------------------------------------------
            // FISH
            // -------------------------------------------------

            case "fish":

                if (taskCount == 0)
                    return fishLowOpacityPrefab;

                if (taskCount == 1)
                    return fishMediumOpacityPrefab;

                return fishFullPrefab;

            // -------------------------------------------------
            // TREE
            // -------------------------------------------------

            case "tree":

                if (taskCount == 0)
                    return treeLowOpacityPrefab;

                if (taskCount == 1)
                    return treeMediumOpacityPrefab;

                return treeFullPrefab;

            // -------------------------------------------------
            // SUN
            // -------------------------------------------------

            case "sun":

                if (taskCount == 0)
                    return sunLowOpacityPrefab;

                if (taskCount == 1)
                    return sunMediumOpacityPrefab;

                return sunFullPrefab;

            default:

                Debug.LogWarning(
                    "Unsupported recognized label: " +
                    label
                );

                return null;
        }
    }

    // =========================================================
    // SPAWN
    // =========================================================

    private void SpawnModel(
        GameObject prefab)
    {
        if (modelPreviewPoint == null)
        {
            Debug.LogError(
                "ModelPreviewPoint is not assigned."
            );

            return;
        }

        if (prefab == null)
        {
            Debug.LogError(
                "Selected model prefab is null."
            );

            return;
        }

        DestroyCurrentModel();

        currentModel =
            Instantiate(
                prefab,
                modelPreviewPoint
            );

        currentModel.transform.localPosition =
            Vector3.zero;

        currentModel.transform.localRotation =
            Quaternion.Euler(
                modelRotation
            );

        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        if (
            currentModel.GetComponent<
                ModelRotate>() == null
        )
        {
            currentModel.AddComponent<
                ModelRotate>();
        }

        ApplySketchBasedScale();
    }

    // =========================================================
    // SKETCH-BASED SCALE
    // =========================================================

    private void ApplySketchBasedScale()
    {
        if (currentModel == null)
        {
            return;
        }

        float calculatedScale =
            Mathf.Lerp(
                minimumModelScale,
                maximumModelScale,
                currentSketchSizeRatio
            );

        calculatedScale *=
            scaleMultiplier;

        currentModel.transform.localScale =
            Vector3.one *
            calculatedScale;

        Debug.Log(
            "Recognized object: " +
            currentRecognizedLabel
        );

        Debug.Log(
            "Sketch size ratio: " +
            currentSketchSizeRatio
                .ToString("F2")
        );

        Debug.Log(
            "Applied model scale: " +
            calculatedScale
                .ToString("F2")
        );
    }

    // =========================================================
    // DESTROY CURRENT MODEL
    // =========================================================

    public void DestroyCurrentModel()
    {
        if (currentModel == null)
        {
            return;
        }

        Destroy(
            currentModel
        );

        currentModel = null;
    }
}
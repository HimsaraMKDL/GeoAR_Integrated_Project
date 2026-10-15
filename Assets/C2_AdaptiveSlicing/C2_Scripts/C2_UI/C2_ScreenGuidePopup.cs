using UnityEngine;

/// <summary>
/// Displays a PNG-based guide overlay each time its parent screen opens.
/// Add this component to C2_ScanPanel and C2_SlicingPanel.
/// Does not change AR, slicing, login, or research logging scripts.
/// </summary>
[DisallowMultipleComponent]
public sealed class C2_ScreenGuidePopup : MonoBehaviour
{
    [Header("Popup Overlay Under This Screen")]
    [SerializeField] private GameObject popupOverlay;

    [Header("Display Behaviour")]
    [SerializeField] private bool showEveryTimeScreenOpens = true;

    private bool hasShownThisRun;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (popupOverlay == null)
        {
            Debug.LogWarning(
                "C2 Guide Popup: Popup Overlay is not assigned on " +
                gameObject.name + ".",
                this);
            return;
        }

        bool shouldShow = showEveryTimeScreenOpens || !hasShownThisRun;
        popupOverlay.SetActive(shouldShow);

        if (shouldShow)
        {
            hasShownThisRun = true;
        }
    }

    /// <summary>
    /// Assign this method to the popup's invisible OK / X buttons.
    /// The overlay is fully deactivated so it cannot block AR touch tracing.
    /// </summary>
    public void ClosePopup()
    {
        if (popupOverlay != null)
        {
            popupOverlay.SetActive(false);
        }
    }
}

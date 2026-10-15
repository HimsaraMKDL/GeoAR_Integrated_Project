
using UnityEngine;

[DisallowMultipleComponent]
public class C2_ResultSuccessAudio : MonoBehaviour
{
    // =====================================================
    // SUCCESS AUDIO REFERENCE
    // =====================================================

    [Header("Success Audio")]

    [Tooltip("Assign the AudioSource from C2_SliceSuccessAudio.")]
    [SerializeField]
    private AudioSource successAudioSource;


    // =====================================================
    // PLAY WHEN RESULT PANEL BECOMES ACTIVE
    // =====================================================

    private void OnEnable()
    {
        if (successAudioSource == null)
        {
            Debug.LogWarning(
                "C2 Audio: Success AudioSource is not assigned."
            );

            return;
        }

        if (successAudioSource.clip == null)
        {
            Debug.LogWarning(
                "C2 Audio: Success audio clip is missing."
            );

            return;
        }

        // Play once every time the Result Panel opens.
        // Playback occurs through the separate manager
        // so the sound is not stopped by UI changes.

        successAudioSource.PlayOneShot(
            successAudioSource.clip
        );

        Debug.Log(
            "C2 Audio: Slice success chime played."
        );
    }
}

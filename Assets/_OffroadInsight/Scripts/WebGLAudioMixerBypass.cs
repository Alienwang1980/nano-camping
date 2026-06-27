using UnityEngine;

/// <summary>
/// WebGL: Bypasses the AudioMixer by setting all AudioSource output to null.
/// The NWH VehicleAudioMixer doesn't work in WebGL (DSP effects unsupported),
/// causing all mixer-routed sounds (engine, transmission, surface noise) to be silent.
/// Non-mixer sounds (SuspensionBump, crash) work fine.
/// </summary>
public class WebGLAudioMixerBypass : MonoBehaviour
{
    void Start()
    {
#if UNITY_WEBGL
        // NWH initializes audio in VC_Initialize, which runs in Start() or later.
        // Delay our fix to run after NWH setup.
        Invoke(nameof(BypassMixer), 4f);
        InvokeRepeating(nameof(BypassMixer), 8f, 5f);
#endif
    }

    void BypassMixer()
    {
        var sources = FindObjectsOfType<AudioSource>(true);
        int count = 0;
        foreach (var src in sources)
        {
            if (src.outputAudioMixerGroup != null)
            {
                src.outputAudioMixerGroup = null;
                count++;
            }
        }
        if (count > 0)
            Debug.Log($"[WebGLAudioMixerBypass] Bypassed mixer on {count} AudioSources");
    }
}

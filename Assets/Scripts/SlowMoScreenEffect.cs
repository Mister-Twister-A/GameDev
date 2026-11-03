using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class SlowMoScreenEffect : MonoBehaviour
{
    public static SlowMoScreenEffect Instance { get; private set; }

    private Volume volume;

    private void Awake()
    {
        Instance = this;
        volume = GetComponent<Volume>();
        volume.weight = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    public static void SetIntensity(float value)
    {
        if (Instance != null) Instance.volume.weight = Mathf.Clamp01(value);
    }
}
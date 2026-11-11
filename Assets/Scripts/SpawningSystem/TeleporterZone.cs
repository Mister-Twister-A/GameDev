using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

public class TeleporterZone : MonoBehaviour
{
    [SerializeField] private float radius = 15f;               
    [SerializeField] private float chargeDuration = 40f;       
    [Header("Visuals (optional)")]
    [SerializeField] private Transform radiusVisual;
    [SerializeField] private float chargeProgress;

    [Header("UI hook")]
    public UnityEvent<float> onProgress;           

    public event Action FullyCharged;

    public bool IsActive { get; private set; }
    public bool IsCharged { get; private set; }
    public float Progress { get; private set; }
    public bool PlayerInside { get; private set; }

    private Transform player;

    public void Configure(float duration) => chargeDuration = Mathf.Max(1f, duration);

    void Awake() => SetVisual(false);

    public void Activate()
    {
        if (IsActive) return;
        chargeProgress = 0f;
        IsActive = true;
        SetVisual(true);
    }

    void Update()
    {
        if (!IsActive || IsCharged) return;

        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }

        Vector3 delta = player.position - transform.position;
        delta.y = 0f;
        PlayerInside = delta.sqrMagnitude <= radius * radius;

        if (!PlayerInside) return;
        chargeProgress += Time.deltaTime;
        Progress = Mathf.Clamp01(chargeProgress / chargeDuration);
        UnityEngine.Debug.Log($"progress {Progress}");
        onProgress?.Invoke(Progress);

        if (Progress >= 1f)
        {
            IsCharged = true;
            FullyCharged?.Invoke();
        }
    }

    void SetVisual(bool on)
    {
        if (radiusVisual == null) return;
        radiusVisual.gameObject.SetActive(on);
        if (!on) return;

        Vector3 parentScale = radiusVisual.parent != null ? radiusVisual.parent.lossyScale : Vector3.one;
        float d = radius * 2f;
        radiusVisual.localScale = new Vector3(d / Mathf.Max(0.0001f, parentScale.x),radiusVisual.localScale.y, d / Mathf.Max(0.0001f, parentScale.z));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
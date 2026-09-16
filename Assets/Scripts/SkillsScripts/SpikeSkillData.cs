using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpikeSkillData", menuName = "Scriptable Objects/Skills/Spike Skill")]
public class SpikeSkillData : SkillData
{
    [Header("Spike Prefab")]
    public GameObject spikePrefab;
    public float riseDistance = 1f;
    private ClimbableSurfaceHolder climbableSurfaceHolder;

    [Header("Damage")]
    public float damage = 15f;

    public override void Use(Transform user, List<string> tags)
    {
        climbableSurfaceHolder = user.GetComponent<ClimbableSurfaceHolder>();
        if (climbableSurfaceHolder == null) climbableSurfaceHolder = user.GetComponentInParent<ClimbableSurfaceHolder>();

        if (climbableSurfaceHolder == null)
        {
            Debug.LogError($"cannot use spike skill on {user.name}");
            return;
        }

        if (!climbableSurfaceHolder.IsAnyoneClimbing)
        {
            Debug.LogWarning($"[{skillName}] No active climbers on {user.name}; cannot spawn spike.");
            return;
        }

        List<ClimbableSurfaceHolder.ClimbEntry> activeClimbers = new List<ClimbableSurfaceHolder.ClimbEntry>(climbableSurfaceHolder.ActiveClimbers);

        ClimbableSurfaceHolder.ClimbEntry entry = activeClimbers[Random.Range(0, activeClimbers.Count)];

        ClimbableSurface surface = entry.climbableSurface;
        if (surface == null)
        {
            Debug.LogWarning($"[{skillName}] Chosen climb entry has no ClimbableSurface; cannot spawn spike.");
            return;
        }

        int faceIndex = Random.Range(0, surface.faces.Length);

        SpawnSpike(surface, faceIndex, entry.part, user, tags);
    }

    private void SpawnSpike(ClimbableSurface surface, int faceIndex, Transform part, Transform user, List<string> tags)
    {
        if (spikePrefab == null)
        {
            Debug.LogWarning($"[{skillName}] No spike prefab assigned.");
            return;
        }

        var face = surface.faces[faceIndex];
        Vector3 worldPos = surface.GetFaceWorldCentroid(faceIndex);
        Vector3 worldNormal = surface.transform.TransformDirection(face.normal).normalized;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, worldNormal);

        GameObject spikeObj = Object.Instantiate(spikePrefab, worldPos, rot);
        if (part != null) spikeObj.transform.SetParent(part);
        spikeObj.GetComponent<HitBox>().tagsToExclude = tags;

        SpikeSkillLogic hazard = spikeObj.GetComponent<SpikeSkillLogic>();
        if (hazard == null) hazard = spikeObj.AddComponent<SpikeSkillLogic>();
        hazard.Init(worldNormal * riseDistance, (int)damage, user.root.gameObject);
    }
}
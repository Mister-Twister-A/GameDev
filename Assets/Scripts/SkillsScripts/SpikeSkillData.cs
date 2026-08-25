using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpikeSkillData", menuName = "Scriptable Objects/Skills/Spike Skill")]
public class SpikeSkillData : SkillData
{
    [Header("Spike Prefab")]
    public GameObject spikePrefab;
    [Tooltip("How far along the face normal the spike rises out of the surface.")]
    public float riseDistance = 1f;
    private ClimbableSurfaceHolder climbableSurfaceHolder;

    [Header("Damage")]
    public float damage = 15f;
    public override void Use(Transform user, List<string> tags)
    {
        climbableSurfaceHolder = user.GetComponent<ClimbableSurfaceHolder>();
        if (climbableSurfaceHolder == null) climbableSurfaceHolder = user.GetComponentInParent<ClimbableSurfaceHolder>();
        
        if(climbableSurfaceHolder == null)
        {
            Debug.LogError($"cannot use spke skill on {user.name}");
            return;
        }
        ClimbableSurface surface = climbableSurfaceHolder.curPartClimbableSurface;

        if (surface== null)
        {
            Debug.LogWarning($"[{skillName}] No ClimbableSurface found under {user.name}; cannot spawn spike.");
            return;
        }

        int faceIndex = Random.Range(0,surface.faces.Length);

        SpawnSpike(surface, faceIndex,user , tags);
    }

    private void SpawnSpike(ClimbableSurface surface, int faceIndex, Transform user, List<string> tags)
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
        if (climbableSurfaceHolder.curPart != null) spikeObj.transform.SetParent(climbableSurfaceHolder.curPart);
        spikeObj.GetComponent<HitBox>().tagsToExclude = tags;
        
 
        SpikeSkillLogic hazard = spikeObj.GetComponent<SpikeSkillLogic>();
        if (hazard == null) hazard = spikeObj.AddComponent<SpikeSkillLogic>();
        hazard.Init(worldNormal * riseDistance, (int)damage, user.root.gameObject);
    }
}
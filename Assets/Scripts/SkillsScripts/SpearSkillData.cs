using System.Collections.Generic;
using UnityEngine;

// CreateAssetMenu on the abstract SkillData does nothing (can't instantiate abstract SOs),
// so every concrete skill gets its own menu entry.
[CreateAssetMenu(fileName = "SpearSkill", menuName = "Scriptable Objects/Skills/Spear")]
public class SpearSkillData : SkillData
{
    [Header("Spear")]
    public SpearProjectile spearPrefab;
    public float damage = 25f;
    public Vector3 spawnOffset = new Vector3(0f, 1.4f, 0.5f);

    [Header("Leap + Aim")]
    public bool leapAndAim = true;
    public float backSpeed = 4.5f;
    public float upSpeed = 7f;
    public float wallPush = 0.5f;
    public float slowMoDelay = 0.25f;
    [Range(0f, 1f)] public float airControl = 0.25f;

    [Header("Slow Motion")]
    [Range(0.05f, 1f)] public float slowScale = 0.2f;
    public float slowInTime = 0.1f;
    public float slowOutTime = 0.12f;
    public float maxAimTime = 2.5f;

    [Header("Aim")]
    public LayerMask aimMask = ~0;
    public float aimRange = 300f;

    public override SkillActivation CreateActivation()
    {
        return leapAndAim ? new SpearLeapActivation(this) : null;
    }
    public override void Use(Transform user, List<string> tags)
    {
        Fire(user, tags, user.forward);
    }

    public override void Use(Transform user, List<string> tags, Vector3 aimPoint)
    {
        Vector3 origin = user.TransformPoint(spawnOffset);
        Vector3 dir = aimPoint - origin;
        if (dir.sqrMagnitude < 0.01f) dir = user.forward;
        Fire(user, tags, dir);
    }

    private void Fire(Transform user, List<string> tags, Vector3 direction)
    {
        if (spearPrefab == null)
        {
            Debug.LogWarning($"{name}: spearPrefab is not assigned.");
            return;
        }

        Vector3 origin = user.TransformPoint(spawnOffset);

        SkillUser skillUser = user.GetComponentInParent<SkillUser>();
        Transform owner = skillUser != null ? skillUser.transform : user.root;

        SpearProjectile spear = Instantiate(spearPrefab, origin, Quaternion.LookRotation(direction));
        spear.Launch(direction, owner, tags, damage);
    }
}
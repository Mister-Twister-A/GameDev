using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Skills/Projectile")]
public class ProjectileSkill : SkillData
{
    public EnemyProjectile projectilePrefab;
    public Vector3 spawnOffset = new Vector3(0f, 0f, 0.5f);
    public float projectileSpeed = 16f;
    public int damage = 10;
    public int projectileCount = 1;
    public float spreadDegrees = 1.5f;
    public LayerMask worldHitMask = ~0;

    public override void Use(Transform user, List<string> tags)
    {
        Fire(user, tags, user.position + user.forward * 10f);
    }

    public override void Use(Transform user, List<string> tags, Vector3 aimPoint)
    {
        Fire(user, tags, aimPoint);
    }

    private void Fire(Transform user, List<string> tags, Vector3 aimPoint)
    {
        if (projectilePrefab == null) return;

        Vector3 origin = user.TransformPoint(spawnOffset);
        Vector3 baseDir = (aimPoint - origin).normalized;
        float spread = Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);

        // the projectile ignores its shooter's colliders
        Transform owner = user.root;
        EnemyData enemy = user.GetComponentInParent<EnemyData>();
        if (enemy != null) owner = enemy.transform;

        for (int i = 0; i < Mathf.Max(1, projectileCount); i++)
        {
            Vector3 dir = (baseDir + UnityEngine.Random.insideUnitSphere * spread).normalized;

            EnemyProjectile instance = UnityEngine.Object.Instantiate(
                projectilePrefab, origin, Quaternion.LookRotation(dir));

            if (instance.TryGetComponent(out HitBox hitBox))
            {
                hitBox.owner = user.gameObject;
                hitBox.attackDamage = damage;
                hitBox.tagsToExclude = new List<string>(tags);
            }

            instance.Init(dir, projectileSpeed, owner, worldHitMask, tags);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
 
[CreateAssetMenu(fileName = "SlashSkill", menuName = "Scriptable Objects/Skills/Slash")]
public class SlashSkill : SkillData
{
    public GameObject hitboxPrefab;
    public float spawnDistance = 1f;
    public float duration = 0.1f;
    public int damage = 10;
 
    public override void Use(Transform user, List<string> tags)
    {
        GameObject instance = UnityEngine.Object.Instantiate(hitboxPrefab, user.transform);
        instance.transform.localPosition = Vector3.forward * spawnDistance;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (instance.TryGetComponent(out HitBox hitBox))
        {
            hitBox.owner = user.gameObject;
            hitBox.attackDamage = damage;
            hitBox.tagsToExclude = new List<string> (tags);
        }
 
        UnityEngine.Object.Destroy(instance, duration);
    }
}
 
using UnityEngine;
 
[CreateAssetMenu(fileName = "SlashSkill", menuName = "Scriptable Objects/Skills/Slash")]
public class SlashSkill : SkillData
{
    public GameObject hitboxPrefab;
    public float spawnDistance = 1f;
    public float duration = 0.1f;
    public int damage = 10;
 
    public override void Use(GameObject user)
    {
        GameObject instance = Object.Instantiate(hitboxPrefab, user.transform);
        instance.transform.localPosition = Vector3.forward * spawnDistance;
        instance.transform.localRotation = Quaternion.identity;
 
        if (instance.TryGetComponent(out HitBox hitBox))
        {
            hitBox.owner = user;
            hitBox.attackDamage = damage;
        }
 
        Object.Destroy(instance, duration);
    }
}
 
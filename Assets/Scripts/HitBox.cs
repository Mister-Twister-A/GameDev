using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public int attackDamage;
    public GameObject owner;

    public List<string> tagsToExclude = new List<string>();

    [Header("Falloff")]
    public bool useFalloff;
    public float falloffRadius = 4f;
    [Range(0f, 1f)] public float edgeDamageMultiplier = 0.4f;
    
    [Header("Knockback")]
    public bool canKnockback;
    public float knockbackPower;
    public float knockbackRadius;
    private bool knockbackTriggered = false;


    

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        foreach (string tag in tagsToExclude)
        {
            if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag)) return;
        }

        if (other.TryGetComponent<HurtBox>(out HurtBox hurtbox))
        {
            hurtbox.TakeDamage((int)CalculateDamage(other));
            if (canKnockback && other.transform && !knockbackTriggered){
                knockbackTriggered= true;
                Debug.Log("Triggering Kockback");
                Knockback.Explosion(transform.position, knockbackRadius, knockbackPower);
            }
        }
    }

    private float CalculateDamage(Collider other)
    {
        if (!useFalloff || falloffRadius <= 0f) return attackDamage;
        Vector3 center = transform.position;
        float distance = Vector3.Distance(center, ClosestPointOn(other, center));
        float t = Mathf.Clamp01(distance / falloffRadius);
        return attackDamage * Mathf.Lerp(1f, edgeDamageMultiplier, t);
    }

    private static Vector3 ClosestPointOn(Collider c, Vector3 point)
    {
        if (c is MeshCollider mesh && !mesh.convex) return c.bounds.ClosestPoint(point);
        return c.ClosestPoint(point);
    }
}
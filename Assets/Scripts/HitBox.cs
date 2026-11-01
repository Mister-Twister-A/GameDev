using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public int attackDamage;
    public GameObject owner;

    public List<string> tagsToExclude = new List<string>();

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        foreach (string tag in tagsToExclude)
        {
            if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag)) return;
        }

        if (other.TryGetComponent<HurtBox>(out HurtBox hurtbox))
        {
            hurtbox.TakeDamage(attackDamage);
        }
    }
}
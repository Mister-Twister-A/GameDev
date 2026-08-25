using System;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public int attackDamage;
    public GameObject owner;

    public List<string> tagsToExclude;

    private void OnTriggerEnter(Collider other)
    {

        if (owner != null && other.gameObject == owner) return;
        foreach(string tag in tagsToExclude)
        {
            if (other.CompareTag(tag)) return;
        }

        if (other.TryGetComponent<HurtBox>(out HurtBox hurtbox))
        {
            hurtbox.TakeDamage(attackDamage);
        }
    }
}

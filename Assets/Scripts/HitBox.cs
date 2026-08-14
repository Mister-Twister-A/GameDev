using UnityEngine;

public class HitBox : MonoBehaviour
{
    public int attackDamage;
    public GameObject owner;

    private void OnTriggerEnter(Collider other)
    {

         if (owner != null && other.gameObject == owner) return;

        if (other.TryGetComponent<HurtBox>(out HurtBox hurtbox))
        {
            hurtbox.TakeDamage(attackDamage);
        }
    }
}

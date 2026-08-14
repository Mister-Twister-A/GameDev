using UnityEngine;

public class HurtBox : MonoBehaviour
{
    public void TakeDamage(int damage)
    {
        Debug.Log($" Took {damage} damage.");
        EnemyData data = GetComponentInParent<EnemyData>();
        if (data)
        {
            data.health -= damage;
        }
    }
}

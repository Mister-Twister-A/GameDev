using UnityEngine;

public class HurtBox : MonoBehaviour
{
    public void TakeDamage(float damage)
    {
        
        EnemyData data = GetComponentInParent<EnemyData>();
        Debug.Log($" {data.transform.name} Took {damage} damage.");
        if (data)
        {
            data.health -= damage;
        }
    }
}

using UnityEngine;

public class HurtBox : MonoBehaviour
{
    private EnemyData data;

    private void Awake()
    {
        data = GetComponentInParent<EnemyData>();
    }

    public void TakeDamage(float damage)
    {
        if (data == null) return;

        Debug.Log($"{data.transform.name} took {damage} damage.");
        data.health -= damage;
    }
}
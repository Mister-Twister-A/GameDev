using UnityEngine;

public class WeakPointTest : EnemyData
{
    public override void Behaviour()
    {
        if (health <= 0)
        {
            OnDeath();
        }
    }
    private void Update()
    {
        Behaviour();
    }

    public override void Spawn(Transform pos)
    {
        throw new System.NotImplementedException();
    }

    public override void OnDeath()
    {
        EnemyData data = transform.parent.GetComponentInParent<EnemyData>();
        data.OnDeath();
        Destroy(gameObject);
    }

}

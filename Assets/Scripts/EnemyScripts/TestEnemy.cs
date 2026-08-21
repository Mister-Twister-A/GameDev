using UnityEngine;

public class TestEnemy : EnemyData
{
    
    public Transform player;
    public float slashRange = 2f;

    private SkillUser skillUser;

    void Start()
    {
        skillUser = GetComponentInChildren<SkillUser>();
    }

    void Update()
    {
        Behaviour(); 
        if (health <= 0)
        {
            OnDeath();
        }
    }

    public override void Behaviour()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= slashRange)
        {
            if (skillUser != null)
            {
                skillUser.TryUseSkill();
            }
        }
    }

    public override void OnDeath()
    {
        Destroy(gameObject);
    }

    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}

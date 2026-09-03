using UnityEngine;

public class TestEnemy : EnemyData
{
    
    public Transform player;
    public float slashRange = 2f;

    private SkillUser skillUser;

    void Start()
    {
        skillUser = GetComponentInChildren<SkillUser>();
        player = GameObject.FindWithTag("Player").transform;
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
                skillUser.TryUseSkill(0);
            }
        }
    }

    public override void OnDeath()
    {
        if (TryGetComponent<EnemyClimbController>(out EnemyClimbController enemyClimbController))
        {
            if (enemyClimbController.CurrentSurface != null)
            {
                enemyClimbController.CurrentSurface.climbableSurfaceHolder.RegisterClimberExit(enemyClimbController);
            }
        }
        Destroy(gameObject);
    }

    public override void Spawn(Transform pos)
    {
        Instantiate(gameObject, pos.position, Quaternion.identity);
    }
}

using UnityEngine;

[RequireComponent(typeof(EnemyClimbController))]
public class RangedEnemy : EnemyData
{
    private enum MoveState { Chase, Orbit }

    [Header("Ranges")]
    [SerializeField] private float engageRange = 10f;       
    [SerializeField] private float disengageRange = 12.5f;  
    [SerializeField] private float preferredDistance = 8f;  
    [SerializeField] private float distanceTolerance = 3f;  

    [Header("Orbit")]
    [SerializeField] private float orbitSpeed = 4f;
    [SerializeField] private Vector2 flipIntervalRange = new Vector2(2f, 5f);
    [SerializeField] private float stuckCheckInterval = 0.4f;
    [SerializeField, Range(0f, 1f)] private float stuckSpeedFraction = 0.25f;
    [SerializeField] private float maxTimeWithoutLineOfSight = 2f;

    [Header("Shooting")]
    [Tooltip("Index of the ProjectileSkill in this enemy's SkillUser.")]
    [SerializeField] private int shootSkillIndex = 0;
    [Tooltip("Used for line-of-sight and lead calculation. Put it where the projectile spawns.")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Vector2 firstShotDelayRange = new Vector2(0.5f, 1.5f);
    [SerializeField, Range(0f, 1f)] private float leadAmount = 0.7f;
    [SerializeField] private float aimHeight = 0f;         
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    private EnemyClimbController climb;
    private EnemyGroundNavigator groundNav;
    private EnemySurfaceNavigator surfaceNav;
    private PlayerClimbController playerClimb;
    private SkillUser skillUser;
    private Transform player;

    private MoveState state = MoveState.Chase;
    private int orbitDir = 1;
    private float flipTimer;
    private float stuckTimer;
    private Vector3 lastStuckLocalPos;
    private ClimbableSurface lastStuckSurface;
    private float noLosTimer;
    private float firstShotTimer;

    private Vector3 playerVelocity;
    private Vector3 lastPlayerPos;

    void Start()
    {
        climb = GetComponent<EnemyClimbController>();
        groundNav = GetComponent<EnemyGroundNavigator>();
        surfaceNav = GetComponent<EnemySurfaceNavigator>();
        skillUser = GetComponentInChildren<SkillUser>();
        playerClimb = climb.playerController;
        player = playerClimb != null ? playerClimb.transform : GameObject.FindWithTag("Player").transform;
        lastPlayerPos = player.position;
        firstShotTimer = Random.Range(firstShotDelayRange.x, firstShotDelayRange.y); // desync multiple shooters
    }

    void Update()
    {
        Behaviour();
        if (health <= 0) OnDeath();
    }

    public override void Behaviour()
    {
        if (player == null || climb == null) return;

        TrackPlayerVelocity();

        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        bool sameMode = playerClimb == null || climb.IsClimbing == playerClimb.IsClimbing;
        bool los = HasLineOfSight();

        if (state == MoveState.Chase)
        {
            if (dist <= engageRange && sameMode && los) EnterOrbit();
        }
        else
        {
            noLosTimer = los ? 0f : noLosTimer + Time.deltaTime;
            if (dist > disengageRange || !sameMode || noLosTimer > maxTimeWithoutLineOfSight)
                EnterChase();
        }

        if (state == MoveState.Orbit) Orbit();

        climb.FacingOverride = state == MoveState.Orbit ? toPlayer : (Vector3?)null;

        firstShotTimer -= Time.deltaTime;
        if (firstShotTimer <= 0f && skillUser != null && dist <= disengageRange && los)
            skillUser.TryUseSkill(shootSkillIndex, LeadPoint(MuzzlePos));
    }


    private void EnterOrbit()
    {
        state = MoveState.Orbit;
        orbitDir = Random.value < 0.5f ? -1 : 1;
        noLosTimer = 0f;
        ResetFlipTimer();
        ResetStuckSample();
        SetNavigators(false);
    }

    private void EnterChase()
    {
        state = MoveState.Chase;
        SetNavigators(true);
    }

    private void SetNavigators(bool on)
    {
        if (groundNav != null) groundNav.enabled = on;
        if (surfaceNav != null)
        {
            surfaceNav.enabled = on;
            if (on) surfaceNav.InvalidatePath();
        }
    }

    private void Orbit()
    {
        // transform.up is the face normal while climbing and world up on the ground
        Vector3 up = transform.up;
        Vector3 planar = Vector3.ProjectOnPlane(player.position - transform.position, up);
        float planarDist = planar.magnitude;
        if (planarDist < 0.01f) return;

        Vector3 radial = planar / planarDist;                      
        Vector3 tangent = Vector3.Cross(up, radial) * orbitDir;    
        float radialWeight = Mathf.Clamp((planarDist - preferredDistance) / distanceTolerance, -1f, 1f);

        Vector3 dir = (tangent + radial * radialWeight).normalized;
        climb.MoveTowards(transform.position + dir * 3f, orbitSpeed);

        flipTimer -= Time.deltaTime;
        if (flipTimer <= 0f) FlipOrbit();

        CheckStuck();
    }

    private void FlipOrbit()
    {
        orbitDir = -orbitDir;
        ResetFlipTimer();
        ResetStuckSample();
    }

    private void ResetFlipTimer() => flipTimer = Random.Range(flipIntervalRange.x, flipIntervalRange.y);
    private void CheckStuck()
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckCheckInterval) return;

        ClimbableSurface surface = climb.CurrentSurface;
        float moved;
        if (surface != null && surface == lastStuckSurface)
        {
            Vector3 local = surface.transform.InverseTransformPoint(transform.position);
            moved = surface.transform.TransformVector(local - lastStuckLocalPos).magnitude;
        }
        else
        {
            moved = float.MaxValue; 
        }

        if (moved < orbitSpeed * stuckCheckInterval * stuckSpeedFraction) FlipOrbit();
        else ResetStuckSample();
    }

    private void ResetStuckSample()
    {
        stuckTimer = 0f;
        lastStuckSurface = climb.CurrentSurface;
        lastStuckLocalPos = lastStuckSurface != null
            ? lastStuckSurface.transform.InverseTransformPoint(transform.position)
            : transform.position;
    }

    private Vector3 MuzzlePos => muzzle != null ? muzzle.position : transform.position + transform.up * 0.5f;
    private Vector3 PlayerCenter => player.position + player.up * aimHeight;

    private float ProjectileSpeed
    {
        get
        {
            ProjectileSkill skill = skillUser != null ? skillUser.GetSkill(shootSkillIndex) as ProjectileSkill : null;
            return skill != null ? skill.projectileSpeed : 16f;
        }
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = MuzzlePos;
        Vector3 target = PlayerCenter;
        Vector3 delta = target - origin;
        float len = delta.magnitude;
        if (len < 0.01f) return true;

        RaycastHit[] hits = Physics.RaycastAll(origin, delta / len, len, lineOfSightMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;                                 // ourselves
            if (h.collider.transform.IsChildOf(player) || h.collider.transform == player) continue;  // the player
            return false;
        }
        return true;
    }

    private void TrackPlayerVelocity()
    {
        if (Time.deltaTime <= 0f) return;
        Vector3 instant = (player.position - lastPlayerPos) / Time.deltaTime;
        playerVelocity = Vector3.Lerp(playerVelocity, instant, 0.25f); 
        lastPlayerPos = player.position;
    }

    private Vector3 LeadPoint(Vector3 origin)
    {
        float speed = Mathf.Max(0.1f, ProjectileSpeed);
        Vector3 pos = PlayerCenter;
        float t = Vector3.Distance(origin, pos) / speed;
        for (int i = 0; i < 2; i++)
            t = Vector3.Distance(origin, pos + playerVelocity * t) / speed;
        return pos + playerVelocity * t * leadAmount;
    }

    public override void OnDeath()
    {
        if (climb != null) climb.FacingOverride = null;
        if (TryGetComponent<EnemyClimbController>(out EnemyClimbController enemyClimbController))
        {
            if (enemyClimbController.CurrentSurface != null)
                enemyClimbController.CurrentSurface.climbableSurfaceHolder.RegisterClimberExit(enemyClimbController);
        }
        Destroy(gameObject);
    }

    public override void Spawn(Transform pos)
    {
        Instantiate(gameObject, pos.position, Quaternion.identity);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, disengageRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
#endif
}
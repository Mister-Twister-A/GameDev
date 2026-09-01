using System.Collections.Generic;
using UnityEngine;
public class TestTitan: EnemyData, ISurfaceWalker
{
    [Header("Ground Check")]
    public LayerMask groundMask = ~0;

    public float rayStartHeight = 1f;
    public float rayMaxDistance = 3f;
    public float groundSnapLerp = 15f;

    [Header("Attack")]
    public float expCoolDown = 3f;

    private ClimbableSurfaceHolder climbableSurfaceHolder;

    private SkillUser skillUser;
    [SerializeField] private float explosionForce = 30f;

    [Header("Facing")]
    public Transform modelRoot;
    public float turnSpeed = 8f;    public float yawOffset = 0f;

    [Header("Pivot Offset")]
    public float pivotHeightAboveFeet = 0f;

    public Vector3 Position => transform.position;

    public ClimbableSurface CurrentSurface => throw new System.NotImplementedException();

    public int CurrentFaceIndex => throw new System.NotImplementedException();

    public Transform Transform_ => transform;

    private void Awake()
    {
        climbableSurfaceHolder = GetComponent<ClimbableSurfaceHolder>();
        skillUser = GetComponent<SkillUser>();
    }

    public void MoveTowards(Vector3 worldTargetPoint, float speed)
    {
        Vector3 toTarget = worldTargetPoint - transform.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, Vector3.up);

        if (flatDir.sqrMagnitude > 0.0001f)
            flatDir.Normalize();

        Vector3 horizontalDelta = flatDir * speed * Time.deltaTime;
        Vector3 nextPos = transform.position + horizontalDelta;

        nextPos = ApplyGroundHeight(nextPos);

        transform.position = nextPos;

        FaceDirection(flatDir);
    }

    private Vector3 ApplyGroundHeight(Vector3 candidatePos)
    {
        Vector3 rayOrigin = candidatePos + Vector3.up * rayStartHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                rayStartHeight + rayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y + pivotHeightAboveFeet;
            float newY = Mathf.Lerp(candidatePos.y, targetY, groundSnapLerp * Time.deltaTime);
            return new Vector3(candidatePos.x, newY, candidatePos.z);
        }

        return candidatePos;
    }

    private void FaceDirection(Vector3 flatDir)
    {
        if (flatDir.sqrMagnitude <= 0.0001f) return;

        Transform model = modelRoot != null ? modelRoot : transform;
        float targetYaw = Quaternion.LookRotation(flatDir, Vector3.up).eulerAngles.y + yawOffset;
        Vector3 currentEuler = model.eulerAngles;
        float newYaw = Mathf.LerpAngle(currentEuler.y, targetYaw, turnSpeed * Time.deltaTime);

        model.rotation = Quaternion.Euler(currentEuler.x, newYaw, currentEuler.z);
    }
    
    private void Update()
    {
        Behaviour();
    }

    public override void Behaviour()
    {
        // if (climbableSurfaceHolder.curPlayerTarget == null) return;
        // if(!climbableSurfaceHolder.curPlayerTarget.IsClimbing) return;
        if(!climbableSurfaceHolder.IsAnyoneClimbing) return;

        //skillUser.TryUseSkill(0);
    }

    public override void OnDeath()
    {
        climbableSurfaceHolder.unClimbable = true;
        ICollection<ClimbableSurfaceHolder.ClimbEntry> ActiveClimbers = new List<ClimbableSurfaceHolder.ClimbEntry>(climbableSurfaceHolder.ActiveClimbers);;
        foreach (ClimbableSurfaceHolder.ClimbEntry entry in ActiveClimbers)
        {
            if(entry.controller is PlayerClimbController player)
            {
                player.ExitClimbState();
                Vector3 currentEuler = player.transform.rotation.eulerAngles;
                Quaternion tgtRotation = Quaternion.Euler(0f, currentEuler.y, 0f);
                player.transform.rotation = tgtRotation;
                Vector3 direction = player.transform.position - transform.position;
                if(direction.y < 0)
                {
                    direction.y = 0;
                }
                player.verticalVelocity = direction.normalized * explosionForce;
            }
            else if(entry.controller is EnemyClimbController enemy)
            {
                enemy.ExitClimbState();
                EnemySurfaceNavigator nav=  enemy.transform.GetComponent<EnemySurfaceNavigator>();
                if(nav) nav.InvalidatePath();
                Vector3 currentEuler = enemy.transform.rotation.eulerAngles;
                Quaternion tgtRotation = Quaternion.Euler(0f, currentEuler.y, 0f);
                enemy.transform.rotation = tgtRotation;
                Vector3 direction = enemy.transform.position - transform.position;
                if(direction.y < 0)
                {
                    direction.y = 0;
                }
                enemy.verticalVelocity = direction.normalized * explosionForce;
            }
            else
            {
                Debug.Log("Unknown ISurfaceLocator controller type");
            }
        }

        Destroy(gameObject);
    }
}
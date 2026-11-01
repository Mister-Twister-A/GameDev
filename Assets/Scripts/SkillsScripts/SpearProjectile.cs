using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flies with a manual sphere sweep (no tunneling), sticks to whatever it hits,
/// FOLLOWS that object if it moves (giants!), then explodes after a fuse.
/// Prefab pivot should be at the spear TIP, +Z pointing along the shaft.
/// </summary>
public class SpearProjectile : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 45f;
    public float gravityScale = 0.15f;
    public float radius = 0.15f;
    public float maxFlightTime = 4f;
    public LayerMask stickMask = ~0;

    [Header("Explosion")]
    public float fuseTime = 1.2f;
    public float explosionRadius = 4f;
    [Range(0f, 1f)] public float edgeDamageMultiplier = 0.4f;
    public LayerMask damageMask = ~0;
    public GameObject explosionVfx;

    private enum State { Flying, Stuck, Done }
    private State state = State.Flying;

    private Transform owner;
    private List<string> ignoredTags;
    private float damage;
    private Vector3 velocity;
    private float flightTimer;
    private float fuseTimer;
    private Transform stuckTarget;
    private Vector3 stuckLocalPos;
    private Quaternion stuckLocalRot;

    private static readonly RaycastHit[] sweepBuffer = new RaycastHit[16];
    private static readonly Collider[] overlapBuffer = new Collider[64];
    private static readonly HashSet<HurtBox> damagedThisBlast = new HashSet<HurtBox>();

    public void Launch(Vector3 direction, Transform owner, List<string> ignoredTags, float damage)
    {
        this.owner = owner;
        this.ignoredTags = ignoredTags;
        this.damage = damage;
        velocity = direction.normalized * speed;
        transform.rotation = Quaternion.LookRotation(velocity);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Flying: Fly(); break;
            case State.Stuck: TickStuck(); break;
        }
    }

    private void Fly()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        flightTimer += dt;
        if (flightTimer >= maxFlightTime) { Explode(); return; }

        velocity += Physics.gravity * gravityScale * dt;
        Vector3 step = velocity * dt;
        float dist = step.magnitude;
        Vector3 dir = step / dist;

        int count = Physics.SphereCastNonAlloc(transform.position, radius, dir, sweepBuffer,
            dist, stickMask, QueryTriggerInteraction.Ignore);
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (IsIgnored(sweepBuffer[i].collider)) continue;
            if (sweepBuffer[i].distance < bestDist)
            {
                bestDist = sweepBuffer[i].distance;
                best = i;
            }
        }

        if (best >= 0)
        {
            RaycastHit hit = sweepBuffer[best];
            Vector3 p = hit.distance > 0f ? hit.point : transform.position;
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(dir);
            Stick(hit.collider.transform);
            return;
        }

        transform.position += step;
        transform.rotation = Quaternion.LookRotation(velocity);
    }

    private void Stick(Transform target)
    {
        state = State.Stuck;
        fuseTimer = fuseTime;
        stuckTarget = target;
        stuckLocalPos = target.InverseTransformPoint(transform.position);
        stuckLocalRot = Quaternion.Inverse(target.rotation) * transform.rotation;
    }

    private void TickStuck()
    {
        if (stuckTarget == null || !stuckTarget.gameObject.activeInHierarchy)
        {
            Explode();
            return;
        }

        transform.position = stuckTarget.TransformPoint(stuckLocalPos);
        transform.rotation = stuckTarget.rotation * stuckLocalRot;

        fuseTimer -= Time.deltaTime;
        if (fuseTimer <= 0f) Explode();
    }

    private void Explode()
    {
        if (state == State.Done) return;
        state = State.Done;

        Vector3 center = transform.position;

        int count = Physics.OverlapSphereNonAlloc(center, explosionRadius, overlapBuffer,
            damageMask, QueryTriggerInteraction.Collide);

        damagedThisBlast.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (IsIgnored(col)) continue;

            if (!col.TryGetComponent(out HurtBox hurt))
                hurt = col.GetComponentInParent<HurtBox>();
            if (hurt == null) continue;

            if (!damagedThisBlast.Add(hurt)) continue;

            float d = Vector3.Distance(center, col.bounds.ClosestPoint(center));
            float t = Mathf.Clamp01(d / explosionRadius);
            float mult = Mathf.Lerp(1f, edgeDamageMultiplier, t);
            hurt.TakeDamage(damage * mult);
        }

        if (explosionVfx != null)
        {
            GameObject vfx = Instantiate(explosionVfx, center, Quaternion.identity);
            Destroy(vfx, 3f); // swap for pooling later
        }

        Destroy(gameObject);
    }

    private bool IsIgnored(Collider c)
    {
        if (owner != null && c.transform.IsChildOf(owner)) return true;
        if (ignoredTags != null)
        {
            foreach (string t in ignoredTags)
            {
                if (!string.IsNullOrEmpty(t) && c.CompareTag(t)) return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
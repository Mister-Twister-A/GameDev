using System.Collections.Generic;
using UnityEngine;
public class SpearProjectile : MonoBehaviour
{
    [Header("Model")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Header("Flight")]
    public float speed = 45f;
    public float gravityScale = 0.15f;
    public float radius = 0.15f;
    public float maxFlightTime = 4f;
    public LayerMask stickMask = ~0;

    [Header("Explosion")]
    public float fuseTime = 1.2f;
    public HitBox explosionHitBoxPrefab;
    public float explosionRadius = 4f;
    public float hitBoxLifetime = 0.2f;
    public GameObject explosionVfx;

    private enum State { Flying, Stuck, Done }
    private State state = State.Flying;
    private bool launched;

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

    private void Awake()
    {
        if (transform.parent != null && transform.parent.GetComponentInParent<SpearProjectile>() != null)
        {
            Debug.LogWarning($"{name}: nested SpearProjectile under '{transform.parent.name}'. " +
                             "Remove this component from the child model.", this);
            enabled = false;
        }
    }

    public void Launch(Vector3 direction, Transform owner, List<string> ignoredTags, float damage)
    {
        launched = true;
        this.owner = owner;
        this.ignoredTags = ignoredTags;
        this.damage = damage;
        velocity = direction.normalized * speed;
        FaceDirection(velocity);
    }

    private void Update()
    {
        if (!launched) return; // never simulate a spear nobody launched

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
        if (dist < 0.0001f) return;
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
            FaceDirection(dir);
            Stick(hit.collider.transform);
            return;
        }

        transform.position += step;
        FaceDirection(velocity);
    }

    private void FaceDirection(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(modelRotationOffset);
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

        SpawnExplosionHitBox(center);

        if (explosionVfx != null)
        {
            GameObject vfx = Instantiate(explosionVfx, center, Quaternion.identity);
            Destroy(vfx, 3f); // swap for pooling later
        }

        Destroy(gameObject);
    }

    private void SpawnExplosionHitBox(Vector3 center)
    {
        if (explosionHitBoxPrefab == null)
        {
            Debug.LogWarning($"{name}: explosionHitBoxPrefab is not assigned, the explosion deals no damage.", this);
            return;
        }

        HitBox hitBox = Instantiate(explosionHitBoxPrefab, center, Quaternion.identity);
        hitBox.attackDamage = Mathf.RoundToInt(damage);
        hitBox.owner = owner != null ? owner.gameObject : null;
        hitBox.tagsToExclude = ignoredTags != null ? new List<string>(ignoredTags) : new List<string>();

        foreach (Collider c in hitBox.GetComponentsInChildren<Collider>())
            c.isTrigger = true;

        SphereCollider sphere = hitBox.GetComponentInChildren<SphereCollider>();
        if (sphere != null)
        {
            Vector3 s = sphere.transform.lossyScale;
            float authoredRadius = sphere.radius * Mathf.Max(s.x, Mathf.Max(s.y, s.z));
            if (authoredRadius > 0.0001f)
                hitBox.transform.localScale *= explosionRadius / authoredRadius;
        }

        if (!hitBox.TryGetComponent(out Rigidbody rb))
            rb = hitBox.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Destroy(hitBox.gameObject, hitBoxLifetime);
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
        Vector3 flightDir = transform.rotation * (Quaternion.Inverse(Quaternion.Euler(modelRotationOffset)) * Vector3.forward);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, flightDir * 3f);
        Gizmos.DrawWireSphere(transform.position + flightDir * 3f, 0.1f);
    }
}
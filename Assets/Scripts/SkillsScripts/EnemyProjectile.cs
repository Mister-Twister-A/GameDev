using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.15f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float lingerAfterHit = 0.1f;

    private Vector3 direction;
    private float speed;
    private Transform owner;
    private LayerMask hitMask;
    private List<string> ignoreTags;
    private bool stopped;

    public void Init(Vector3 dir, float projectileSpeed, Transform shooter, LayerMask mask, List<string> tagsToIgnore)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        owner = shooter;
        hitMask = mask;
        ignoreTags = tagsToIgnore;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (stopped) return;

        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore)
            && !ShouldIgnore(hit.collider))
        {
            transform.position = hit.point;
            stopped = true;
            Destroy(gameObject, lingerAfterHit);
            return;
        }

        transform.position += direction * step;
    }

    private bool ShouldIgnore(Collider col)
    {
        if (owner != null && col.transform.IsChildOf(owner)) return true;

        if (ignoreTags != null)
        {
            foreach (string tag in ignoreTags)
            {
                if (col.CompareTag(tag) || col.transform.root.CompareTag(tag)) return true;
            }
        }
        return false;
    }
}
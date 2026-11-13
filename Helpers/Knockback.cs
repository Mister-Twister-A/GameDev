using UnityEngine;

public static class Knockback
{
    private static readonly AnimationCurve LinearFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public static void Launch(EnemyClimbController target, Vector3 direction, float force)
    {
        if (target != null) target.Launch(direction, force);
    }

    public static void Explosion(Vector3 origin, float radius, float force,float upwardBias = 0.4f, AnimationCurve falloff = null)
    {
        falloff ??= LinearFalloff;
        var all = EnemyClimbController.All;

        for (int i = all.Count - 1; i >= 0; i--)
        {
            EnemyClimbController enemy = all[i];
            Vector3 offset = enemy.transform.position - origin;
            float dist = offset.magnitude;
            if (dist > radius) continue;

            Vector3 dir = dist > 0.001f ? offset / dist : Vector3.zero;
            dir = (dir + Vector3.up * upwardBias).normalized;

            enemy.Launch(dir, force * Mathf.Clamp01(falloff.Evaluate(dist / radius)));
        }
    }
}
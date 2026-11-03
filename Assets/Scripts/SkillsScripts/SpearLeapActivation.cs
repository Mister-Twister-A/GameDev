using UnityEngine;

public class SpearLeapActivation : SkillActivation
{
    private enum Phase { Leaping, Aiming, Recovering }

    private readonly SpearSkillData d;
    private PlayerClimbController player;
    private Phase phase;
    private float airTimer;
    private float aimTimer;
    private float baseFixedDelta;

    public SpearLeapActivation(SpearSkillData data)
    {
        d = data;
    }

    public override bool CanBegin(SkillContext context)
    {
        player = context.Get<PlayerClimbController>();
        return player != null && player.camera != null && player.isGrounded;
    }

    protected override void OnBegin()
    {
        baseFixedDelta = Time.fixedDeltaTime;

        Vector3 velocity = ComputeLeapVelocity();   

        player.canAttachToSurfaces = false;
        player.isAiming = true;
        player.inputControlMultiplier = d.airControl;
        player.LaunchLeap(velocity);

        airTimer = 0f;
        phase = Phase.Leaping;
    }

    public override void Tick()
    {
        switch (phase)
        {
            case Phase.Leaping: TickLeaping(); break;
            case Phase.Aiming: TickAiming(); break;
            case Phase.Recovering: TickRecovering(); break;
        }
    }

    public override void End()
    {
        ResetTime();
        ClearPlayerFlags();
    }

    private Vector3 ComputeLeapVelocity()
    {
        Vector3 camFwd = Vector3.ProjectOnPlane(player.camera.transform.forward, Vector3.up);
        if (camFwd.sqrMagnitude < 0.01f) camFwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
        Vector3 back = -camFwd.normalized;
        if (player.TryGetSurfaceNormal(out Vector3 n))
        {
            Vector3 nFlat = Vector3.ProjectOnPlane(n, Vector3.up);
            if (nFlat.sqrMagnitude > 0.01f)
            {
                nFlat.Normalize();
                float into = Vector3.Dot(back, nFlat);
                if (into < 0f) back -= nFlat * into;
                back = (back + nFlat * d.wallPush).normalized;
            }
        }

        return back * d.backSpeed + Vector3.up * d.upSpeed;
    }

    private void TickLeaping()
    {
        airTimer += Time.unscaledDeltaTime;

        if (Landed()) { CancelAndRecover(); return; }

        if (!ctx.IsHeld) { ThrowAndRecover(); return; }

        if (airTimer >= d.slowMoDelay)
        {
            player.camera.SetAiming(true);
            aimTimer = 0f;
            phase = Phase.Aiming;
        }
    }

    private void TickAiming()
    {
        airTimer += Time.unscaledDeltaTime;
        aimTimer += Time.unscaledDeltaTime;

        EaseTimeScale(d.slowScale, d.slowInTime);

        if (Landed()) { CancelAndRecover(); return; }
        if (!ctx.IsHeld || aimTimer >= d.maxAimTime) ThrowAndRecover();
    }

    private void TickRecovering()
    {
        EaseTimeScale(1f, d.slowOutTime);
        if (Mathf.Approximately(Time.timeScale, 1f))
        {
            ResetTime();
            IsFinished = true;
        }
    }

    private void ThrowAndRecover()
    {
        Fire(GetAimPoint());
        BeginRecovery();
    }

    private void CancelAndRecover()
    {
        Cancel();
        BeginRecovery();
    }

    private void BeginRecovery()
    {
        ClearPlayerFlags();
        phase = Phase.Recovering;
    }

    private void ClearPlayerFlags()
    {
        if (player == null) return;
        player.isAiming = false;
        player.inputControlMultiplier = 1f;
        player.canAttachToSurfaces = true;
        if (player.camera != null) player.camera.SetAiming(false);
    }

    private bool Landed()
    {
        return airTimer > 0.2f && (player.isGrounded || player.controller.isGrounded);
    }

    private Vector3 GetAimPoint()
    {
        Transform cam = player.camera.transform;

        float depth = Mathf.Max(0.5f, Vector3.Dot(player.camera.AimPivot - cam.position, cam.forward));
        Vector3 origin = cam.position + cam.forward * depth;

        if (Physics.Raycast(origin, cam.forward, out RaycastHit hit, d.aimRange, d.aimMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return origin + cam.forward * d.aimRange;
    }

    private void EaseTimeScale(float target, float time)
    {
        float speed = Mathf.Abs(1f - d.slowScale) / Mathf.Max(0.0001f, time);
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, target, speed * Time.unscaledDeltaTime);
        Time.fixedDeltaTime = baseFixedDelta * Time.timeScale;
        SlowMoScreenEffect.SetIntensity(Mathf.InverseLerp(1f, d.slowScale, Time.timeScale));
    }

    private void ResetTime()
    {
        Time.timeScale = 1f;
        if (baseFixedDelta > 0f) Time.fixedDeltaTime = baseFixedDelta;
        SlowMoScreenEffect.SetIntensity(0f);
    }
}
using UnityEngine;

[RequireComponent(typeof(HitBox))]
public class SpikeSkillLogic: MonoBehaviour
{
    [Header("Timing")]
    public float riseDuration = 0.25f;
    public float holdDuration = 0.6f;
    public float retractDuration = 0.2f;

    private enum State { Rising, Holding, Retracting }
    private State state = State.Rising;

    private HitBox hitBox;
    private Collider hitCollider;

    private Vector3 hiddenLocalPos;
    private Vector3 risenLocalPos;
    private float timer;
    private bool hitboxActive;

    private void Awake()
    {
        hitBox = GetComponent<HitBox>();
        hitCollider = GetComponent<Collider>();
        if (hitCollider != null) hitCollider.enabled = false;
    }

    public void Init(Vector3 riseOffsetWorld, int damage, GameObject owner)
    {
        hitBox.attackDamage = damage;
        hitBox.owner = owner;
        hiddenLocalPos = transform.localPosition;
        Vector3 offsetLocal = transform.parent != null? transform.parent.InverseTransformDirection(riseOffsetWorld): riseOffsetWorld;
        risenLocalPos = hiddenLocalPos + offsetLocal;
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case State.Rising:
                float t = riseDuration > 0f ? Mathf.Clamp01(timer / riseDuration) : 1f;
                transform.localPosition = Vector3.Lerp(hiddenLocalPos, risenLocalPos, t);
                if (!hitboxActive && t >= 0.7f)  SetHitboxActive(true);

                if (t >= 1f)
                {
                    state = State.Holding;
                    timer = 0f;
                }
                break;

            case State.Holding:
                if (timer >= holdDuration)
                {
                    state = State.Retracting;
                    timer = 0f;
                    SetHitboxActive(false); 
                }
                break;

            case State.Retracting:
                float rt = retractDuration > 0f ? Mathf.Clamp01(timer / retractDuration) : 1f;
                transform.localPosition = Vector3.Lerp(risenLocalPos, hiddenLocalPos, rt);
                if (rt >= 1f)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    private void SetHitboxActive(bool active)
    {
        hitboxActive = active;
        if (hitCollider != null) hitCollider.enabled = active;
    }
}
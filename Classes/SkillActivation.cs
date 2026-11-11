using System.Collections.Generic;
using UnityEngine;

public class SkillContext
{
    public SkillUser User { get; }
    public SkillData Data { get; }
    public int SlotIndex { get; }
    public Transform UserTransform { get; }
    public List<string> Tags { get; }
    public KeyCode Key { get; }
    public bool IsHeld => Key != KeyCode.None && Input.GetKey(Key);

    public SkillContext(SkillUser user, SkillData data, int slotIndex, Transform userTransform,
                        List<string> tags, KeyCode key)
    {
        User = user;
        Data = data;
        SlotIndex = slotIndex;
        UserTransform = userTransform;
        Tags = tags;
        Key = key;
    }
    public T Get<T>() where T : Component => User.GetComponentInParent<T>();
}

public abstract class SkillActivation
{
    protected SkillContext ctx;

    public bool IsFinished { get; protected set; }
    public virtual bool CanBegin(SkillContext context) => true;

    public void Begin(SkillContext context)
    {
        ctx = context;
        OnBegin();
    }

    protected abstract void OnBegin();

    public abstract void Tick();
    public virtual void End() { }
    protected void Fire(Vector3 aimPoint) => ctx.User.FireActivatedSkill(ctx, aimPoint);
    protected void Cancel() => ctx.User.ForceCooldown(ctx.SlotIndex);
}
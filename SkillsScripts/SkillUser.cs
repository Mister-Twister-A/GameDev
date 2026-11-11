using System.Collections.Generic;
using UnityEngine;

public class SkillUser : MonoBehaviour
{
    [System.Serializable]
    public class SkillSlot
    {
        public SkillData skill;
        public List<string> collisionTags = new List<string>();
        public KeyCode key = KeyCode.None;
        [HideInInspector] public float cooldownTimer;
    }

    [SerializeField] private List<SkillSlot> skills = new List<SkillSlot>();

    private HurtBox hurtBox;
    private SkillActivation activation;

    public bool IsBusy => activation != null;

    private void Awake()
    {
        hurtBox = GetComponentInChildren<HurtBox>();
        if (hurtBox == null)
            Debug.LogWarning($"{name}: SkillUser found no HurtBox in children, skills can't be used.", this);
    }

    private void OnDisable()
    {
        if (activation != null)
        {
            activation.End();
            activation = null;
        }
    }

    private void Update()
    {
        foreach (SkillSlot slot in skills)
        {
            if (slot != null && slot.cooldownTimer > 0f)
            {
                slot.cooldownTimer = Mathf.Max(0f, slot.cooldownTimer - Time.deltaTime);
            }
        }

        if (activation != null)
        {
            activation.Tick();
            if (activation.IsFinished)
            {
                activation.End();
                activation = null;
            }
            return;
        }

        PollKeys();
    }

    private void PollKeys()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            SkillSlot slot = skills[i];
            if (slot == null || slot.key == KeyCode.None) continue;

            if (Input.GetKeyDown(slot.key))
            {
                TryUseSkill(i);
                break;
            }
        }
    }

    public bool TryUseSkill(int skillIndex)
    {
        return TryUse(skillIndex, null);
    }

    public bool TryUseSkill(int skillIndex, Vector3 aimPoint)
    {
        return TryUse(skillIndex, aimPoint);
    }

    public SkillData GetSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count) return null;
        return skills[skillIndex] != null ? skills[skillIndex].skill : null;
    }

    public bool IsReady(int skillIndex)
    {
        if (hurtBox == null || activation != null) return false;
        if (skillIndex < 0 || skillIndex >= skills.Count) return false;

        SkillSlot slot = skills[skillIndex];
        return slot != null && slot.skill != null && slot.cooldownTimer <= 0f;
    }
    public void ForceCooldown(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count) return;

        SkillSlot slot = skills[skillIndex];
        if (slot != null && slot.skill != null) slot.cooldownTimer = slot.skill.cooldown;
    }

    public void FireActivatedSkill(SkillContext context, Vector3 aimPoint)
    {
        context.Data.Use(context.UserTransform, context.Tags, aimPoint);
        ForceCooldown(context.SlotIndex);
    }

    private bool TryUse(int skillIndex, Vector3? aimPoint)
    {
        if (hurtBox == null) return false;
        if (activation != null) return false;
        if (skillIndex < 0 || skillIndex >= skills.Count) return false;

        SkillSlot slot = skills[skillIndex];

        if (slot == null || slot.skill == null || slot.cooldownTimer > 0f) return false;

        SkillActivation act = slot.skill.CreateActivation();
        if (act != null)
        {
            var context = new SkillContext(this, slot.skill, skillIndex, hurtBox.transform,
                                           slot.collisionTags, slot.key);
            if (!act.CanBegin(context)) return false;

            activation = act;
            act.Begin(context);
            return true;
        }

        if (aimPoint.HasValue)
            slot.skill.Use(hurtBox.transform, slot.collisionTags, aimPoint.Value);
        else
            slot.skill.Use(hurtBox.transform, slot.collisionTags);

        slot.cooldownTimer = slot.skill.cooldown;
        return true;
    }
}
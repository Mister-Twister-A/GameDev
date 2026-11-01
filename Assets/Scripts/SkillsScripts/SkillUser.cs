using System.Collections.Generic;
using UnityEngine;

public class SkillUser : MonoBehaviour
{
    [System.Serializable]
    public class SkillSlot
    {
        public SkillData skill;
        public List<string> collisionTags = new List<string>();
        [HideInInspector] public float cooldownTimer;
    }

    [SerializeField] private List<SkillSlot> skills = new List<SkillSlot>();

    private HurtBox hurtBox;

    private void Awake()
    {
        hurtBox = GetComponentInChildren<HurtBox>();
        if (hurtBox == null)
            Debug.LogWarning($"{name}: SkillUser found no HurtBox in children, skills can't be used.", this);
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

    private bool TryUse(int skillIndex, Vector3? aimPoint)
    {
        if (hurtBox == null) return false;
        if (skillIndex < 0 || skillIndex >= skills.Count) return false;

        SkillSlot slot = skills[skillIndex];

        if (slot == null || slot.skill == null || slot.cooldownTimer > 0f) return false;

        if (aimPoint.HasValue)
            slot.skill.Use(hurtBox.transform, slot.collisionTags, aimPoint.Value);
        else
            slot.skill.Use(hurtBox.transform, slot.collisionTags);

        slot.cooldownTimer = slot.skill.cooldown;
        return true;
    }
}
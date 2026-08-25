using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillUser : MonoBehaviour
{
    [System.Serializable] public class SkillSlot
    {
        public SkillData skill;
        public List<string> collisionTags = new List<string>();
        [HideInInspector] public float cooldownTimer;
    }

    [SerializeField] private List<SkillSlot> skills = new List<SkillSlot>();

    private HurtBox hurtBox;

    void Start()
    {
        hurtBox = GetComponentInChildren<HurtBox>();
    }

    void Update()
    {
        foreach (SkillSlot slot in skills)
        {
            if (slot != null && slot.cooldownTimer > 0f)
            {
                slot.cooldownTimer -= Time.deltaTime;
            }
        }
    }

    public void TryUseSkill(int skillIndex)
    {
        if (hurtBox == null)return;
        if (skillIndex < 0 || skillIndex >= skills.Count) return;

        SkillSlot slot = skills[skillIndex];

        if (slot.skill == null || slot.cooldownTimer > 0f) return;

        slot.skill.Use(hurtBox.transform, slot.collisionTags);
        slot.cooldownTimer = slot.skill.cooldown;
    }
}
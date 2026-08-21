using UnityEngine;
 
public class SkillUser : MonoBehaviour
{
    public SkillData equippedSkill;

    private HurtBox hurtBox;
 
    float cooldownTimer;

    void Start()
    {
        hurtBox = GetComponentInChildren<HurtBox>();
    }
    void Update()
    {
        cooldownTimer -= Time.deltaTime;
    }

    public void TryUseSkill()
    {
        if (cooldownTimer <= 0f && equippedSkill != null && hurtBox != null)
        {
            equippedSkill.Use(hurtBox.transform);
            cooldownTimer = equippedSkill.cooldown;
        }
    }
}
 
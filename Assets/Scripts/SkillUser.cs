using UnityEngine;
 
public class SkillUser : MonoBehaviour
{
    public SkillData equippedSkill;
 
    float cooldownTimer;
 
    void Update()
    {
        cooldownTimer -= Time.deltaTime;
 
        if (Input.GetKeyDown(KeyCode.Mouse0) && cooldownTimer <= 0f && equippedSkill != null)
        {
            equippedSkill.Use(gameObject);
            cooldownTimer = equippedSkill.cooldown;
        }
    }
}
 
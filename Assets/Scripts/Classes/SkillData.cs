using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public abstract class SkillData : ScriptableObject
{
    public string skillName;
    public float cooldown;
    public abstract void Use(Transform user);
}

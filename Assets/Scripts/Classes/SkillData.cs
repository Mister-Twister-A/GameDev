using System.Collections.Generic;
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    public string skillName;
    public float cooldown;

    public abstract void Use(Transform user, List<string> tags);

    public virtual void Use(Transform user, List<string> tags, Vector3 aimPoint)
    {
        Use(user, tags);
    }
}
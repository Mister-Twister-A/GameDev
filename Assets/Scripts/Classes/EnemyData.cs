using UnityEngine;

//[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public abstract class EnemyData : MonoBehaviour
{
    public string enemyName;
    public float health;
    public int cost;

    public abstract void Spawn(Transform pos);
    public  abstract void Behaviour();
    public abstract void OnDeath();
}

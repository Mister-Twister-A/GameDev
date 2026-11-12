using UnityEngine;

//[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public abstract class EnemyData : MonoBehaviour
{
    public string enemyName;
    public float health;
    public int cost;

    public abstract void Spawn(Vector3 pos);
    public  abstract void Behaviour();
    public abstract void OnDeath(); 
}

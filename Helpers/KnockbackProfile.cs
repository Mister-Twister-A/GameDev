using UnityEngine;

[CreateAssetMenu(fileName = "KnockbackProfile", menuName = "Enemies/Knockback Profile")]
public class KnockbackProfile : ScriptableObject
{
    public bool immune = false;

    [Min(0.01f)] public float mass = 1f;      
    public float maxSpeed = 30f;           
    public float gravityScale = 1f;
    public float airDrag = 0f;          
    public float minAirTime = 0.15f;       
    public float stunDuration = 0.6f;         

    [Range(0f, 1f)] public float inheritMomentum = 1f;  
    public bool grabSurfaceInFlight = false;             
}
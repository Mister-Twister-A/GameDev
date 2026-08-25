using UnityEngine;

public class PlayerData : EnemyData
{

    public override void Behaviour()
    {
        throw new System.NotImplementedException();
    }

    public override void OnDeath()
    {
        Application.Quit();
    }
}

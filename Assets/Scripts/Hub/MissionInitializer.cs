using UnityEngine;
 
public class MissionInitializer : MonoBehaviour
{
    public EnemySpawner spwn1;
    void Start()
    {
        MissionData data = GameSession.Instance.PendingMission;
 
        Debug.Log($"Starting '{data.missionId}' at difficulty {data.difficulty}, seed {data.seed}");
        spwn1.curWave = data.difficulty;
 
        Random.InitState(data.seed); 
    }
}
 
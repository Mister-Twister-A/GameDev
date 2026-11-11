using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// One per map scene (replaces MissionInitializer).
/// Flow: spawn the single giant -> break the weakpoint -> charge zone appears around the walking giant ->
/// stay inside it until charged -> giant explodes -> next level.
/// </summary>
public class LevelDirector : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private Transform giantSpawnPoint;

    [Header("Giants")]
    [SerializeField] private GameObject[] giantPrefabs;

    [Header("Difficulty scaling per level")]
    [SerializeField] private int waveStepPerLevel = 3;
    [SerializeField] private float giantHealthStep = 0.5f;      
    [SerializeField] private float chargeTimeBase = 40f;
    [SerializeField] private float chargeTimeStep = 15f;
    [SerializeField] private float chargeCreditMultiplier = 1.5f;

    [Header("Flow")]
    [SerializeField] private float exitDelay = 3f;              

    [Header("UI")]
    public UnityEvent<string> onObjectiveChanged;

    private TestTitan giant;
    private int levelIndex;

    void Start()
    {
        MissionData mission = GameSession.Instance.PendingMission;
        levelIndex = mission.levelIndex;

        Debug.Log($"Level {levelIndex + 1}/{mission.levelCount} of '{mission.missionId}' " +
                  $"(difficulty {mission.difficulty}, seed {mission.seed})");

        Random.InitState(mission.seed + levelIndex * 7919);

        if (!SetupGiant()) return;

        spawner.Begin(Mathf.Max(1, mission.difficulty + levelIndex * waveStepPerLevel));
        SetObjective("Destroy the weak point on the giant's head");
    }

    bool SetupGiant()
    {
        Transform player = null;
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null) player = playerGo.transform;

        TestTitan[] existing = FindObjectsByType<TestTitan>(FindObjectsSortMode.None);

        if (giantPrefabs != null && giantPrefabs.Length > 0)
        {
            foreach (var g in existing) Destroy(g.gameObject);   

            GameObject prefab = giantPrefabs[Mathf.Min(levelIndex, giantPrefabs.Length - 1)];
            Vector3 pos = giantSpawnPoint ? giantSpawnPoint.position : Vector3.zero;
            Quaternion rot = giantSpawnPoint ? giantSpawnPoint.rotation : Quaternion.identity;
            giant = Instantiate(prefab, pos, rot).GetComponent<TestTitan>();
        }
        else
        {
            if (existing.Length == 0) { Debug.LogError("LevelDirector: no giant prefab and none in scene."); return false; }
            giant = existing[0];
            for (int i = 1; i < existing.Length; i++)
            {
                Debug.LogWarning("Only one giant per stage - removing extra.");
                Destroy(existing[i].gameObject);
            }
        }

        if (giant == null) { Debug.LogError("Giant prefab is missing a TestTitan component."); return false; }

        giant.Init(1f + giantHealthStep * levelIndex,
                   chargeTimeBase + chargeTimeStep * levelIndex,
                   player);
        giant.WeakpointBroken += OnWeakpointBroken;
        giant.ChargeCompleted += OnChargeCompleted;

        if (giant.EnemySpawnPoint != null)
            spawner.spawnLocation = giant.EnemySpawnPoint;

        return true;
    }

    void OnWeakpointBroken(TestTitan g)
    {
        spawner.creditMultiplier = chargeCreditMultiplier;
        SetObjective("Weak point destroyed! Stay inside the zone around the giant to charge it");
    }

    void OnChargeCompleted(TestTitan g)
    {
        spawner.Pause();  
        SetObjective("The giant is going down!");
        Invoke(nameof(FinishLevel), exitDelay);
    }

    void FinishLevel() => GameSession.Instance.CompleteLevel();

    public void FailLevel() => GameSession.Instance.FailMission();

    void SetObjective(string text) => onObjectiveChanged?.Invoke(text);

    void OnDestroy()
    {
        if (giant != null)
        {
            giant.WeakpointBroken -= OnWeakpointBroken;
            giant.ChargeCompleted -= OnChargeCompleted;
        }
    }
}
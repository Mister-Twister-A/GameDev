using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }
    public MissionData PendingMission { get; private set; } = new MissionData();
    public bool IsLoading { get; private set; }

    public string hubSceneName = "Hub";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("GameSession").AddComponent<GameSession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void StartMission(MissionData data)
    {
        if (data.levelScenes == null || data.levelScenes.Length == 0)
        {
            Debug.LogError("GameSession: MissionData has no levelScenes.");
            return;
        }
        data.levelCount = data.levelScenes.Length;
        data.levelIndex = 0;
        PendingMission = data;
        LoadScene(data.levelScenes[0]);
    }

    public void CompleteLevel()
    {
        if (IsLoading) return;

        if (PendingMission.IsLastLevel)
        {
            Debug.Log($"Mission '{PendingMission.missionId}' complete!");
            ReturnToHub();
            return;
        }

        PendingMission.levelIndex++;
        LoadScene(PendingMission.levelScenes[PendingMission.levelIndex]);
    }

    public void FailMission()
    {
        Debug.Log("Mission failed.");
        ReturnToHub();
    }

    public void ReturnToHub()
    {
        PendingMission = new MissionData();
        LoadScene(hubSceneName);
    }

    public static string[] PickLevelMaps(string[] pool, int count, int seed)
    {
        var rng = new System.Random(seed);
        var result = new string[count];
        int last = -1;
        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(pool.Length);
            if (pool.Length > 1 && idx == last)
                idx = (idx + 1 + rng.Next(pool.Length - 1)) % pool.Length;
            result[i] = pool[idx];
            last = idx;
        }
        return result;
    }

    void LoadScene(string sceneName)
    {
        if (IsLoading) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        IsLoading = true;
        Time.timeScale = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone)
            yield return null;
        IsLoading = false;
    }
}
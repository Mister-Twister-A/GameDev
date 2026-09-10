using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }
    public MissionData PendingMission { get; private set; } = new MissionData();

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
    public void StartMission(MissionData data, string missionSceneName = "Mission")
    {
        PendingMission = data;
        SceneManager.LoadScene(missionSceneName, LoadSceneMode.Single);
    }
    public void StartMissionAsync(MissionData data, string missionSceneName = "Mission")
    {
        PendingMission = data;
        StartCoroutine(LoadMissionRoutine(missionSceneName));
    }

    IEnumerator LoadMissionRoutine(string missionSceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(missionSceneName, LoadSceneMode.Single);
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;
    }
    public void ReturnToHub(string hubSceneName = "Hub")
    {
        SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
    }
}
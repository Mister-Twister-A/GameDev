using UnityEngine;
using UnityEngine.UI;

public class HubMissionLauncher : MonoBehaviour
{
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private Button startButton;
    [SerializeField] private string missionId = "1";
    [SerializeField] private string[] mapScenes = { "MissionTest"};   
    [SerializeField] private int levelCount = 3;

    void Start()
    {
        startButton.onClick.AddListener(OnStartPressed);
    }

    void OnStartPressed()
    {
        int seed = Random.Range(int.MinValue, int.MaxValue);
        var data = new MissionData
        {
            difficulty = Mathf.RoundToInt(difficultySlider.value),
            missionId = missionId,
            seed = seed,
            levelCount = levelCount,
            levelScenes = GameSession.PickLevelMaps(mapScenes, levelCount, seed),
        };
        GameSession.Instance.StartMission(data);
    }
}
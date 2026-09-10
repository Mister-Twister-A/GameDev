using UnityEngine;
using UnityEngine.UI;

public class HubMissionLauncher : MonoBehaviour
{
    [SerializeField] private Slider difficultySlider;  
    [SerializeField] private Button startButton;
    [SerializeField] private string missionId = "1";
    [SerializeField] private string missionSceneName = "MissionTest";

    void Start()
    {
        startButton.onClick.AddListener(OnStartPressed);
    }

    void OnStartPressed()
    {
        var data = new MissionData
        {
            difficulty = Mathf.RoundToInt(difficultySlider.value),
            missionId = missionId,
            seed = Random.Range(int.MinValue, int.MaxValue),
        };
        GameSession.Instance.StartMissionAsync(data, missionSceneName);
    }
}
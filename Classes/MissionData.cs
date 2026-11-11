using System;

[Serializable]
public class MissionData
{
    public string missionId = "1";
    public int difficulty = 1;          
    public int seed;

    public int levelCount = 3;
    public int levelIndex;             
    public string[] levelScenes;        
    public bool IsLastLevel => levelIndex >= levelCount - 1;
}
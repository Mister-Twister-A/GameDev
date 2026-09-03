using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public List<EnemyData> enemies = new List<EnemyData>();
    public List<EnemyData> enemiesToSpawn = new List<EnemyData>();

    [Header("Spawning")]
    public int curCredits = 0;
    public int curWave = 1;
    public Transform spawnLocation;

    public float waveInterval;
    public float spawnInterval;

    private float waveTimer;
    private float spawnTimer;




    private void Start() {
        GenerateWave();
    }

    private void FixedUpdate()
    {
        if(spawnTimer < 0)
        {
            if (enemiesToSpawn.Count > 0)
            {
                enemiesToSpawn[0].Spawn(spawnLocation);
                enemiesToSpawn.RemoveAt(0);
                spawnTimer = spawnInterval;
            }
            else
            {
                waveTimer =0f;
            }
        }
        else
        {
            spawnTimer -= Time.deltaTime;
            waveTimer -= Time.deltaTime;
        }
    }

    void GenerateWave()
    {
        curCredits= curWave * 20;
        GenerateEnemies();

        if(enemiesToSpawn.Count == 0) return;
        spawnInterval = waveInterval / enemiesToSpawn.Count;
        waveTimer = waveInterval;

    }

    void GenerateEnemies()
    {
        List<EnemyData> generatedEnemies = new List<EnemyData>();
        while (curCredits> 0)
        {
            int randId = Random.Range(0, enemies.Count);
            int randCost = enemies[randId].cost;

            if (curCredits - randCost >= 0)
            {
                generatedEnemies.Add(enemies[randId]);
                curCredits -= randCost;
            }
            else if(curCredits <= 0)
            {
                break;
            }
        }
        enemiesToSpawn.Clear();
        enemiesToSpawn = generatedEnemies;
        enemiesToSpawn.Add(enemies[1]);
    }
}

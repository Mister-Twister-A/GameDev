using System.Collections.Generic;
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

    public float waveInterval = 20f;
    public float spawnInterval;

    public float creditMultiplier = 1f;

    private Transform[] overridePoints;
    private bool running;
    private float waveTimer;
    private float spawnTimer;
    private readonly List<EnemyData> affordable = new List<EnemyData>();

    public bool IsRunning => running;
    public void Begin(int startWave)
    {
        curWave = Mathf.Max(1, startWave);
        enemiesToSpawn.Clear();
        spawnTimer = 0f;
        running = true;
        GenerateWave();
    }

    public void Pause() => running = false;
    public void Resume() => running = true;

    public void SetSpawnPoints(Transform[] points) => overridePoints = points;

    private void FixedUpdate()
    {
        if (!running) return;

        float dt = Time.fixedDeltaTime;
        waveTimer -= dt;
        spawnTimer -= dt;

        if (waveTimer <= 0f)
        {
            curWave++;
            GenerateWave();
            Debug.Log($"Generating Wave {curWave}");
        }

        if (spawnTimer <= 0f && enemiesToSpawn.Count > 0)
        {
            Transform point = GetSpawnPoint();
            if (point == null) return;  

            enemiesToSpawn[0].Spawn(point);
            enemiesToSpawn.RemoveAt(0);
            spawnTimer = spawnInterval;
        }
    }

    private Transform GetSpawnPoint()
    {
        if (overridePoints != null && overridePoints.Length > 0)
        {
            Transform p = overridePoints[Random.Range(0, overridePoints.Length)];
            if (p != null) return p;
        }
        return spawnLocation;
    }

    private void GenerateWave()
    {
        curCredits = Mathf.RoundToInt(curWave * 10 * creditMultiplier);
        GenerateEnemies();

        waveTimer = waveInterval;
        spawnInterval = enemiesToSpawn.Count > 0 ? waveInterval / enemiesToSpawn.Count : waveInterval;
    }

    private void GenerateEnemies()
    {
        int credits = curCredits;

        while (credits > 0)
        {
            affordable.Clear();
            foreach (var e in enemies)
                if (e.cost > 0 && e.cost <= credits) affordable.Add(e);

            if (affordable.Count == 0) break;

            EnemyData pick = affordable[Random.Range(0, affordable.Count)];
            enemiesToSpawn.Add(pick);
            credits -= pick.cost;
        }
        curCredits = credits;
    }
}
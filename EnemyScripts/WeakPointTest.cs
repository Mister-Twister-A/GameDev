using System;
using System.Diagnostics;
using UnityEngine;

public class WeakPointTest : EnemyData
{
    [SerializeField] private float baseHealth = 10f;

    public event Action Destroyed;
    public float MaxHealth { get; private set; }
    public bool IsDestroyed { get; private set; }

    void Awake()
    {
        UnityEngine.Debug.Log("awaked");
        MaxHealth = baseHealth;
        health = Mathf.RoundToInt(MaxHealth);
    }
    public void ApplyHealthScale(float scale)
    {
        MaxHealth = baseHealth * scale;
        health = Mathf.RoundToInt(MaxHealth);
    }


    public void TakeDamage(float amount) => health -= amount;

    void Update()
    {
        if (!IsDestroyed && health <= 0)
            OnDeath();
    }

    public override void OnDeath()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        gameObject.SetActive(false);
        Destroyed?.Invoke();
    }
    public override void Behaviour()
    {
        throw new System.NotImplementedException();
    }
    public override void Spawn(Vector3 pos)
    {
        throw new System.NotImplementedException();
    }
}
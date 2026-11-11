using System;
using UnityEngine;

public class WeakPointTest : EnemyData
{
    [SerializeField] private float baseHealth = 100f;

    public event Action Destroyed;
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDestroyed { get; private set; }

    void Awake()
    {
        MaxHealth = baseHealth;
        Health = MaxHealth;
    }

    public void ApplyHealthScale(float scale)
    {
        MaxHealth = baseHealth * scale;
        Health = MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed || amount <= 0f) return;

        Health -= amount;
        if (Health > 0f) return;

        IsDestroyed = true;
        gameObject.SetActive(false);
        Destroyed?.Invoke();
    }
}
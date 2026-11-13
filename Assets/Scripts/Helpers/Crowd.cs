using System;
using System.Collections.Generic;
using UnityEngine;
public sealed class CrowdMap<T> where T : struct, IEquatable<T>
{
    private readonly Dictionary<T, int> counts = new();
    private readonly Dictionary<UnityEngine.Object, T> slots = new();

    public void Report(UnityEngine.Object owner, T key)
    {
        if (slots.TryGetValue(owner, out T old))
        {
            if (old.Equals(key)) return;
            Decrement(old);
        }

        slots[owner] = key;
        counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
    }

    public void Remove(UnityEngine.Object owner)
    {
        if (!slots.TryGetValue(owner, out T old)) return;
        slots.Remove(owner);
        Decrement(old);
    }

    public int Count(T key) => counts.TryGetValue(key, out int c) ? c : 0;

    public void Clear()
    {
        counts.Clear();
        slots.Clear();
    }

    private void Decrement(T key)
    {
        if (!counts.TryGetValue(key, out int c)) return;
        if (c <= 1) counts.Remove(key);
        else counts[key] = c - 1;
    }
}

public static class Crowd
{
    public static readonly CrowdMap<GroundNodeRef> Ground = new();
    public static readonly CrowdMap<FaceRef> Faces = new();
    public static float GroundPenaltyPerEnemy = 2f;
    public static float FacePenaltyPerEnemy = 3f;
    public const int MaxCounted = 3;

    public static float WithGroundPenalty(float cost, GroundNodeRef node) =>
        cost < 0f ? cost : cost + Mathf.Min(Ground.Count(node), MaxCounted) * GroundPenaltyPerEnemy;

    public static float WithFacePenalty(float cost, FaceRef face) =>
        cost < 0f ? cost : cost + Mathf.Min(Faces.Count(face), MaxCounted) * FacePenaltyPerEnemy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Ground.Clear();
        Faces.Clear();
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;
public class ClimbEntryPointRegestry : MonoBehaviour
{
    public static ClimbEntryPointRegestry Instance { get; private set; }
 
    private readonly List<(FaceRef face, GroundNodeRef node)> entries = new();
 
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
 
        Instance = this;
        Rebuild();
    }
    public void Rebuild()
    {
        entries.Clear();
 
        var surfaces = FindObjectsByType<ClimbableSurface>(FindObjectsInactive.Exclude);
 
        foreach (var surface in surfaces)
        {
            if (surface.faces == null) continue;
 
            for (int i = 0; i < surface.faces.Length; i++)
            {
                var face = surface.faces[i];
                if (!face.IsEntryPoint || !face.HasLinkedGroundNode) continue;
 
                entries.Add((new FaceRef(surface, i), face.LinkedGroundNode));
            }
        }
    }
    public bool TryFindNearestEntry(Vector3 fromWorldPosition, out FaceRef entryFace, out GroundNodeRef groundNode)
    {
        entryFace = default;
        groundNode = default;
 
        float bestSqrDist = float.MaxValue;
        bool found = false;
 
        foreach (var (face, node) in entries)
        {
            if (!node.IsValid) continue;
 
            float sqrDist = (node.WorldPosition - fromWorldPosition).sqrMagnitude;
            if (sqrDist >= bestSqrDist) continue;
 
            bestSqrDist = sqrDist;
            entryFace = face;
            groundNode = node;
            found = true;
        }
 
        return found;
    }
}

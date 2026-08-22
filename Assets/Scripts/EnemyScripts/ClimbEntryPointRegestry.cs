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
    public bool TryFindNearestEntryOnSurface(ClimbableSurface surface, Vector3 fromWorldPosition,
        out FaceRef entryFace, out GroundNodeRef groundNode)
    {
        entryFace = default;
        groundNode = default;

        float bestSqrDist = float.MaxValue;
        bool found = false;

        foreach (var (face, cachedNode) in entries)   
        {
            if (face.surface != surface) continue;

            float sqrDist = (face.WorldPosition - fromWorldPosition).sqrMagnitude;
            if (sqrDist >= bestSqrDist) continue;

            GroundNodeRef node = cachedNode;  

            if (surface.isDynamic)             
            {
                GroundNodeGraph graph = face.Face.linkedGroundGraph;
                if (graph == null) continue;

                int nearestIndex = graph.FindNearestNodeIndex(face.WorldPosition);
                if (nearestIndex < 0) continue;

                node = new GroundNodeRef(graph, nearestIndex);
            }

            if (!node.IsValid) continue;   

            bestSqrDist = sqrDist;
            entryFace = face;
            groundNode = node;
            found = true;
        }

        return found;
    }
}

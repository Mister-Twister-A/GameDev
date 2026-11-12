using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum NodeFlags
{
    None             = 0,
    NoCharacterSpawn = 1 << 0, 
    NoCeiling        = 1 << 1, 
}

[Serializable]
public class GroundNode
{
    public Vector3 position;
    public NodeFlags flags;
    public List<GroundEdge> edges = new();
}

public class GroundNodeGraph : MonoBehaviour
{
    [SerializeField] private List<GroundNode> nodes = new();

    [SerializeField, HideInInspector] private float cellSize = 16f;
    [SerializeField, HideInInspector] private Vector3Int[] cellKeys = Array.Empty<Vector3Int>();
    [SerializeField, HideInInspector] private int[] cellStarts = Array.Empty<int>();  
    [SerializeField, HideInInspector] private int[] cellNodeIndices = Array.Empty<int>();

    private Dictionary<Vector3Int, Vector2Int> cellLookup; 
    private readonly HashSet<int> occupied = new();
    private static readonly List<int> scratch = new(256);

    public IReadOnlyList<GroundNode> Nodes => nodes;

    public void SetNodes(List<GroundNode> newNodes, float newCellSize = 16f)
    {
        nodes = newNodes;
        occupied.Clear();
        BuildSpatialGrid(Mathf.Max(0.5f, newCellSize));
    }

    public GroundNode GetNode(int index) => nodes[index];


    private Vector3Int WorldToCell(Vector3 p) => new Vector3Int(
        Mathf.FloorToInt(p.x / cellSize),
        Mathf.FloorToInt(p.y / cellSize),
        Mathf.FloorToInt(p.z / cellSize));

    private void BuildSpatialGrid(float newCellSize)
    {
        cellSize = newCellSize;

        var buckets = new Dictionary<Vector3Int, List<int>>();
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3Int key = WorldToCell(nodes[i].position);
            if (!buckets.TryGetValue(key, out var list))
                buckets[key] = list = new List<int>();
            list.Add(i);
        }

        cellKeys = new Vector3Int[buckets.Count];
        cellStarts = new int[buckets.Count + 1];
        cellNodeIndices = new int[nodes.Count];

        int c = 0, cursor = 0;
        foreach (var kv in buckets)
        {
            cellKeys[c] = kv.Key;
            cellStarts[c] = cursor;
            foreach (int idx in kv.Value) cellNodeIndices[cursor++] = idx;
            c++;
        }
        cellStarts[c] = cursor;

        cellLookup = null; // rebuilt lazily
    }

    private void EnsureRuntimeLookup()
    {
        if (cellLookup != null && cellLookup.Count == cellKeys.Length) return;

        cellLookup = new Dictionary<Vector3Int, Vector2Int>(cellKeys.Length);
        for (int i = 0; i < cellKeys.Length; i++)
            cellLookup[cellKeys[i]] = new Vector2Int(cellStarts[i], cellStarts[i + 1] - cellStarts[i]);
    }

    public void GatherCandidates(
        Vector3 anchor, float minDistance, float maxDistance,
        NodeFlags requiredFlags, NodeFlags forbiddenFlags,
        List<int> results)
    {
        results.Clear();
        if (nodes.Count == 0 || cellKeys.Length == 0) return;

        EnsureRuntimeLookup();

        float minSqr = minDistance * minDistance;
        float maxSqr = maxDistance * maxDistance;

        Vector3 extent = new Vector3(maxDistance, maxDistance, maxDistance);
        Vector3Int minCell = WorldToCell(anchor - extent);
        Vector3Int maxCell = WorldToCell(anchor + extent);

        for (int cx = minCell.x; cx <= maxCell.x; cx++)
        for (int cy = minCell.y; cy <= maxCell.y; cy++)
        for (int cz = minCell.z; cz <= maxCell.z; cz++)
        {
            if (!cellLookup.TryGetValue(new Vector3Int(cx, cy, cz), out Vector2Int range))
                continue;

            int end = range.x + range.y;
            for (int i = range.x; i < end; i++)
            {
                int idx = cellNodeIndices[i];
                GroundNode node = nodes[idx];

                float distSqr = (node.position - anchor).sqrMagnitude;
                if (distSqr < minSqr || distSqr > maxSqr) continue;

                if (occupied.Contains(idx)) continue;

                if ((node.flags & requiredFlags) != requiredFlags) continue;
                if ((node.flags & forbiddenFlags) != 0) continue;

                results.Add(idx);
            }
        }
    }

    public bool TryPickSpawnNode(
        Vector3 anchor, float minDistance, float maxDistance,
        NodeFlags requiredFlags, NodeFlags forbiddenFlags,
        out int nodeIndex, bool claim = true)
    {
        GatherCandidates(anchor, minDistance, maxDistance, requiredFlags, forbiddenFlags, scratch);

        if (scratch.Count == 0)
        {
            nodeIndex = -1;
            return false;
        }

        nodeIndex = scratch[UnityEngine.Random.Range(0, scratch.Count)];
        if (claim) occupied.Add(nodeIndex);
        return true;
    }

    public void ClaimNode(int index) => occupied.Add(index);
    public void ReleaseNode(int index) => occupied.Remove(index);
    public bool IsOccupied(int index) => occupied.Contains(index);

    public int FindNearestNodeIndex(Vector3 worldPosition)
    {
        if (nodes == null || nodes.Count == 0) return -1;

        int bestIndex = -1;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < nodes.Count; i++)
        {
            float sqrDist = (nodes[i].position - worldPosition).sqrMagnitude;
            if (sqrDist >= bestSqrDist) continue;

            bestSqrDist = sqrDist;
            bestIndex = i;
        }

        return bestIndex;
    }

    private void OnDrawGizmosSelected()
    {
        if (nodes == null) return;

        Gizmos.color = Color.yellow;
        foreach (GroundNode node in nodes)
            Gizmos.DrawSphere(node.position, 0.15f);

        Gizmos.color = Color.white;
        for (int i = 0; i < nodes.Count; i++)
        {
            foreach (GroundEdge edge in nodes[i].edges)
            {
                if (edge.neighborIndex < 0 || edge.neighborIndex >= nodes.Count) continue;
                if (edge.neighborIndex <= i) continue;

                Gizmos.DrawLine(nodes[i].position, nodes[edge.neighborIndex].position);
            }
        }

        // Spatial grid cells
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        foreach (Vector3Int key in cellKeys)
            Gizmos.DrawWireCube((Vector3)key * cellSize + Vector3.one * (cellSize * 0.5f),
                                Vector3.one * cellSize);
    }
}
using System.Collections.Generic;
using UnityEngine;

public static class GroundPathfinder
{
    private class Node
    {
        public int nodeIndex;
        public Node parent;
        public float gCost;
        public float fCost;
    }

    public static List<int> FindPath(GroundNodeGraph graph, int startNodeIndex, int goalNodeIndex, int maxIterations = 4000)
    {
        if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0) return null;
        if (startNodeIndex < 0 || startNodeIndex >= graph.Nodes.Count) return null;
        if (goalNodeIndex < 0 || goalNodeIndex >= graph.Nodes.Count) return null;
        if (startNodeIndex == goalNodeIndex) return new List<int> { startNodeIndex };

        var open = new List<Node>();
        var openLookup = new Dictionary<int, Node>();
        var closed = new HashSet<int>();

        var startNode = new Node { nodeIndex = startNodeIndex, gCost = 0f };
        startNode.fCost = Heuristic(graph, startNodeIndex, goalNodeIndex);

        open.Add(startNode);
        openLookup[startNodeIndex] = startNode;

        int iterations = 0;

        while (open.Count > 0 && iterations++ < maxIterations)
        {
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].fCost < open[bestIndex].fCost) bestIndex = i;

            Node current = open[bestIndex];
            open.RemoveAt(bestIndex);
            openLookup.Remove(current.nodeIndex);
            closed.Add(current.nodeIndex);

            if (current.nodeIndex == goalNodeIndex)
                return ReconstructPath(current);

            foreach (int neighborIndex in graph.Nodes[current.nodeIndex].neighbors)
            {
                if (neighborIndex < 0 || neighborIndex >= graph.Nodes.Count || closed.Contains(neighborIndex)) continue;

                float tentativeG = current.gCost + GetEdgeCost(graph, current.nodeIndex, neighborIndex);

                if (openLookup.TryGetValue(neighborIndex, out Node existing))
                {
                    if (tentativeG >= existing.gCost) continue;

                    existing.gCost = tentativeG;
                    existing.fCost = tentativeG + Heuristic(graph, neighborIndex, goalNodeIndex);
                    existing.parent = current;
                }
                else
                {
                    var node = new Node { nodeIndex = neighborIndex, parent = current, gCost = tentativeG };
                    node.fCost = tentativeG + Heuristic(graph, neighborIndex, goalNodeIndex);
                    open.Add(node);
                    openLookup[neighborIndex] = node;
                }
            }
        }

        return null;
    }

    private static float GetEdgeCost(GroundNodeGraph graph, int fromIndex, int toIndex) =>
        Vector3.Distance(graph.Nodes[fromIndex].position, graph.Nodes[toIndex].position);

    private static float Heuristic(GroundNodeGraph graph, int fromIndex, int goalIndex) =>
        Vector3.Distance(graph.Nodes[fromIndex].position, graph.Nodes[goalIndex].position);

    private static List<int> ReconstructPath(Node end)
    {
        var path = new List<int>();

        for (Node current = end; current != null; current = current.parent)
            path.Add(current.nodeIndex);

        path.Reverse();
        return path;
    }
}
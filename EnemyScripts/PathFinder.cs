using System;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    private sealed class Node<T>
    {
        public T node;
        public Node<T> parent;
        public float gCost;
        public float fCost;
    }

    public static List<T> FindPath<T>(T start, T goal, int maxIterations = 4000)
        where T : struct, IPathNode<T>, IEquatable<T>
    {
        if (!start.IsValid || !goal.IsValid) return null;
        if (start.Equals(goal)) return new List<T> { start };

        var open = new List<Node<T>>();
        var openLookup = new Dictionary<T, Node<T>>();
        var closed = new HashSet<T>();
        var edges = new List<PathEdge<T>>(8);

        var startNode = new Node<T> { node = start };
        startNode.fCost = Heuristic(start, goal);

        open.Add(startNode);
        openLookup[start] = startNode;

        int iterations = 0;

        while (open.Count > 0 && iterations++ < maxIterations)
        {
            int bestIndex = 0;

            for (int i = 1; i < open.Count; i++)
                if (open[i].fCost < open[bestIndex].fCost)
                    bestIndex = i;

            Node<T> current = open[bestIndex];

            open.RemoveAt(bestIndex);
            openLookup.Remove(current.node);
            closed.Add(current.node);

            if (current.node.Equals(goal))
                return ReconstructPath(current);

            edges.Clear();
            current.node.GetEdges(edges);

            for (int i = 0; i < edges.Count; i++)
            {
                PathEdge<T> edge = edges[i];
                T neighbor = edge.node;

                if (!neighbor.IsValid || closed.Contains(neighbor) || edge.cost < 0f)
                    continue;

                float tentativeG = current.gCost + edge.cost;

                if (openLookup.TryGetValue(neighbor, out Node<T> existing))
                {
                    if (tentativeG >= existing.gCost) continue;

                    existing.gCost = tentativeG;
                    existing.fCost = tentativeG + Heuristic(neighbor, goal);
                    existing.parent = current;
                    continue;
                }

                var node = new Node<T>
                {
                    node = neighbor,
                    parent = current,
                    gCost = tentativeG
                };

                node.fCost = tentativeG + Heuristic(neighbor, goal);

                open.Add(node);
                openLookup[neighbor] = node;
            }
        }

        return null;
    }

    private static float Heuristic<T>(T a, T b)
        where T : struct, IPathNode<T>, IEquatable<T>
        => Vector3.Distance(a.WorldPosition, b.WorldPosition);

    private static List<T> ReconstructPath<T>(Node<T> end)
    {
        var path = new List<T>();

        for (Node<T> current = end; current != null; current = current.parent)
            path.Add(current.node);

        path.Reverse();
        return path;
    }
}
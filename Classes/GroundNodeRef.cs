using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct GroundNodeRef : IPathNode<GroundNodeRef>, IEquatable<GroundNodeRef>
{
    public readonly GroundNodeGraph graph;
    public readonly int index;

    public GroundNodeRef(GroundNodeGraph graph, int index)
    {
        this.graph = graph;
        this.index = index;
    }

    public bool IsValid =>
        graph != null &&
        index >= 0 &&
        index < graph.Nodes.Count;

    public Vector3 WorldPosition => graph.Nodes[index].position;

    public void GetEdges(List<PathEdge<GroundNodeRef>> edges)
    {
        if (!IsValid) return;

        GroundNode node = graph.Nodes[index];

        for (int i = 0; i < node.edges.Count; i++)
        {
            GroundEdge edge = node.edges[i];

            if (edge.neighborIndex < 0 || edge.neighborIndex >= graph.Nodes.Count)
                continue;

            edges.Add(new PathEdge<GroundNodeRef>(
                new GroundNodeRef(graph, edge.neighborIndex),
                edge.cost));
        }
    }

    public bool Equals(GroundNodeRef other) =>
        graph == other.graph && index == other.index;

    public override bool Equals(object obj) =>
        obj is GroundNodeRef other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(graph, index);

    public static bool operator ==(GroundNodeRef a, GroundNodeRef b) => a.Equals(b);
    public static bool operator !=(GroundNodeRef a, GroundNodeRef b) => !a.Equals(b);
}
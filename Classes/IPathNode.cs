using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPathNode<TSelf>where TSelf : struct, IPathNode<TSelf>, IEquatable<TSelf>
{
    bool IsValid { get; }
    Vector3 WorldPosition { get; }
    void GetEdges(List<PathEdge<TSelf>> edges);
}
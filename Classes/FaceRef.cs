using System;
using System.Collections.Generic;
using UnityEngine;

[Flags] public enum FaceFlags
{
    None        = 0,
    EntryPoint  = 1 << 0,
    NoTraverse  = 1 << 1,
    Slow        = 1 << 2,
}

public struct FaceRef : IPathNode<FaceRef>, IEquatable<FaceRef>
{
    public ClimbableSurface surface;
    public int faceIndex;

    public FaceRef(ClimbableSurface surface, int faceIndex)
    {
        this.surface = surface;
        this.faceIndex = faceIndex;
    }

    public bool IsValid =>
        surface != null &&
        surface.faces != null &&
        faceIndex >= 0 &&
        faceIndex < surface.faces.Length;

    public ClimbableSurface.Face Face => surface.faces[faceIndex];
    public bool IsEntryPoint => IsValid && Face.IsEntryPoint;

    public Vector3 WorldPosition => WorldCentroid();

    public Vector3 WorldCentroid()
    {
        var verts = Face.vertices;
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < verts.Length; i++)
            sum += surface.transform.TransformPoint(verts[i]);

        return sum / verts.Length;
    }

    public Vector3 WorldNormal() => surface.transform.TransformDirection(Face.normal).normalized;

    public void GetEdges(List<PathEdge<FaceRef>> edges)
    {
        if (!IsValid) return;
        if ((Face.flags & FaceFlags.NoTraverse) != 0) return;

        var face = Face;
        int edgeCount = face.neighborIndices != null ? face.neighborIndices.Length : 0;

        for (int i = 0; i < edgeCount; i++)
        {
            FaceRef neighbor = GetNeighbor(i);

            if (!neighbor.IsValid) continue;
            if ((neighbor.Face.flags & FaceFlags.NoTraverse) != 0) continue;

            edges.Add(new PathEdge<FaceRef>(
                neighbor,
                GetEdgeCost(i, neighbor)));
        }
    }

    private FaceRef GetNeighbor(int edgeIndex)
    {
        var face = Face;
        int internalNeighbor = face.neighborIndices[edgeIndex];

        if (internalNeighbor >= 0)
            return new FaceRef(surface, internalNeighbor);

        if (face.externalNeighborFace == null ||
            face.externalNeighborSurface == null ||
            edgeIndex >= face.externalNeighborFace.Length ||
            edgeIndex >= face.externalNeighborSurface.Length)
            return default;

        int externalFace = face.externalNeighborFace[edgeIndex];
        ClimbableSurface externalSurface = face.externalNeighborSurface[edgeIndex];

        return externalFace >= 0 && externalSurface != null
            ? new FaceRef(externalSurface, externalFace)
            : default;
    }

    private const float SlowCostMultiplier = 2.5f;
    private float GetEdgeCost(int edgeIndex, FaceRef neighbor)
    {
        float dist = Vector3.Distance(WorldPosition, neighbor.WorldPosition);

        if ((neighbor.Face.flags & FaceFlags.Slow) != 0)   
        dist *= SlowCostMultiplier;                     

        return dist;
    }

    public bool Equals(FaceRef other) =>
        surface == other.surface && faceIndex == other.faceIndex;

    public override bool Equals(object obj) =>
        obj is FaceRef other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = faceIndex;
            h = (h * 397) ^
                (surface != null ? surface.GetEntityId().GetHashCode() : 0);

            return h;
        }
    }

    public static bool operator ==(FaceRef a, FaceRef b) => a.Equals(b);
    public static bool operator !=(FaceRef a, FaceRef b) => !a.Equals(b);
}
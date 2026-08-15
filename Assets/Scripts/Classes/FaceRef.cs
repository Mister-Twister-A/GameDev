using System;
using UnityEngine;

// Identifies one face on one ClimbableSurface. This is the "node" the A*
// pathfinder operates on, so it can path across a single mesh AND across
// mesh boundaries (e.g. ground -> giant's leg) using the same struct.
public struct FaceRef : IEquatable<FaceRef>
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

    public Vector3 WorldCentroid()
    {
        var verts = Face.vertices;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < verts.Length; i++)
            sum += surface.transform.TransformPoint(verts[i]);
        return sum / verts.Length;
    }

    public Vector3 WorldNormal() => surface.transform.TransformDirection(Face.normal).normalized;

    public bool Equals(FaceRef other) => surface == other.surface && faceIndex == other.faceIndex;
    public override bool Equals(object obj) => obj is FaceRef other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = faceIndex;
            h = (h * 397) ^ (surface != null ? surface.GetEntityId().GetHashCode() : 0);
            return h;
        }
    }

    public static bool operator ==(FaceRef a, FaceRef b) => a.Equals(b);
    public static bool operator !=(FaceRef a, FaceRef b) => !a.Equals(b);
}
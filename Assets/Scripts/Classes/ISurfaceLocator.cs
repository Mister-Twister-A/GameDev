
using UnityEngine;
public interface ISurfaceLocator
{
    ClimbableSurface CurrentSurface { get; }
    int CurrentFaceIndex { get; }
    Vector3 Position { get; }
}
using System.Collections.Generic;
using UnityEngine;

public class ClimbableSurfaceHolder : MonoBehaviour
{
    public class ClimbEntry
    {
        public ISurfaceLocator controller;
        public Transform part;
        public ClimbableSurface climbableSurface;
    }
    private Dictionary<ISurfaceLocator, ClimbEntry> activeClimbers = new();

    public bool unClimbable = false;
    public bool IsAnyoneClimbing => activeClimbers.Count > 0;
    public ICollection<ClimbEntry> ActiveClimbers => activeClimbers.Values;

    public void RegisterClimberEnter(Transform part, ISurfaceLocator climber)
    {
        if (unClimbable || climber == null) return;

        activeClimbers[climber] = new ClimbEntry
        {
            controller = climber,
            part = part,
            climbableSurface = part.GetComponent<ClimbableSurface>()
        };
    }

    public void RegisterClimberExit(ISurfaceLocator climber)
    {
        if (climber != null) activeClimbers.Remove(climber);
    }

    public bool TryGetEntry(ISurfaceLocator climber, out ClimbEntry entry)
    {
        return activeClimbers.TryGetValue(climber, out entry);
    }

    
}

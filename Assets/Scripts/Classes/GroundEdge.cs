using System;

[Serializable] public struct GroundEdge
{
    public int neighborIndex;
    public float cost;

    public GroundEdge(int neighborIndex, float cost)
    {
        this.neighborIndex = neighborIndex;
        this.cost = cost;
    }
}
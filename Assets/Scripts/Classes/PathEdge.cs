public readonly struct PathEdge<T>
{
    public readonly T node;
    public readonly float cost;

    public PathEdge(T node, float cost)
    {
        this.node = node;
        this.cost = cost;
    }
}
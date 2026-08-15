using System.Collections.Generic;
using UnityEngine;

public static class SurfaceFacePathfinder
{
    private class Node
    {
        public FaceRef face;
        public Node parent;
        public float gCost;
        public float fCost;
    }

    public static List<FaceRef> FindPath(FaceRef start, FaceRef goal, int maxIterations = 4000)
    {
        if (!start.IsValid || !goal.IsValid) return null;
        if (start == goal) return new List<FaceRef> { start };

        var open = new List<Node>();
        var openLookup = new Dictionary<FaceRef, Node>();
        var closed = new HashSet<FaceRef>();

        var startNode = new Node { face = start, parent = null, gCost = 0f };
        startNode.fCost = Heuristic(start, goal);
        open.Add(startNode);
        openLookup[start] = startNode;

        int iterations = 0;
        while (open.Count > 0 && iterations++ < maxIterations)
        {
            int bestIdx = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].fCost < open[bestIdx].fCost) bestIdx = i;

            Node current = open[bestIdx];
            open.RemoveAt(bestIdx);
            openLookup.Remove(current.face);
            closed.Add(current.face);

            if (current.face == goal)
                return ReconstructPath(current);

            foreach (var neighbor in GetNeighbors(current.face))
            {
                if (!neighbor.IsValid || closed.Contains(neighbor)) continue;

                float tentativeG = current.gCost +
                    Vector3.Distance(current.face.WorldCentroid(), neighbor.WorldCentroid());

                if (openLookup.TryGetValue(neighbor, out var existing))
                {
                    if (tentativeG < existing.gCost)
                    {
                        existing.gCost = tentativeG;
                        existing.fCost = tentativeG + Heuristic(neighbor, goal);
                        existing.parent = current;
                    }
                }
                else
                {
                    var node = new Node
                    {
                        face = neighbor,
                        parent = current,
                        gCost = tentativeG,
                        fCost = tentativeG + Heuristic(neighbor, goal)
                    };
                    open.Add(node);
                    openLookup[neighbor] = node;
                }
            }
        }

        return null; 
    }

    private static float Heuristic(FaceRef a, FaceRef b) =>
        Vector3.Distance(a.WorldCentroid(), b.WorldCentroid());

    private static IEnumerable<FaceRef> GetNeighbors(FaceRef f)
    {
        var face = f.Face;
        int edgeCount = face.neighborIndices != null ? face.neighborIndices.Length : 0;

        for (int i = 0; i < edgeCount; i++)
        {
            int internalNeighbor = face.neighborIndices[i];
            if (internalNeighbor >= 0)
            {
                yield return new FaceRef(f.surface, internalNeighbor);
                continue;
            }

            if (face.externalNeighborFace != null && i < face.externalNeighborFace.Length)
            {
                int extFace = face.externalNeighborFace[i];
                var extSurface = face.externalNeighborSurface[i];
                if (extFace >= 0 && extSurface != null)
                    yield return new FaceRef(extSurface, extFace);
            }
        }
    }

    private static List<FaceRef> ReconstructPath(Node end)
    {
        var path = new List<FaceRef>();
        var n = end;
        while (n != null)
        {
            path.Add(n.face);
            n = n.parent;
        }
        path.Reverse();
        return path;
    }
}
using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class GroundNode
{
    public Vector3 position;
    public List<GroundEdge> edges = new();
}
public class GroundNodeGraph : MonoBehaviour
{
    [SerializeField] private List<GroundNode> nodes = new();

    public IReadOnlyList<GroundNode> Nodes => nodes;

    public void SetNodes(List<GroundNode> newNodes) => nodes = newNodes;

    public GroundNode GetNode(int index) => nodes[index];

    private void OnDrawGizmosSelected()
    {
        if (nodes == null) return;

        Gizmos.color = Color.yellow;

        foreach (GroundNode node in nodes)
            Gizmos.DrawSphere(node.position, 0.15f);

        Gizmos.color = Color.white;

        for (int i = 0; i < nodes.Count; i++)
        {
            foreach (GroundEdge edge in nodes[i].edges)
            {
                if (edge.neighborIndex < 0 || edge.neighborIndex >= nodes.Count) continue;
                if (edge.neighborIndex <= i) continue;

                Gizmos.DrawLine(nodes[i].position, nodes[edge.neighborIndex].position);
            }
        }
    }
}
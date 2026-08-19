using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable] public class GroundNode{
    public Vector3 position;
    public List<int> neighbors = new();
}

public class GroundNodeGraph : MonoBehaviour
{
    [SerializeField] private List<GroundNode> nodes = new();
    public IReadOnlyList<GroundNode> Nodes => nodes;
    public void SetNodes(List<GroundNode> newNodes){
        nodes = newNodes;
    }

    public GroundNode GetNode(int index){
        return nodes[index];
    }

    private void OnDrawGizmosSelected()
    {
        if (nodes == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (GroundNode node in nodes)
        {
            Gizmos.DrawSphere(node.position, 0.15f);
        }

        Gizmos.color = Color.white;

        for (int i = 0; i < nodes.Count; i++){
            GroundNode node = nodes[i];

            foreach (int neighborIndex in node.neighbors){
                if (neighborIndex < 0 || neighborIndex >= nodes.Count)
                    continue;

                if (neighborIndex <= i)
                    continue;

                Gizmos.DrawLine(node.position,nodes[neighborIndex].position);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GroundGraphBaker : MonoBehaviour
{
    [Header("Bake Area")]
    [SerializeField]private Vector3 boundsCenter;
    [SerializeField]private Vector3 boundsSize = new Vector3(50f, 10f, 50f);

    [Header("Sampling")]
    [SerializeField]private float gridSpacing = 2f;
    [SerializeField]private float sampleRadius = 0.5f;
    [SerializeField]private int navMeshAreaMask = NavMesh.AllAreas;
    [SerializeField] private LayerMask groundMask;

    [Header("Connections")]
    [SerializeField]private float maxLinkDistance = 3f;

    [Header("Character Clearance")]
    [SerializeField]private float agentRadius = 0.5f;
    [SerializeField]private float agentHeight = 2f;
    [SerializeField]private LayerMask obstacleMask = ~0;
    [SerializeField]private GroundNodeGraph graph;

    public void Bake()
    {
        if (graph == null){
            Debug.LogError("GroundGraphBaker: No GroundNodeGraph assigned.",this);
            return;
        }

        if (gridSpacing <= 0f)
        {
            Debug.LogError("GroundGraphBaker: Grid spacing must be greater than zero.",this);
            return;
        }

        List<GroundNode> nodes = GenerateNodes();

        BuildConnections(nodes);

        graph.SetNodes(nodes);

        Debug.Log($"Ground graph baked. Nodes: {nodes.Count}",graph);

#if UNITY_EDITOR
        EditorUtility.SetDirty(graph);
#endif
    }

   private List<GroundNode> GenerateNodes(){
    List<GroundNode> nodes = new();

    Bounds bounds = new Bounds(boundsCenter,boundsSize);

    for (float x = bounds.min.x;x <= bounds.max.x;x += gridSpacing)
    {
        for (float z = bounds.min.z;z <= bounds.max.z;z += gridSpacing)
        {
            Vector3 rayStart = new Vector3(x,bounds.max.y + 1f,z);

            float rayDistance =bounds.size.y + 2f;

            if (!Physics.Raycast(rayStart,Vector3.down,out RaycastHit groundHit,rayDistance,groundMask,QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 surfacePosition = groundHit.point;

            if (!NavMesh.SamplePosition(surfacePosition,out NavMeshHit navHit,sampleRadius,navMeshAreaMask))
            {
                continue;
            }

            nodes.Add(new GroundNode
            {
                position = navHit.position
            });
        }
    }

    return nodes;
}

    private void BuildConnections(List<GroundNode> nodes)
    {
        float maxDistanceSqr =maxLinkDistance * maxLinkDistance;

        for (int i = 0; i < nodes.Count; i++)
        {
            GroundNode a = nodes[i];

            for (int j = i + 1; j < nodes.Count; j++)
            {
                GroundNode b = nodes[j];

                Vector3 difference =b.position - a.position;

                if (difference.sqrMagnitude > maxDistanceSqr)
                    continue;

                if (!CanWalkBetween(a.position, b.position))
                {
                    continue;
                }

                a.neighbors.Add(j);
                b.neighbors.Add(i);
            }
        }
    }

    private bool CanWalkBetween(Vector3 start, Vector3 end){
        if (NavMesh.Raycast(start, end, out _, navMeshAreaMask))
            return false;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.001f) return false;
        if (agentHeight < agentRadius * 2f) return false;

        direction /= distance;

        Vector3 bottom = start + Vector3.up * agentRadius;
        Vector3 top = start + Vector3.up * (agentHeight - agentRadius);

        return !Physics.CapsuleCast(bottom,top,agentRadius,direction,distance,obstacleMask,QueryTriggerInteraction.Ignore);
    }
    private void OnDrawGizmosSelected(){
        Gizmos.color = Color.cyan;

        Bounds bounds = new Bounds(boundsCenter,boundsSize);

        Gizmos.DrawWireCube(bounds.center,bounds.size);
    }
}


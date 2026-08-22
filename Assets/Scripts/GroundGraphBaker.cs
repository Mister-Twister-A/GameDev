using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

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
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private float navMeshConfirmRadius = 0.15f;
    [SerializeField] private float minSurfaceSeparation = 0.3f;
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

        if (navMeshSurface == null)
        {
            Debug.LogError("GroundGraphBaker: No NavMeshSurface assigned.", this);
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

   private List<GroundNode> GenerateNodes()
    {
        List<GroundNode> nodes = new();
 
        Bounds bounds = new Bounds(boundsCenter, boundsSize);
 
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshSurface.agentTypeID,
            areaMask = navMeshAreaMask
        };
 
        NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(navMeshSurface.agentTypeID);
        //float maxWalkableSlope = buildSettings.agentSlope;
 
        const float skinWidth = 0.02f;
 
        for (float x = bounds.min.x; x <= bounds.max.x; x += gridSpacing)
        {
            for (float z = bounds.min.z; z <= bounds.max.z; z += gridSpacing)
            {
                List<RaycastHit> columnHits = CollectColumnSurfaces(x, z, bounds, skinWidth);
 
                foreach (RaycastHit hit in columnHits)
                {
                   if (Vector3.Angle(hit.normal, Vector3.up) > 89f)
                        continue;
 
                    if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshConfirmRadius, filter))
                        continue;
 
                    nodes.Add(new GroundNode
                    {
                        position = navHit.position
                    });
                }
            }
        }
 
        return nodes;
    }
    private List<RaycastHit> CollectColumnSurfaces(float x, float z, Bounds bounds, float skinWidth)
    {
        List<RaycastHit> hits = new();
 
        Vector3 origin = new Vector3(x, bounds.max.y, z);
        float remaining = bounds.size.y;
 
        while (remaining > 0f)
        {
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, remaining, groundMask, QueryTriggerInteraction.Ignore))
                break;
 
            if (hits.Count == 0 || (hits[^1].point.y - hit.point.y) >= minSurfaceSeparation)
            {
                hits.Add(hit);
            }
 
            float traveled = origin.y - hit.point.y;
            remaining -= traveled + skinWidth;
            origin = hit.point - Vector3.up * skinWidth;
        }
 
        return hits;
    }
    private void BuildConnections(List<GroundNode> nodes){   
        float maxDistanceSqr = maxLinkDistance * maxLinkDistance;

        for (int i = 0; i < nodes.Count; i++){
            GroundNode a = nodes[i];

            for (int j = i + 1; j < nodes.Count; j++){
                GroundNode b = nodes[j];
                Vector3 difference = b.position - a.position;

                if (difference.sqrMagnitude > maxDistanceSqr) continue;
                if (!CanWalkBetween(a.position, b.position)) continue;

                float cost = difference.magnitude;

                a.edges.Add(new GroundEdge(j, cost));
                b.edges.Add(new GroundEdge(i, cost));
            }
        }
    }

    private bool CanWalkBetween(Vector3 start, Vector3 end){

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshSurface.agentTypeID,
            areaMask = navMeshAreaMask
        };
        if (NavMesh.Raycast(start, end, out _, filter))
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


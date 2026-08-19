using System.Collections.Generic;
using UnityEngine;

public class EnemyGroundNavigator : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Graph")]
    public GroundNodeGraph groundGraph;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float repathInterval = 0.5f;
    public float waypointReachedDistance = 0.5f;
    public float directChaseDistance = 1.5f;

    private ISurfaceWalker self;
    private EnemyClimbController climbController;
    private List<int> path;
    private int pathIndex;
    private float repathTimer;
    private int lastStartNode = -1;
    private int lastGoalNode = -1;
    private bool hasLastPath;

    

    private void Awake()
    {
        self = GetComponent<ISurfaceWalker>();
        climbController = GetComponent<EnemyClimbController>();

        if (climbController == null)
            Debug.LogError($"{name}: EnemySurfaceNavigator needs an EnemyClimbController on the same object.");

        if (self == null)
            Debug.LogError($"{name}: EnemyGroundNavigator needs a component implementing ISurfaceWalker on the same object.");

        if (groundGraph == null)
            Debug.LogError($"{name}: EnemyGroundNavigator has no GroundNodeGraph assigned.");

        if (target == null)
            Debug.LogError($"{name}: EnemyGroundNavigator has no target assigned.");
    }

    private void Update()
    {
        if (self == null || groundGraph == null || target == null) return;
        if(climbController.IsClimbing == true) return;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            Repath();
            repathTimer = repathInterval;
        }

        FollowPath();
    }

    private void Repath()
    {
        int startNode = FindNearestNode(self.Position);
        int goalNode = FindNearestNode(target.position);

        if (startNode < 0 || goalNode < 0) return;

        if (hasLastPath && startNode == lastStartNode && goalNode == lastGoalNode && path != null)
            return;

        lastStartNode = startNode;
        lastGoalNode = goalNode;
        hasLastPath = true;

        if (startNode == goalNode)
        {
            path = new List<int> { startNode };
            pathIndex = 0;
            return;
        }

        List<int> newPath = GroundPathfinder.FindPath(groundGraph, startNode, goalNode);

        if (newPath != null)
        {
            path = newPath;
            pathIndex = 0;
        }
        else
        {
            path = null;
        }
    }

    private void FollowPath()
    {
        float distanceToTarget = Vector3.Distance(self.Position, target.position);

        if (distanceToTarget <= directChaseDistance)
        {
            self.MoveTowards(target.position, moveSpeed);
            return;
        }

        if (path == null || path.Count == 0) return;

        if (pathIndex >= path.Count - 1)
        {
            self.MoveTowards(target.position, moveSpeed);
            return;
        }

        Vector3 waypoint = groundGraph.Nodes[path[pathIndex + 1]].position;

        self.MoveTowards(waypoint, moveSpeed);

        if (HasReachedWaypoint(waypoint))
            pathIndex++;
    }

    private bool HasReachedWaypoint(Vector3 waypoint)
    {
        Vector3 planarOffset = Vector3.ProjectOnPlane(self.Position - waypoint, Vector3.up);
        return planarOffset.magnitude <= waypointReachedDistance;
    }

    private int FindNearestNode(Vector3 position)
    {
        if (groundGraph == null || groundGraph.Nodes == null || groundGraph.Nodes.Count == 0)
            return -1;

        int bestIndex = -1;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < groundGraph.Nodes.Count; i++)
        {
            float distanceSqr = (groundGraph.Nodes[i].position - position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
using System.Collections.Generic;
using UnityEngine;
public class EnemySurfaceNavigator : MonoBehaviour
{
    [Header("Target")]
    public MonoBehaviour targetBehaviour;
    private ISurfaceLocator target;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float repathInterval = 0.5f;
    public float waypointReachedDistance = 0.3f;

    private ISurfaceWalker self;
    private EnemyClimbController climbController;
    private List<FaceRef> path;
    private int pathIndex;
    private float repathTimer;
    private FaceRef lastStart;
    private FaceRef lastGoal;
    private bool hasLastPath;

    void Awake()
    {
        self = GetComponent<ISurfaceWalker>();
        target = targetBehaviour as ISurfaceLocator;
        climbController = GetComponent<EnemyClimbController>();

        if (climbController == null)
            Debug.LogError($"{name}: EnemySurfaceNavigator needs an EnemyClimbController on the same object.");

        if (self == null)
            Debug.LogError($"{name}: EnemySurfaceNavigator needs a component implementing ISurfaceWalker on the same object.");
            
        if (targetBehaviour != null && target == null)
            Debug.LogError($"{name}: targetBehaviour does not implement ISurfaceLocator.");
    }

    void Update()
    {
        if (self == null || target == null) return;

        if(!climbController.IsClimbing) return;
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f){
            Repath();
            repathTimer = repathInterval;
        }

        FollowPath();
    }

    void Repath()
    {
        var start = new FaceRef(self.CurrentSurface, self.CurrentFaceIndex);
        var goal = new FaceRef(target.CurrentSurface, target.CurrentFaceIndex);

        if (!start.IsValid || !goal.IsValid) return;

        if (hasLastPath && start == lastStart && goal == lastGoal && path != null) return;

        lastStart = start;
        lastGoal = goal;
        hasLastPath = true;

        if (start == goal){
            path = new List<FaceRef> { start };
            pathIndex = 0;
            return;
        }

        var newPath = Pathfinder.FindPath(start, goal);
        if (newPath != null){
            path = newPath;
            pathIndex = 0;
        }
    }

    void FollowPath()
    {
        if (path == null || path.Count == 0) return;

        var actualCurrentFace = new FaceRef(self.CurrentSurface, self.CurrentFaceIndex);
        var actualGoalFace = new FaceRef(target.CurrentSurface, target.CurrentFaceIndex);

        if (actualCurrentFace.IsValid && actualCurrentFace == actualGoalFace){
            self.MoveTowards(target.Position, moveSpeed);
            return;
        }

        if (pathIndex >= path.Count - 1){
            repathTimer = 0f;
            return;
        }

        FaceRef nextFace = path[pathIndex + 1];
        Vector3 waypoint = EdgeCrossingPoint(path[pathIndex], nextFace);

        self.MoveTowards(waypoint, moveSpeed);

        if (HasReachedWaypoint(waypoint))
            pathIndex++;
    }

    bool HasReachedWaypoint(Vector3 waypoint)
    {
        var currentFace = new FaceRef(self.CurrentSurface, self.CurrentFaceIndex);
        if (!currentFace.IsValid) return Vector3.Distance(self.Position, waypoint) <= waypointReachedDistance;
 
        Vector3 normal = currentFace.WorldNormal();
        Vector3 planarOffset = Vector3.ProjectOnPlane(self.Position - waypoint, normal);
        return planarOffset.magnitude <= waypointReachedDistance;
    }


    Vector3 EdgeCrossingPoint(FaceRef from, FaceRef to)
    {
        var face = from.Face;
        int edgeCount = face.vertices.Length;

        for (int i = 0; i < edgeCount; i++)
        {
            bool matchesInternal = from.surface == to.surface && face.neighborIndices[i] == to.faceIndex;
            bool matchesExternal =face.externalNeighborSurface != null &&
                                face.externalNeighborFace != null &&
                                i < face.externalNeighborSurface.Length &&
                                i < face.externalNeighborFace.Length &&
                                face.externalNeighborSurface[i] == to.surface &&
                                face.externalNeighborFace[i] == to.faceIndex;
            if (matchesInternal || matchesExternal)
            {
                Vector3 a = from.surface.transform.TransformPoint(face.vertices[i]);
                Vector3 b = from.surface.transform.TransformPoint(face.vertices[(i + 1) % edgeCount]);
                return (a + b) * 0.5f;
            }
        }

        return to.WorldCentroid();
    }
}
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyClimbController : MonoBehaviour, ISurfaceWalker
{
    public enum State { Normal, Climbing, Launched }

    [Header("References")]
    public CharacterController controller;
    [SerializeField] private Transform enemyModel;

    public Transform CurrentTarget;
    public PlayerClimbController playerController;

    private Transform EntryPoint;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float climbSpeed = 4f;
    public float gravity = -9.81f;
    public float airDrag = 3f;
    public float turnSpeed = 120f;   // max yaw speed, degrees/sec
    [Range(0.05f, 1f)] public float minTurnSpeedFactor = 0.3f;   // speed multiplier when facing away from move direction
    public float jumpForce = 6f;
    public Vector3? FacingOverride;
    [SerializeField] private float retargetInterval = 0.4f;
    
    

    [Header("Climbing")]
    public LayerMask climbableLayer;
    public float surfaceOffset;
    public float edgeEpsilon = 0.0001f;

    public Transform Transform_ => transform;

    [SerializeField] private float ArrivalThreshold = 0.3f;
    [SerializeField] private float WallPushDistance = 0.5f;

    State state = State.Normal;
    ClimbableSurface currentSurface;
    int currentFaceIndex = -1;
    public Vector3 verticalVelocity;
    Vector3 desiredWorldDirection = Vector3.zero;
    float desiredSpeed = 0f;

    Vector3 localSurfacePoint;

    [Header("Knockback")]
    [SerializeField] private KnockbackProfile knockbackProfile;
    float stunTimer;
    float airTime;
    Vector3 trackedVelocity;
    Vector3 lastPos;

    public State CurrentBehaviourState => state;
    public bool IsClimbing => state == State.Climbing;
    public bool CanAct => state != State.Launched && stunTimer <= 0f;

    public static readonly List<EnemyClimbController> All = new();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    // ISurfaceWalker
    public ClimbableSurface CurrentSurface => currentSurface;
    public int CurrentFaceIndex => currentFaceIndex;
    public Vector3 Position => transform.position;

    public void MoveTowards(Vector3 worldTargetPoint, float speed)
    {
        if (!CanAct) { desiredWorldDirection = Vector3.zero; desiredSpeed = 0f; return; }

        Vector3 toTarget = worldTargetPoint - transform.position;

        if (state == State.Climbing && currentSurface != null &&
            currentFaceIndex >= 0 &&
            currentFaceIndex < currentSurface.faces.Length)
        {
            Vector3 normal = currentSurface.transform.TransformDirection(currentSurface.faces[currentFaceIndex].normal).normalized;
            toTarget = Vector3.ProjectOnPlane(toTarget, normal);
        }

        desiredWorldDirection =toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;

        desiredSpeed = speed;
    }

    void Reset() => controller = GetComponent<CharacterController>();

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        surfaceOffset = controller.height * 0.5f;
        if (knockbackProfile == null) knockbackProfile = ScriptableObject.CreateInstance<KnockbackProfile>();
        lastPos = transform.position;

        var markerGO = new GameObject($"{name}_EntryMarker")
        {
            //hideFlags = HideFlags.HideInHierarchy
        };
        EntryPoint = markerGO.transform;
        if(playerController == null) playerController = GameObject.FindWithTag("Player").GetComponent<PlayerClimbController>();
    }
    private float retargetTimer;
    void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            RefreshTarget();
        }
        if (state == State.Normal) NormalUpdate();
        else if (state == State.Launched) LaunchedUpdate();
        else ClimbingUpdate();
    }

    private void RefreshTarget()
    {
        
        var playerisclimbing = playerController.IsClimbing;
        if(state == State.Normal && playerisclimbing && playerController.CurrentSurface != null 
        && ClimbEntryPointRegestry.Instance.TryFindNearestEntryOnSurface(
            playerController.CurrentSurface, playerController.transform.position,
            out FaceRef entryFace, out GroundNodeRef entryNode))
        {
            bool reachedGroundNode =(transform.position - entryNode.WorldPosition).sqrMagnitude<= ArrivalThreshold * ArrivalThreshold;
            //Debug.Log(reachedGroundNode);
            EntryPoint.position = reachedGroundNode ? entryFace.WorldPosition - entryFace.WorldNormal() * WallPushDistance : entryNode.WorldPosition;
            CurrentTarget = EntryPoint;
            if (EntryPoint == null)
        {
            Debug.LogError($"{name}: EntryPoint not assigned!", this);
            return;
        }
            //Debug.Log($"reachedGroundNode={reachedGroundNode} target={EntryPoint.position} dist={(transform.position - EntryPoint.position).magnitude}");
        }
        else if (state == State.Climbing && !playerisclimbing)
        {
            ExitClimbState();
            verticalVelocity = transform.up * jumpForce;
            Vector3 currentEuler = transform.rotation.eulerAngles;
            Quaternion tgtRotation = Quaternion.Euler(0f, currentEuler.y, 0f);
            transform.rotation = tgtRotation;
            CurrentTarget = playerController.transform;
        }
        else
        {
            CurrentTarget = playerController.transform;
        }
    }

    void NormalUpdate()
    {
        Vector3 flatDir = Vector3.ProjectOnPlane(desiredWorldDirection, transform.up);
        if (flatDir.sqrMagnitude > 0.0001f) flatDir.Normalize();

        float speed = Mathf.Min(desiredSpeed, walkSpeed) * AlignmentFactor(flatDir);
        Vector3 move = flatDir * speed;

        if (flatDir.sqrMagnitude > 0.0001f || FacingOverride.HasValue)
{
            Vector3 faceDir = FacingOverride.HasValue? Vector3.ProjectOnPlane(FacingOverride.Value, transform.up): flatDir;

            if (faceDir.sqrMagnitude > 0.0001f)
            {
                TurnModel(faceDir.normalized, transform.up);
            }
        }

        if (controller.isGrounded && verticalVelocity.y < 0) verticalVelocity = Vector3.zero;

        verticalVelocity += Vector3.down * -gravity * Time.deltaTime;

        Vector3 horizontal = new Vector3(verticalVelocity.x, 0f, verticalVelocity.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, airDrag * Time.deltaTime);
        verticalVelocity = new Vector3(horizontal.x, verticalVelocity.y, horizontal.z);

        Vector3 horizontalVel = move + new Vector3(verticalVelocity.x, 0f, verticalVelocity.z);
        horizontalVel = Vector3.ClampMagnitude(horizontalVel, walkSpeed);

        controller.Move((horizontalVel + Vector3.up * verticalVelocity.y) * Time.deltaTime); 
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (state == State.Launched)
        {
            if (!knockbackProfile.grabSurfaceInFlight || airTime < knockbackProfile.minAirTime) return;
        }
        else if (state != State.Normal) return;
        if (((1 << hit.gameObject.layer) & climbableLayer) == 0) return;

        var surface = hit.collider.GetComponent<ClimbableSurface>();
        if (surface == null || !surface.IsValid()) return;

        Ray ray = new Ray(hit.point + hit.normal * 0.1f, -hit.normal);

        if (!Physics.Raycast(ray, out RaycastHit rayHit, 1f, climbableLayer))
            return;

        int faceIndex = surface.GetFaceFromTriangle(rayHit.triangleIndex);
        if (faceIndex < 0) return;

        EnterClimbState(surface, faceIndex, rayHit.point, rayHit.normal);
    }

    void EnterClimbState(ClimbableSurface surface, int faceIndex, Vector3 point, Vector3 normal)
    {
        if(surface.climbableSurfaceHolder.unClimbable) return;
        state = State.Climbing;
        verticalVelocity = Vector3.zero;
        currentSurface = surface;
        currentFaceIndex = faceIndex;
        controller.enabled = false;

        transform.position = point + normal * surfaceOffset;
        localSurfacePoint = surface.transform.InverseTransformPoint(point);
        AlignToNormal(normal);
    }

    public void ExitClimbState()
    {
        state = State.Normal;
        controller.enabled = true;
        if (currentSurface != null) currentSurface.climbableSurfaceHolder.RegisterClimberExit(this);
        currentSurface = null;
        currentFaceIndex = -1;
    }
    public void DetachFromSurface(float launchForce = 0f)
    {
        if (state != State.Climbing) return;

        ExitClimbState();
        verticalVelocity = transform.up * launchForce;
        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
    }

    void ClimbingUpdate()
    {
        //Debug.Log($"sqrMgnt {desiredWorldDirection.sqrMagnitude}");

        if (currentSurface != null && currentFaceIndex >= 0)
        {
            Vector3 normal = currentSurface.transform.TransformDirection(
                currentSurface.faces[currentFaceIndex].normal).normalized;

            transform.position = currentSurface.transform.TransformPoint(localSurfacePoint) + normal * surfaceOffset;
            AlignToNormal(normal);
        }

        if (desiredWorldDirection.sqrMagnitude < 0.00001f){ 
            return;
        }

        float speed = Mathf.Min(desiredSpeed, climbSpeed) * AlignmentFactor(desiredWorldDirection);
        Vector3 move = desiredWorldDirection * speed * Time.deltaTime;

        if (desiredWorldDirection.sqrMagnitude >= 0.0001f)
        {
            Vector3 normal = currentSurface.transform.TransformDirection(currentSurface.faces[currentFaceIndex].normal).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(FacingOverride ?? desiredWorldDirection, normal);
            if (forward.sqrMagnitude >= 0.0001f)
            {
                forward.Normalize();
                TurnModel(forward, normal);
            }
        }

        TryMove(move);
    }

    void LateUpdate()
    {
        if (Time.deltaTime > 0f) trackedVelocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
    }

    // Yaw is rate-limited; tilt always follows the surface instantly, so the turn is always around the normal.
    void TurnModel(Vector3 faceDir, Vector3 up)
    {
        Transform model = enemyModel != null ? enemyModel : transform;
        model.rotation = Quaternion.FromToRotation(model.up, up) * model.rotation;
        Quaternion target = Quaternion.LookRotation(faceDir, up);
        model.rotation = Quaternion.RotateTowards(model.rotation, target, turnSpeed * Time.deltaTime);
    }

    // Sharp turns are slow turns. Strafing enemies (FacingOverride) are exempt.
    float AlignmentFactor(Vector3 moveDir)
    {
        if (FacingOverride.HasValue || moveDir.sqrMagnitude < 0.0001f) return 1f;
        Transform model = enemyModel != null ? enemyModel : transform;
        float dot = Vector3.Dot(model.forward, moveDir.normalized);
        return Mathf.Lerp(minTurnSpeedFactor, 1f, Mathf.Clamp01(dot));
    }

    // ---------------- Knockback ----------------
    public void Launch(Vector3 direction, float force)
    {
        KnockbackProfile p = knockbackProfile;
        if (p.immune || direction.sqrMagnitude < 0.0001f) return;

        Vector3 deltaV = direction.normalized * (force / Mathf.Max(0.01f, p.mass));

        bool wasLaunched = state == State.Launched;
        if (state == State.Climbing) DetachFromSurface(0f);

        // Re-entrant: a second hit while airborne adds to the current velocity.
        Vector3 start = wasLaunched ? verticalVelocity : trackedVelocity * p.inheritMomentum;
        verticalVelocity = Vector3.ClampMagnitude(start + deltaV, p.maxSpeed);

        state = State.Launched;
        airTime = 0f;
        desiredWorldDirection = Vector3.zero;
        desiredSpeed = 0f;
    }

    void LaunchedUpdate()
    {
        KnockbackProfile p = knockbackProfile;
        airTime += Time.deltaTime;

        verticalVelocity.y += gravity * p.gravityScale * Time.deltaTime;
        Vector3 h = new Vector3(verticalVelocity.x, 0f, verticalVelocity.z);
        h = Vector3.MoveTowards(h, Vector3.zero, p.airDrag * Time.deltaTime);
        verticalVelocity = new Vector3(h.x, verticalVelocity.y, h.z);

        CollisionFlags flags = controller.Move(verticalVelocity * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity.y > 0f) verticalVelocity.y = 0f;

        if (airTime >= p.minAirTime && controller.isGrounded) Land();
    }

    void Land()
    {
        state = State.Normal;   // OnControllerColliderHit re-grabs the giant on the next frame if we landed on it
        verticalVelocity = Vector3.zero;
        stunTimer = knockbackProfile.stunDuration;
    }

    void TryMove(Vector3 worldMove, int depth = 0)
    {
        if (depth > 8 || currentSurface == null) return;
        if (currentFaceIndex < 0 ||
            currentFaceIndex >= currentSurface.faces.Length) return;

        var face = currentSurface.faces[currentFaceIndex];

        Vector3 normal = currentSurface.transform.TransformDirection(face.normal).normalized;
        Vector3 currentWorld = transform.position - normal * surfaceOffset;
        Vector3 targetWorld = transform.position + worldMove - normal * surfaceOffset;
        Vector3 currentLocal = currentSurface.transform.InverseTransformPoint(currentWorld);
        Vector3 targetLocal = currentSurface.transform.InverseTransformPoint(targetWorld);

        currentLocal = FlattenOntoFace(face, currentLocal);
        targetLocal = FlattenOntoFace(face, targetLocal);

        if (IsInsideFace(face, targetLocal))
        {
            transform.position = currentSurface.transform.TransformPoint(targetLocal) + normal * surfaceOffset;
            localSurfacePoint = targetLocal;
            return;
        }

        int edge = FindCrossedEdge(face, currentLocal, targetLocal, out float t);

        if (edge < 0)
        {
            edge = FindClosestEdge(face, targetLocal);
            t = 1f;
        }

        Vector3 crossingLocal = Vector3.Lerp(currentLocal, targetLocal, t);
        Vector3 crossingWorld = currentSurface.transform.TransformPoint(crossingLocal);

        int neighborIndex = face.neighborIndices[edge];

        ClimbableSurface neighborSurface = currentSurface;
        int resolvedNeighborIndex = neighborIndex;

        if (neighborIndex < 0 &&
            face.externalNeighborSurface != null &&
            face.externalNeighborSurface[edge] != null)
        {
            neighborSurface = face.externalNeighborSurface[edge];
            resolvedNeighborIndex = face.externalNeighborFace[edge];
        }

        if (resolvedNeighborIndex < 0)
        {
            transform.position = crossingWorld + normal * surfaceOffset;
            localSurfacePoint = currentSurface.transform.InverseTransformPoint(crossingWorld);
            return;
        }

        var neighbor = neighborSurface.faces[resolvedNeighborIndex];

        Vector3 neighborNormal = neighborSurface.transform.TransformDirection(neighbor.normal).normalized;
        Quaternion hinge = Quaternion.FromToRotation(normal, neighborNormal);
        Vector3 neighborLocalPoint = neighborSurface.transform.InverseTransformPoint(crossingWorld);
        int neighborEdge = FindClosestEdge(neighbor, neighborLocalPoint);

        Vector3 snappedLocal = ClosestPointOnEdge(neighbor, neighborEdge, neighborLocalPoint);

        Vector3 snappedWorld = neighborSurface.transform.TransformPoint(snappedLocal);

        transform.position = snappedWorld + neighborNormal * surfaceOffset;
        localSurfacePoint = snappedLocal;
        currentSurface = neighborSurface;
        currentFaceIndex = resolvedNeighborIndex;

        //transform.SetParent(neighborSurface.transform, true);
        neighborSurface.climbableSurfaceHolder.RegisterClimberEnter(neighborSurface.transform, this);
        AlignToNormal(neighborNormal);

        float remaining = 1f - t;
        if (remaining <= edgeEpsilon) return;

        TryMove(
            hinge * (worldMove * remaining),
            depth + 1);
    }

    bool IsInsideFace(ClimbableSurface.Face face, Vector3 point)
    {
        GetFaceBasis(face, out Vector3 u, out Vector3 v);
        Vector2 p = To2D(point, face, u, v);

        bool inside = false;
        int n = face.vertices.Length;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = To2D(face.vertices[i], face, u, v);
            Vector2 b = To2D(face.vertices[j], face, u, v);

            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    Vector3 ClosestPointOnEdge(ClimbableSurface.Face face, int edgeIndex, Vector3 point)
    {
        Vector3 a = face.vertices[edgeIndex];
        Vector3 b = face.vertices[(edgeIndex + 1) % face.vertices.Length];

        Vector3 ab = b - a;

        if (ab.sqrMagnitude <= edgeEpsilon)
            return a;

        float t = Mathf.Clamp01(
            Vector3.Dot(point - a, ab) / ab.sqrMagnitude);

        return a + ab * t;
    }

    int FindCrossedEdge(ClimbableSurface.Face face, Vector3 current, Vector3 target, out float movementT)
    {
        movementT = float.MaxValue;

        GetFaceBasis(face, out Vector3 u, out Vector3 v);

        Vector2 start = To2D(current, face, u, v);
        Vector2 end = To2D(target, face, u, v);

        int bestEdge = -1;
        int n = face.vertices.Length;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = To2D(face.vertices[i], face, u, v);
            Vector2 b = To2D(
                face.vertices[(i + 1) % n], face, u, v);

            if (!SegmentIntersection(
                    start, end, a, b, out float t, out _))
                continue;

            if (t <= edgeEpsilon || t >= movementT)
                continue;

            movementT = t;
            bestEdge = i;
        }

        return bestEdge;
    }

    bool SegmentIntersection(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2, out float t, out float u)
    {
        t = u = 0f;

        Vector2 r = p2 - p;
        Vector2 s = q2 - q;

        float denominator = Cross2D(r, s);
        if (Mathf.Abs(denominator) <= edgeEpsilon)
            return false;

        Vector2 qp = q - p;

        t = Cross2D(qp, s) / denominator;
        u = Cross2D(qp, r) / denominator;

        return t >= -edgeEpsilon && t <= 1f + edgeEpsilon &&
               u >= -edgeEpsilon && u <= 1f + edgeEpsilon;
    }

    int FindClosestEdge(ClimbableSurface.Face face, Vector3 point)
    {
        GetFaceBasis(face, out Vector3 u, out Vector3 v);
        Vector2 p = To2D(point, face, u, v);

        float bestDistance = float.MaxValue;
        int bestEdge = 0;
        int n = face.vertices.Length;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = To2D(face.vertices[i], face, u, v);
            Vector2 b = To2D(
                face.vertices[(i + 1) % n], face, u, v);

            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;

            float t = lengthSq > edgeEpsilon
                ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq)
                : 0f;

            Vector2 closest = a + ab * t;
            float distance = (p - closest).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestEdge = i;
            }
        }

        return bestEdge;
    }

    Vector3 FlattenOntoFace(ClimbableSurface.Face face, Vector3 localPoint)
    {
        Vector3 rel = localPoint - face.vertices[0];
        float off = Vector3.Dot(rel, face.normal);
        return localPoint - face.normal * off;
    }

    void AlignToNormal(Vector3 normal)
    {
        transform.rotation = Quaternion.Normalize(Quaternion.FromToRotation(transform.up, normal) * transform.rotation);
    }

    void GetFaceBasis(ClimbableSurface.Face face, out Vector3 u, out Vector3 v)
    {
        u = (face.vertices[1] - face.vertices[0]).normalized;
        v = Vector3.Cross(face.normal, u).normalized;
    }

    Vector2 To2D(Vector3 point, ClimbableSurface.Face face, Vector3 u, Vector3 v)
    {
        Vector3 rel = point - face.vertices[0];
        return new Vector2(
            Vector3.Dot(rel, u),
            Vector3.Dot(rel, v));
    }

    float Cross2D(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}
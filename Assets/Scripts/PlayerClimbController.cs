using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class PlayerClimbController : MonoBehaviour, ISurfaceLocator
{
    public enum State { Normal, Climbing }

    [Header("References")]
    public CharacterController controller;
    public ThirdPersonCam camera;

    [SerializeField] private Transform playerModel;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float climbSpeed = 4f;
    public float jumpForce = 6f;
    public float airDrag = 3f;
    public float gravity = -9.81f;

    public float rotationSpeed = 10f;

    [Header("Climbing")]
    public LayerMask climbableLayer;
    public float surfaceOffset;
    public float edgeEpsilon = 0.0001f;

    [Header("Navigation")]
    public ClimbableSurface CurrentSurface => currentSurface;
    public int CurrentFaceIndex => currentFaceIndex;
    public Vector3 Position => transform.position;

    State state = State.Normal;
    ClimbableSurface currentSurface;
    int currentFaceIndex = -1;
    Vector3 verticalVelocity;

    void Reset() => controller = GetComponent<CharacterController>();

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        surfaceOffset = controller.height * 0.5f;
    }

    void Update()
    {
        if (state == State.Normal) NormalUpdate();
        else ClimbingUpdate();

        bool grounded = state == State.Climbing || (state == State.Normal && controller.isGrounded);

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            ExitClimbState();
            verticalVelocity = transform.up * jumpForce;
            Vector3 currentEuler = transform.rotation.eulerAngles;
            Quaternion tgtRotation = Quaternion.Euler(0f, currentEuler.y, 0f);
            transform.rotation = tgtRotation;
        }
    }

    void NormalUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
       //Vector3 move = (transform.right * h + transform.forward * v) * walkSpeed;
        Vector3 right = Vector3.Cross(transform.up, camera.MovementDirection).normalized;
        Vector3 move = (camera.MovementDirection * v + right * h) * walkSpeed;
         if (move.sqrMagnitude > 0.01f){
            Vector3 forward = Vector3.ProjectOnPlane(move,transform.up).normalized;

            if (forward.sqrMagnitude > 0.01f){
                Quaternion targetRotation =Quaternion.LookRotation(forward, transform.up);
                playerModel.rotation = Quaternion.Slerp(playerModel.rotation,targetRotation,rotationSpeed * Time.deltaTime);
            }
        }
        
        if (controller.isGrounded && verticalVelocity.y < 0) verticalVelocity = Vector3.zero;

        verticalVelocity += Vector3.down * -gravity * Time.deltaTime;

        Vector3 horizontal = new Vector3(verticalVelocity.x, 0f, verticalVelocity.z);

        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, airDrag * Time.deltaTime);

        verticalVelocity = new Vector3(horizontal.x, verticalVelocity.y, horizontal.z);

        Vector3 horizontalVel = move + new Vector3(verticalVelocity.x, 0f, verticalVelocity.z);

        horizontalVel = Vector3.ClampMagnitude(horizontalVel, walkSpeed);

        controller.Move( (horizontalVel + Vector3.up * verticalVelocity.y) *Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (state != State.Normal) return;
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

    void EnterClimbState(ClimbableSurface surface,int faceIndex,Vector3 point,Vector3 normal)
    {
        state = State.Climbing;
        currentSurface = surface;
        currentFaceIndex = faceIndex;
        controller.enabled = false;

        transform.position = point + normal * surfaceOffset;
        AlignToNormal(normal);
    }

    void ExitClimbState()
    {
        state = State.Normal;
        controller.enabled = true;
        currentSurface = null;
        currentFaceIndex = -1;
    }

    void ClimbingUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h == 0f && v == 0f) return;

       // Vector3 move =(transform.right * h + transform.forward * v) *climbSpeed * Time.deltaTime;
        Vector3 right = Vector3.Cross(transform.up, camera.MovementDirection).normalized;
        Vector3 move = (camera.MovementDirection * v + right * h) * climbSpeed * Time.deltaTime;

        if (move.sqrMagnitude >= 0.0001f){
            Vector3 normal = currentSurface.transform.TransformDirection(currentSurface.faces[currentFaceIndex].normal).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(move, normal);
            if (forward.sqrMagnitude >= 0.0001f){
                forward.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(forward,normal);

                float rotT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);

                playerModel.rotation = Quaternion.Slerp(playerModel.rotation,targetRotation,rotT);
            }
        }
        TryMove(move);
    }

    void TryMove(Vector3 worldMove, int depth = 0)
    {
        if (depth > 8 || currentSurface == null) return;
        if (currentFaceIndex < 0 ||
            currentFaceIndex >= currentSurface.faces.Length) return;

        var face = currentSurface.faces[currentFaceIndex];

        Vector3 normal = currentSurface.transform.TransformDirection(face.normal).normalized;
        Vector3 currentWorld =transform.position - normal * surfaceOffset;
        Vector3 targetWorld =transform.position + worldMove - normal * surfaceOffset;
        Vector3 currentLocal =currentSurface.transform.InverseTransformPoint(currentWorld);
        Vector3 targetLocal =currentSurface.transform.InverseTransformPoint(targetWorld);

        currentLocal = FlattenOntoFace(face, currentLocal);
        targetLocal = FlattenOntoFace(face, targetLocal);

        if (IsInsideFace(face, targetLocal))
        {
            transform.position =currentSurface.transform.TransformPoint(targetLocal) +normal * surfaceOffset;
            return;
        }

        int edge = FindCrossedEdge(face, currentLocal, targetLocal, out float t);

        if (edge < 0)
        {
            edge = FindClosestEdge(face, targetLocal);
            t = 1f;
        }

        Vector3 crossingLocal =Vector3.Lerp(currentLocal, targetLocal, t);
        Vector3 crossingWorld =currentSurface.transform.TransformPoint(crossingLocal);

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
            return;
        }

        var neighbor = neighborSurface.faces[resolvedNeighborIndex];

        Vector3 neighborNormal =neighborSurface.transform.TransformDirection(neighbor.normal).normalized;
        Quaternion hinge =Quaternion.FromToRotation(normal, neighborNormal);
        Vector3 neighborLocalPoint =neighborSurface.transform.InverseTransformPoint(crossingWorld);
        int neighborEdge =FindClosestEdge(neighbor, neighborLocalPoint);

        Vector3 snappedLocal =ClosestPointOnEdge(neighbor, neighborEdge, neighborLocalPoint);

        Vector3 snappedWorld =neighborSurface.transform.TransformPoint(snappedLocal);

        transform.position =snappedWorld + neighborNormal * surfaceOffset;

        currentSurface = neighborSurface;
        currentFaceIndex = resolvedNeighborIndex;

        transform.SetParent(neighborSurface.transform, true);
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

    Vector3 ClosestPointOnEdge(ClimbableSurface.Face face, int edgeIndex, Vector3 point){
    Vector3 a = face.vertices[edgeIndex];
    Vector3 b = face.vertices[(edgeIndex + 1) % face.vertices.Length];

    Vector3 ab = b - a;

    if (ab.sqrMagnitude <= edgeEpsilon)
        return a;

    float t = Mathf.Clamp01(
        Vector3.Dot(point - a, ab) / ab.sqrMagnitude);

    return a + ab * t;
    }

    int FindCrossedEdge(ClimbableSurface.Face face,Vector3 current, Vector3 target,out float movementT)
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

    bool SegmentIntersection(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2,out float t,out float u)
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

    int FindClosestEdge(ClimbableSurface.Face face,Vector3 point)
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
        transform.rotation =Quaternion.Normalize(Quaternion.FromToRotation(transform.up,normal) * transform.rotation);
    }

    void GetFaceBasis( ClimbableSurface.Face face, out Vector3 u, out Vector3 v)
    {
        u = (face.vertices[1] - face.vertices[0]).normalized;
        v = Vector3.Cross(face.normal, u).normalized;
    }

    Vector2 To2D(Vector3 point,ClimbableSurface.Face face,Vector3 u,Vector3 v)
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


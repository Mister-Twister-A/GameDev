using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCam : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    public Vector3 lookAtOffset = Vector3.zero;

    [Header("Orbit Space")]
    public bool relativeToTargetRotation = true;

    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 1f;
    public float maxDistance = 6f;
    public float zoomSpeed = 4f;

    [Header("Orbit Limits")]
    [Range(1f, 89f)] public float maxPitch = 75f;
    [Range(-89f, 1f)] public float minPitch = -30f;

    [Header("Mouse Input")]
    public float mouseSensitivityX = 3f;
    public float mouseSensitivityY = 3f;
    public bool invertY = false;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.06f;
    public float rotationSmoothTime = 0.06f;

    [Header("Collision")]
    public bool collisionEnabled = true;
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.25f;
    public float collisionPadding = 0.15f;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;
    public bool toggleCursorWithEscape = true;

    float yaw; 
    float pitch; 
    Vector3 positionVelocity;

    public Vector3 MovementDirection { get; private set; }

    void Start()
    {
        pitch = 15f;
        if (lockCursorOnStart){
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        ReadMouseInput();

        if (toggleCursorWithEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }

    void ReadMouseInput()
    {
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw += mx * mouseSensitivityX;
        pitch += (invertY ? my : -my) * mouseSensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivot = target.TransformPoint(pivotOffset);
        Vector3 playerUp = target.up;
        Vector3 playerForward = Vector3.ProjectOnPlane(target.forward, playerUp).normalized;

        if (playerForward.sqrMagnitude < 0.0001f)
            playerForward = Vector3.ProjectOnPlane(target.right, playerUp).normalized;

        Vector3 playerRight = Vector3.Cross(playerUp, playerForward).normalized;

        Vector3 referenceForward = relativeToTargetRotation? playerForward: Vector3.forward;

        Vector3 referenceUp = relativeToTargetRotation? playerUp: Vector3.up;

        Vector3 referenceRight = relativeToTargetRotation? playerRight: Vector3.right;

        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        float horizontal = Mathf.Cos(pitchRad);
        float vertical = Mathf.Sin(pitchRad);

        Vector3 horizontalDirection =(-referenceRight * Mathf.Sin(yawRad)) +(-referenceForward * Mathf.Cos(yawRad));

        Vector3 offsetDir =horizontalDirection * horizontal +referenceUp * vertical;

        offsetDir.Normalize();


        Vector3 moveDir = Vector3.ProjectOnPlane(referenceForward * Mathf.Cos(yawRad) + referenceRight * Mathf.Sin(yawRad), referenceUp).normalized;
        MovementDirection = moveDir;

        float finalDistance = distance;

        if (collisionEnabled)
        {
            if (Physics.SphereCast(pivot,collisionRadius,offsetDir,out RaycastHit hit,distance,collisionMask,QueryTriggerInteraction.Ignore))
            {
                finalDistance = Mathf.Clamp(hit.distance - collisionPadding,0.1f,distance);
            }
        }

        Vector3 wantedPosition = pivot + offsetDir * finalDistance;

        transform.position = Vector3.SmoothDamp(transform.position,wantedPosition,ref positionVelocity,positionSmoothTime);

        Vector3 lookTarget = pivot + lookAtOffset;
        Quaternion wantedRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized,referenceUp);

        float rotT = 1f - Mathf.Exp(-(1f / Mathf.Max(0.0001f, rotationSmoothTime)) *Time.deltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation,wantedRotation,rotT);
    }
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 pivot = Application.isPlaying ? target.TransformPoint(pivotOffset) : target.position + pivotOffset;

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(pivot, distance);

        Gizmos.color = Color.yellow;
        DrawPitchRing(pivot, maxPitch);
        Gizmos.color = Color.deepPink;
        DrawPitchRing(pivot, minPitch);
    }

    void DrawPitchRing(Vector3 pivot, float pitchDeg)
    {   
        float pitchRad = pitchDeg * Mathf.Deg2Rad;

        float horizontal = Mathf.Cos(pitchRad) * distance;
        float vertical = Mathf.Sin(pitchRad) * distance;

        Vector3 up = relativeToTargetRotation? target.up: Vector3.up;

        Vector3 forward = relativeToTargetRotation? Vector3.ProjectOnPlane(target.forward, up).normalized: Vector3.forward;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        Vector3 right = Vector3.Cross(up, forward).normalized;

        const int segments = 64;

        Vector3 prev =pivot+ (-forward * horizontal)+ (up * vertical);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;

            Vector3 next =pivot+ (-right * Mathf.Sin(t) * horizontal)+ (-forward * Mathf.Cos(t) * horizontal)+ (up * vertical);

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
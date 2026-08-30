using UnityEngine;

public class ClimbableSurfaceHolder : MonoBehaviour
{
    public PlayerClimbController curPlayerTarget;

    public Transform curPart;

    public ClimbableSurface curPartClimbableSurface;

    public bool unClimbable = false;

    public void RegisterPlayerEnter(Transform _curPart, Transform player)
    {
        curPart = _curPart;
       // player.transform.SetParent(_curPart, true);
        curPartClimbableSurface = _curPart.GetComponent<ClimbableSurface>();
        curPlayerTarget = player.GetComponent<PlayerClimbController>();
    }

    public void RegisterPlayerExit()
    {
        curPart = null;
        curPlayerTarget = null;
        curPartClimbableSurface = null;
    }
}

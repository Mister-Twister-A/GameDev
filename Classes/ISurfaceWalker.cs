using UnityEngine;

public interface ISurfaceWalker : ISurfaceLocator
{
    void MoveTowards(Vector3 worldTargetPoint, float speed);
}
 
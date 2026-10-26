using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;

    public void PlaySlashAnim()
    {
        animator.SetTrigger("Slash");
    }
}

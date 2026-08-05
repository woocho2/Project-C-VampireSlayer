using UnityEngine;

public class PlayerAniController : MonoBehaviour
{
    private Animator m_ani;

    private void Awake()
    {
        m_ani = GetComponentInChildren<Animator>();
    }

    public void UpdateMovementAnimation(float speed, bool isGrounded)
    {
        if (m_ani == null) return;
        m_ani.SetFloat("MoveSpeed", speed);
        m_ani.SetBool("IsGrounded", isGrounded);
    }

    public void PlayDrawSword()
    {
        if (m_ani != null) m_ani.SetTrigger("DrawSword");
    }

    public void PlayAttack()
    {
        if (m_ani != null) m_ani.SetTrigger("DoAttack");
    }

    public void PlayPutSword()
    {
        if (m_ani != null) m_ani.SetTrigger("PutSword");
    }

    public bool IsPlayingCombatAction()
    {
        if (m_ani != null)
        {
            AnimatorStateInfo stateInfo = m_ani.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName("Drawing Sword") ||
                   stateInfo.IsName("Sword Slash") ||
                   stateInfo.IsName("Putting Sword");
        }
        return false;
    }
}
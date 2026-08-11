using UnityEngine;

// 플레이어의 애니메이션 상태를 관리하는 컨트롤러 클래스입니다.
public class PlayerAniController : MonoBehaviour
{
    // 애니메이터 컴포넌트를 담을 변수입니다.
    private Animator m_ani;

    private void Awake()
    {
        BindAnimator(); // 시작할 때 초기 1회 바인딩
    }

    public void BindAnimator()
    {
        // 켜져 있는 자식 오브젝트 중 Animator를 다시 찾아서 덮어씌웁니다.
        m_ani = GetComponentInChildren<Animator>();

        if (m_ani == null)
        {
            Debug.LogWarning("활성화된 하위 프리팹에서 Animator를 찾을 수 없습니다.");
        }
    }

    // 이동 속도와 지상 체류 여부를 받아 애니메이터 파라미터에 전달합니다.
    public void UpdateMovementAnimation(float speed, bool isGrounded)
    {
        // 애니메이터가 없으면 에러를 방지하기 위해 여기서 멈춥니다.
        if (m_ani == null) return;

        // 애니메이터의 "MoveSpeed" 파라미터에 현재 이동 속도를 전달합니다.
        m_ani.SetFloat("MoveSpeed", speed);

        // 애니메이터의 "IsGrounded" 파라미터에 땅에 닿아있는지 여부를 전달합니다.
        m_ani.SetBool("IsGrounded", isGrounded);
    }

    // 무기를 뽑는 애니메이션 트리거를 실행합니다.
    public void PlayDrawWeapon()
    {
        if (m_ani != null) m_ani.SetTrigger("DrawWeapon");
    }

    // 공격 애니메이션 트리거를 실행합니다.
    public void PlayAttack()
    {
        if (m_ani != null) m_ani.SetTrigger("DoAttack");
    }

    // 무기를 집어넣는 애니메이션 트리거를 실행합니다.
    public void PlayPutWeapon()
    {
        if (m_ani != null) m_ani.SetTrigger("PutWeapon");
    }

    // 현재 전투 관련 액션 애니메이션(발도, 공격, 납도 등)이 재생 중인지 확인합니다.
    public bool IsPlayingCombatAction()
    {
        if (m_ani != null)
        {
            // 베이스 레이어(0번)의 현재 애니메이션 상태 정보를 가져옵니다.
            AnimatorStateInfo stateInfo = m_ani.GetCurrentAnimatorStateInfo(0);

            // 재생 중인 애니메이션 이름이 아래 세 가지 중 하나라도 포함되면 true를 반환합니다.
            return stateInfo.IsName("Drawing Weapon") ||
                   stateInfo.IsName("Attack Weapon") ||
                   stateInfo.IsName("Putting Weapon");
        }
        return false;
    }
}
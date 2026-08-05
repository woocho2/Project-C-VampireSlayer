using UnityEngine;

// 이 스크립트가 작동하려면 아래 컴포넌트들이 반드시 캐릭터에 붙어있어야 합니다.
[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(PlayerAniController))]
[RequireComponent(typeof(PlayerInputController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("데이터 참조")]
    private PlayerDataSO m_cData;

    [Header("컴포넌트 캐싱")]
    private PlayerAniController m_aniController;
    private PlayerInputController m_inputController;
    private CharacterController m_cc;

    [Header("전투 상태 변수")]
    private bool isArmed = false;
    private float combatTimer = 0f;

    private void Awake()
    {
        // 1. 필요한 컴포넌트들을 연결합니다.
        m_aniController = GetComponent<PlayerAniController>();
        m_inputController = GetComponent<PlayerInputController>();
        m_cc = GetComponent<CharacterController>();
    }

    public void Setup(PlayerDataSO data)
    {
        m_cData = data;

        if (m_cData == null)
        {
            Debug.LogError("PlayerCombatController에 올바른 데이터가 주입되지 않았습니다.");
        }
    }

    private void Update()
    {
        if (m_cData == null) return;

        // 매 프레임마다 공격 입력이 있었는지 확인하고, 전투 상태(타이머)를 갱신합니다.
        HandleAttackInput();
        ManageCombatState();
    }

    /// <summary>
    /// 인풋 컨트롤러에서 들어온 공격 신호를 처리합니다.
    /// </summary>
    private void HandleAttackInput()
    {
        // 인풋 컨트롤러의 AttackTriggered가 true(눌림) 상태인지 확인합니다.
        if (m_inputController != null && m_inputController.AttackTriggered)
        {
            // 공중이 아닌 바닥에 서 있을 때만 액션을 허용합니다.
            if (m_cc != null && m_cc.isGrounded)
            {
                combatTimer = 0f; // 액션을 취했으므로 무기 넣기 타이머를 초기화합니다.

                if (!isArmed)
                {
                    // 무기를 들고 있지 않다면 발도 애니메이션을 실행합니다.
                    isArmed = true;
                    if (m_aniController != null) m_aniController.PlayDrawSword();
                }
                else
                {
                    // 이미 무기를 들고 있다면 공격 애니메이션을 실행합니다.
                    if (m_aniController != null) m_aniController.PlayAttack();
                }
            }

            // 처리가 끝난 공격 신호는 false로 돌려놓아 다음 클릭 전까지 중복 실행되지 않게 소비합니다.
            m_inputController.AttackTriggered = false;
        }
    }

    /// <summary>
    /// 공격 후 일정 시간이 지나면 무기를 자동으로 집어넣는 로직입니다.
    /// </summary>
    private void ManageCombatState()
    {
        if (isArmed)
        {
            combatTimer += Time.deltaTime;

            if (combatTimer >= m_cData.startBattleTime)
            {
                isArmed = false;
                combatTimer = 0f;

                if (m_aniController != null) m_aniController.PlayPutSword();
            }
        }
    }
}
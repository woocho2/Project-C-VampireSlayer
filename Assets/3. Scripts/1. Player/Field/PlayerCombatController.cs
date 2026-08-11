using UnityEngine;

// 이 스크립트가 정상적으로 작동하기 위해 캐릭터 오브젝트에 반드시 필요한 컴포넌트들을 강제로 지정합니다.
[RequireComponent(typeof(PlayerAniController))]
[RequireComponent(typeof(PlayerInputController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("데이터 참조")]
    private PlayerDataSO m_cData; // 플레이어의 스탯 및 전투 관련 데이터가 담긴 ScriptableObject

    [Header("컴포넌트 캐싱")]
    private PlayerAniController m_aniController;       // 애니메이션 제어 컴포넌트
    private PlayerInputController m_inputController;   // 키보드/마우스 입력 제어 컴포넌트
    private CharacterController m_cc;                 // 캐릭터 이동 및 물리 충돌 제어 컴포넌트

    [Header("전투 상태 변수")]
    private bool isArmed = false;     // 현재 캐릭터가 무기를 꺼내 들고 있는 상태인지 여부
    private float combatTimer = 0f;   // 전투 상태(발도 유지)를 확인하기 위한 시간 측정 타이머

    private void Awake()
    {
        // 1. 게임 시작 시 필요한 컴포넌트들을 미리 찾아 변수에 담아둡니다 (성능 최적화 및 참조 확보).
        m_aniController = GetComponent<PlayerAniController>();
        m_inputController = GetComponent<PlayerInputController>();
        m_cc = GetComponent<CharacterController>();
    }

    public void Setup(PlayerDataSO data)
    {
        m_cData = data;

        // 데이터가 정상적으로 전달되지 않았다면 에러를 출력합니다.
        if (m_cData == null)
        {
            Debug.LogError("PlayerCombatController에 올바른 데이터가 주입되지 않았습니다.");
        }
    }

    private void Update()
    {
        // 데이터가 아직 세팅되지 않았다면 아래 로직을 실행하지 않고 건너뜁니다.
        if (m_cData == null) return;

        // 매 프레임마다 공격 키가 눌렸는지 검사하고, 비전투 시 무기를 집어넣는 타이머를 관리합니다.
        HandleAttackInput();
        ManageCombatState();
    }

    /// <summary>
    /// 인풋 컨트롤러에서 들어온 공격 신호를 처리합니다.
    /// </summary>
    private void HandleAttackInput()
    {
        // 인풋 컴포넌트가 존재하고, 공격 키가 입력된 상태(True)인지 확인합니다.
        if (m_inputController != null && m_inputController.AttackTriggered)
        {
            // 현재 발도, 공격, 납도 등 전투 액션 애니메이션이 재생 중인지 확인합니다.
            bool isCombatAction = m_aniController != null && m_aniController.IsPlayingCombatAction();

            // 공중에 떠 있지 않고 바닥에 안정적으로 서 있을 때만 전투 액션을 허용합니다.
            if (m_cc != null && m_cc.isGrounded && !isCombatAction)
            {
                combatTimer = 0f; // 새로운 행동을 했으므로 무기 자동 납도 타이머를 0으로 초기화합니다.

                // 무기를 아직 안 꺼낸 상태라면
                if (!isArmed)
                {
                    isArmed = true; // 무기를 든 상태로 변경합니다.
                    if (m_aniController != null) m_aniController.PlayDrawWeapon(); // 발도(무기 뽑기) 애니메이션을 재생합니다.
                }
                else
                {
                    // 이미 무기를 들고 있는 상태라면
                    if (m_aniController != null) m_aniController.PlayAttack(); // 곧바로 공격 애니메이션을 실행합니다.
                }
            }

            // 입력받은 공격 신호를 처리했으므로 즉시 false로 초기화하여, 다음 클릭 전까지 중복 실행되는 것을 막습니다.
            m_inputController.AttackTriggered = false;
        }
    }

    /// <summary>
    /// 공격 후 일정 시간이 지나면 무기를 자동으로 집어넣는 로직입니다.
    /// </summary>
    private void ManageCombatState()
    {
        // 무기를 들고 있는 상태일 때만 타이머를 작동시킵니다.
        if (isArmed)
        {
            combatTimer += Time.deltaTime; // 게임 시간이 흐름에 따라 타이머에 시간을 누적합니다.

            // 누적된 시간이 데이터에 정의된 전투 유지 시간(startBattleTime) 이상이 되면
            if (combatTimer >= m_cData.startBattleTime)
            {
                isArmed = false;   // 비전투 상태로 전환합니다.
                combatTimer = 0f;  // 타이머를 초기화합니다.

                // 납도(무기 집어넣기) 애니메이션을 재생합니다.
                if (m_aniController != null) m_aniController.PlayPutWeapon();
            }
        }
    }
}
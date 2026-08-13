using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem; // [추가] 새로운 Input System 네임스페이스 적용

[RequireComponent(typeof(UnitController))]
public class PlayerBattleController : MonoBehaviour
{
    private UnitController m_unitPlayer;
    private UnitController m_unitEnemy;
    private System.Action turnFinishedCallback;
    private PlayerAniController m_aniController;
    private Animator m_ani;
    private bool isMyTurn = false;

    // ==========================================
    // [1] 유니티 생명주기 (초기화)
    // ==========================================
    private void Awake()
    {
        m_unitPlayer = GetComponent<UnitController>();
        m_aniController = GetComponent<PlayerAniController>();
    }

    // ==========================================
    // [2] 턴 루프 (실행 흐름)
    // ==========================================

    /// <summary>
    /// 1. 턴 시작: BattleManager에 의해 호출되며, 버튼 상호작용을 켜고 UI를 노출합니다.
    /// </summary>
    public void StartTurn(UnitController enemy, System.Action onTurnFinished)
    {
        m_unitEnemy = enemy;
        turnFinishedCallback = onTurnFinished;
        isMyTurn = true; // [추가] 키보드 입력 허용

        if (BattleUIManager.Instance != null)
        {
            // [핵심 복구] 버튼을 누르면 각각의 명령 함수가 실행되도록 다시 연결해 줍니다.
            BattleUIManager.Instance.btn_attack.onClick.RemoveAllListeners();
            BattleUIManager.Instance.btn_attack.onClick.AddListener(OnAttackCommand);
            BattleUIManager.Instance.btn_attack.interactable = true;

            BattleUIManager.Instance.btn_skill.onClick.RemoveAllListeners();
            BattleUIManager.Instance.btn_skill.onClick.AddListener(OnSkillCommand);
            BattleUIManager.Instance.btn_skill.interactable = true;

            BattleUIManager.Instance.ShowPlayerActionUI(true, true);
        }
    }

    // 매 프레임 키보드 입력을 감지합니다.
    private void Update()
    {
        // 내 턴이 아니거나 UI가 열려있지 않다면 입력을 무시합니다.
        if (!isMyTurn) return;

        // [수정 완료] 새로운 Input System 문법 적용
        if (Keyboard.current != null)
        {
            // F 버튼: 일반 공격
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                OnAttackCommand();
            }
            // E 버튼: 스킬
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                OnSkillCommand();
            }
        }
    }

    /// <summary>
    /// 2-A. 공격 명령: UI 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnAttackCommand()
    {
        if (m_unitEnemy == null || turnFinishedCallback == null)
        {
            EndTurn();
            return;
        }

        DisableButtonsAndHideUI();
        BattleCameraManager.Instance.ResetToMainView();
        StartCoroutine(AttackRoutine());
    }

    /// <summary>
    /// 2-B. 스킬 명령: UI 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnSkillCommand()
    {
        if (m_unitEnemy == null || turnFinishedCallback == null)
        {
            EndTurn();
            return;
        }

        DisableButtonsAndHideUI();
        BattleCameraManager.Instance.ResetToMainView();
        int skillDamage = Mathf.RoundToInt(m_unitPlayer.power * 2.0f);
        m_unitEnemy.TakeDamage(skillDamage);

        EndTurn(); // 스킬은 즉발이므로 바로 턴을 종료합니다.
    }

    // ==========================================
    // [3] 연출 코루틴 및 종속(Helper) 함수
    // ==========================================

    /// <summary>
    /// 공격 타임라인 연출을 관리하는 코루틴 (OnAttackCommand와 세트)
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        // 1. 공격 시작 전 UI 알림창을 띄우고 2초간 대기합니다.
        BattleUIManager.Instance.ShowActionNotification("공격합니다!");
        yield return new WaitForSeconds(2.0f);

        // 2. 대기가 끝나면 알림창을 숨기고 본 공격(타임라인)을 시작합니다.
        BattleUIManager.Instance.HideActionNotification();

        PlayableDirector attackTimeline = BattleManager.Instance.playerAttackDirector;

        if (attackTimeline != null && attackTimeline.playableAsset != null)
        {
            attackTimeline.Play();
            yield return null; // 1프레임 대기 (타임라인 재생 버그 방지)
            yield return new WaitUntil(() => attackTimeline.state != PlayState.Playing);
        }
        else
        {
            Debug.LogError("플레이어 공격 타임라인이 연결되지 않았습니다.");
            EndTurn();
            yield break;
        }

        BattleCameraManager.Instance?.ResetToMainView();
        yield return new WaitForSeconds(0.5f);

        EndTurn(); // 연출이 모두 끝나면 턴을 종료합니다.
    }

    /// <summary>
    /// 프리팹이 교체되어도 안전하게 호출할 수 있는 애니메이터 확인 함수 (AttackRoutine과 세트)
    /// </summary>
    private Animator GetAnimator()
    {
        if (m_ani == null)
        {
            m_ani = m_unitPlayer.GetComponentInChildren<Animator>();
        }
        return m_ani;
    }

    /// <summary>
    /// 행동을 시작할 때 입력을 막기 위해 UI를 숨기는 보조 함수
    /// </summary>
    private void DisableButtonsAndHideUI()
    {
        isMyTurn = false; // 행동을 선택했으므로 키보드 입력을 다시 차단합니다.

        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.ShowPlayerActionUI(false);
        }
    }

    /// <summary>
    /// 3. 턴 종료: 모든 행동과 연출이 끝난 후 BattleManager로 권한을 넘깁니다.
    /// </summary>
    private void EndTurn()
    {
        turnFinishedCallback?.Invoke();
    }
}
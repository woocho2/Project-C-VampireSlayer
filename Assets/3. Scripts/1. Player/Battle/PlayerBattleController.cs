using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(UnitController))]
public class PlayerBattleController : MonoBehaviour
{
    private UnitController m_unitPlayer;
    private UnitController m_unitEnemy;
    private System.Action turnFinishedCallback;
    private PlayerAniController m_aniController;
    private Animator m_ani;

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

        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.btn_attack.interactable = true;
            BattleUIManager.Instance.btn_skill.interactable = true;
            BattleUIManager.Instance.ShowPlayerActionUI(true);
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
        PlayableDirector attackTimeline = BattleManager.Instance.playerAttackDirector;

        if (attackTimeline != null && attackTimeline.playableAsset != null)
        {
            Animator anim = GetAnimator();

            foreach (var track in attackTimeline.playableAsset.outputs)
            {
                if (track.streamName == "PlayerAnimTrack")
                {
                    attackTimeline.SetGenericBinding(track.sourceObject, anim);
                    break;
                }
            }

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
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.btn_attack.interactable = false;
            BattleUIManager.Instance.btn_skill.interactable = false;
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
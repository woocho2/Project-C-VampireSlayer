using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

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
    // [추가] 방어(QTE) 판정 설정 및 상태 변수
    // ==========================================
    [Header("방어 판정 설정 (현실 시간/초)")]
    public float dodgeDuration = 0.8f;
    public float parryDuration = 0.5f;
    public float actionCooldown = 1.0f;

    private float dodgeExpirationTime = -1f;
    private float parryExpirationTime = -1f;
    private float cooldownExpirationTime = -1f;
    private bool isDefendingMode = false;

    private void Awake()
    {
        m_unitPlayer = GetComponent<UnitController>();
        m_aniController = GetComponent<PlayerAniController>();
    }

    // ==========================================
    // [1] 공격 턴 로직
    // ==========================================
    public void StartTurn(UnitController enemy, System.Action onTurnFinished)
    {
        m_unitEnemy = enemy;
        turnFinishedCallback = onTurnFinished;
        isMyTurn = true;

        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.btn_attack.onClick.RemoveAllListeners();
            BattleUIManager.Instance.btn_attack.onClick.AddListener(OnAttackCommand);
            BattleUIManager.Instance.btn_attack.interactable = true;

            BattleUIManager.Instance.btn_skill.onClick.RemoveAllListeners();
            BattleUIManager.Instance.btn_skill.onClick.AddListener(OnSkillCommand);
            BattleUIManager.Instance.btn_skill.interactable = true;

            BattleUIManager.Instance.ShowPlayerActionUI(true, true);
        }
    }

    public void OnAttackCommand()
    {
        if (m_unitEnemy == null || turnFinishedCallback == null) { EndTurn(); return; }
        DisableButtonsAndHideUI();
        BattleCameraManager.Instance.ResetToMainView();
        StartCoroutine(AttackRoutine());
    }

    public void OnSkillCommand()
    {
        if (m_unitEnemy == null || turnFinishedCallback == null) { EndTurn(); return; }
        DisableButtonsAndHideUI();
        BattleCameraManager.Instance.ResetToMainView();
        int skillDamage = Mathf.RoundToInt(m_unitPlayer.power * 2.0f);
        m_unitEnemy.TakeDamage(skillDamage);
        EndTurn();
    }

    private IEnumerator AttackRoutine()
    {
        BattleUIManager.Instance.ShowActionNotification("공격합니다!");
        yield return new WaitForSeconds(2.0f);
        BattleUIManager.Instance.HideActionNotification();

        PlayableDirector attackTimeline = BattleManager.Instance.playerAttackDirector;
        if (attackTimeline != null && attackTimeline.playableAsset != null)
        {
            attackTimeline.Play();
            yield return null;
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
        EndTurn();
    }

    private void DisableButtonsAndHideUI()
    {
        isMyTurn = false;
        if (BattleUIManager.Instance != null) BattleUIManager.Instance.ShowPlayerActionUI(false);
    }

    private void EndTurn()
    {
        turnFinishedCallback?.Invoke();
    }

    // ==========================================
    // [2] 방어 턴(QTE) 및 키보드 입력 로직
    // ==========================================
    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1. 내 공격 턴일 때의 입력 감지
        if (isMyTurn)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame) OnAttackCommand();
            else if (Keyboard.current.eKey.wasPressedThisFrame) OnSkillCommand();
        }
        // 2. 적의 공격을 방어하는 턴일 때의 입력 감지
        else if (isDefendingMode)
        {
            float currentTime = Time.unscaledTime; // 배속에 영향받지 않는 현실 시간 기준

            if (currentTime > cooldownExpirationTime)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    Debug.Log("플레이어: 패링 시도!");
                    parryExpirationTime = currentTime + parryDuration;
                    cooldownExpirationTime = currentTime + actionCooldown;
                }
                else if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    Debug.Log("플레이어: 회피 시도!");
                    dodgeExpirationTime = currentTime + dodgeDuration;
                    cooldownExpirationTime = currentTime + actionCooldown;
                }
            }
        }
    }

    /// <summary>
    /// 적이 공격을 시작할 때 호출하여 플레이어의 방어 입력을 활성화합니다.
    /// </summary>
    public void EnableDefenseMode()
    {
        isDefendingMode = true;
        dodgeExpirationTime = -1f;
        parryExpirationTime = -1f;
        cooldownExpirationTime = -1f;
    }

    /// <summary>
    /// 적의 타격이 끝났을 때 호출하여 방어 입력을 차단합니다.
    /// </summary>
    public void DisableDefenseMode()
    {
        isDefendingMode = false;
    }

    /// <summary>
    /// 적이 타격하는 순간 호출하여, 플레이어가 현재 방어에 성공했는지 판정 결과를 반환합니다.
    /// </summary>
    public EnemyBattleController.QTEResult GetCurrentDefenseResult()
    {
        float currentTime = Time.unscaledTime;

        Debug.Log($"QTE 판정 시작! 현재 시간: {currentTime:F2} | 패링 만료 시간: {parryExpirationTime:F2} | 회피 만료 시간: {dodgeExpirationTime:F2}");

        if (currentTime <= parryExpirationTime) return EnemyBattleController.QTEResult.Parried;
        if (currentTime <= dodgeExpirationTime) return EnemyBattleController.QTEResult.Dodged;

        Debug.Log("모든 판정 시간 초과. 방어 실패 (None 반환)");
        return EnemyBattleController.QTEResult.None;
    }

    private Animator GetAnimator()
    {
        if (m_ani == null) m_ani = m_unitPlayer.GetComponentInChildren<Animator>();
        return m_ani;
    }
}
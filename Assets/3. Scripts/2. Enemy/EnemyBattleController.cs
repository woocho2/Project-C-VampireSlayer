using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UnitController))]
public class EnemyBattleController : MonoBehaviour
{
    private UnitController m_unitEnemy;
    private Animator m_ani;

    public enum QTEResult { None, Dodged, Parried }
    public QTEResult currentQTE = QTEResult.None;

    // ==========================================
    // [개선] QTE 타이밍 변수화
    // 하드코딩된 숫자를 없애고 유니티 인스펙터에서 타임라인 길이에 맞춰 설정할 수 있게 합니다.
    // ==========================================
    [Header("QTE 타이밍 설정 (초)")]
    [Tooltip("적의 공격 데미지가 들어가는 시점")]
    public double damageTime = 4.55;
    [Tooltip("회피 입력이 가능해지는 시작 시간")]
    public double dodgeStartTime = 4.05;
    [Tooltip("패링 입력이 가능해지는 시작 시간")]
    public double parryStartTime = 4.30;

    private void Awake()
    {
        m_unitEnemy = GetComponent<UnitController>();
    }

    // ==========================================
    // [개선] 안전장치 추가
    // 적 턴 도중 게임이 종료되거나 에러가 났을 때 배속이 0.5로 굳는 것을 방지합니다.
    // ==========================================
    private void OnDisable()
    {
        Time.timeScale = 1.0f;
    }

    public void ExecuteTurn(List<UnitController> playerUnits, System.Action onTurnFinished)
    {
        StartCoroutine(EnemyTurnRoutine(playerUnits, onTurnFinished));
    }

    private IEnumerator EnemyTurnRoutine(List<UnitController> playerUnits, System.Action onTurnFinished)
    {
        Debug.Log($"{m_unitEnemy.unitName}의 턴 시작!");

        List<UnitController> alivePlayers = playerUnits.FindAll(p => p.currentHP > 0);

        if (alivePlayers.Count > 0)
        {
            UnitController target = alivePlayers[Random.Range(0, alivePlayers.Count)];
            BattleManager.Instance.CurrentTarget = target;

            int pattern = 0;
            yield return new WaitForSeconds(0.5f);

            string actionMsg = pattern == 0 ? "적의 공격!" : "적의 강력한 스킬 준비!";
            if (BattleUIManager.Instance != null)
            {
                BattleUIManager.Instance.ShowActionNotification(actionMsg);
            }

            yield return new WaitForSeconds(2.0f);

            if (BattleUIManager.Instance != null)
            {
                BattleUIManager.Instance.HideActionNotification();
            }

            if (pattern == 0)
            {
                Debug.Log($"{m_unitEnemy.unitName}이(가) {target.unitName}에게 공격!");
                yield return StartCoroutine(AttackRoutine(target));
            }
        }

        onTurnFinished?.Invoke();
    }

    private IEnumerator AttackRoutine(UnitController target)
    {
        currentQTE = QTEResult.None;
        Time.timeScale = 0.5f;
        bool hasDealtDamage = false;

        if (BattleUIManager.Instance != null)
            BattleUIManager.Instance.ShowPlayerActionUI(true, false);

        PlayableDirector attackTimeline = BattleManager.Instance.enemyAttackDirector;

        if (attackTimeline != null && attackTimeline.playableAsset != null)
        {
            attackTimeline.Play();
            yield return null;

            while (attackTimeline.state == PlayState.Playing)
            {
                double currentTime = attackTimeline.time;

                // [A] QTE 입력 감지
                if (currentQTE == QTEResult.None && currentTime < damageTime)
                {
                    // 변수화된 타이밍을 사용하여 판정 범위를 유연하게 만듭니다.
                    bool isDodgeWindow = (currentTime >= dodgeStartTime && currentTime <= damageTime);
                    bool isParryWindow = (currentTime >= parryStartTime && currentTime <= damageTime);

                    if (isParryWindow && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        Debug.Log("패링 시도 성공");
                        currentQTE = QTEResult.Parried;
                        Time.timeScale = 1.0f;
                        if (BattleUIManager.Instance != null)
                            BattleUIManager.Instance.ShowActionNotification("패링 성공!");
                    }
                    else if (isDodgeWindow && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                    {
                        Debug.Log("회피 시도 성공");
                        currentQTE = QTEResult.Dodged;
                        Time.timeScale = 1.0f;
                        if (BattleUIManager.Instance != null)
                            BattleUIManager.Instance.ShowActionNotification("회피 성공!");
                    }
                }

                // [B] 데미지 적용 시점 판정
                if (currentTime >= damageTime && !hasDealtDamage)
                {
                    hasDealtDamage = true;
                    Time.timeScale = 1.0f; // 판정 완료 즉시 원래 속도 복구

                    if (currentQTE == QTEResult.None)
                    {
                        target.TakeDamage(m_unitEnemy.power);
                    }
                    else if (currentQTE == QTEResult.Parried)
                    {
                        m_unitEnemy.TakeDamage(target.power);
                    }
                }

                yield return null;
            }
        }
        else
        {
            Debug.LogError("적 공격 타임라인(enemyAttackDirector)이 설정되지 않았습니다. 즉발 데미지로 대체합니다.");
            Time.timeScale = 1.0f;
            target.TakeDamage(m_unitEnemy.power);
            yield return new WaitForSeconds(1.0f);
        }

        // 타임라인 종료 후 상태 정리
        Time.timeScale = 1.0f;

        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideActionNotification();
            BattleUIManager.Instance.ShowPlayerActionUI(false, false);
        }

        if (BattleCameraManager.Instance != null)
            BattleCameraManager.Instance.ResetToMainView();

        yield return new WaitForSeconds(0.5f);
    }

    private Animator GetAnimator()
    {
        if (m_ani == null)
        {
            m_ani = m_unitEnemy.GetComponentInChildren<Animator>();
        }
        return m_ani;
    }
}
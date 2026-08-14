using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(UnitController))]
public class EnemyBattleController : MonoBehaviour
{
    private UnitController m_unitEnemy;
    private Animator m_ani;

    public enum QTEResult { None, Dodged, Parried }

    private void Awake()
    {
        m_unitEnemy = GetComponent<UnitController>();
    }

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
            yield return new WaitForSeconds(2.0f);

            if (BattleUIManager.Instance != null) BattleUIManager.Instance.HideActionNotification();

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
        string message = $"{m_unitEnemy.unitName}가 {target.unitName}에게 강력한 발차기 공격을 합니다.";
        if (BattleUIManager.Instance != null) BattleUIManager.Instance.ShowActionNotification(message);

        yield return new WaitForSeconds(2f);
        if (BattleUIManager.Instance != null) BattleUIManager.Instance.HideActionNotification();

        Time.timeScale = 0.5f;

        if (BattleUIManager.Instance != null)
            BattleUIManager.Instance.ShowPlayerActionUI(true, false);

        PlayerBattleController playerController = target.GetComponent<PlayerBattleController>();
        if (playerController != null) playerController.EnableDefenseMode();

        PlayableDirector attackTimeline = BattleManager.Instance.enemyAttackDirector;

        if (attackTimeline != null && attackTimeline.playableAsset != null)
        {
            attackTimeline.Play();
            yield return null;

            yield return new WaitUntil(() => attackTimeline.state != PlayState.Playing);
        }
        else
        {
            Debug.LogError("적 공격 타임라인이 없습니다.");
            Time.timeScale = 1.0f;
            target.TakeDamage(m_unitEnemy.power);
            yield return new WaitForSeconds(1.0f);
        }

        if (playerController != null) playerController.DisableDefenseMode();

        Time.timeScale = 1.0f;
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideActionNotification();
            BattleUIManager.Instance.ShowPlayerActionUI(false, false);
        }
        if (BattleCameraManager.Instance != null) BattleCameraManager.Instance.ResetToMainView();

        yield return new WaitForSeconds(0.5f);
    }

    // ==========================================
    // [추가] TimeLineController의 시그널에 의해 호출되는 판정 함수
    // ==========================================
    public void ResolveEnemyAttack(UnitController target)
    {
        Debug.Log("[디버그 3] EnemyBattleController.ResolveEnemyAttack 진입 성공!");
        Time.timeScale = 1.0f;

        QTEResult defenseResult = QTEResult.None;
        PlayerBattleController playerController = target.GetComponent<PlayerBattleController>();

        if (playerController != null)
        {
            defenseResult = playerController.GetCurrentDefenseResult();
            playerController.DisableDefenseMode();
        }

        Debug.Log($"최종 도출된 방어 결과: {defenseResult}");

        if (defenseResult == QTEResult.Parried)
        {
            m_unitEnemy.TakeDamage(target.power);
            if (BattleUIManager.Instance != null) BattleUIManager.Instance.ShowActionNotification("패링 성공!");
        }
        else if (defenseResult == QTEResult.Dodged)
        {
            if (BattleUIManager.Instance != null) BattleUIManager.Instance.ShowActionNotification("회피 성공!");
        }
        else
        {
            target.TakeDamage(m_unitEnemy.power);
        }
    }

    private Animator GetAnimator()
    {
        if (m_ani == null) m_ani = m_unitEnemy.GetComponentInChildren<Animator>();
        return m_ani;
    }
}
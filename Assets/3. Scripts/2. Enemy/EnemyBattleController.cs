using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EnemyBattleController : MonoBehaviour
{
    private UnitController m_unitEnemy;

    private void Awake()
    {
        m_unitEnemy = GetComponent<UnitController>();
    }

    // ==========================================
    // [1] 적 턴 진입점
    // ==========================================
    public void ExecuteTurn(List<UnitController> playerUnits, System.Action onTurnFinished)
    {
        StartCoroutine(EnemyTurnRoutine(playerUnits, onTurnFinished));
    }

    // ==========================================
    // [2] 적 AI 및 행동 루프
    // ==========================================
    private IEnumerator EnemyTurnRoutine(List<UnitController> playerUnits, System.Action onTurnFinished)
    {
        Debug.Log($"{m_unitEnemy.unitName}의 턴 시작!");

        // 1. 타겟 탐색 (살아있는 플레이어만 필터링)
        List<UnitController> alivePlayers = playerUnits.FindAll(p => p.currentHP > 0);

        if (alivePlayers.Count > 0)
        {
            // 무작위 타겟 지정
            UnitController target = alivePlayers[Random.Range(0, alivePlayers.Count)];
            BattleManager.Instance.CurrentTarget = target;

            // 2. 공격 패턴 결정 (0: 일반 공격, 1: 스킬)
            int pattern = Random.Range(0, 2);

            yield return new WaitForSeconds(1f); // 연출 전 대기

            // 3. 데미지 계산 및 적용
            if (pattern == 0)
            {
                Debug.Log($"{m_unitEnemy.unitName}이(가) {target.unitName}에게 일반 공격!");
                target.TakeDamage(m_unitEnemy.power);
            }
            else
            {
                Debug.Log($"{m_unitEnemy.unitName}이(가) {target.unitName}에게 강력한 스킬 공격!");
                int skillDamage = Mathf.RoundToInt(m_unitEnemy.power * 1.5f);
                target.TakeDamage(skillDamage);
            }

            yield return new WaitForSeconds(1.5f); // 타격 후 대기
        }

        // 4. 턴 종료 알림
        onTurnFinished?.Invoke();
    }
}
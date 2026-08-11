using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// 타임라인 연출 중 발생하는 시그널(이벤트) 처리를 전담하는 클래스입니다.
public class TimeLineController : MonoBehaviour
{
    /// <summary>
    /// 타임라인 신호(Signal): 인트로 타임라인 시작 프레임에 배치하여 깜빡임 UI 켬
    /// </summary>
    public void Signal_PlayBattleIntro()
    {
        // BattleManager에 등록된 적 유닛의 이름을 가져옵니다.
        if (BattleManager.Instance != null && BattleManager.Instance.EnemyUnitScript != null)
        {
            string enemyName = BattleManager.Instance.EnemyUnitScript.unitName;
            BattleUIManager.Instance.PlayBattleStartUI(enemyName);
        }
    }

    /// <summary>
    /// 타임라인 신호(Signal): 인트로 타임라인 종료 프레임에 배치하여 깜빡임 UI 끔
    /// </summary>
    public void Signal_HideBattleIntro()
    {
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideBattleStartUI();
        }
    }

    /// <summary>
    /// 타임라인 신호: 몬스터 이름이 쾅쾅 박히는 연출 실행
    /// </summary>
    public void Signal_MonsterNameEffect()
    {
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.MonsterNameEffect();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 타임라인 신호: 타겟의 바로 앞까지 대쉬하여 이동합니다.
    /// </summary>
    public void Signal_DashToTarget()
    {
        // (가정) BattleManager에 현재 공격자와 타겟을 저장해둔 프로퍼티가 있다고 전제합니다.
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        UnitController target = BattleManager.Instance.CurrentTarget;

        if (attacker != null && target != null)
        {
            // 타겟 위치에서 공격자 쪽으로 약간 떨어진(예: 1.5 유닛) 거리를 계산합니다.
            Vector3 direction = (attacker.transform.position - target.transform.position).normalized;
            direction.y = 0; // 수직 이동 방지
            Vector3 destination = target.transform.position + (direction * 1.5f);

            // DOTween을 사용하여 0.2초 동안 타겟 앞까지 매우 빠르게 이동시킵니다.
            attacker.transform.DOMove(destination, 0.5f).SetEase(Ease.OutExpo);
        }
    }

    /// <summary>
    /// 타임라인 신호: 공격을 마친 후 원래 자리로 복귀합니다.
    /// </summary>
    public void Signal_ReturnToOrigin()
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;

        if (attacker != null)
        {
            // UnitController에 저장해둔 원래 자리로 0.3초 동안 부드럽게 복귀합니다.
            attacker.transform.DOMove(attacker.originalPosition, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 타임라인 신호(Signal): 살아있는 모든 아군 유닛에게 무기 뽑기(발도) 애니메이션 트리거 전달
    /// </summary>
    public void Signal_DrawWeapon()
    {
        // BattleManager에서 생성된 아군 유닛 리스트를 가져옵니다.
        List<UnitController> players = BattleManager.Instance.PlayerUnits;

        foreach (UnitController unit in players)
        {
            Animator playerAnim = unit.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DrawWeapon");
        }
    }

    /// <summary>
    /// 타임라인 신호(Signal): 살아있는 모든 아군 유닛에게 무기 뽑기(발도) 애니메이션 트리거 전달
    /// </summary>
    public void Signal_DoAttack()
    {
        // BattleManager에서 생성된 아군 유닛 리스트를 가져옵니다.
        UnitController attacker = BattleManager.Instance.CurrentAttacker;

        if (attacker != null)
        {
            Animator playerAnim = attacker.GetComponentInChildren<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("DoAttack"); 
            }
        }
        else
        {
            Debug.LogError("현재 공격자 (CurrentAttacker) 데이터가 지정되지 않았습니다.");
        }
    }

    public void Signal_DoSkill1()
    {
        // BattleManager에서 생성된 아군 유닛 리스트를 가져옵니다.
        UnitController attacker = BattleManager.Instance.CurrentAttacker;

        if (attacker != null)
        {
            Animator playerAnim = attacker.GetComponentInChildren<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("DoSkill1");
            }
        }
        else
        {
            Debug.LogError("현재 공격자 (CurrentAttacker) 데이터가 지정되지 않았습니다.");
        }
    }

    public void Signal_DoSkill2()
    {
        // BattleManager에서 생성된 아군 유닛 리스트를 가져옵니다.
        UnitController attacker = BattleManager.Instance.CurrentAttacker;

        if (attacker != null)
        {
            Animator playerAnim = attacker.GetComponentInChildren<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("DoSkill2");
            }
        }
        else
        {
            Debug.LogError("현재 공격자 (CurrentAttacker) 데이터가 지정되지 않았습니다.");
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 스킬 타격 프레임에서 호출될 시그널 함수
    /// </summary>
    public void Signal_ApplyDamage(float skillMultiplier)
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        UnitController target = BattleManager.Instance.CurrentTarget;

        if (attacker != null && target != null)
        {
            // 공격자의 공격력 * 스킬 계수로 원본 데미지(Raw Damage) 계산
            int rawDamage = Mathf.RoundToInt(attacker.power * skillMultiplier);

            // 타겟에게 데미지 전달 (방어력 상쇄 처리는 Target의 TakeDamage 내부에서 알아서 수행됨)
            target.TakeDamage(rawDamage);
        }
    }
}
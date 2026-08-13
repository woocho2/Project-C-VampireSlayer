using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// 타임라인 연출 중 발생하는 시그널(이벤트) 처리를 전담하는 클래스입니다.
public class TimeLineController : MonoBehaviour
{
    // ==========================================
    // [1] 인트로 연출 시그널
    // ==========================================
    public void Signal_PlayBattleIntro()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.EnemyUnits.Count > 0)
        {
            string enemyName = BattleManager.Instance.EnemyUnits[0].unitName;
            BattleUIManager.Instance.PlayBattleStartUI(enemyName);
        }
    }

    public void Signal_MonsterNameEffect()
    {
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.MonsterNameEffect();
        }
    }

    public void Signal_HideBattleIntro()
    {
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideBattleStartUI();
        }
    }

    // ==========================================
    // [2] 이동 연출 시그널
    // ==========================================
    public void Signal_DashToTarget()
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        UnitController target = BattleManager.Instance.CurrentTarget;

        if (attacker != null && target != null)
        {
            Vector3 direction = (attacker.transform.position - target.transform.position).normalized;
            direction.y = 0;
            Vector3 destination = target.transform.position + (direction * 1.5f);

            attacker.transform.DOMove(destination, 0.5f).SetEase(Ease.OutExpo);
        }
    }

    public void Signal_ReturnToOrigin()
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;

        if (attacker != null)
        {
            attacker.transform.DOMove(attacker.originalPosition, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    // ==========================================
    // [3] 애니메이션 트리거 시그널
    // ==========================================
    public void Signal_DrawWeapon()
    {
        List<UnitController> players = BattleManager.Instance.PlayerUnits;

        foreach (UnitController unit in players)
        {
            Animator playerAnim = unit.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DrawWeapon");
        }
    }

    public void Signal_DoAttack()
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        if (attacker != null)
        {
            Animator playerAnim = attacker.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DoAttack");
        }
    }

    public void Signal_DoSkill1()
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        if (attacker != null)
        {
            Animator playerAnim = attacker.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DoSkill1");
        }
    }

    public void Signal_DoSkill2()
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        if (attacker != null)
        {
            Animator playerAnim = attacker.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DoSkill2");
        }
    }

    // ==========================================
    // [4] 데미지 판정 시그널
    // ==========================================
    public void Signal_ApplyDamage(float skillMultiplier)
    {
        UnitController attacker = BattleManager.Instance.CurrentAttacker;
        UnitController target = BattleManager.Instance.CurrentTarget;

        if (attacker != null && target != null)
        {
            int rawDamage = Mathf.RoundToInt(attacker.power);
            target.TakeDamage(rawDamage);
        }
    }
}
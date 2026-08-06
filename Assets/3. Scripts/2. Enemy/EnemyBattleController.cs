using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트가 부착된 게임오브젝트에는 반드시 UnitController 컴포넌트가 있어야 함을 강제합니다.
[RequireComponent(typeof(UnitController))]
public class EnemyBattleController : MonoBehaviour
{
    // 내 유닛의 정보를 담고 있는 UnitController 컴포넌트 참조 변수
    private UnitController myUnit;

    // 게임 오브젝트가 활성화될 때 최초 실행되는 함수
    private void Awake()
    {
        // 같은 오브젝트에 붙어 있는 UnitController 컴포넌트를 찾아 변수에 저장
        myUnit = GetComponent<UnitController>();
    }

    /// <summary>
    /// BattleManager가 적의 턴일 때 호출하는 함수입니다.
    /// </summary>
    /// <param name="playerUnits">전체 플레이어 유닛 리스트</param>
    /// <param name="onTurnFinished">턴이 끝났을 때 실행할 콜백(알림) 함수</param>
    public void ExecuteTurn(List<UnitController> playerUnits, System.Action onTurnFinished)
    {
        // 턴 진행 과정을 프레임 단위로 나누어 처리하기 위해 코루틴 시작
        StartCoroutine(EnemyTurnRoutine(playerUnits, onTurnFinished));
    }

    // 적의 턴 행동 순서를 제어하는 코루틴 함수
    private IEnumerator EnemyTurnRoutine(List<UnitController> playerUnits, System.Action onTurnFinished)
    {
        // 유닛의 이름과 함께 턴이 시작되었음을 콘솔에 출력
        Debug.Log($"{myUnit.unitName}의 턴 시작!");

        // 1. 플레이어 목록 중 체력(currentHP)이 0보다 큰(살아있는) 타겟들만 골라내어 리스트로 저장
        List<UnitController> alivePlayers = playerUnits.FindAll(p => p.currentHP > 0);

        // 살아있는 플레이어가 한 명 이상 존재할 경우에만 행동 진행
        if (alivePlayers.Count > 0)
        {
            // 살아있는 플레이어 중 무작위로 한 명을 공격 대상(target)으로 선정
            UnitController target = alivePlayers[Random.Range(0, alivePlayers.Count)];

            // 2. 0 또는 1 중 무작위로 숫자를 뽑아 공격 패턴 결정 (0 = 일반 공격, 1 = 스킬)
            int pattern = Random.Range(0, 2);

            // 행동 전 카메라 이동이나 연출을 보여주기 위해 1초 동안 대기
            yield return new WaitForSeconds(1f);

            // 결정된 패턴이 0(일반 공격)일 때
            if (pattern == 0)
            {
                Debug.Log($"{myUnit.unitName}이(가) {target.unitName}에게 일반 공격!");
                target.TakeDamage(10); // 타겟에게 10의 데미지 입힘 (임시 수치)
            }
            // 결정된 패턴이 1(스킬 공격)일 때
            else
            {
                Debug.Log($"{myUnit.unitName}이(가) {target.unitName}에게 강력한 스킬 공격!");
                target.TakeDamage(20); // 타겟에게 20의 데미지 입힘 (임시 수치)
            }

            // 공격 및 타격 연출(애니메이션 등)이 끝날 때까지 1.5초 동안 대기
            yield return new WaitForSeconds(1.5f);
        }

        // 3. 적의 모든 행동이 끝났으므로, 대기시켜 둔 턴 종료 콜백 함수가 존재할 경우 실행하여 턴 종료를 알림
        onTurnFinished?.Invoke();
    }
}
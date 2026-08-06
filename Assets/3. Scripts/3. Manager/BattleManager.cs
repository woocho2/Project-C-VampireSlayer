using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

// 전투의 전체적인 상태를 나타내는 열거형
public enum BattleState { Start, TurnProgress, Won, Lost }

public class BattleManager : MonoBehaviour
{
    [Header("전투 상태 관리")]
    public BattleState state; // 현재 전투 상태

    [Header("스폰 위치 설정")]
    public Transform[] playerStations; // 아군 배치 위치 목록
    public Transform enemyStation; // 적 배치 위치

    [Header("연출 설정")]
    public PlayableDirector introDirector; // 전투 시작 시네마틱 연출 컴포넌트

    private List<UnitController> playerUnits = new List<UnitController>(); // 생성된 아군 유닛 리스트
    private UnitController enemyUnitScript; // 생성된 적 유닛 컨트롤러

    private void Start()
    {
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    // 전투 초기화 및 캐릭터 스폰, 시작 연출을 담당하는 코루틴
    private IEnumerator SetupBattle()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            yield break;
        }

        PlayerDataSO[] party = GameManager.Instance.partyMembers;
        int stationIndex = 0;

        // 파티원 데이터를 순회하며 살아있는 아군을 지정된 자리에 생성
        for (int i = 0; i < party.Length; i++)
        {
            if (party[i] != null && party[i].currentHealth > 0 && stationIndex < playerStations.Length)
            {
                GameObject playerGO = Instantiate(party[i].playerPrefab, playerStations[stationIndex].position, playerStations[stationIndex].rotation);
                UnitController unit = playerGO.GetComponent<UnitController>();

                if (unit != null)
                {
                    unit.Setup(party[i]);
                    playerUnits.Add(unit);
                }
                stationIndex++;
            }
        }

        // 아군이 전멸 상태라면 즉시 패배 처리
        if (playerUnits.Count == 0)
        {
            state = BattleState.Lost;
            EndBattle();
            yield break;
        }

        // 몬스터 생성 및 데이터 세팅
        EnemyDataSO m_eData = GameManager.Instance.encounteredMonster;

        if (m_eData != null && m_eData.monsterPrefab != null)
        {
            GameObject enemyGO = Instantiate(m_eData.monsterPrefab, enemyStation.position, enemyStation.rotation);
            enemyUnitScript = enemyGO.GetComponent<UnitController>();

            if (enemyUnitScript != null)
            {
                enemyUnitScript.Setup(m_eData);
            }
        }

        // 전투 시작 UI 호출
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.PlayBattleStartUI(m_eData.name);
        }

        // 시네마틱 연출 재생 및 완료 대기
        if (introDirector != null)
        {
            introDirector.Play();
            yield return new WaitUntil(() => introDirector.state != PlayState.Playing);
        }

        // 연출 종료 시 카메라가 메인 뷰로 안전하게 복귀하도록 BaseCamera 우선순위 조정
        if (BattleCameraManager.Instance != null)
        {
            BattleCameraManager.Instance.ResetToMainView();
            if (BattleCameraManager.Instance.BaseCamera != null)
            {
                BattleCameraManager.Instance.BaseCamera.Priority = 15;
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (BattleCameraManager.Instance != null && BattleCameraManager.Instance.BaseCamera != null)
        {
            BattleCameraManager.Instance.BaseCamera.Priority = 10;
        }

        // 본격적인 턴제 루프 진입
        state = BattleState.TurnProgress;
        StartCoroutine(TurnLoopRoutine());
    }

    // 타임라인에서 호출: 적이 아군을 바라보도록 회전
    public void Signal_LookAtPlayer()
    {
        if (enemyUnitScript != null && playerUnits.Count > 0)
        {
            Transform targetPlayer = playerUnits[0].transform;
            StartCoroutine(SmoothLookAtRoutine(enemyUnitScript.transform, targetPlayer.position, 0.5f));
        }
        else
        {
            Debug.LogError("조건 실패: 몬스터나 파티원 데이터가 비어있습니다!");
        }
    }

    // 부드러운 회전 처리를 위한 코루틴
    private IEnumerator SmoothLookAtRoutine(Transform enemyTransform, Vector3 targetPosition, float duration)
    {
        Quaternion startRotation = enemyTransform.rotation;
        Vector3 directionToPlayer = (targetPosition - enemyTransform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer == Vector3.zero) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            enemyTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        enemyTransform.rotation = targetRotation;
    }

    // 타임라인에서 호출: 아군 발도 애니메이션 트리거
    public void Signal_DrawSwords()
    {
        foreach (UnitController unit in playerUnits)
        {
            Animator playerAnim = unit.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DrawSword");
        }
    }

    // 턴제 전투의 핵심 루프 코루틴
    private IEnumerator TurnLoopRoutine()
    {
        while (state == BattleState.TurnProgress)
        {
            List<UnitController> aliveUnits = GetAliveUnits();

            if (CheckBattleEnd(aliveUnits)) yield break;

            UnitController nextTurnUnit = GetNextUnitAndAdvanceTime(aliveUnits);
            bool isTurnFinished = false;

            // 아군 턴
            if (playerUnits.Contains(nextTurnUnit))
            {
                PlayerBattleController pController = nextTurnUnit.GetComponent<PlayerBattleController>();
                if (pController != null)
                {
                    if (BattleCameraManager.Instance != null)
                    {
                        BattleCameraManager.Instance.SetPlayerBackView(nextTurnUnit.transform, enemyUnitScript.transform);
                    }

                    // 카메라가 플레이어 등 뒤로 완전히 이동할 때까지 대기
                    CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
                    if (brain != null)
                    {
                        float timeout = 0.5f;
                        float elapsed = 0f;
                        while (!brain.IsBlending && elapsed < timeout)
                        {
                            elapsed += Time.deltaTime;
                            yield return null;
                        }
                        yield return new WaitUntil(() => !brain.IsBlending);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.5f);
                    }

                    CalculateAndDisplayTurnOrder(aliveUnits);
                    pController.StartTurn(enemyUnitScript, () => { isTurnFinished = true; });
                }
            }
            // 적군 턴
            else
            {
                EnemyBattleController eController = nextTurnUnit.GetComponent<EnemyBattleController>();
                if (eController != null)
                {
                    if (BattleCameraManager.Instance != null)
                    {
                        BattleCameraManager.Instance.SetEnemyView(nextTurnUnit.transform);
                    }
                    eController.ExecuteTurn(playerUnits, () => { isTurnFinished = true; });
                }
            }

            // 턴 행동(공격, 스킬 등) 완료 대기
            yield return new WaitUntil(() => isTurnFinished);

            // 행동 종료 후 수치 초기화
            nextTurnUnit.InitializeActionValue();

            // 카메라 메인 뷰 복귀 및 튐 현상 방지를 위한 블렌딩 완료 대기 로직 적용
            if (BattleCameraManager.Instance != null)
            {
                BattleCameraManager.Instance.ResetToMainView();
            }

            CinemachineBrain mainBrain = Camera.main.GetComponent<CinemachineBrain>();
            if (mainBrain != null)
            {
                float timeout = 0.5f;
                float elapsed = 0f;
                while (!mainBrain.IsBlending && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // 메인 뷰로 복귀하는 전환이 끝날 때까지 대기하여 다음 턴 카메라와 충돌 방지
                yield return new WaitUntil(() => !mainBrain.IsBlending);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    // 생존 유닛 추출
    private List<UnitController> GetAliveUnits()
    {
        List<UnitController> aliveUnits = new List<UnitController>();
        foreach (UnitController unit in playerUnits)
        {
            if (unit.currentHP > 0) aliveUnits.Add(unit);
        }
        if (enemyUnitScript != null && enemyUnitScript.currentHP > 0)
        {
            aliveUnits.Add(enemyUnitScript);
        }
        return aliveUnits;
    }

    // 전투 종료 판정
    private bool CheckBattleEnd(List<UnitController> aliveUnits)
    {
        bool hasPlayer = false;
        bool hasEnemy = false;

        foreach (UnitController unit in aliveUnits)
        {
            if (playerUnits.Contains(unit)) hasPlayer = true;
            else hasEnemy = true;
        }

        if (!hasPlayer)
        {
            state = BattleState.Lost;
            EndBattle();
            return true;
        }
        if (!hasEnemy)
        {
            state = BattleState.Won;
            EndBattle();
            return true;
        }

        return false;
    }

    // 행동치 계산 및 다음 턴 유닛 반환
    private UnitController GetNextUnitAndAdvanceTime(List<UnitController> aliveUnits)
    {
        UnitController nextUnit = null;
        float lowestAV = float.MaxValue;

        foreach (UnitController unit in aliveUnits)
        {
            if (unit.currentActionValue < lowestAV)
            {
                lowestAV = unit.currentActionValue;
                nextUnit = unit;
            }
        }

        foreach (UnitController unit in aliveUnits)
        {
            unit.currentActionValue -= lowestAV;
        }

        return nextUnit;
    }

    // 턴 오더 예측 및 UI 표시
    private void CalculateAndDisplayTurnOrder(List<UnitController> aliveUnits)
    {
        List<Sprite> predictedTurns = new List<Sprite>();
        Dictionary<UnitController, float> simulatedAVs = new Dictionary<UnitController, float>();

        foreach (UnitController unit in aliveUnits)
        {
            simulatedAVs.Add(unit, unit.currentActionValue);
        }

        for (int i = 0; i < 5; i++)
        {
            UnitController nextUnit = null;
            float lowestAV = float.MaxValue;

            foreach (var kvp in simulatedAVs)
            {
                if (kvp.Value < lowestAV)
                {
                    lowestAV = kvp.Value;
                    nextUnit = kvp.Key;
                }
            }

            if (nextUnit != null)
            {
                predictedTurns.Add(nextUnit.unitPortrait);

                List<UnitController> keys = new List<UnitController>(simulatedAVs.Keys);
                foreach (UnitController key in keys)
                {
                    simulatedAVs[key] -= lowestAV;
                }

                simulatedAVs[nextUnit] += (10000f / nextUnit.speed);
            }
        }

        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.UpdateTurnOrderUI(predictedTurns);
        }
    }

    // 승패 후처리 로직
    private void EndBattle()
    {
        if (state == BattleState.Won)
        {
            Debug.Log("전투 승리! 보상 화면으로 이동합니다.");
        }
        else if (state == BattleState.Lost)
        {
            Debug.Log("전투 패배. 게임 오버 씬으로 이동합니다.");
        }
    }
}
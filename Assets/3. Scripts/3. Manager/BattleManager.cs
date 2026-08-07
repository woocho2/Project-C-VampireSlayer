using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

// 전투의 전체적인 진행 상태를 나타내는 열거형(Enum) 정의
public enum BattleState { Start, TurnProgress, Won, Lost }

public class BattleManager : MonoBehaviour
{
    [Header("전투 상태 관리")]
    public BattleState state; // 현재 진행 중인 전투 상태 (시작, 진행 중, 승리, 패배)

    [Header("스폰 위치 설정")]
    public Transform[] playerStations; // 아군 캐릭터들이 소환되어 배치될 위치 배열
    public Transform enemyStation;    // 적(몬스터)이 소환되어 배치될 위치

    [Header("연출 설정")]
    public PlayableDirector introDirector; // 전투 시작 시 인트로 타임라인 컷신을 재생할 컴포넌트

    private List<UnitController> playerUnits = new List<UnitController>(); // 씬에 생성된 아군 유닛 컨트롤러들을 관리하는 리스트
    private UnitController enemyUnitScript;                             // 씬에 생성된 적 유닛 컨트롤러 참조 변수

    private void Start()
    {
        // 게임 시작 시 전투 상태를 'Start'로 변경하고 초기화 코루틴 실행
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    /// <summary>
    /// 전투 진입 시 캐릭터 스폰, 데이터 주입, 인트로 연출, 턴 루프 진입을 담당하는 초기화 코루틴
    /// </summary>
    private IEnumerator SetupBattle()
    {
        // 게임 데이터 매니저가 존재하는지 안전성 체크
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            yield break;
        }

        // 1. 파티원 데이터를 읽어와 남아있는 체력이 0보다 큰 살아있는 멤버만 씬에 생성
        PlayerDataSO[] party = GameManager.Instance.partyMembers;
        int stationIndex = 0;

        for (int i = 0; i < party.Length; i++)
        {
            if (party[i] != null && party[i].currentHealth > 0 && stationIndex < playerStations.Length)
            {
                // 아군 프리팹 생성 및 초기 위치/회전값 설정
                GameObject playerGO = Instantiate(party[i].playerPrefab, playerStations[stationIndex].position, playerStations[stationIndex].rotation);
                UnitController unit = playerGO.GetComponent<UnitController>();

                if (unit != null)
                {
                    unit.Setup(party[i]); // 데이터 주입
                    playerUnits.Add(unit); // 관리 리스트에 추가
                }
                stationIndex++;
            }
        }

        // 아군이 한 명도 스폰되지 않았따면 전멸 상태이므로 패배 처리 후 종료
        if (playerUnits.Count == 0)
        {
            state = BattleState.Lost;
            EndBattle();
            yield break;
        }

        // 2. 만난 적(몬스터) 프리팹 생성 및 데이터 주입
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

        // 3. 전투 시작 알림 UI 연출 재생
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.PlayBattleStartUI(m_eData.name);
        }

        // 4. 인트로 타임라인 연출 재생 및 완전 종료까지 대기
        if (introDirector != null)
        {
            introDirector.Play();
            yield return new WaitUntil(() => introDirector.state != PlayState.Playing);
        }

        // 5. 타임라인 종료 직후 메인 카메라인 BaseCamera의 우선순위를 15로 높여 
        // 카메라가 다른 곳으로 튀는 현상을 원천 방지하며 메인 뷰로 고정
        if (BattleCameraManager.Instance != null)
        {
            BattleCameraManager.Instance.ResetToMainView();
            if (BattleCameraManager.Instance.BaseCamera != null)
            {
                BattleCameraManager.Instance.BaseCamera.Priority = 15;
            }
        }

        yield return new WaitForSeconds(0.5f); // 메인 뷰 전환 안정화 대기

        // 카메라 고정용 임시 우선순위(15)를 기본값(10)으로 원상 복구
        if (BattleCameraManager.Instance != null && BattleCameraManager.Instance.BaseCamera != null)
        {
            BattleCameraManager.Instance.BaseCamera.Priority = 10;
        }

        // 6. 전투 상태를 진행 중으로 바꾸고 본격적인 턴 루프 실행
        state = BattleState.TurnProgress;
        StartCoroutine(TurnLoopRoutine());
    }

    /// <summary>
    /// 타임라인 신호(Signal): 적 몬스터가 0번 아군을 바라보도록 부드럽게 회전
    /// </summary>
    public void Signal_LookAtPlayer()
    {
        if (enemyUnitScript != null && playerUnits.Count > 0)
        {
            Transform targetPlayer = playerStations[1].transform;
            StartCoroutine(SmoothLookAtRoutine(enemyUnitScript.transform, targetPlayer.position, 0.5f));
        }
        else
        {
            Debug.LogError("조건 실패: 몬스터나 파티원 데이터가 비어있습니다!");
        }
    }

    /// <summary>
    /// 지정한 시간(duration) 동안 몬스터의 회전각을 구면 선형 보간(Slerp)으로 부드럽게 돌리는 코루틴
    /// </summary>
    private IEnumerator SmoothLookAtRoutine(Transform enemyTransform, Vector3 targetPosition, float duration)
    {
        Quaternion startRotation = enemyTransform.rotation;
        Vector3 directionToPlayer = (targetPosition - enemyTransform.position).normalized;
        directionToPlayer.y = 0; // 수평 회전만 적용되도록 Y축 고정

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

        enemyTransform.rotation = targetRotation; // 최종 오차 보정
    }

    /// <summary>
    /// 타임라인 신호(Signal): 살아있는 모든 아군 유닛에게 무기 뽑기(발도) 애니메이션 트리거 전달
    /// </summary>
    public void Signal_DrawSwords()
    {
        foreach (UnitController unit in playerUnits)
        {
            Animator playerAnim = unit.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("DrawWeapon");
        }
    }

    /// <summary>
    /// 전투가 끝날 때까지 턴을 계산하고 순차적으로 행동 권한을 넘겨주는 메인 턴제 루프 코루틴
    /// </summary>
    private IEnumerator TurnLoopRoutine()
    {
        while (state == BattleState.TurnProgress)
        {
            // 1. 살아있는 모든 유닛(아군 + 적군) 수집
            List<UnitController> aliveUnits = GetAliveUnits();

            // 2. 승패 검사 (어느 한쪽이 전부 사망했으면 턴 루프 종료)
            if (CheckBattleEnd(aliveUnits)) yield break;

            // 3. 행동 수치(AV)가 가장 낮은 유닛을 색인하고, 그 수치만큼 전체 시간에 따른 AV 차감
            UnitController nextTurnUnit = GetNextUnitAndAdvanceTime(aliveUnits);
            bool isTurnFinished = false; // 턴 행동 완료 콜백 플래그

            // 4. 다음 턴 유닛이 아군인 경우
            if (playerUnits.Contains(nextTurnUnit))
            {
                PlayerBattleController pController = nextTurnUnit.GetComponent<PlayerBattleController>();
                if (pController != null)
                {
                    // 카메라를 해당 플레이어의 등 뒤(숄더뷰) 구도로 전환
                    if (BattleCameraManager.Instance != null)
                    {
                        BattleCameraManager.Instance.SetPlayerBackView(nextTurnUnit.transform, enemyUnitScript.transform);
                    }

                    // 카메라가 백뷰 구도로 완벽하게 전환(블렌딩)될 때까지 대기
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

                    // 카메라 전환이 끝난 후 턴 오더 UI를 갱신하고 플레이어 턴 시작
                    CalculateAndDisplayTurnOrder(aliveUnits);
                    pController.StartTurn(enemyUnitScript, () => { isTurnFinished = true; });
                }
            }
            // 5. 다음 턴 유닛이 적 몬스터인 경우
            else
            {
                EnemyBattleController eController = nextTurnUnit.GetComponent<EnemyBattleController>();
                if (eController != null)
                {
                    // 카메라를 적 단독 뷰 구도로 전환
                    if (BattleCameraManager.Instance != null)
                    {
                        BattleCameraManager.Instance.SetEnemyView(nextTurnUnit.transform);
                    }

                    // AI 행동 수행 시작 (콜백 전달)
                    eController.ExecuteTurn(playerUnits, () => { isTurnFinished = true; });
                }
            }

            // 6. 플레이어의 버튼 입력이나 AI 연출이 끝나 콜백(isTurnFinished = true)이 올 때까지 대기
            yield return new WaitUntil(() => isTurnFinished);

            // 7. 행동을 마친 유닛의 AV를 속도 공식(10000 / Speed)에 따라 초기화
            nextTurnUnit.InitializeActionValue();

            // 8. 턴이 끝나면 카메라를 메인 전체 뷰로 복귀
            if (BattleCameraManager.Instance != null)
            {
                BattleCameraManager.Instance.ResetToMainView();
            }

            // 카메라가 메인 뷰로 돌아가는 전환 애니메이션이 완전히 끝날 때까지 대기하여 다음 턴 카메라와 화면 튐 방지
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

                yield return new WaitUntil(() => !mainBrain.IsBlending);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    /// <summary>
    /// 현재 필드에서 체력이 0보다 큰(살아있는) 아군과 적 유닛 목록을 반환
    /// </summary>
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

    /// <summary>
    /// 생존한 유닛을 확인하여 아군 전멸(패배) 또는 적 처치(승리) 조건 판정
    /// </summary>
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

    /// <summary>
    /// 가장 행동 수치(AV)가 낮아 행동할 차례가 된 유닛을 찾고, 해당 수치만큼 전체 생존 유닛의 AV를 감소시킴
    /// </summary>
    private UnitController GetNextUnitAndAdvanceTime(List<UnitController> aliveUnits)
    {
        UnitController nextUnit = null;
        float lowestAV = float.MaxValue;

        // 가장 작은 AV를 가진 유닛 검색
        foreach (UnitController unit in aliveUnits)
        {
            if (unit.currentActionValue < lowestAV)
            {
                lowestAV = unit.currentActionValue;
                nextUnit = unit;
            }
        }

        // 전체 유닛의 AV 차감 (시간의 흐름 구현)
        foreach (UnitController unit in aliveUnits)
        {
            unit.currentActionValue -= lowestAV;
        }

        return nextUnit;
    }

    /// <summary>
    /// 향후 진행될 5턴을 시뮬레이션하여 턴 순서 UI(초상화)를 예측 계산 및 업데이트
    /// </summary>
    private void CalculateAndDisplayTurnOrder(List<UnitController> aliveUnits)
    {
        List<Sprite> predictedTurns = new List<Sprite>();
        Dictionary<UnitController, float> simulatedAVs = new Dictionary<UnitController, float>();

        // 가상의 시뮬레이션용 AV 딕셔너리 복사본 생성
        foreach (UnitController unit in aliveUnits)
        {
            simulatedAVs.Add(unit, unit.currentActionValue);
        }

        // 5턴 치의 행동 순서 예측
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

                // 가상 시뮬레이션 상에서 턴을 마친 유닛의 AV 충전
                simulatedAVs[nextUnit] += (10000f / nextUnit.speed);
            }
        }

        // 예측된 초상화 리스트를 UI 매니저로 전달
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.UpdateTurnOrderUI(predictedTurns);
        }
    }

    /// <summary>
    /// 전투 종료(승리/패배) 시 처리 로직
    /// </summary>
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
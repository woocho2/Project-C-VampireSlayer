using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum BattleState { Start, TurnProgress, Won, Lost }

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("전투 상태 관리")]
    public BattleState state;

    [Header("스폰 위치 설정 (고정 깡통 슬롯)")]
    public Transform[] playerStations;
    public Transform[] enemyStations;

    [Header("연출 설정")]
    public PlayableDirector introDirector;
    public PlayableDirector playerAttackDirector;
    public PlayableDirector enemyAttackDirector;

    private List<UnitController> playerUnits = new List<UnitController>();
    private List<UnitController> enemyUnits = new List<UnitController>();

    public List<UnitController> PlayerUnits => playerUnits;
    public List<UnitController> EnemyUnits => enemyUnits;

    public UnitController CurrentAttacker { get; set; }
    public UnitController CurrentTarget { get; set; }

    // ==========================================
    // [1] 유니티 생명주기
    // ==========================================
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    // ==========================================
    // [2] 전투 초기화 및 인트로 (Setup)
    // ==========================================
    private IEnumerator SetupBattle()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            yield break;
        }

        // 1. 아군 스폰 및 셋업
        PlayerDataSO[] party = GameManager.Instance.partyMembers;
        int stationIndex = 0;

        for (int i = 0; i < party.Length; i++)
        {
            if (party[i] != null && party[i].currentHealth > 0 && stationIndex < playerStations.Length)
            {
                UnitController unit = playerStations[stationIndex].GetComponent<UnitController>();
                if (unit != null)
                {
                    unit.Setup(party[i]);
                    if (BattleUIManager.Instance != null && stationIndex < BattleUIManager.Instance.playerStatusUIs.Length)
                    {
                        PlayerStatusUI targetUI = BattleUIManager.Instance.playerStatusUIs[stationIndex];
                        if (targetUI != null)
                        {
                            targetUI.gameObject.SetActive(true);
                            unit.LinkUI(targetUI);
                        }
                    }
                    playerUnits.Add(unit);
                }
                stationIndex++;
            }
        }

        // 남은 아군 슬롯 비활성화
        for (int i = stationIndex; i < playerStations.Length; i++)
        {
            if (playerStations[i] != null) playerStations[i].gameObject.SetActive(false);
            if (BattleUIManager.Instance != null && i < BattleUIManager.Instance.playerStatusUIs.Length)
            {
                if (BattleUIManager.Instance.playerStatusUIs[i] != null)
                    BattleUIManager.Instance.playerStatusUIs[i].gameObject.SetActive(false);
            }
        }

        // 2. 적 스폰 및 셋업 (리더인 1번 인덱스 사용)
        EnemyDataSO m_eData = GameManager.Instance.encounteredMonster;
        if (m_eData != null && enemyStations.Length > 1 && enemyStations[1] != null)
        {
            UnitController enemyUnit = enemyStations[1].GetComponent<UnitController>();
            if (enemyUnit != null)
            {
                enemyUnit.Setup(m_eData);
                enemyUnits.Add(enemyUnit);
            }
        }

        foreach (Transform station in enemyStations)
        {
            if (station != null)
            {
                UnitController uc = station.GetComponent<UnitController>();
                if (uc != null && !enemyUnits.Contains(uc))
                {
                    station.gameObject.SetActive(false);
                }
            }
        }

        if (playerUnits.Count == 0)
        {
            state = BattleState.Lost;
            EndBattle();
            yield break;
        }

        // 3. 인트로 연출 바인딩 및 재생
        if (introDirector != null && enemyUnits.Count > 0)
        {
            Animator enemyAnim = enemyUnits[0].GetComponentInChildren<Animator>();
            foreach (var track in introDirector.playableAsset.outputs)
            {
                if (track.streamName == "EnemyAnimTrack")
                {
                    introDirector.SetGenericBinding(track.sourceObject, enemyAnim);
                    break;
                }
            }
        }

        if (introDirector != null)
        {
            introDirector.Play();
            yield return new WaitUntil(() => introDirector.state != PlayState.Playing);
        }

        if (BattleUIManager.Instance != null) BattleUIManager.Instance.HideBattleStartUI();

        // 4. 카메라 복구 및 메인 루프 진입
        if (BattleCameraManager.Instance != null)
        {
            BattleCameraManager.Instance.ResetToMainView();
            if (BattleCameraManager.Instance.BaseCamera != null)
                BattleCameraManager.Instance.BaseCamera.Priority = 15;
        }

        yield return new WaitForSeconds(0.5f);

        if (BattleCameraManager.Instance != null && BattleCameraManager.Instance.BaseCamera != null)
            BattleCameraManager.Instance.BaseCamera.Priority = 10;

        CalculateAndDisplayTurnOrder(GetAliveUnits());

        state = BattleState.TurnProgress;
        StartCoroutine(TurnLoopRoutine());
    }

    // ==========================================
    // [3] 메인 턴 루프 (Core Loop)
    // ==========================================
    private IEnumerator TurnLoopRoutine()
    {
        while (state == BattleState.TurnProgress)
        {
            List<UnitController> aliveUnits = GetAliveUnits();
            if (CheckBattleEnd(aliveUnits)) yield break;

            UnitController nextTurnUnit = GetNextUnitAndAdvanceTime(aliveUnits);
            bool isTurnFinished = false;
            CurrentAttacker = nextTurnUnit;

            // 아군 턴
            if (playerUnits.Contains(nextTurnUnit))
            {
                // (기존 코드) 타겟팅 로직 등...
                List<UnitController> aliveEnemies = GetAliveEnemies();
                if (aliveEnemies.Count > 0)
                {
                    CurrentTarget = aliveEnemies[0];
                }

                PlayerBattleController pController = nextTurnUnit.GetComponent<PlayerBattleController>();

                if (pController != null)
                {
                    if (BattleCameraManager.Instance != null && CurrentTarget != null)
                        BattleCameraManager.Instance.SetPlayerBackView(nextTurnUnit.transform, CurrentTarget.transform);

                    yield return StartCoroutine(WaitForCameraBlend());

                    CalculateAndDisplayTurnOrder(aliveUnits);
                    pController.StartTurn(CurrentTarget, () => { isTurnFinished = true; });
                }
            }
            // 적군 턴
            else
            {
                EnemyBattleController eController = nextTurnUnit.GetComponent<EnemyBattleController>();
                if (eController != null)
                {
                    if (BattleCameraManager.Instance != null)
                        BattleCameraManager.Instance.SetEnemyView(nextTurnUnit.transform);

                    CalculateAndDisplayTurnOrder(aliveUnits);
                    eController.ExecuteTurn(playerUnits, () => { isTurnFinished = true; });
                }
            }

            yield return new WaitUntil(() => isTurnFinished);

            nextTurnUnit.InitializeActionValue();

            if (BattleCameraManager.Instance != null)
                BattleCameraManager.Instance.ResetToMainView();

            yield return StartCoroutine(WaitForCameraBlend());
        }
    }

    // ==========================================
    // [4] 보조 로직 함수 (Helpers)
    // ==========================================

    // 카메라 블렌딩 대기를 처리하는 공통 헬퍼 코루틴 (가독성을 위해 분리)
    private IEnumerator WaitForCameraBlend()
    {
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
    }

    private List<UnitController> GetAliveUnits()
    {
        List<UnitController> aliveUnits = new List<UnitController>();
        foreach (UnitController unit in playerUnits)
        {
            if (unit.currentHP > 0) aliveUnits.Add(unit);
        }

        foreach (UnitController unit in enemyUnits)
        {
            if (unit.currentHP > 0) aliveUnits.Add(unit);
        }
        return aliveUnits;
    }

    /// <summary>
    ///  현재 살아있는 적군 유닛만 걸러내어 변환합니다. (플레이어 자동 타겟팅용)
    /// </summary>
    public List<UnitController> GetAliveEnemies()
    {
        List<UnitController> aliveEnemies = new List<UnitController>();
        foreach (UnitController unit in enemyUnits)
        {
            if (unit.currentHP > 0) aliveEnemies.Add(unit);
        }
        return aliveEnemies;
    }

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

    // ==========================================
    // [5] 전투 종료 판정
    // ==========================================
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
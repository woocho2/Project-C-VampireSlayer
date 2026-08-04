using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { Start, PlayerTurn, EnemyTurn, Won, Lost }

public class BattleManager : MonoBehaviour
{
    [Header("전투 상태 관리")]
    public BattleState state;

    [Header("스폰 위치 설정 (다중)")]
    public Transform[] playerStations;
    public Transform enemyStation;

    // 소환된 파티원들의 스크립트를 담아둘 리스트입니다.
    private List<UnitController> playerUnits = new List<UnitController>();
    private UnitController enemyUnitScript;

    public GameObject monsterCloseUpCamera;

    private void Start()
    {
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            yield break;
        }

        // 1. 파티원 필터링 및 순차 생성
        PlayerDataSO[] party = GameManager.Instance.partyMembers;
        int stationIndex = 0;

        for (int i = 0; i < party.Length; i++)
        {
            // 빈 슬롯이 아니며, 살아있고, 남은 스폰 자리가 있을 때만 소환합니다.
            if (party[i] != null && party[i].currentHealth > 0 && stationIndex < playerStations.Length)
            {
                GameObject playerGO = Instantiate(party[i].playerPrefab, playerStations[stationIndex].position, playerStations[stationIndex].rotation);
                UnitController unit = playerGO.GetComponent<UnitController>();

                if (unit != null)
                {
                    unit.Setup(party[i]);
                    playerUnits.Add(unit);
                    Debug.Log(unit.unitName + " 준비 완료! (배치 자리: " + stationIndex + ")");
                }

                stationIndex++;
            }
        }

        // 2. 예외 처리 (전멸 상태 검사)
        if (playerUnits.Count == 0)
        {
            Debug.LogError("전투 가능한 파티원이 없습니다. 게임 오버 씬으로 이동해야 합니다.");
            state = BattleState.Lost;
            EndBattle();
            yield break;
        }

        // 3. 몬스터 생성 및 셋업 (누락되었던 로직 복구)
        EnemyDataSO m_eData = GameManager.Instance.encounteredMonster;
        GameObject enemyGO = null; // 아래 회전 로직에서 참조할 수 있도록 지역 변수로 선언합니다.

        if (m_eData != null && m_eData.monsterPrefab != null)
        {
            enemyGO = Instantiate(m_eData.monsterPrefab, enemyStation.position, enemyStation.rotation);
            enemyUnitScript = enemyGO.GetComponent<UnitController>();

            if (enemyUnitScript != null)
            {
                enemyUnitScript.Setup(m_eData);
                Debug.Log(enemyUnitScript.unitName + "이(가) 나타났다!");
            }
        }
        else
        {
            Debug.LogError("GameManager에서 몬스터 데이터를 불러오지 못했습니다.");
        }

        // --- 연출 시작 ---
        if (monsterCloseUpCamera != null) monsterCloseUpCamera.SetActive(true);

        // (UI 연출 로직이 있다면 이 부분에 추가)
        UIManager_Battle.Instance.PlayBattleStartUI(m_eData.name);

        yield return new WaitForSeconds(1f);

        // 4. 몬스터가 파티의 '첫 번째' 플레이어를 향해 회전하는 로직
        if (enemyGO != null && playerUnits.Count > 0)
        {
            float turnDuration = 1.0f;
            float elapsed = 0f;
            Quaternion startRotation = enemyGO.transform.rotation;

            // 시선을 리스트의 0번(리더) 캐릭터에게 맞춥니다.
            Vector3 directionToPlayer = (playerUnits[1].transform.position - enemyGO.transform.position).normalized;
            directionToPlayer.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            while (elapsed < turnDuration)
            {
                elapsed += Time.deltaTime;
                enemyGO.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / turnDuration);
                yield return null;
            }
            enemyGO.transform.rotation = targetRotation;
        }

        yield return new WaitForSeconds(1f);

        if (monsterCloseUpCamera != null) monsterCloseUpCamera.SetActive(false);

        yield return new WaitForSeconds(2f);

        // 5. 소환된 '모든' 파티원에게 동시에 발도 애니메이션 트리거를 보냅니다.
        foreach (UnitController unit in playerUnits)
        {
            Animator playerAnim = unit.GetComponentInChildren<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("DrawSword");
            }
        }

        yield return new WaitForSeconds(1.5f);

        state = BattleState.PlayerTurn;
        PlayerTurn();
    }

    private void PlayerTurn()
    {
        Debug.Log("플레이어 측 턴 시작!");
        // TODO: 행동 대기 상태 구현
    }

    private void EndBattle()
    {
        if (state == BattleState.Won)
        {
            Debug.Log("전투 승리!");
        }
        else if (state == BattleState.Lost)
        {
            Debug.Log("전투 패배.");
        }
    }
}
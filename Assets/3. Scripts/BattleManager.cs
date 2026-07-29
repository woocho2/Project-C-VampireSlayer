using System.Collections;
using UnityEngine;

public enum BattleState { Start, PlayerTurn, EnemyTurn, Won, Lost }

public class BattleManager : MonoBehaviour
{
    [Header("전투 상태 관리")]
    public BattleState state;

    [Header("스폰 위치 설정")]
    public Transform playerStation;
    public Transform enemyStation;

    // 프리팹 변수를 지우고 유닛 스크립트 캐싱 변수만 남깁니다.
    private UnitController playerUnitScript;
    private UnitController enemyUnitScript;

    public GameObject monsterCloseUpCamera;


    private void Start()
    {
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()
    {
        // 싱글톤 GameManager가 존재하는지 먼저 확인합니다.
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            yield break;
        }

        // 1. 플레이어 생성 및 셋업 (GameManager에서 데이터를 가져옵니다)
        CharacterDataSO pData = GameManager.Instance.playerData;
        if (pData != null && pData.playerPrefab != null)
        {
            // PlayerDataSO 안에 등록된 배틀용 프리팹을 지정된 위치에 생성합니다.
            GameObject playerGO = Instantiate(pData.playerPrefab, playerStation.position, playerStation.rotation);
            playerUnitScript = playerGO.GetComponent<UnitController>();

            if (playerUnitScript != null)
            {
                // 플레이어 셋업 오버로딩 함수 호출
                playerUnitScript.Setup(pData);
                Debug.Log(playerUnitScript.unitName + " 준비 완료!");
            }
        }
        else
        {
            Debug.LogError("GameManager에 PlayerData가 연결되지 않았거나 프리팹이 누락되었습니다.");
        }

        // 2. 몬스터 생성 및 셋업 (기존 로직과 동일하게 GameManager에서 가져옵니다)
        MonsterDataSO mData = GameManager.Instance.encounteredMonster;
        if (mData != null && mData.monsterPrefab != null)
        {
            GameObject enemyGO = Instantiate(mData.monsterPrefab, enemyStation.position, enemyStation.rotation);
            enemyUnitScript = enemyGO.GetComponent<UnitController>();

            if (enemyUnitScript != null)
            {
                // 몬스터 셋업 오버로딩 함수 호출
                enemyUnitScript.Setup(mData);
                Debug.Log(enemyUnitScript.unitName + "이(가) 나타났다!");
            }
        }
        else
        {
            Debug.LogError("GameManager에서 몬스터 데이터를 불러오지 못했습니다.");
        }

        if (monsterCloseUpCamera != null) monsterCloseUpCamera.SetActive(true);

        if (UIManager_Battle.Instance != null && mData != null)
        {
            UIManager_Battle.Instance.PlayBattleStartUI(mData.monsterName);
        }

        // 3. 등장 애니메이션을 위해 2초 대기 후 플레이어 턴 시작
        yield return new WaitForSeconds(3f);
        state = BattleState.PlayerTurn;
        PlayerTurn();
    }

    private void PlayerTurn()
    {
        Debug.Log("플레이어의 턴! 행동을 선택하세요.");
        // 유저 입력(버튼 클릭) 대기 상태
    }

    public void OnAttackButton()
    {
        if (state != BattleState.PlayerTurn) return;
        StartCoroutine(PlayerAttack());
    }

    private IEnumerator PlayerAttack()
    {
        Debug.Log("플레이어가 공격합니다!");

        // 임시로 플레이어의 공격력을 10으로 고정하여 데미지를 줍니다.
        // 추후 GameManager.Instance.playerData.baseDamage 등을 활용해 계산식을 개선할 수 있습니다.
        bool isDead = enemyUnitScript.TakeDamage(10);

        yield return new WaitForSeconds(1f);

        if (isDead)
        {
            state = BattleState.Won;
            EndBattle();
        }
        else
        {
            state = BattleState.EnemyTurn;
            StartCoroutine(EnemyTurn());
        }
    }

    private IEnumerator EnemyTurn()
    {
        Debug.Log("적의 턴입니다.");
        yield return new WaitForSeconds(1f);

        // 몬스터가 플레이어를 공격하는 로직
        // 추후 mData.baseDamage 등을 활용해 구현할 수 있습니다.
        bool isPlayerDead = playerUnitScript.TakeDamage(5);

        yield return new WaitForSeconds(1f);

        if (isPlayerDead)
        {
            state = BattleState.Lost;
            EndBattle();
        }
        else
        {
            state = BattleState.PlayerTurn;
            PlayerTurn();
        }
    }

    private void EndBattle()
    {
        if (state == BattleState.Won)
        {
            Debug.Log("전투 승리!");
            // TODO: 경험치 획득 및 플레이어 현재 체력(currentHP)을 PlayerDataSO에 저장 후 필드 복귀
        }
        else if (state == BattleState.Lost)
        {
            Debug.Log("전투 패배.");
            // TODO: 게임 오버 처리
        }
    }
}
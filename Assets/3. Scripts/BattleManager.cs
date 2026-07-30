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
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            yield break;
        }

        // 1. 플레이어 생성 및 셋업
        CharacterDataSO pData = GameManager.Instance.playerData;
        GameObject playerGO = null;
        if (pData != null && pData.playerPrefab != null)
        {
            playerGO = Instantiate(pData.playerPrefab, playerStation.position, playerStation.rotation);
            playerUnitScript = playerGO.GetComponent<UnitController>();

            if (playerUnitScript != null)
            {
                playerUnitScript.Setup(pData);
                Debug.Log(playerUnitScript.unitName + " 준비 완료!");
            }
        }
        else
        {
            Debug.LogError("GameManager에 PlayerData가 연결되지 않았거나 프리팹이 누락되었습니다.");
        }

        // 2. 몬스터 생성 및 셋업
        MonsterDataSO mData = GameManager.Instance.encounteredMonster;
        GameObject enemyGO = null;
        if (mData != null && mData.monsterPrefab != null)
        {
            // 몬스터는 enemyStation의 회전값을 그대로 가지고 태어납니다. (에디터에서 뒤를 보게 셋팅 필요)
            enemyGO = Instantiate(mData.monsterPrefab, enemyStation.position, enemyStation.rotation);
            enemyUnitScript = enemyGO.GetComponent<UnitController>();

            if (enemyUnitScript != null)
            {
                enemyUnitScript.Setup(mData);
                Debug.Log(enemyUnitScript.unitName + "이(가) 나타났다!");
            }
        }
        else
        {
            Debug.LogError("GameManager에서 몬스터 데이터를 불러오지 못했습니다.");
        }

        // --- 연출 시작 ---
        if (monsterCloseUpCamera != null) monsterCloseUpCamera.SetActive(true);

        if (UIManager_Battle.Instance != null && mData != null)
        {
            UIManager_Battle.Instance.PlayBattleStartUI(mData.monsterName);
        }

        // 3-1. 몬스터가 뒤를 돌고 있는 상태로 1초 대기합니다.
        yield return new WaitForSeconds(1f);

        // 3-2. 몬스터가 플레이어를 향해 부드럽게 회전하는 로직
        if (enemyGO != null && playerGO != null)
        {
            float turnDuration = 1.0f; // 회전하는 데 걸릴 총 시간 (1초)
            float elapsed = 0f;        // 경과 시간 측정용 변수

            Quaternion startRotation = enemyGO.transform.rotation; // 현재 몬스터의 회전값

            // 몬스터 위치에서 플레이어 위치로 향하는 방향 벡터를 구합니다.
            Vector3 directionToPlayer = (playerGO.transform.position - enemyGO.transform.position).normalized;
            // 몬스터가 위아래로 기울어지는(인사하는) 현상을 막기 위해 Y축 높이 차이를 무시합니다.
            directionToPlayer.y = 0;

            // 방향 벡터를 Quaternion 회전 데이터로 변환하여 최종 목표 각도를 설정합니다.
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            // 경과 시간이 1초(turnDuration)에 도달할 때까지 매 프레임 반복합니다.
            while (elapsed < turnDuration)
            {
                elapsed += Time.deltaTime;

                // Quaternion.Slerp를 사용하여 시작 각도에서 목표 각도까지 부드럽게 보간(보정)합니다.
                enemyGO.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / turnDuration);

                yield return null; // 다음 프레임이 렌더링될 때까지 루프를 일시 정지합니다.
            }

            // while문이 끝난 후 미세한 오차가 남을 수 있으므로 강제로 목표 각도를 완벽히 맞춥니다.
            enemyGO.transform.rotation = targetRotation;
        }

        // 3-3. 회전 완료 후 남은 1초를 대기하여 총 3초의 연출 시간을 맞춥니다.
        yield return new WaitForSeconds(1f);

        if (monsterCloseUpCamera != null) monsterCloseUpCamera.SetActive(false);

        yield return new WaitForSeconds(2f);

        if (playerGO != null)
        {
            Animator playerAnim = playerGO.GetComponentInChildren<Animator>();
            if (playerAnim != null)
            {
                // 1단계에서 생성한 트리거를 작동시켜 발도 애니메이션을 시작합니다.
                playerAnim.SetTrigger("DrawSword");

                // 발도 애니메이션이 재생되는 실제 시간(예: 1.5초)만큼 턴 시작을 추가로 대기시킵니다.
                // 이 대기 시간이 없으면 검을 뽑는 도중에 플레이어 턴이 시작되어버립니다.
                yield return new WaitForSeconds(1.5f);
            }
        }

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
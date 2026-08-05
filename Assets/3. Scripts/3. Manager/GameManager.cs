using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("파티 시스템 데이터")]
    // 전체 파티원 목록 (게임 시작 전 인스펙터에서 SO 에셋들을 미리 등록)
    public PlayerDataSO[] partyMembers;

    // 현재 맵에서 조종 중인 캐릭터의 인덱스 (PartyManager가 이 값을 참조하고 변경함)
    public int currentPartyIndex = 0;

    [Header("현재 활성 플레이어 (자동 갱신)")]
    // PartyManager가 캐릭터를 교체할 때마다 자동으로 덮어씌워주는 현재 조종 캐릭터 데이터
    public PlayerDataSO playerData;

    [Header("전투 진입 데이터")]
    public EnemyDataSO encounteredMonster;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartBattleTransition(EnemyDataSO targetMonsterData)
    {
        if (isTransitioning) return;

        encounteredMonster = targetMonsterData;
        StartCoroutine(BattleTransitionRoutine());
    }

    private IEnumerator BattleTransitionRoutine()
    {
        isTransitioning = true;

        Time.timeScale = 0.01f;
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;

        SceneManager.LoadScene("BattleScene");

        isTransitioning = false;
    }
}
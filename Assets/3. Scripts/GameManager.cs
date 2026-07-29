using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스 (어디서든 접근 가능하도록 설정)
    public static GameManager Instance { get; private set; }

    [Header("플레이어 데이터")]
    // 게임 전체에서 하나로 유지되어야 할 플레이어 데이터를 보관합니다.
    // 에디터에서 생성한 PlayerDataSO 에셋을 여기에 할당합니다.
    public CharacterDataSO playerData;

    [Header("전투 진입 데이터")]
    // 필드에서 부딪힌 몬스터의 데이터를 배틀 씬으로 넘겨주기 위해 임시 보관합니다.
    public MonsterDataSO encounteredMonster;

    private bool isTransitioning = false;

    private void Awake()
    {
        // 씬이 넘어가도 파괴되지 않도록 싱글톤을 셋업합니다.
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

    // 필드 몬스터와 충돌 시 호출되는 씬 전환 로직
    public void StartBattleTransition(MonsterDataSO targetMonsterData)
    {
        if (isTransitioning) return;

        // 조우한 몬스터 데이터를 저장하고 코루틴을 실행합니다.
        encounteredMonster = targetMonsterData;
        StartCoroutine(BattleTransitionRoutine());
    }

    private IEnumerator BattleTransitionRoutine()
    {
        isTransitioning = true;

        // 전투 돌입 시 멈추는 연출을 위해 시간을 느리게 합니다.
        Time.timeScale = 0.01f;
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;

        // 배틀 씬을 로드합니다.
        SceneManager.LoadScene("BattleScene");

        isTransitioning = false;
    }
}
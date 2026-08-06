using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴 적용: 씬이 전환되어도 파괴되지 않고 유지되는 전역 매니저 클래스
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
    public EnemyDataSO encounteredMonster; // 전투 진입 시 마주친 몬스터의 데이터 (SO)

    private bool isTransitioning = false; // 중복 전투 전환을 방지하기 위한 플래그 변수

    private void Awake()
    {
        // 싱글톤 초기화: 이미 존재하지 않으면 자신을 등록하고 씬 전환 시 파괴되지 않도록 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 중복 생성된 GameManager 파괴
        }
    }

    // 외부(예: 몬스터와 충돌 등)에서 전투 전환을 요청할 때 호출하는 함수
    public void StartBattleTransition(EnemyDataSO targetMonsterData)
    {
        if (isTransitioning) return; // 이미 전환 중이면 중복 실행 방지

        encounteredMonster = targetMonsterData; // 만난 몬스터 데이터 저장
        StartCoroutine(BattleTransitionRoutine()); // 연출 및 씬 이동 코루틴 실행
    }

    // 슬로우 모션 연출 후 전투 씬으로 이동하는 코루틴
    private IEnumerator BattleTransitionRoutine()
    {
        isTransitioning = true;

        Time.timeScale = 0.01f; // 게임 속도를 극단적으로 낮춰 슬로우 모션 연출
        yield return new WaitForSecondsRealtime(2f); // 실제 시간 기준 2초 대기
        Time.timeScale = 1f; // 게임 속도를 원래대로 복구

        SceneManager.LoadScene("BattleScene"); // 전투 씬으로 이동

        isTransitioning = false;
    }
}
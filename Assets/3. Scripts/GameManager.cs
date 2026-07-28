using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 전역 접근을 위한 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    private bool isTransitioning = false; // 중복 전환 방지 플래그

    private void Awake()
    {
        // 싱글톤 패턴 초기화
        if (Instance == null)
        {
            Instance = this;
            // 씬이 전환되어도 GameManager 객체가 파괴되지 않도록 설정 (선택 사항)
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 뱀파이어가 플레이어와 충돌했을 때 호출할 함수
    public void StartBattleTransition()
    {
        // 이미 씬 전환이 진행 중이라면 중복 실행 방지
        if (isTransitioning) return;

        StartCoroutine(BattleTransitionRoutine());
    }

    private IEnumerator BattleTransitionRoutine()
    {
        isTransitioning = true;

        // 1. 게임 속도를 0.5배속으로 변경
        Time.timeScale = 0.05f;

        // 2. 현실 시간 기준으로 정확히 2초 대기
        yield return new WaitForSecondsRealtime(2f);

        // 3. 다음 씬으로 넘어가기 전, 게임 속도를 다시 정상(1.0)으로 복구
        // 복구하지 않으면 다음 씬에서도 0.5배속이 유지됩니다.
        Time.timeScale = 1f;

        // 4. 배틀 씬 로드
        SceneManager.LoadScene("BattleScene");
    }
}
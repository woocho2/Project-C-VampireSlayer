using UnityEngine;
using Unity.Cinemachine;

public class BattleCameraManager : MonoBehaviour
{
    // 싱글톤 패턴 적용: 씬 어디서든 'BattleCameraManager.Instance'로 쉽게 접근할 수 있게 합니다.
    public static BattleCameraManager Instance { get; private set; }

    [Header("시네머신 카메라 등록 (역할별 1대씩)")]
    public CinemachineCamera BaseCamera;        // 전체 뷰 (Priority: 10)
    public CinemachineCamera playerBackCamera;  // 아군 등 뒤 (기본 Priority: 9)
    public CinemachineCamera playerFrontCamera; // 아군 앞모습 (기본 Priority: 9)
    public CinemachineCamera enemyCamera;       // 적 단독 뷰 (기본 Priority: 9)

    // IntroCamera는 Timeline(PlayableDirector)이 직접 제어하므로 스크립트에서 제외합니다.

    private void Awake()
    {
        // 싱글톤 초기화: 이미 존재하지 않으면 자신을 등록하고, 이미 있으면 중복 생성된 객체를 파괴합니다.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 모든 액션 카메라의 우선순위를 메인 카메라(10)보다 낮춰 전체 뷰로 복귀합니다.
    /// </summary>
    public void ResetToMainView()
    {
        // 모든 특수 카메라의 Priority(우선순위)를 9로 낮춰서 기본 카메라가 다시 화면을 비추게 만듭니다.
        if (playerBackCamera) playerBackCamera.Priority = 9;
        if (playerFrontCamera) playerFrontCamera.Priority = 9;
        if (enemyCamera) enemyCamera.Priority = 9;
    }

    /// <summary>
    /// 아군 턴 시작 시: 아군 등 뒤로 카메라 이동 (행동 선택 UI 출력 시점)
    /// </summary>
    public void SetPlayerBackView(Transform activePlayer, Transform enemy)
    {
        // 다른 카메라들을 초기화(메인 뷰로 돌림)
        ResetToMainView();

        if (activePlayer != null && enemy != null)
        {
            // 1. Follow 타겟 설정 (아군 위치 추적)
            playerBackCamera.Target.TrackingTarget = activePlayer;

            // 2. 플레이어 등 뒤 좌표 계산 (플레이어 정면 반대 방향으로 2칸 뒤, 위로 1.5칸 올림)
            Vector3 backPosition = activePlayer.position - (activePlayer.forward * 2f) + (Vector3.up * 1.5f);
            playerBackCamera.transform.position = backPosition;

            // 3. 적을 무조건 강제로 바라보게 회전값 고정 (카메라 위치에서 적을 향하는 벡터 계산 후 회전 적용)
            Vector3 lookDirection = (enemy.position - playerBackCamera.transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                playerBackCamera.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // 우선순위를 11로 높여서 이 카메라가 화면에 잡히도록(Live 상태가 되도록) 만듭니다.
        playerBackCamera.Priority = 11;
    }

    /// <summary>
    /// 아군 스킬 버튼 클릭 시: 아군 앞모습으로 카메라 이동 (스킬 목록 UI 출력 시점)
    /// </summary>
    public void SetPlayerFrontView(Transform activePlayer)
    {
        // 아군을 추적하고 바라보도록 타겟 설정
        playerFrontCamera.Target.TrackingTarget = activePlayer;
        playerFrontCamera.Target.LookAtTarget = activePlayer;

        // 백뷰 카메라(11)보다 더 높은 우선순위(12)를 주어 카메라가 자연스럽게 앞모습으로 전환되도록 함
        playerFrontCamera.Priority = 12;
    }

    /// <summary>
    /// 적 턴 시작 시: 적을 비추는 카메라로 이동
    /// </summary>
    public void SetEnemyView(Transform activeEnemy)
    {
        // 다른 카메라 초기화
        ResetToMainView();

        // 적을 추적하고 바라보도록 타겟 설정
        enemyCamera.Target.TrackingTarget = activeEnemy;
        enemyCamera.Target.LookAtTarget = activeEnemy;

        // 우선순위를 11로 주어 적 카메라를 활성화
        enemyCamera.Priority = 11;
    }
}
using UnityEngine;

// 유니티 에디터의 Create 메뉴에 항목을 추가하여 SO 에셋을 쉽게 만들 수 있게 합니다.
// 메뉴 이름도 네이밍 통일에 맞추어 Enemy Data로 변경했습니다.
[CreateAssetMenu(fileName = "New Enemy Data", menuName = "TurnBased/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public int monsterID;                     // 몬스터 고유 ID
    public string monsterName;            // 몬스터 이름
    public GameObject monsterPrefab;  // 배틀 씬에서 스폰할 몬스터의 외형 프리팹

    [Header("전투 스탯")]
    public int maxHealth;            // 최대 체력
    public int baseDamage;         // 기본 공격력
    public int speed;                  // 행동 속도 (턴 우선순위에 사용)

    [Header("필드 이동 및 탐지 설정")]
    public float patrolRadius = 10f;         // 배회 반경
    public float patrolSpeed = 3f;           // 배회 속도
    public float chaseSpeed = 8f;           // 플레이어 추적 속도
    public float detectionRadius = 10f;    // 플레이어 탐지 반경
    public float maxChaseDistance = 20f; // 플레이어 추적 최대 거리 (이 거리 이상이면 추적 포기)
    public float viewAngle = 120f;          // 몬스터의 좌우 시야각 범위 (예: 120도면 좌로 60도, 우로 60도)
    public float eyeHeight = 1.5f;           // 레이캐스트가 발사될 몬스터의 눈 높이 오프셋 (발밑에서 쏘면 바닥에 부딪힐 수 있음)
}
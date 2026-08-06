using UnityEngine;

// 이 클래스는 유니티에서 플레이어의 기본 정보, 스탯, 이동 및 전투 설정을 에셋 파일로 저장(ScriptableObject)할 수 있게 해줍니다.
[CreateAssetMenu(fileName = "New Player Data", menuName = "TurnBased/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string playerName = "Hero";                  // 플레이어의 이름 (기본값: Hero)
    public GameObject playerPrefab;                     // 전투 씬에서 소환될 플레이어 프리팹
    public GameObject fieldPlayerPrefab;                // 필드 씬에서 조작할 플레이어 프리팹
    public Sprite playerturnSprite;                     // 턴제 UI 등에서 사용될 플레이어 초상화 이미지

    [Header("전투 스탯")]
    public int maxHealth = 100;                         // 최대 체력
    public int currentHealth = 100;                     // 현재 체력
    public int baseDamage = 15;                         // 기본 공격력
    public int baseSpeed = 5;                           // 기본 속도 스탯

    [Header("필드 이동 및 회전 수치")]
    public float walkSpeed = 4f;                        // 걷기 이동 속도
    public float runSpeed = 10f;                        // 달리기 이동 속도
    public float jumpHeight = 2f;                       // 점프 높이
    public float gravity = -19.62f;                      // 적용될 중력 값
    public float rotationSpeed = 5f;                    // 회전 속도

    [Header("필드 가감속 설정")]
    public float acceleration = 15f;                    // 가속도 (속도가 붙는 빠름 정도)
    public float deceleration = 2f;                     // 감속도 (멈출 때 미끄러지는 정도)

    [Header("필드 전투 설정")]
    public float startBattleTime = 10f;                 // 전투 관련 상태(발도 등)를 유지하는 시간
}
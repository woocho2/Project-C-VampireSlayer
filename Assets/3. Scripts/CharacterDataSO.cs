using UnityEngine;

// 유니티 에디터 우클릭 메뉴에 생성 버튼을 추가합니다.
[CreateAssetMenu(fileName = "New Player Data", menuName = "TurnBased/Player Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("기본 정보")]
    // 플레이어의 이름입니다.
    public string playerName = "Hero";

    // 배틀 씬에서 소환할 플레이어의 외형 프리팹(CharacterController가 없는 배틀 전용 배리언트)입니다.
    public GameObject playerPrefab;

    [Header("전투 스탯")]
    // 최대 체력 수치입니다.
    public int maxHealth = 100;

    // 필드와 배틀 씬을 오가며 유지될 플레이어의 '현재' 체력입니다.
    public int currentHealth = 100;

    // 기본 공격력입니다.
    public int baseDamage = 15;
}
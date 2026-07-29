using UnityEngine;

// 유니티 에디터의 Create 메뉴에 항목을 추가하여 SO 에셋을 쉽게 만들 수 있게 합니다.
[CreateAssetMenu(fileName = "New Monster Data", menuName = "TurnBased/Monster Data")]
public class MonsterDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public int monsterID;             // 몬스터 고유 ID
    public string monsterName;        // 몬스터 이름
    public GameObject monsterPrefab;  // 배틀 씬에서 스폰할 몬스터의 외형 프리팹

    [Header("전투 스탯")]
    public int maxHealth;             // 최대 체력
    public int baseDamage;            // 기본 공격력
    public int speed;                 // 행동 속도 (턴 우선순위에 사용)

    // 스킬 데이터나 속성(불, 물, 풀 등) 데이터도 이 곳에 추가할 수 있습니다.
}
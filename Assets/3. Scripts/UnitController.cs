using UnityEngine;
using UnityEngine.UI;

public class UnitController : MonoBehaviour
{
    [Header("유닛 정보")]
    public string unitName; // 유닛의 이름
    public int maxHP;       // 최대 체력
    public int currentHP;   // 현재 체력
    public int defense;
    public int maxAP;
    public int currentAP;
    public int power;
    public bool isPlayer;

    [Header("시각 모델 생성용")]
    private GameObject spawnedVisualModel; // 생성된 자식 모델을 담아둘 변수

    [Header("위치 정보")]
    public Vector3 originalPosition;

    // ==========================================
    // 턴 계산용 데이터 및 초상화
    // ==========================================
    [Header("턴 및 렌더링 정보")]
    public Sprite unitPortrait;        // UI에 표시될 캐릭터/몬스터의 초상화 스프라이트
    public float speed = 100f;         // 속도 스탯 (행동치 계산에 사용)
    public float currentActionValue;   // 현재 남은 행동 수치 (0에 가까워질수록 먼저 턴이 옴)

    [Header("UI 연결 및 설정")]
    public Slider hpSlider;            // 체력을 표시하는 슬라이더 UI
    private Camera mainCamera;         // 메인 카메라 캐싱용

    private void Awake()
    {
        if (Camera.main != null) mainCamera = Camera.main;
        if (hpSlider != null) hpSlider.gameObject.SetActive(false); // 평소에는 체력바 숨김
    }

    /// <summary>
    /// [플레이어용] 데이터 셋업 함수
    /// </summary>
    public void Setup(PlayerDataSO data)
    {
        unitName = data.playerName;
        maxHP = data.maxHealth;
        currentHP = data.currentHealth;
        defense = data.defense;
        maxAP = data.maxAP;
        currentAP = data.currentAP;
        power = data.power;
        isPlayer = true;
        originalPosition = transform.position; // [추가] 자신의 스폰 위치 저장

        // SO(ScriptableObject) 데이터에서 초상화 이미지를 가져와 저장
        unitPortrait = data.playerturnSprite;

        InitializeActionValue(); // 행동치 초기화

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        // [핵심] 만약 이전에 생성된 모델이 있다면 제거하고 새로 생성합니다.
        if (spawnedVisualModel != null)
        {
            Destroy(spawnedVisualModel);
        }

        // SO 데이터에 연결된 시각 모델 프리팹이 존재한다면 고정 깡통(transform)의 자식으로 생성합니다.
        // (이때 playerPrefab 대신 외형만 있는 프리팹을 SO에 새로 만들어 연결해주어야 합니다)
        if (data.playerPrefab != null)
        {
            spawnedVisualModel = Instantiate(data.playerPrefab, transform);
            spawnedVisualModel.transform.localPosition = Vector3.zero;
            spawnedVisualModel.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// [몬스터용] 데이터 셋업 함수
    /// </summary>
    public void Setup(EnemyDataSO data)
    {
        unitName = data.monsterName;
        maxHP = data.maxHealth;
        currentHP = maxHP;
        defense = 0;
        maxAP = 0;
        currentAP = 0;
        power = data.power;
        isPlayer = false;
        
        originalPosition = transform.position; // [추가] 자신의 스폰 위치 저장

        // SO(ScriptableObject) 데이터에서 초상화 이미지를 가져와 저장
        unitPortrait = data.monsterturnSprite;

        InitializeActionValue(); // 행동치 초기화

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }


    }



    /// <summary>
    /// 스폰되거나 턴을 소모했을 때 자신의 행동 수치를 (10000 / 속도)로 리셋합니다. (스타레일식 턴제 속도 공식)
    /// </summary>
    public void InitializeActionValue()
    {
        if (speed <= 0) speed = 1f; // 0으로 나누는 에러 방지
        currentActionValue = 10000f / speed;
    }

    // 데미지를 입을 때 호출되는 함수 (사망 여부를 bool로 반환)
    public bool TakeDamage(int damage)
    {
        int finalDamage = damage;

        // 타겟이 플레이어(아군)일 경우에만 방어력 공식을 적용하여 데미지를 감소시킵니다.
        if (isPlayer)
        {
            // 공식: 적 공격력 * (100 / (캐릭터 방어력 + 100))
            float damageReduction = 100f / (defense + 100f);
            finalDamage = Mathf.RoundToInt(damage * damageReduction);

            // 방어력이 높아 데미지가 0 이하로 떨어지는 것을 방지 (최소 1 데미지)
            if (finalDamage <= 0) finalDamage = 1;
        }
        // 타겟이 에너미(적군)일 경우 isPlayer가 false이므로 트루 데미지(rawDamage)가 그대로 적용됩니다.

        currentHP -= finalDamage;
        Debug.Log($"{unitName}이(가) {finalDamage}의 데미지를 입었습니다. (남은 체력: {currentHP})");

        hpSlider.gameObject.SetActive(true);
        if (hpSlider != null) hpSlider.value = currentHP;

        if (currentHP <= 0)
        {
            currentHP = 0;
            return true; // 사망 처리
        }
        return false;
    }
}
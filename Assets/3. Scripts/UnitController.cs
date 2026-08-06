using UnityEngine;
using UnityEngine.UI;

public class UnitController : MonoBehaviour
{
    [Header("유닛 정보")]
    public string unitName; // 유닛의 이름
    public int maxHP;       // 최대 체력
    public int currentHP;   // 현재 체력

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
    /// [몬스터용] 데이터 셋업 함수
    /// </summary>
    public void Setup(EnemyDataSO data)
    {
        unitName = data.monsterName;
        maxHP = data.maxHealth;
        currentHP = maxHP;

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
    /// [플레이어용] 데이터 셋업 함수
    /// </summary>
    public void Setup(PlayerDataSO data)
    {
        unitName = data.playerName;
        maxHP = data.maxHealth;
        currentHP = data.currentHealth;

        // SO(ScriptableObject) 데이터에서 초상화 이미지를 가져와 저장
        unitPortrait = data.playerturnSprite;

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
        hpSlider.gameObject.SetActive(true); // 데미지를 입으면 체력바 활성화
        currentHP -= damage;

        if (hpSlider != null) hpSlider.value = currentHP;

        // 체력이 0 이하가 되면 사망 처리 (true 반환)
        if (currentHP <= 0)
        {
            currentHP = 0;
            return true;
        }
        return false;
    }
}
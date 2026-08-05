using UnityEngine;
using UnityEngine.UI;

public class UnitController : MonoBehaviour
{
    [Header("유닛 정보")]
    public string unitName;
    public int maxHP;
    public int currentHP;

    // ==========================================
    // [추가] 턴 계산용 데이터 및 초상화
    // ==========================================
    [Header("턴 및 렌더링 정보")]
    public Sprite unitPortrait;        // UI에 표시될 캐릭터/몬스터의 초상화
    public float speed = 100f;         // 속도 스탯 (인스펙터나 SO에서 할당)
    public float currentActionValue;   // 현재 남은 행동 수치

    [Header("UI 연결 및 설정")]
    public Slider hpSlider;
    private Camera mainCamera;

    private void Awake()
    {
        if (Camera.main != null) mainCamera = Camera.main;
        if (hpSlider != null) hpSlider.gameObject.SetActive(false);
    }

    /// <summary>
    /// [몬스터용] 데이터 셋업 함수
    /// </summary>
    public void Setup(EnemyDataSO data)
    {
        unitName = data.monsterName;
        maxHP = data.maxHealth;
        currentHP = maxHP;

        // [추가] SO 데이터에서 초상화 이미지를 꺼내와 내 컨트롤러에 저장합니다.
        unitPortrait = data.monsterturnSprite;

        InitializeActionValue();

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

        // [추가] SO 데이터에서 초상화 이미지를 꺼내와 내 컨트롤러에 저장합니다.
        unitPortrait = data.playerturnSprite;

        InitializeActionValue();

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    /// <summary>
    /// 스폰되거나 턴을 소모했을 때 자신의 행동 수치를 (10000 / 속도)로 리셋합니다.
    /// </summary>
    public void InitializeActionValue()
    {
        if (speed <= 0) speed = 1f; // 0으로 나누기 방지
        currentActionValue = 10000f / speed;
    }

    public bool TakeDamage(int damage)
    {
        hpSlider.gameObject.SetActive(true);
        currentHP -= damage;

        if (hpSlider != null) hpSlider.value = currentHP;

        if (currentHP <= 0)
        {
            currentHP = 0;
            return true;
        }
        return false;
    }
}
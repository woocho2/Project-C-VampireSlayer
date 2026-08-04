using UnityEngine;
using UnityEngine.UI;

public class UnitController : MonoBehaviour
{
    [Header("유닛 정보")]
    public string unitName;
    public int maxHP;
    public int currentHP;

    [Header("UI 연결 및 설정")]
    // Screen Space - Overlay 캔버스에 있는 HP Bar 슬라이더를 연결합니다.
    public Slider hpSlider;

    private Camera mainCamera;

    private void Awake()
    {
        // 씬의 메인 카메라를 찾아 캐싱합니다.
        if (Camera.main != null)
        {
            mainCamera = Camera.main;
        }

        hpSlider.gameObject.SetActive(false);
    }


    /// <summary>
    /// [몬스터용] 데이터 셋업 함수
    /// </summary>
    public void Setup(EnemyDataSO data)
    {
        unitName = data.monsterName;
        maxHP = data.maxHealth;
        currentHP = maxHP; // 몬스터는 매번 새로 스폰되므로 최대 체력으로 시작합니다.

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    /// <summary>
    /// [플레이어용] 데이터 셋업 함수 (오버로딩)
    /// </summary>
    public void Setup(PlayerDataSO data)
    {
        unitName = data.playerName;
        maxHP = data.maxHealth;

        // 플레이어는 이전 전투(또는 필드)에서 소모된 체력을 그대로 이어받아야 합니다.
        currentHP = data.currentHealth;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    /// <summary>
    /// 데미지 처리 및 UI 갱신 함수
    /// </summary>
    public bool TakeDamage(int damage)
    {
        hpSlider.gameObject.SetActive(true);

        currentHP -= damage;

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            return true; // 사망 시 true 반환
        }

        return false;
    }
}
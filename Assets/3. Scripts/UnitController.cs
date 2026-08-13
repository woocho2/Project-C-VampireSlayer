using UnityEngine;

public class UnitController : MonoBehaviour
{
    // ==========================================
    // [변수 선언부]
    // ==========================================
    [Header("유닛 정보")]
    public string unitName;
    public int maxHP;
    public int currentHP;
    public int defense;
    public int maxAP;
    public int currentAP;
    public int power;
    public bool isPlayer;

    [Header("시각 모델 생성용")]
    private GameObject spawnedVisualModel;

    [Header("위치 정보")]
    public Vector3 originalPosition;

    [Header("턴 및 렌더링 정보")]
    public Sprite unitPortrait;
    public float speed = 100f;
    public float currentActionValue;

    [Header("UI 연동")]
    private PlayerStatusUI myStatusUI;

    // ==========================================
    // [1] 데이터 셋업 및 초기화
    // ==========================================
    public void Setup(PlayerDataSO data)
    {
        unitName = data.playerName;
        maxHP = data.maxHealth;
        currentHP = data.currentHealth;
        defense = data.defense;
        maxAP = data.maxAP;
        currentAP = data.currentAP;
        power = data.power;
        speed = data.baseSpeed;
        isPlayer = true;

        originalPosition = transform.position;
        unitPortrait = data.playerturnSprite;

        InitializeActionValue();
        SpawnVisualModel(data.playerPrefab);
    }

    public void Setup(EnemyDataSO data)
    {
        unitName = data.monsterName;
        maxHP = data.maxHealth;
        currentHP = maxHP;
        defense = 0;
        maxAP = 0;
        currentAP = 0;
        power = data.power;
        speed = data.baseSpeed;
        isPlayer = false;

        originalPosition = transform.position;
        unitPortrait = data.monsterturnSprite;

        InitializeActionValue();
        SpawnVisualModel(data.monsterPrefab);

        // ==========================================
        // [추가] 적 데이터 셋업 시 중앙 체력바 UI 초기화
        // ==========================================
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.SetupEnemyHPBar(maxHP, currentHP);
        }
    }

    /// <summary>
    /// 빈 깡통 위치에 실제 캐릭터 모델을 생성하여 부착하는 보조 함수
    /// </summary>
    private void SpawnVisualModel(GameObject prefab)
    {
        if (spawnedVisualModel != null) Destroy(spawnedVisualModel);

        if (prefab != null)
        {
            spawnedVisualModel = Instantiate(prefab, transform);
            spawnedVisualModel.transform.localPosition = Vector3.zero;
            spawnedVisualModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError($"{unitName}의 데이터에 3D 모델 프리팹이 비어있습니다.");
        }
    }

    public void LinkUI(PlayerStatusUI ui)
    {
        myStatusUI = ui;
        if (myStatusUI != null)
        {
            myStatusUI.SetupUI(unitPortrait, maxHP, currentHP, maxAP, currentAP, unitName);
        }
    }

    public void InitializeActionValue()
    {
        if (speed <= 0) speed = 1f;
        currentActionValue = 10000f / speed;
    }

    // ==========================================
    // [2] 전투 상태 변화 처리 (피격, 스킬 소모)
    // ==========================================
    public bool TakeDamage(int damage)
    {
        int finalDamage = damage;

        if (isPlayer)
        {
            float damageReduction = 100f / (defense + 100f);
            finalDamage = Mathf.RoundToInt(damage * damageReduction);
            if (finalDamage <= 0) finalDamage = 1;
        }

        currentHP -= finalDamage;
        Debug.Log($"{unitName}이(가) {finalDamage}의 데미지를 입었습니다. (남은 체력: {currentHP})");

        // 플레이어일 경우 개별 상태창 UI 업데이트
        if (myStatusUI != null) myStatusUI.UpdateHP(currentHP);

        // ==========================================
        // [추가] 적일 경우 중앙 체력바 UI 업데이트 및 숨김 처리
        // ==========================================
        if (!isPlayer && BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.UpdateEnemyHPBar(currentHP);

            if (currentHP <= 0)
            {
                BattleUIManager.Instance.HideEnemyHPBar();
            }
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            return true;
        }
        return false;
    }

    public bool ConsumeAP(int amount)
    {
        if (currentAP >= amount)
        {
            currentAP -= amount;
            if (myStatusUI != null) myStatusUI.UpdateAP(currentAP);
            return true;
        }

        Debug.Log($"{unitName}의 행동력(AP)이 부족합니다.");
        return false;
    }
}
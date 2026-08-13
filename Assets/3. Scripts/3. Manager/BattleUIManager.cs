using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    [Header("전투 시작 패널 UI")]
    public GameObject IntroPanel;
    public TextMeshProUGUI[] warningTexts;
    public TextMeshProUGUI monsterNameText;

    [Header("연출 설정")]
    public float blinkSpeed = 5f;

    [Header("전투 정보 패널 UI")]
    [SerializeField] GameObject TurnPanel;
    [SerializeField] GameObject GamePanel;
    public PlayerStatusUI[] playerStatusUIs;

    [Header("적 체력바 UI")]
    [SerializeField] Slider EnemyHPBar;

    [Header("액션 버튼")]
    public Button btn_attack;
    public Button btn_skill;
    public Button btn_evasion;
    public Button btn_parrying;

    [Header("턴 오더 UI 설정")]
    public GameObject turnSlotPrefab;
    public Transform turnSlotParent;

    [Header("전투 알림 UI")]
    public GameObject actionNotificationPanel;
    public TextMeshProUGUI actionNotificationText;

    private readonly float[] alphaLevels = { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
    private List<Image> spawnedPortraitSlots = new List<Image>();

    // ==========================================
    // [1] 초기화
    // ==========================================
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeTurnSlots();

        if (GamePanel != null) GamePanel.SetActive(false);
        if (TurnPanel != null) TurnPanel.SetActive(false);
        if (actionNotificationPanel != null) actionNotificationPanel.SetActive(false);
        if (EnemyHPBar != null) EnemyHPBar.gameObject.SetActive(false); // 초기 시작 시 적 체력바 숨김
    }

    private void InitializeTurnSlots()
    {
        if (turnSlotPrefab == null || turnSlotParent == null)
        {
            Debug.LogError("턴 슬롯 프리팹 또는 부모가 할당되지 않았습니다.");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject slotGO = Instantiate(turnSlotPrefab, turnSlotParent);
            Image portraitImage = slotGO.transform.GetChild(0).GetComponent<Image>();

            if (portraitImage != null)
            {
                spawnedPortraitSlots.Add(portraitImage);
                slotGO.SetActive(false); // 초기 생성 시 숨김 처리
            }
        }
    }

    // ==========================================
    // [2] 인트로 연출
    // ==========================================
    public void PlayBattleStartUI(string enemyName)
    {
        IntroPanel.SetActive(true);
        monsterNameText.gameObject.SetActive(false);
        if (GamePanel != null) GamePanel.SetActive(false);

        float duration = 1f / blinkSpeed;
        foreach (var text in warningTexts)
        {
            if (text != null)
            {
                text.DOKill();
                text.DOFade(0f, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }
    }

    public void MonsterNameEffect()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.EnemyUnits.Count > 0)
        {
            string enemyName = BattleManager.Instance.EnemyUnits[0].unitName;

            if (monsterNameText != null)
            {
                monsterNameText.gameObject.SetActive(true);
                monsterNameText.text = enemyName;
                monsterNameText.DOKill();

                StopAllCoroutines();
                StartCoroutine(TypeTextPunchRoutine(enemyName));
            }
        }
    }

    private IEnumerator TypeTextPunchRoutine(string textToPrint)
    {
        monsterNameText.text = textToPrint;
        monsterNameText.maxVisibleCharacters = 0;

        int totalCharacters = textToPrint.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            monsterNameText.maxVisibleCharacters = i;

            if (i > 0)
            {
                monsterNameText.transform.localScale = Vector3.one * 2.0f;
                monsterNameText.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
                monsterNameText.transform.DOShakePosition(0.15f, 5f, 15, 90f);
            }
            yield return new WaitForSeconds(0.08f);
        }
    }

    public void HideBattleStartUI()
    {
        foreach (var text in warningTexts)
        {
            if (text != null) text.DOKill();
        }

        if (monsterNameText != null) monsterNameText.DOKill();
        IntroPanel.SetActive(false);
        if (GamePanel != null) GamePanel.SetActive(true);
    }

    // ==========================================
    // [3] 메인 루프 UI 제어 및 턴 버튼 스위칭
    // ==========================================
    public void SwitchTurnButtons(bool isPlayerTurn)
    {
        if (btn_attack != null) btn_attack.gameObject.SetActive(isPlayerTurn);
        if (btn_skill != null) btn_skill.gameObject.SetActive(isPlayerTurn);

        if (btn_evasion != null) btn_evasion.gameObject.SetActive(!isPlayerTurn);
        if (btn_parrying != null) btn_parrying.gameObject.SetActive(!isPlayerTurn);
    }

    public void ShowPlayerActionUI(bool isOpen, bool isPlayerTurn = true)
    {
        if (TurnPanel != null)
        {
            RectTransform panelRect = TurnPanel.GetComponent<RectTransform>();
            panelRect.DOKill();

            if (isOpen)
            {
                SwitchTurnButtons(isPlayerTurn);

                TurnPanel.SetActive(true);
                panelRect.localScale = new Vector3(0f, 1f, 1f);
                panelRect.DOScaleX(1f, 0.3f).SetEase(Ease.OutBack);
            }
            else
            {
                panelRect.DOScaleX(0f, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    TurnPanel.SetActive(false);
                });
            }
        }
    }

    public void UpdateTurnOrderUI(List<Sprite> turnSprites)
    {
        if (spawnedPortraitSlots.Count == 0) return;

        for (int i = 0; i < spawnedPortraitSlots.Count; i++)
        {
            if (i < turnSprites.Count && turnSprites[i] != null)
            {
                spawnedPortraitSlots[i].transform.parent.gameObject.SetActive(true);
                spawnedPortraitSlots[i].sprite = turnSprites[i];

                Color slotColor = spawnedPortraitSlots[i].color;
                slotColor.a = alphaLevels[i];
                spawnedPortraitSlots[i].color = slotColor;
            }
            else
            {
                spawnedPortraitSlots[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void ShowActionNotification(string message)
    {
        if (actionNotificationPanel != null && actionNotificationText != null)
        {
            actionNotificationPanel.SetActive(true);
            actionNotificationText.text = message;

            actionNotificationText.transform.DOKill();
            actionNotificationText.transform.localScale = Vector3.one * 0.5f;
            actionNotificationText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    public void HideActionNotification()
    {
        if (actionNotificationPanel != null)
        {
            actionNotificationPanel.SetActive(false);
        }
    }

    // ==========================================
    // [4] 적 체력바 연동 함수
    // ==========================================

    /// <summary>
    /// 적이 처음 스폰되거나 타겟이 변경되었을 때 체력바의 최대치와 현재 수치를 초기화합니다.
    /// </summary>
    public void SetupEnemyHPBar(float maxHP, float currentHP)
    {
        if (EnemyHPBar != null)
        {
            EnemyHPBar.gameObject.SetActive(true);
            EnemyHPBar.maxValue = maxHP;
            EnemyHPBar.value = currentHP;
        }
    }

    /// <summary>
    /// 적이 데미지를 입었을 때 슬라이더 수치를 부드럽게 깎아줍니다.
    /// </summary>
    public void UpdateEnemyHPBar(float currentHP)
    {
        if (EnemyHPBar != null && EnemyHPBar.gameObject.activeInHierarchy)
        {
            // DOTween을 활용해 0.3초 동안 체력 슬라이더가 깎이는 타격감 연출
            EnemyHPBar.DOValue(currentHP, 0.3f).SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// 적이 죽었거나 전투가 끝났을 때 체력바를 숨깁니다.
    /// </summary>
    public void HideEnemyHPBar()
    {
        if (EnemyHPBar != null)
        {
            EnemyHPBar.gameObject.SetActive(false);
        }
    }
}
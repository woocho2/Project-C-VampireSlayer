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
    public Button btn_attack;
    public Button btn_skill;

    [Header("턴 오더 UI 설정")]
    public GameObject turnSlotPrefab;
    public Transform turnSlotParent;

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
                slotGO.SetActive(true);
            }
        }
    }

    // ==========================================
    // [2] 인트로 연출 (세트)
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
    // [3] 메인 루프 UI 제어
    // ==========================================
    public void ShowPlayerActionUI(bool isOpen)
    {
        if (TurnPanel != null) TurnPanel.SetActive(isOpen);
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
}
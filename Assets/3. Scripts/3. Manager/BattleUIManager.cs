using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // DOTween 사용을 위한 네임스페이스 추가

public class BattleUIManager : MonoBehaviour
{
    // 싱글톤 패턴 적용: 씬 어디서든 'BattleUIManager.Instance'로 쉽게 접근할 수 있게 합니다.
    public static BattleUIManager Instance { get; private set; }

    [Header("전투 시작 패널 UI")]
    public GameObject battleStartPanel; // 전투 시작 시 경고 문구 등을 띄우는 전체 패널
    public TextMeshProUGUI[] warningTexts; // 깜빡이는 경고 텍스트들
    public TextMeshProUGUI monsterNameText; // 만난 몬스터의 이름이 표시되는 텍스트

    [Header("연출 설정")]
    public float blinkSpeed = 5f; // 텍스트가 깜빡이는 속도

    [Header("전투 정보 패널 UI")]
    [SerializeField] GameObject battleInfoPanel; // 플레이어의 행동 선택 UI 패널
    public Button btn_attack; // 공격 버튼
    public Button btn_skill; // 스킬 버튼
    [SerializeField] private CanvasGroup battleInfoCanvasGroup; // UI의 투명도와 상호작용 여부를 제어하기 위한 CanvasGroup 컴포넌트

    public float fadeDuration = 0.3f; // UI가 서서히 나타나거나 사라지는(페이드) 시간

    // ==========================================
    // 턴 오더 UI - 프리팹 동적 생성 방식
    // ==========================================
    [Header("턴 오더 UI 설정")]
    public GameObject turnSlotPrefab;     // 마스크와 이미지가 세팅된 턴 슬롯 프리팹 1개
    public Transform turnSlotParent;      // Vertical Layout Group(세로 정렬)이 부착된 빈 부모 오브젝트

    // 위에서부터 아래로 적용될 알파(투명도) 값을 배열로 고정해 둠 (가까운 턴일수록 진하게, 멀수록 연하게)
    private readonly float[] alphaLevels = { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };

    // 코드로 찍어낸 5개의 프리팹에서 '실제 초상화 Image 컴포넌트'만 빼서 보관할 리스트
    private List<Image> spawnedPortraitSlots = new List<Image>();

    private void Awake()
    {
        // 싱글톤 초기화: 이미 존재하지 않으면 자신을 등록하고, 이미 있으면 파괴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 씬이 시작되면 프리팹을 이용해 5개의 빈 턴 슬롯을 미리 생성(풀링 개념)해 둠
        InitializeTurnSlots();

        // 전투 정보 패널을 처음에는 투명하게 만들고 클릭되지 않도록 설정
        if (battleInfoCanvasGroup != null)
        {
            battleInfoCanvasGroup.alpha = 0f;
            battleInfoCanvasGroup.interactable = false;
            battleInfoCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 프리팹을 복제하여 5개의 턴 슬롯을 생성하는 함수입니다.
    /// </summary>
    private void InitializeTurnSlots()
    {
        if (turnSlotPrefab == null || turnSlotParent == null)
        {
            Debug.LogError("턴 슬롯 프리팹 또는 부모(Vertical Layout Group)가 할당되지 않았습니다.");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject slotGO = Instantiate(turnSlotPrefab, turnSlotParent);
            Image portraitImage = slotGO.transform.GetChild(0).GetComponent<Image>();

            if (portraitImage != null)
            {
                spawnedPortraitSlots.Add(portraitImage);
                slotGO.SetActive(false);
            }
        }
    }

    /// <summary>
    /// [수정됨] 타임라인 시그널에서 호출되어 패널을 켜고 DOTween 깜빡임을 시작하는 함수
    /// </summary>
    public void PlayBattleStartUI(string enemyName)
    {
        battleStartPanel.SetActive(true);

        monsterNameText.gameObject.SetActive(false);

        // 깜빡임 1회에 걸리는 시간 계산 (기존 blinkSpeed와 유사한 느낌 도출)
        float duration = 1f / blinkSpeed;

        // 경고 텍스트들을 DOTween을 이용해 무한히 깜빡이게(Yoyo) 만듭니다.
        foreach (var text in warningTexts)
        {
            if (text != null)
            {
                // 이전 트윈이 남아있다면 끄고, 알파값을 0에서 1로 왕복하는 애니메이션 실행
                text.DOKill();
                text.DOFade(0f, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }
    }

    /// <summary>
    /// [추가됨] 타임라인 종료 시그널에서 호출되어 깜빡임을 멈추고 패널을 끄는 함수
    /// </summary>
    public void HideBattleStartUI()
    {
        // 텍스트에 적용되어 무한 반복 중이던 DOTween 애니메이션을 강제로 제거합니다.
        foreach (var text in warningTexts)
        {
            if (text != null) text.DOKill();
        }

        if (monsterNameText != null) monsterNameText.DOKill();

        // UI 패널을 화면에서 숨깁니다.
        battleStartPanel.SetActive(false);
    }

    public void MonsterNameEffect()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.EnemyUnitScript != null)
        {
            string enemyName = BattleManager.Instance.EnemyUnitScript.unitName;

            if (monsterNameText != null)
            {
                monsterNameText.gameObject.SetActive(true);

                monsterNameText.text = enemyName;

                // 1. 기존 트윈 애니메이션 초기화
                monsterNameText.DOKill();

                // 글자 하나씩 타이핑되면서 박히는 연출 코루틴 실행
                StopAllCoroutines();
                StartCoroutine(TypeTextPunchRoutine(enemyName));
            }
        }
    }

    /// <summary>
    /// 텍스트가 한 글자씩 나타나며 타격감을 주는 코루틴
    /// </summary>
    private IEnumerator TypeTextPunchRoutine(string textToPrint)
    {
        monsterNameText.text = textToPrint;
        monsterNameText.maxVisibleCharacters = 0; // 처음에 글자를 숨김

        int totalCharacters = textToPrint.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            monsterNameText.maxVisibleCharacters = i; // 글자를 하나씩 늘려감

            if (i > 0)
            {
                // 글자가 한 글자씩 찍힐 때마다 쾅! 커졌다가 돌아오는 스케일 연출
                monsterNameText.transform.localScale = Vector3.one * 2.0f;
                monsterNameText.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

                // 미세한 흔들림 
                monsterNameText.transform.DOShakePosition(0.15f, 5f, 15, 90f);
            }

            // 글자가 나타나는 간격 (속도 조절 가능)
            yield return new WaitForSeconds(0.08f);
        }
    }

    // 플레이어 행동 UI(공격/스킬 버튼 등)를 켜거나 끌 때 호출하는 함수
    public void ShowPlayerActionUI(bool isOpen)
    {
        if (battleInfoCanvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeUIRoutine(isOpen));
        }
    }

    // CanvasGroup의 알파값을 서서히 조절하여 페이드 인/아웃 효과를 내는 코루틴
    private IEnumerator FadeUIRoutine(bool isOpen)
    {
        float startAlpha = battleInfoCanvasGroup.alpha;
        float targetAlpha = isOpen ? 1f : 0f;
        float elapsed = 0f;

        if (isOpen)
        {
            battleInfoCanvasGroup.interactable = true;
            battleInfoCanvasGroup.blocksRaycasts = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            battleInfoCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        battleInfoCanvasGroup.alpha = targetAlpha;

        if (!isOpen)
        {
            battleInfoCanvasGroup.interactable = false;
            battleInfoCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// BattleManager에서 턴 순서가 계산될 때마다 호출하여 초상화와 투명도를 갱신합니다.
    /// </summary>
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
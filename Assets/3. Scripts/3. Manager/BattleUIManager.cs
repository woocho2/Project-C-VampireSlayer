using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public float panelDisplayTime = 3f; // 전투 시작 패널이 유지되는 시간

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
            // 부모 아래에 프리팹을 1개 생성합니다.
            GameObject slotGO = Instantiate(turnSlotPrefab, turnSlotParent);

            // 마스크가 있는 프리팹의 경우 최상단은 Mask(Image)이고, 자식이 실제 사진(Image)임
            // slotGO.transform.GetChild(0)을 사용하여 자식 오브젝트의 Image 컴포넌트를 가져옴
            Image portraitImage = slotGO.transform.GetChild(0).GetComponent<Image>();

            if (portraitImage != null)
            {
                spawnedPortraitSlots.Add(portraitImage);
                slotGO.SetActive(false); // 처음에는 안 보이게 꺼둡니다.
            }
            else
            {
                Debug.LogError("프리팹의 첫 번째 자식 오브젝트에 Image 컴포넌트가 없습니다!");
            }
        }
    }

    // 전투 시작 시 패널을 켜고 몬스터 이름을 세팅한 뒤 연출 코루틴을 실행하는 함수
    public void PlayBattleStartUI(string enemyName)
    {
        battleStartPanel.SetActive(true);
        monsterNameText.text = enemyName;
        StartCoroutine(BattleStartSequence());
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

        // 열릴 때는 상호작용 가능하게 변경
        if (isOpen)
        {
            battleInfoCanvasGroup.interactable = true;
            battleInfoCanvasGroup.blocksRaycasts = true;
        }

        // 지정된 시간 동안 알파값 선형 보간
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            battleInfoCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        battleInfoCanvasGroup.alpha = targetAlpha;

        // 닫힐 때는 상호작용 차단
        if (!isOpen)
        {
            battleInfoCanvasGroup.interactable = false;
            battleInfoCanvasGroup.blocksRaycasts = false;
        }
    }

    // 전투 시작 패널의 텍스트들을 깜빡거리게 만들고 시간이 지나면 꺼주는 코루틴
    private IEnumerator BattleStartSequence()
    {
        float timer = 0f;

        while (timer < panelDisplayTime)
        {
            timer += Time.deltaTime;
            // Mathf.PingPong을 이용해 알파값이 0과 1 사이를 왕복하도록 계산 (깜빡임 효과)
            float alphaValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            foreach (var text in warningTexts)
            {
                if (text != null)
                {
                    Color c = text.color;
                    c.a = alphaValue;
                    text.color = c;
                }
            }

            if (monsterNameText != null)
            {
                Color c = monsterNameText.color;
                c.a = alphaValue;
                monsterNameText.color = c;
            }

            yield return null;
        }

        battleStartPanel.SetActive(false); // 시간이 다 되면 패널 끔
    }

    /// <summary>
    /// BattleManager에서 턴 순서가 계산될 때마다 호출하여 초상화와 투명도를 갱신합니다.
    /// </summary>
    public void UpdateTurnOrderUI(List<Sprite> turnSprites)
    {
        if (spawnedPortraitSlots.Count == 0) return;

        for (int i = 0; i < spawnedPortraitSlots.Count; i++)
        {
            // 가져온 턴 데이터 개수만큼 슬롯을 채움
            if (i < turnSprites.Count && turnSprites[i] != null)
            {
                // 생성된 프리팹 전체(부모)를 활성화합니다.
                spawnedPortraitSlots[i].transform.parent.gameObject.SetActive(true);

                // 스프라이트 사진을 덮어씌웁니다.
                spawnedPortraitSlots[i].sprite = turnSprites[i];

                // 미리 고정해둔 배열에서 알파값을 가져와 적용합니다. (순서에 따라 투명도 조절)
                Color slotColor = spawnedPortraitSlots[i].color;
                slotColor.a = alphaLevels[i];
                spawnedPortraitSlots[i].color = slotColor;
            }
            else
            {
                // 데이터가 비어있다면 프리팹(부모) 전체를 화면에서 숨깁니다.
                spawnedPortraitSlots[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager_Battle : MonoBehaviour
{
    public static UIManager_Battle Instance { get; private set; }

    [Header("전투 시작 패널 UI")]
    public GameObject battleStartPanel;
    public TextMeshProUGUI[] warningTexts;
    public TextMeshProUGUI monsterNameText;

    [Header("연출 설정")]
    public float blinkSpeed = 5f;
    public float panelDisplayTime = 3f;

    // ==========================================
    // [수정] 턴 오더 UI - 프리팹 동적 생성 방식
    // ==========================================
    [Header("턴 오더 UI 설정")]
    public GameObject turnSlotPrefab;     // 마스크와 이미지가 세팅된 턴 슬롯 프리팹 1개
    public Transform turnSlotParent;      // Vertical Layout Group이 부착된 빈 부모 오브젝트

    // 위에서부터 아래로 적용될 알파(투명도) 값을 배열로 고정해 둡니다.
    private readonly float[] alphaLevels = { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };

    // 코드로 찍어낸 5개의 프리팹에서 '실제 초상화 Image 컴포넌트'만 빼서 보관할 리스트입니다.
    private List<Image> spawnedPortraitSlots = new List<Image>();

    private void Awake()
    {
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
        // 씬이 시작되면 프리팹을 이용해 5개의 빈 턴 슬롯을 미리 생성(풀링)해 둡니다.
        InitializeTurnSlots();
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

            // [매우 중요] 마스크가 있는 프리팹의 경우 최상단은 Mask(Image)이고, 자식이 실제 사진(Image)입니다.
            // slotGO.transform.GetChild(0)을 사용하여 자식 오브젝트의 Image 컴포넌트를 가져옵니다.
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

    public void PlayBattleStartUI(string enemyName)
    {
        battleStartPanel.SetActive(true);
        monsterNameText.text = enemyName;
        StartCoroutine(BattleStartSequence());
    }

    private IEnumerator BattleStartSequence()
    {
        float timer = 0f;

        while (timer < panelDisplayTime)
        {
            timer += Time.deltaTime;
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

        battleStartPanel.SetActive(false);
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
                // 생성된 프리팹 전체(부모)를 활성화합니다.
                spawnedPortraitSlots[i].transform.parent.gameObject.SetActive(true);

                // 스프라이트 사진을 덮어씌웁니다.
                spawnedPortraitSlots[i].sprite = turnSprites[i];

                // 고정해둔 배열에서 알파값을 가져와 적용합니다.
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
using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위한 네임스페이스

public class UIManager_Battle : MonoBehaviour
{
    // BattleManager 등 외부에서 쉽게 접근할 수 있도록 싱글톤 패턴 적용
    public static UIManager_Battle Instance { get; private set; }

    [Header("전투 시작 패널 UI")]
    public GameObject battleStartPanel;         // 화면을 덮는 전체 패널 오브젝트
    public TextMeshProUGUI[] warningTexts;      // 반짝거릴 Warning 텍스트들 (배열로 선언하여 여러 개 동시 제어)
    public TextMeshProUGUI monsterNameText;     // 하단에 몬스터 이름이 찍힐 텍스트

    [Header("연출 설정")]
    public float blinkSpeed = 5f;               // 글씨가 깜빡이는 속도
    public float panelDisplayTime = 3f;         // 패널이 화면에 떠 있는 총 시간

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// BattleManager가 전투 씬 진입 직후 몬스터 이름을 넘겨주며 호출할 함수입니다.
    /// </summary>
    public void PlayBattleStartUI(string enemyName)
    {
        // 1. 패널을 활성화하고 몬스터 이름을 텍스트에 박아넣습니다.
        battleStartPanel.SetActive(true);
        monsterNameText.text = enemyName;

        // 2. 깜빡임 효과 및 자동 종료 코루틴을 시작합니다.
        StartCoroutine(BattleStartSequence());
    }

    private IEnumerator BattleStartSequence()
    {
        float timer = 0f;

        // panelDisplayTime(예: 3초) 동안 반복하며 텍스트를 깜빡이게 합니다.
        while (timer < panelDisplayTime)
        {
            timer += Time.deltaTime;

            // Mathf.PingPong을 이용하여 0 ~ 1 사이의 값을 부드럽게 왕복시킵니다.
            float alphaValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            // Warning 텍스트 배열을 순회하며 알파(투명도) 값을 실시간으로 갱신합니다.
            foreach (var text in warningTexts)
            {
                if (text != null)
                {
                    Color c = text.color;
                    c.a = alphaValue;
                    text.color = c;
                }
            }

            // 몬스터 이름 텍스트도 같이 깜빡이게 적용합니다.
            if (monsterNameText != null)
            {
                Color c = monsterNameText.color;
                c.a = alphaValue;
                monsterNameText.color = c;
            }

            yield return null; // 다음 프레임까지 대기
        }

        // 연출 시간이 모두 끝나면 패널을 다시 비활성화하여 화면에서 지웁니다.
        battleStartPanel.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.UI;

public class FieldUIManager : MonoBehaviour
{
    // 외부에서 쉽게 접근할 수 있도록 싱글톤 인스턴스 생성
    public static FieldUIManager Instance { get; private set; }

    [SerializeField] GameObject m_gameInfoPanel;
    [SerializeField] Image m_DavidMask;
    [SerializeField] Image m_TeresMask;

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

    void Start()
    {
        // 게임 시작 시 정보 패널을 활성화
        if (m_gameInfoPanel != null)
        {
            m_gameInfoPanel.SetActive(true);
        }
    }

    /// <summary>
    /// PartyManager에서 호출하여 현재 조종 중인 캐릭터의 UI만 활성화합니다.
    /// </summary>
    public void UpdatePartyUI(int activeIndex)
    {
        // 1. 우선 모든 캐릭터의 마스크(초상화) UI를 끕니다.
        if (m_DavidMask != null) m_DavidMask.gameObject.SetActive(false);
        if (m_TeresMask != null) m_TeresMask.gameObject.SetActive(false);

        // 2. 인덱스에 맞춰 해당하는 UI만 다시 켭니다.
        // (0번이 David, 1번이 Teres라고 가정합니다. 기획에 맞춰 인덱스는 수정 가능합니다.)
        if (activeIndex == 0)
        {
            if (m_DavidMask != null) m_DavidMask.gameObject.SetActive(true);
        }
        else if (activeIndex == 1)
        {
            if (m_TeresMask != null) m_TeresMask.gameObject.SetActive(true);
        }
    }
}
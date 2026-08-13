using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 하단에 배치된 개별 캐릭터의 상태창 UI를 제어하는 클래스입니다.
public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public Image portraitImage; // 캐릭터 얼굴 이미지
    public Slider hpSlider;     // 붉은색 체력바
    public Slider apSlider;     // 노란색 AP바
    public TextMeshProUGUI txt_Name;

    // ==========================================
    // [1] UI 초기화
    // ==========================================
    /// <summary>
    /// 캐릭터가 씬에 생성될 때 최초 1회 호출되어 UI의 기본값을 세팅합니다.
    /// </summary>
    public void SetupUI(Sprite portrait, int maxHP, int currentHP, int maxAP, int currentAP, string name)
    {
        if (portraitImage != null) portraitImage.sprite = portrait;

        if (txt_Name != null) txt_Name.text = name;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (apSlider != null)
        {
            apSlider.maxValue = maxAP;
            apSlider.value = currentAP;
        }

        gameObject.SetActive(true);
    }

    // ==========================================
    // [2] 상태 갱신
    // ==========================================
    public void UpdateHP(int currentHP)
    {
        if (hpSlider != null) hpSlider.value = currentHP;
    }

    public void UpdateAP(int currentAP)
    {
        if (apSlider != null) apSlider.value = currentAP;
    }
}
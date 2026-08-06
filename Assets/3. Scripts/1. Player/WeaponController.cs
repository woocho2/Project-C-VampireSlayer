using UnityEngine;

// 캐릭터의 무기(검 등) 장착 상태를 관리하고 칼을 뽑거나 집어넣는 애니메이션 이벤트를 처리하는 클래스입니다[cite: 15].
public class WeaponController : MonoBehaviour
{
    [Header("가짜 무기(Prop) 할당")]
    // 주의: Skinned Mesh 원본이 아닌, 새롭게 배치한 정적 메쉬 오브젝트를 할당해야 합니다[cite: 15].
    [SerializeField] private GameObject handSword;     // 캐릭터 손 뼈대에 미리 부착되어 있는 가짜 검 오브젝트[cite: 15]
    [SerializeField] private GameObject scabbardSword; // 캐릭터의 칼집(등이나 허리 뼈대)에 부착되어 있는 가짜 검 오브젝트[cite: 15]

    private void Awake()
    {
        // 현재 이 스크립트에서는 초기화 로직이 비어있습니다[cite: 15].
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출할 발도(칼 뽑기) 함수[cite: 15]
    /// </summary>
    public void DrawSword()
    {
        // 손에 든 검과 칼집에 있는 검 오브젝트가 인스펙터에 모두 정상적으로 연결되었는지 확인합니다[cite: 15].
        if (handSword != null && scabbardSword != null)
        {
            // 칼집에 꽂혀 있던 검은 안 보이게 끄고, 손에 쥐여진 검을 보이도록 켭니다[cite: 15].
            scabbardSword.SetActive(false);
            handSword.SetActive(true);
        }
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출할 납도(칼 집어넣기) 함수[cite: 15]
    /// </summary>
    public void PutSword()
    {
        // 손에 든 검과 칼집에 있는 검 오브젝트가 모두 유효한지 확인합니다[cite: 15].
        if (handSword != null && scabbardSword != null)
        {
            // 손에 들고 있던 검은 안 보이게 끄고, 칼집에 있는 검을 다시 보이도록 켭니다[cite: 15].
            handSword.SetActive(false);
            scabbardSword.SetActive(true);
        }
    }
}
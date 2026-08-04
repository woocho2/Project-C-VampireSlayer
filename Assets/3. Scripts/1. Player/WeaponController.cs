using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("가짜 무기(Prop) 할당")]
    // 주의: Skinned Mesh 원본이 아닌, 새롭게 배치한 정적 메쉬 오브젝트를 할당해야 합니다.
    [SerializeField] private GameObject handSword;     // 손 뼈대에 부착된 가짜 검
    [SerializeField] private GameObject scabbardSword; // 칼집(등/허리) 뼈대에 부착된 가짜 검

    /// <summary>
    /// 애니메이션 이벤트에서 호출할 발도(칼 뽑기) 함수
    /// </summary>
    private void Awake()
    {
    }

    public void DrawSword()
    {
        // 두 오브젝트가 모두 Inspector에 정상적으로 할당되었는지 안전 검사
        if (handSword != null && scabbardSword != null)
        {
            // 칼집에 있는 검을 안 보이게 숨기고, 손에 있는 검을 렌더링합니다.
            scabbardSword.SetActive(false);
            handSword.SetActive(true);
        }
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출할 납도(칼 집어넣기) 함수
    /// </summary>
    public void PutSword()
    {
        if (handSword != null && scabbardSword != null)
        {
            // 손에 있는 검을 숨기고, 칼집에 있는 검을 다시 렌더링합니다.
            handSword.SetActive(false);
            scabbardSword.SetActive(true);
        }
    }
}
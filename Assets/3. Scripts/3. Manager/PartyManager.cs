using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

// 부모 빈 오브젝트(컨트롤러 집합)에 부착되어 하위 자식 프리팹들의 교체를 관리합니다.
public class PartyManager : MonoBehaviour
{
    [System.Serializable]
    public class PartyMember
    {
        public PlayerDataSO data;
        public GameObject visualInstance; // 하위에 생성될 시각적 자식 프리팹
    }

    [Header("컴포넌트 참조")]
    private PlayerMoveController m_moveController;
    private PlayerCombatController m_combatController;
    private PlayerAniController m_aniController;

    [Header("카메라 연동")]
    public CinemachineCamera freeLookCamera;

    [Header("디버그용 (인스펙터 확인용)")]
    public List<PartyMember> spawnedParty = new List<PartyMember>();

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.partyMembers.Length == 0)
        {
            Debug.LogError("GameManager에 파티원 데이터가 없습니다.");
            return;
        }

        // 부모 오브젝트에 부착된 컨트롤러들을 캐싱합니다.
        m_moveController = GetComponent<PlayerMoveController>();
        m_combatController = GetComponent<PlayerCombatController>();
        m_aniController = GetComponent<PlayerAniController>();

        PlayerDataSO[] partyDataArray = GameManager.Instance.partyMembers;

        for (int i = 0; i < partyDataArray.Length; i++)
        {
            if (partyDataArray[i] == null) continue;

            // 1. 자식 프리팹을 부모 오브젝트(this.transform) 아래에 종속시켜 생성합니다.
            GameObject visualChild = Instantiate(partyDataArray[i].fieldPlayerPrefab, transform);

            // 2. 자식의 위치와 회전값을 부모의 정중앙(0,0,0)으로 초기화합니다.
            visualChild.transform.localPosition = Vector3.zero;
            visualChild.transform.localRotation = Quaternion.identity;

            // 3. 현재 인덱스에 맞는 캐릭터만 활성화합니다.
            bool isActive = (i == GameManager.Instance.currentPartyIndex);
            visualChild.SetActive(isActive);

            spawnedParty.Add(new PartyMember { data = partyDataArray[i], visualInstance = visualChild });
        }

        // 초기 시작 시 첫 번째 캐릭터의 데이터와 애니메이터를 셋팅합니다.
        ApplyCharacterData(GameManager.Instance.currentPartyIndex);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // T 키를 누르면 다음 캐릭터로 조종 권한을 교체합니다.
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwapToNextCharacter();
        }
    }

    public void SwapToNextCharacter()
    {
        if (spawnedParty.Count <= 1) return;

        int currentIndex = GameManager.Instance.currentPartyIndex;

        // 이전 캐릭터의 시각 모델을 끕니다. 위치나 회전값을 저장할 필요가 없습니다.
        spawnedParty[currentIndex].visualInstance.SetActive(false);

        // 인덱스를 순환시켜 다음 캐릭터를 지정합니다.
        GameManager.Instance.currentPartyIndex = (currentIndex + 1) % spawnedParty.Count;
        int nextIndex = GameManager.Instance.currentPartyIndex;

        // 다음 캐릭터의 시각 모델을 켭니다.
        spawnedParty[nextIndex].visualInstance.SetActive(true);

        // 변경된 캐릭터에 맞춰 데이터와 애니메이터 바인딩을 일괄 갱신합니다.
        ApplyCharacterData(nextIndex);
    }

    /// <summary>
    /// 캐릭터 교체 시 부모 컨트롤러들에게 새 스탯을 주입하고 참조를 재설정하는 함수입니다.
    /// </summary>
    private void ApplyCharacterData(int index)
    {
        PartyMember activeMember = spawnedParty[index];

        // 1. 각 컨트롤러에 새로운 캐릭터 데이터(속도, 공격력 등)를 덮어씌웁니다.
        if (m_moveController != null) m_moveController.Setup(activeMember.data);
        if (m_combatController != null) m_combatController.Setup(activeMember.data);

        // 2. 새로 켜진 자식 프리팹의 Animator를 찾아 조종할 수 있도록 재연결합니다.
        if (m_aniController != null) m_aniController.BindAnimator();

        // 3. 카메라 추적 타겟을 설정합니다 (이제 부모 빈 오브젝트 자체를 추적합니다).
        if (freeLookCamera != null)
        {
            freeLookCamera.Target.TrackingTarget = transform;
        }

        // 4. 전역 매니저 및 UI 상태를 갱신합니다.
        GameManager.Instance.playerData = activeMember.data;

        if (FieldUIManager.Instance != null)
        {
            FieldUIManager.Instance.UpdatePartyUI(index);
        }
    }
}
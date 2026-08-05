using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PartyManager : MonoBehaviour
{
    [System.Serializable]
    public class PartyMember
    {
        public PlayerDataSO data;
        public GameObject instanceGO;
    }

    [Header("스폰 설정")]
    public Transform spawnPoint;

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

        PlayerDataSO[] partyDataArray = GameManager.Instance.partyMembers;

        for (int i = 0; i < partyDataArray.Length; i++)
        {
            if (partyDataArray[i] == null) continue;

            GameObject go = Instantiate(partyDataArray[i].fieldPlayerPrefab, spawnPoint.position, spawnPoint.rotation);

            PlayerMoveController moveController = go.GetComponent<PlayerMoveController>();
            if (moveController != null)
            {
                moveController.Setup(partyDataArray[i]);
            }

            PlayerCombatController combatController = go.GetComponent<PlayerCombatController>();
            if (combatController != null)
            {
                combatController.Setup(partyDataArray[i]);
            }

            bool isActive = (i == GameManager.Instance.currentPartyIndex);
            go.SetActive(isActive);

            spawnedParty.Add(new PartyMember { data = partyDataArray[i], instanceGO = go });
        }

        UpdateActiveCharacterState();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwapToNextCharacter();
        }
    }

    public void SwapToNextCharacter()
    {
        if (spawnedParty.Count <= 1) return;

        int currentIndex = GameManager.Instance.currentPartyIndex;
        PartyMember currentMember = spawnedParty[currentIndex];

        // 1. [상태 추출] 현재 켜져 있는 캐릭터의 달리기 여부를 뽑아냅니다.
        PlayerInputController currentInput = currentMember.instanceGO.GetComponent<PlayerInputController>();
        bool wasRunning = currentInput != null && currentInput.IsRunning;

        // 2. 현재 활성화된 캐릭터의 위치/회전 저장 및 비활성화
        Vector3 lastPos = currentMember.instanceGO.transform.position;
        Quaternion lastRot = currentMember.instanceGO.transform.rotation;
        currentMember.instanceGO.SetActive(false);

        // 3. 인덱스 계산 후 GameManager 데이터 업데이트
        GameManager.Instance.currentPartyIndex = (currentIndex + 1) % spawnedParty.Count;
        int nextIndex = GameManager.Instance.currentPartyIndex;
        PartyMember nextMember = spawnedParty[nextIndex];

        // 4. 다음 캐릭터를 이전 위치로 옮기고 활성화
        nextMember.instanceGO.transform.position = lastPos;
        nextMember.instanceGO.transform.rotation = lastRot;
        nextMember.instanceGO.SetActive(true);

        // 5. [상태 초기화 및 주입] 새로 켜진 캐릭터의 입력을 청소하고 달리기 상태를 덮어씌웁니다.
        PlayerInputController nextInput = nextMember.instanceGO.GetComponent<PlayerInputController>();
        if (nextInput != null)
        {
            nextInput.ResetInput();                 // [추가] 과거의 이동 방향(MoveInput) 찌꺼기를 즉시 삭제
            nextInput.SyncRunningState(wasRunning); // 이후에 필요한 달리기 상태만 다시 덮어씌움
        }

        // 6. 카메라 및 GameManager 활성 데이터 갱신
        UpdateActiveCharacterState();
    }

    private void UpdateActiveCharacterState()
    {
        int currentIndex = GameManager.Instance.currentPartyIndex;
        PartyMember activeMember = spawnedParty[currentIndex];

        if (freeLookCamera != null && activeMember.instanceGO != null)
        {
            freeLookCamera.Target.TrackingTarget = activeMember.instanceGO.transform;
        }

        GameManager.Instance.playerData = activeMember.data;

        if (UIManager_Field.Instance != null)
        {
            UIManager_Field.Instance.UpdatePartyUI(currentIndex);
        }
    }
}
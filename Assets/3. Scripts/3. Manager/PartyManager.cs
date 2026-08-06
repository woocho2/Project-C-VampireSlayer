using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PartyManager : MonoBehaviour
{
    // 파티원 정보를 관리하는 내부 클래스 (데이터와 생성된 게임오브젝트를 묶어줌)
    [System.Serializable]
    public class PartyMember
    {
        public PlayerDataSO data;          // 파티원의 고유 데이터 (SO)
        public GameObject instanceGO;      // 씬에 생성된 실제 캐릭터 게임오브젝트
    }

    [Header("스폰 설정")]
    public Transform spawnPoint;           // 캐릭터들이 처음 소환될 기준 위치

    [Header("카메라 연동")]
    public CinemachineCamera freeLookCamera; // 시네마신 프리룩 카메라 (플레이어를 추적)

    [Header("디버그용 (인스펙터 확인용)")]
    public List<PartyMember> spawnedParty = new List<PartyMember>(); // 소환된 파티원 목록 리스트

    private void Start()
    {
        // GameManager와 파티원 데이터가 정상적으로 존재하는지 검사
        if (GameManager.Instance == null || GameManager.Instance.partyMembers.Length == 0)
        {
            Debug.LogError("GameManager에 파티원 데이터가 없습니다.");
            return;
        }

        PlayerDataSO[] partyDataArray = GameManager.Instance.partyMembers;

        // 파티원 데이터 배열을 순회하며 캐릭터들을 씬에 생성
        for (int i = 0; i < partyDataArray.Length; i++)
        {
            if (partyDataArray[i] == null) continue;

            // 프리팹을 지정된 스폰 위치와 회전값으로 생성
            GameObject go = Instantiate(partyDataArray[i].fieldPlayerPrefab, spawnPoint.position, spawnPoint.rotation);

            // 이동 컨트롤러 설정
            PlayerMoveController moveController = go.GetComponent<PlayerMoveController>();
            if (moveController != null)
            {
                moveController.Setup(partyDataArray[i]);
            }

            // 전투 컨트롤러 설정
            PlayerCombatController combatController = go.GetComponent<PlayerCombatController>();
            if (combatController != null)
            {
                combatController.Setup(partyDataArray[i]);
            }

            // GameManager에 기록된 현재 인덱스와 일치하는 캐릭터만 활성화
            bool isActive = (i == GameManager.Instance.currentPartyIndex);
            go.SetActive(isActive);

            // 리스트에 파티원 정보 등록
            spawnedParty.Add(new PartyMember { data = partyDataArray[i], instanceGO = go });
        }

        // 초기 카메라 및 UI 상태 갱신
        UpdateActiveCharacterState();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // T 키를 누르면 다음 캐릭터로 조종 권한을 교체
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwapToNextCharacter();
        }
    }

    // 다음 캐릭터로 조종 권한을 넘기는 함수
    public void SwapToNextCharacter()
    {
        if (spawnedParty.Count <= 1) return; // 파티원이 1명 이하면 교체할 필요가 없음

        int currentIndex = GameManager.Instance.currentPartyIndex;
        PartyMember currentMember = spawnedParty[currentIndex];

        // 1. [상태 추출] 현재 켜져 있는 캐릭터의 달리기 여부를 뽑아냅니다.
        PlayerInputController currentInput = currentMember.instanceGO.GetComponent<PlayerInputController>();
        bool wasRunning = currentInput != null && currentInput.IsRunning;

        // 2. 현재 활성화된 캐릭터의 위치/회전 저장 및 비활성화
        Vector3 lastPos = currentMember.instanceGO.transform.position;
        Quaternion lastRot = currentMember.instanceGO.transform.rotation;
        currentMember.instanceGO.SetActive(false);

        // 3. 인덱스 계산 후 GameManager 데이터 업데이트 (순환 구조)
        GameManager.Instance.currentPartyIndex = (currentIndex + 1) % spawnedParty.Count;
        int nextIndex = GameManager.Instance.currentPartyIndex;
        PartyMember nextMember = spawnedParty[nextIndex];

        // 4. 다음 캐릭터를 이전 캐릭터의 위치로 옮기고 활성화
        nextMember.instanceGO.transform.position = lastPos;
        nextMember.instanceGO.transform.rotation = lastRot;
        nextMember.instanceGO.SetActive(true);

        // 5. [상태 초기화 및 주입] 새로 켜진 캐릭터의 입력을 청소하고 달리기 상태를 덮어씌웁니다.
        PlayerInputController nextInput = nextMember.instanceGO.GetComponent<PlayerInputController>();
        if (nextInput != null)
        {
            nextInput.ResetInput();                 // 과거의 이동 방향(MoveInput) 찌꺼기를 즉시 삭제
            nextInput.SyncRunningState(wasRunning); // 이전 캐릭터가 달리던 상태였다면 그대로 유지
        }

        // 6. 카메라 및 GameManager 활성 데이터 갱신
        UpdateActiveCharacterState();
    }

    // 현재 활성화된 캐릭터를 기준으로 카메라 추적 대상과 UI를 갱신하는 함수
    private void UpdateActiveCharacterState()
    {
        int currentIndex = GameManager.Instance.currentPartyIndex;
        PartyMember activeMember = spawnedParty[currentIndex];

        // 시네마신 카메라가 새로 켜진 캐릭터를 추적하도록 타겟 변경
        if (freeLookCamera != null && activeMember.instanceGO != null)
        {
            freeLookCamera.Target.TrackingTarget = activeMember.instanceGO.transform;
        }

        // GameManager의 현재 플레이어 데이터 갱신
        GameManager.Instance.playerData = activeMember.data;

        // 필드 UI 매니저에 현재 캐릭터 인덱스를 전달하여 초상화 UI 갱신
        if (FieldUIManager.Instance != null)
        {
            FieldUIManager.Instance.UpdatePartyUI(currentIndex);
        }
    }
}
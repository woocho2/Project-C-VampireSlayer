using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class SpawnManager : MonoBehaviour
{
    [Header("스폰 설정")]
    public Transform spawnPoint;

    [Header("카메라 연동")]
    public CinemachineCamera freeLookCamera;

    private GameObject currentPlayerGO;

    void Start()
    {
        SpawnFieldPlayer();
    }

    void Update()
    {
        // 1. 현재 연결된 키보드 장치가 있는지 안전을 위해 먼저 검사합니다.
        if (Keyboard.current == null) return;

        // 2. 구버전의 Input.GetKeyDown(KeyCode.T) 대신 최신 문법을 사용합니다.
        // Keyboard.current.tKey는 키보드의 T키를 의미하며,
        // wasPressedThisFrame은 이번 프레임에 키가 막 눌렸는지(GetKeyDown과 동일) 판별합니다.
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwapCharacter();
        }
    }

    private void SpawnFieldPlayer()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerData == null) return;

        PlayerDataSO cData = GameManager.Instance.playerData;
        Transform targetSpawn = (spawnPoint != null) ? spawnPoint : transform;

        currentPlayerGO = Instantiate(cData.fieldPlayerPrefab, targetSpawn.position, targetSpawn.rotation);

        if (freeLookCamera != null)
        {
            freeLookCamera.Target.TrackingTarget = currentPlayerGO.transform;
        }
    }

    private void SwapCharacter()
    {
        if (GameManager.Instance == null || GameManager.Instance.partyMembers.Length <= 1) return;

        Vector3 currentPos = currentPlayerGO.transform.position;
        Quaternion currentRot = currentPlayerGO.transform.rotation;

        Destroy(currentPlayerGO);

        GameManager.Instance.currentPartyIndex++;
        if (GameManager.Instance.currentPartyIndex >= GameManager.Instance.partyMembers.Length)
        {
            GameManager.Instance.currentPartyIndex = 0;
        }

        GameManager.Instance.playerData = GameManager.Instance.partyMembers[GameManager.Instance.currentPartyIndex];

        currentPlayerGO = Instantiate(GameManager.Instance.playerData.fieldPlayerPrefab, currentPos, currentRot);

        if (freeLookCamera != null)
        {
            freeLookCamera.Target.TrackingTarget = currentPlayerGO.transform;
        }

        Debug.Log("캐릭터 교체 완료: " + GameManager.Instance.playerData.playerName);
    }
}
using UnityEngine;

// 3D 월드 공간에 배치된 UI 캔버스를 캐릭터 옆에 따라다니게 제어하는 스크립트입니다.
public class UIFollowTarget : MonoBehaviour
{
    [Header("타겟 및 오프셋 설정")]
    [Tooltip("추적할 현재 턴인 캐릭터의 Transform입니다.")]
    public Transform targetCharacter;

    [Tooltip("캐릭터 기준 UI가 띄워질 3D 월드 위치 오프셋입니다. (월드 스페이스이므로 1 = 1미터 단위입니다)")]
    public Vector3 worldOffset = new Vector3(1f, 1f, 0f);

    [Header("회전 설정 (빌보드)")]
    [Tooltip("UI가 항상 메인 카메라 방향을 바라보게 하여 가독성을 높일지 여부입니다.")]
    public bool faceCamera = true;

    private Camera mainCam;

    private void Awake()
    {
        // 씬 내의 메인 카메라를 찾아 메모리에 캐싱해 둡니다.
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        // 타겟이 지정되지 않았거나 카메라를 찾을 수 없으면 연산을 중단합니다.
        if (targetCharacter == null || mainCam == null) return;

        // 1. 위치 이동: 2D 화면 변환 없이 3D 월드 좌표계에서 캐릭터 위치 + 오프셋으로 직접 이동시킵니다.
        transform.position = targetCharacter.position + worldOffset;

        // 2. 회전(투시) 적용: UI가 3D 공간에 있으므로 뒷면이 보이거나 찌그러지는 것을 방지합니다.
        if (faceCamera)
        {
            // 카메라가 바라보는 방향과 동일하게 UI의 앞면(forward)을 맞춥니다.
            // 2번째 사진처럼 약간 비스듬한 깊이감을 원한다면, 이 줄을 지우고 
            // 유니티 에디터에서 TurnPanel의 Y축 Rotation 값을 30~45도 정도로 직접 고정해 두시면 됩니다.
            transform.forward = mainCam.transform.forward;
        }
    }

    /// <summary>
    /// 외부(BattleManager)에서 턴이 시작될 때 이 함수를 호출하여 쫓아다닐 타겟을 지정합니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        targetCharacter = newTarget;
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

// 유니티의 새로운 Input System 컴포넌트가 이 스크립트가 붙은 오브젝트에 반드시 존재하도록 강제합니다.
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputController : MonoBehaviour
{
    [Header("입력 데이터 (읽기 전용)")]
    // 다른 스크립트(이동, 전투 등)에서 현재 입력값을 가져다 쓸 수 있도록 선언합니다.
    // get; private set;을 사용하여 외부 스크립트가 마음대로 값을 수정하지 못하고 읽기만 하도록 보호합니다.
    public Vector2 MoveInput { get; private set; } // 이동 방향 벡터 (W, A, S, D 입력 값)
    public bool IsRunning { get; private set; }     // 달리기 상태 여부

    // 점프와 공격은 지속적인 입력보다 '버튼이 눌린 그 순간' 한 번만 판정되는 것이 중요하므로
    // 외부에서 가져다 쓰고 초기화할 수 있는 형태로 열어둡니다.
    public bool JumpTriggered { get; set; }
    public bool AttackTriggered { get; set; }

    // ==========================================
    // PlayerInput 컴포넌트가 이벤트 발생 시 자동으로 호출하는 콜백 함수들
    // ==========================================

    /// <summary>
    /// 키보드 WASD나 조이스틱 스틱의 이동 입력을 감지했을 때 호출됩니다.
    /// </summary>
    public void OnMove(InputValue value)
    {
        // 입력된 2차원 방향 벡터 값을 가져와 저장합니다.
        MoveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// 달리기 키(예: Shift)의 눌림 상태가 변할 때 호출됩니다.
    /// </summary>
    public void OnRun(InputValue value)
    {
        // 버튼을 누르고 있는 동안은 true, 떼면 false 값을 저장합니다.
        IsRunning = value.isPressed;
    }

    /// <summary>
    /// 점프 키(예: Space)가 눌린 순간 호출됩니다.
    /// </summary>
    public void OnJump(InputValue value)
    {
        // 버튼이 눌려 있는 상태일 때 점프 트리거를 켭니다.
        if (value.isPressed)
        {
            JumpTriggered = true;
        }
    }

    /// <summary>
    /// 공격 키(예: 마우스 좌클릭)가 눌린 순간 호출됩니다.
    /// </summary>
    public void OnAttack(InputValue value)
    {
        // 버튼이 눌려 있는 상태일 때 공격 트리거를 켭니다.
        if (value.isPressed)
        {
            AttackTriggered = true;
        }
    }

    // ==========================================
    // 캐릭터 교체 시 상태 인수인계를 위한 함수
    // ==========================================
    public void SyncRunningState(bool runningState)
    {
        // 조종 중인 캐릭터가 바뀔 때 이전 캐릭터의 달리기 상태를 이어받습니다.
        IsRunning = runningState;
    }

    /// <summary>
    /// 캐릭터를 교체하거나 조작을 멈출 때 남아있는 입력 찌꺼기를 깨끗이 비워주는 함수입니다.
    /// </summary>
    public void ResetInput()
    {
        MoveInput = Vector2.zero; // 이동 입력값을 원점으로 초기화합니다.
        IsRunning = false;        // 달리기 상태를 끕니다.
        JumpTriggered = false;    // 점프 트리거를 초기화합니다.
        AttackTriggered = false;  // 공격 트리거를 초기화합니다.
    }
}
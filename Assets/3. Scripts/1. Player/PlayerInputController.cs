using UnityEngine;
using UnityEngine.InputSystem;

// 유니티의 새로운 Input System 컴포넌트를 강제로 요구합니다.
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputController : MonoBehaviour
{
    [Header("입력 데이터 (읽기 전용)")]
    // 다른 스크립트(이동, 전투)에서 이 값들을 읽어갑니다.
    // get; private set; 을 사용하여 외부에서 임의로 값을 조작하는 것을 방지합니다.
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }

    // 점프와 공격은 누르고 있는 상태가 아니라 '눌린 순간'이 중요하므로 
    // 외부에서 소비(Consume)할 수 있는 형태로 제공하거나 이벤트를 씁니다.
    public bool JumpTriggered { get; set; }
    public bool AttackTriggered { get; set; }

    // ==========================================
    // PlayerInput 컴포넌트가 호출하는 콜백 함수들
    // ==========================================

    /// <summary>
    /// WASD 또는 아날로그 스틱의 이동 값을 수집합니다.
    /// </summary>
    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// 달리기 버튼(Shift)의 눌림 상태를 수집합니다.
    /// </summary>
    public void OnRun(InputValue value)
    {
        IsRunning = value.isPressed;
    }

    /// <summary>
    /// 점프 버튼(Space)이 눌렸을 때 신호를 발생시킵니다.
    /// </summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            JumpTriggered = true;
        }
    }

    /// <summary>
    /// 공격 버튼(마우스 좌클릭)이 눌렸을 때 신호를 발생시킵니다.
    /// </summary>
    public void OnAttack(InputValue value)
    {
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
        IsRunning = runningState;
    }

    // [추가] 교체될 때 이전 입력 찌꺼기를 날려버리는 함수
    public void ResetInput()
    {
        MoveInput = Vector2.zero; // 방향키 입력을 0으로 초기화
        IsRunning = false;
        JumpTriggered = false;
        AttackTriggered = false;
    }
}
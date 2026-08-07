using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerAniController))]
[RequireComponent(typeof(PlayerInputController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerMoveController : MonoBehaviour
{
    private PlayerDataSO m_pData;
    private CharacterController m_characterController;
    private PlayerAniController m_aniController;
    private PlayerInputController m_inputController;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    // 외부 스크립트(예: PlayerCombatController)에서 공중 상태인지 파악할 수 있도록 프로퍼티 개방
    // 전투 스크립트에서 스킬 사용 전 `if (!moveController.IsGrounded) return;` 처리를 위해 사용됩니다.
    public bool IsGrounded => isGrounded;

    private Transform mainCameraTransform;
    private float currentSpeed = 0f;
    private Vector3 lastMoveDirection = Vector3.zero;

    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_aniController = GetComponent<PlayerAniController>();
        m_inputController = GetComponent<PlayerInputController>();

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("MainCamera 태그가 설정된 카메라를 찾을 수 없습니다.");
        }
    }

    public void Setup(PlayerDataSO data)
    {
        m_pData = data;

        if (m_pData == null)
        {
            Debug.LogError("PlayerMoveController에 올바른 데이터가 주입되지 않았습니다.");
        }
    }

    private void Update()
    {
        if (m_pData == null) return;

        // 1. 상태 갱신: Update 최상단에서 바닥 착지 여부를 먼저 갱신하여 판정의 정확도를 높입니다.
        isGrounded = m_characterController.isGrounded;

        // 2. 점프 입력 처리
        if (m_inputController != null && m_inputController.JumpTriggered)
        {
            bool isCombatAction = m_aniController != null && m_aniController.IsPlayingCombatAction();

            // 바닥에 서 있고 전투 액션 중이 아닐 때만 점프 허용
            if (isGrounded && !isCombatAction)
            {
                verticalVelocity.y = Mathf.Sqrt(m_pData.jumpHeight * -2f * m_pData.gravity);
            }
            m_inputController.JumpTriggered = false;
        }

        bool isAction = m_aniController != null && m_aniController.IsPlayingCombatAction();
        Vector3 horizontalMove = Vector3.zero;

        // 3. 이동 및 회전 처리
        if (!isAction)
        {
            horizontalMove = CalculateMovement();

            // [수정 핵심] 바닥에 있을 때(점프 상태가 아닐 때)만 캐릭터 회전을 허용합니다. (회전 락)
            if (isGrounded)
            {
                RotateCharacter(horizontalMove);
            }
        }
        else
        {
            // 액션 중 감속 처리
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * m_pData.acceleration);
            horizontalMove = lastMoveDirection * currentSpeed;
        }

        // 4. 중력 연산 적용
        CalculateGravity();

        // 5. 최종 이동 벡터 계산 및 실행
        Vector3 finalMovement = horizontalMove + verticalVelocity;
        m_characterController.Move(finalMovement * Time.deltaTime);

        // 6. 애니메이션 갱신
        if (m_aniController != null)
        {
            Vector3 horizontalVelocity = new Vector3(m_characterController.velocity.x, 0f, m_characterController.velocity.z);
            m_aniController.UpdateMovementAnimation(horizontalVelocity.magnitude, isGrounded);
        }
    }

    /// <summary>
    /// 플레이어의 키보드 입력을 바탕으로 카메라 시점을 기준 삼아 이동 방향과 속도를 계산합니다.
    /// </summary>
    private Vector3 CalculateMovement()
    {
        float targetSpeed = 0f;

        // [수정된 부분] 
        // isGrounded가 true(바닥에 있음)일 때만 입력을 받아오고, 공중이면 강제로 Vector2.zero(입력 없음)로 처리합니다.
        // 이로 인해 공중에서는 어떠한 이동 명령이나 회전 명령도 새로 생성되지 않습니다.
        Vector2 currentMoveInput = (m_inputController != null && isGrounded)
                                   ? m_inputController.MoveInput
                                   : Vector2.zero;

        if (currentMoveInput.sqrMagnitude > 0.01f)
        {
            // (이하 기존 코드와 동일하게 동작)
            bool currentIsRunning = m_inputController != null && m_inputController.IsRunning;
            targetSpeed = currentIsRunning ? m_pData.runSpeed : m_pData.walkSpeed;

            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            lastMoveDirection = cameraRight * currentMoveInput.x + cameraForward * currentMoveInput.y;
            lastMoveDirection.Normalize();
        }

        if (isGrounded)
        {
            float currentRate = (targetSpeed > currentSpeed) ? m_pData.acceleration : m_pData.deceleration;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * currentRate);
        }

        return lastMoveDirection * currentSpeed;
    }

    private void CalculateGravity()
    {
        // isGrounded 갱신은 Update 최상단으로 옮겼으므로 이곳에서는 중력 가속도만 계산합니다.
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += m_pData.gravity * Time.deltaTime;
    }

    private void RotateCharacter(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_pData.rotationSpeed * Time.deltaTime);
        }
    }
}
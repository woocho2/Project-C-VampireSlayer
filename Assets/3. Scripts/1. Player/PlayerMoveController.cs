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
    private PlayerDataSO m_cData;
    private CharacterController m_characterController;
    private PlayerAniController m_aniController;

    // 입력을 전담할 컨트롤러 캐싱 변수
    private PlayerInputController m_inputController;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    private Transform mainCameraTransform;
    private float currentSpeed = 0f;
    private Vector3 lastMoveDirection = Vector3.zero;

    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_aniController = GetComponent<PlayerAniController>();

        // 1. 인풋 컨트롤러를 캐싱하여 연결합니다.
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
        m_cData = data;

        if (m_cData == null)
        {
            Debug.LogError("PlayerMoveController에 올바른 데이터가 주입되지 않았습니다.");
        }
    }

    private void Update()
    {
        if (m_cData == null) return;

        // 2. 점프 로직: 인풋 컨트롤러에서 점프 신호가 들어왔는지 확인합니다.
        if (m_inputController != null && m_inputController.JumpTriggered)
        {
            bool isCombatAction = m_aniController != null && m_aniController.IsPlayingCombatAction();
            if (isGrounded && !isCombatAction)
            {
                verticalVelocity.y = Mathf.Sqrt(m_cData.jumpHeight * -2f * m_cData.gravity);
            }
            // 처리가 끝난 점프 신호는 끄집어내어 소비(초기화)합니다.
            m_inputController.JumpTriggered = false;
        }

        bool isAction = m_aniController != null && m_aniController.IsPlayingCombatAction();
        Vector3 horizontalMove = Vector3.zero;

        if (!isAction)
        {
            horizontalMove = CalculateMovement();
            RotateCharacter(horizontalMove);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * m_cData.acceleration);
            horizontalMove = lastMoveDirection * currentSpeed;
        }

        CalculateGravity();

        Vector3 finalMovement = horizontalMove + verticalVelocity;
        m_characterController.Move(finalMovement * Time.deltaTime);

        if (m_aniController != null)
        {
            Vector3 horizontalVelocity = new Vector3(m_characterController.velocity.x, 0f, m_characterController.velocity.z);
            m_aniController.UpdateMovementAnimation(horizontalVelocity.magnitude, isGrounded);
        }
    }

    private Vector3 CalculateMovement()
    {
        float targetSpeed = 0f;

        // 3. 내부 변수가 아닌 인풋 컨트롤러의 데이터를 읽어옵니다.
        Vector2 currentMoveInput = m_inputController != null ? m_inputController.MoveInput : Vector2.zero;

        if (currentMoveInput.sqrMagnitude > 0.01f)
        {
            // 달리기 상태도 인풋 컨트롤러에서 읽어옵니다.
            bool currentIsRunning = m_inputController != null && m_inputController.IsRunning;
            targetSpeed = currentIsRunning ? m_cData.runSpeed : m_cData.walkSpeed;

            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            lastMoveDirection = cameraRight * currentMoveInput.x + cameraForward * currentMoveInput.y;
            lastMoveDirection.Normalize();
        }

        float currentRate = (targetSpeed > currentSpeed) ? m_cData.acceleration : m_cData.deceleration;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * currentRate);

        return lastMoveDirection * currentSpeed;
    }

    private void CalculateGravity()
    {
        isGrounded = m_characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += m_cData.gravity * Time.deltaTime;
    }

    private void RotateCharacter(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_cData.rotationSpeed * Time.deltaTime);
        }
    }
}
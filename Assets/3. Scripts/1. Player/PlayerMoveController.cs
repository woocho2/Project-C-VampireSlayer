using UnityEngine;
using UnityEngine.InputSystem;

// 캐릭터 이동에 필수적인 컴포넌트들이 오브젝트에 반드시 존재하도록 강제합니다.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerAniController))]
[RequireComponent(typeof(PlayerInputController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerMoveController : MonoBehaviour
{
    private PlayerDataSO m_pData;                          // 플레이어의 이동 속도, 중력, 점프력 등이 담긴 데이터 에셋
    private CharacterController m_characterController; // 유니티 내장 캐릭터 이동 제어 컴포넌트
    private PlayerAniController m_aniController;         // 플레이어 애니메이션 제어 컴포넌트
    private PlayerInputController m_inputController;    // 플레이어의 키보드/마우스 입력을 전담하여 받아오는 인풋 컨트롤러 캐싱 변수

    private Vector3 verticalVelocity;                 // 수직 방향(점프 및 중력) 속도 벡터
    private bool isGrounded;                          // 캐릭터가 바닥에 닿아있는지 여부

    private Transform mainCameraTransform;            // 메인 카메라의 위치/회전 정보를 담을 트랜스포머
    private float currentSpeed = 0f;                  // 현재 이동 속도 (가감속 처리용)
    private Vector3 lastMoveDirection = Vector3.zero; // 마지막으로 이동했던 방향 벡터

    private void Awake()
    {
        // 필수 컴포넌트들을 미리 찾아 변수에 담아둡니다[cite: 14].
        m_characterController = GetComponent<CharacterController>();
        m_aniController = GetComponent<PlayerAniController>();
        m_inputController = GetComponent<PlayerInputController>();

        // 메인 카메라가 존재하는지 확인하고 위치 정보를 가져옵니다.
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

        // 데이터가 정상적으로 들어왔는지 검사합니다.
        if (m_pData == null)
        {
            Debug.LogError("PlayerMoveController에 올바른 데이터가 주입되지 않았습니다.");
        }
    }

    private void Update()
    {
        // 데이터가 아직 세팅되지 않았다면 아래 로직을 실행하지 않습니다.
        if (m_pData == null) return;

        // 점프 로직: 인풋 컨트롤러에서 점프 신호가 들어왔는지 확인합니다.
        if (m_inputController != null && m_inputController.JumpTriggered)
        {
            // 현재 전투 액션(공격 등) 중인지 확인합니다.
            bool isCombatAction = m_aniController != null && m_aniController.IsPlayingCombatAction();

            // 바닥에 서 있고 전투 액션 중이 아닐 때만 점프를 허용합니다.
            if (isGrounded && !isCombatAction)
            {
                // 물리 공식(v = sqrt(height * -2 * gravity))을 이용해 점프 초기 속도를 계산합니다.
                verticalVelocity.y = Mathf.Sqrt(m_pData.jumpHeight * -2f * m_pData.gravity);
            }
            // 처리가 끝난 점프 신호는 중복 실행을 막기 위해 끄집어내어 소비(초기화)합니다[cite: 14].
            m_inputController.JumpTriggered = false;
        }

        // 현재 전투 애니메이션 액션 중인지 확인합니다.
        bool isAction = m_aniController != null && m_aniController.IsPlayingCombatAction();
        Vector3 horizontalMove = Vector3.zero;

        // 전투 중이 아니라면 정상적으로 이동과 회전을 처리합니다.
        if (!isAction)
        {
            horizontalMove = CalculateMovement();
            RotateCharacter(horizontalMove);
        }
        else
        {
            // 전투 액션 중에는 이동 속도를 서서히 0으로 줄여 멈추게 만듭니다.
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * m_pData.acceleration);
            horizontalMove = lastMoveDirection * currentSpeed;
        }

        // 중력 및 수직 낙하 운동을 계산합니다.
        CalculateGravity();

        // 수평 이동과 수직 낙하(중력/점프)를 합쳐 최종 이동 벡터를 만들고 캐릭터를 움직입니다.
        Vector3 finalMovement = horizontalMove + verticalVelocity;
        m_characterController.Move(finalMovement * Time.deltaTime);

        // 캐릭터의 이동 속도와 지상 여부를 애니메이터에 전달하여 이동 애니메이션을 갱신합니다.
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

        // 3. 내부 변수가 아닌 인풋 컨트롤러에서 이동 입력값(Vector2)을 읽어옵니다[cite: 14].
        Vector2 currentMoveInput = m_inputController != null ? m_inputController.MoveInput : Vector2.zero;

        // 플레이어가 이동 키를 누르고 있다면 (입력 크기가 0보다 크다면)
        if (currentMoveInput.sqrMagnitude > 0.01f)
        {
            // 달리기 상태인지 여부도 인풋 컨트롤러에서 읽어옵니다[cite: 14].
            bool currentIsRunning = m_inputController != null && m_inputController.IsRunning;
            targetSpeed = currentIsRunning ? m_pData.runSpeed : m_pData.walkSpeed;

            // 카메라의 전방/우측 방향을 가져옵니다.
            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            // Y축(높이) 회전은 배제하고 수평 면상에서의 방향만 구하기 위해 Y값을 0으로 맞춥니다.
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 카메라가 바라보는 시점을 기준으로 최종 이동 방향을 계산합니다.
            if (isGrounded)
            {
                lastMoveDirection = cameraRight * currentMoveInput.x + cameraForward * currentMoveInput.y;
                lastMoveDirection.Normalize();
            }
        }

        // 속도가 급격히 변하지 않도록 가속도와 감속도를 적용하여 부드럽게 속도를 보간합니다.
        if (isGrounded)
        {
            float currentRate = (targetSpeed > currentSpeed) ? m_pData.acceleration : m_pData.deceleration;
            currentSpeed = Mathf.SmoothStep(currentSpeed, targetSpeed, Time.deltaTime * currentRate);
        }
        else
        {                       
        }

            return lastMoveDirection * currentSpeed;
    }

    /// <summary>
    /// 캐릭터에게 중력을 적용하고 바닥 착지 상태를 판정합니다.
    /// </summary>
    private void CalculateGravity()
    {
        isGrounded = m_characterController.isGrounded;

        // 바닥에 안정적으로 붙어있을 때는 수직 속도를 약간의 음수(-2)로 유지해 공중에 뜨는 현상을 방지합니다.
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 시간이 흐름에 따라 중력 가속도를 수직 속도에 누적합니다.
        verticalVelocity.y += m_pData.gravity * Time.deltaTime;
    }

    /// <summary>
    /// 이동하는 방향으로 캐릭터의 회전각을 부드럽게 돌려줍니다.
    /// </summary>
    private void RotateCharacter(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // Slerp를 이용해 현재 회전각에서 목표 회전각까지 부드럽게 구면 선형 보간합니다.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_pData.rotationSpeed * Time.deltaTime);
        }
    }
}
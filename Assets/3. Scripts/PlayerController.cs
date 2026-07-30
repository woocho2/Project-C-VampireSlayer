using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent (typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 및 회전 수치 설정")]
    public float walkSpeed = 4f;
    public float runSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 5f;

    [Header("가감속 설정")]
    // 출발 및 가속 속도 (수치가 높을수록 키 입력 시 즉시 출발합니다)
    public float acceleration = 15f;
    // 정지 및 감속 속도 (수치가 낮을수록 얼음판처럼 더 많이 미끄러집니다)
    public float deceleration = 2f;

    [Header("전투 설정")]
    public float startBattleTime = 10f;

    private CharacterController m_cc;
    private Animator m_ani;
    [SerializeField] SwordController m_swordController;

    private Vector2 moveInput;
    private Vector3 verticalVelocity;

    private bool isGrounded;
    private bool isRunning;

    private bool isArmed = false;
    private float combatTimer = 0f;

    private Transform mainCameraTransform;

    private float currentSpeed = 0f;
    private Vector3 lastMoveDirection = Vector3.zero;

    private void Awake()
    {
        m_cc = GetComponent<CharacterController>();
        m_ani = GetComponentInChildren<Animator>();

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("MainCamera 태그가 설정된 카메라를 찾을 수 없습니다.");
        }

        if (m_swordController != null)
        {
            m_swordController.PutSword();
        }
    }

    private void Update()
    {
        bool isAction = IsPlayingAction();
        Vector3 horizontalMove = Vector3.zero;

        if (!isAction)
        {
            horizontalMove = CalculateMovement();
            RotateCharacter(horizontalMove);
        }
        else
        {
            // 액션 중일 때는 목표 속도가 0이므로 deceleration(감속도)를 적용하여 미끄러지듯 멈춤
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * acceleration);
            horizontalMove = lastMoveDirection * currentSpeed;
        }

        CalculateGravity();

        Vector3 finalMovement = horizontalMove + verticalVelocity;
        m_cc.Move(finalMovement * Time.deltaTime);

        ManageCombatState();
        UpdateAnimation();
    }

    private bool IsPlayingAction()
    {
        if (m_ani != null)
        {
            AnimatorStateInfo stateInfo = m_ani.GetCurrentAnimatorStateInfo(0);

            return stateInfo.IsName("Drawing Sword") ||
                   stateInfo.IsName("Sword Slash") ||
                   stateInfo.IsName("Putting Sword");
        }
        return false;
    }

    private Vector3 CalculateMovement()
    {
        float targetSpeed = 0f;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            targetSpeed = isRunning ? runSpeed : walkSpeed;

            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            lastMoveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
            lastMoveDirection.Normalize();
        }

        // 1. 목표 속도가 현재 속도보다 높은지(가속 중인지) 낮은지(감속 중인지) 판별
        // 삼항 연산자를 통해 적용할 변화율(Rate)을 결정합니다.
        float currentRate = (targetSpeed > currentSpeed) ? acceleration : deceleration;

        // 2. 결정된 Rate(가속도 또는 감속도)를 Lerp에 적용하여 현재 속도를 갱신
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * currentRate);

        return lastMoveDirection * currentSpeed;
    }

    private void CalculateGravity()
    {
        isGrounded = m_cc.isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
    }

    private void RotateCharacter(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ManageCombatState()
    {
        if (isArmed)
        {
            combatTimer += Time.deltaTime;

            if (combatTimer >= startBattleTime)
            {
                isArmed = false;
                combatTimer = 0f;

                if (m_ani != null)
                {
                    m_ani.SetTrigger("PutSword");
                }
            }
        }
    }

    private void UpdateAnimation()
    {
        Vector3 horizontalVelocity = new Vector3(m_cc.velocity.x, 0f, m_cc.velocity.z);
        float speedMagnitude = horizontalVelocity.magnitude;

        if (m_ani != null)
        {
            m_ani.SetFloat("MoveSpeed", speedMagnitude);
            m_ani.SetBool("IsGrounded", isGrounded);
        }
    }

    // --- Input System 이벤트 함수 ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && !IsPlayingAction())
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnRun(InputValue value)
    {
        isRunning = value.isPressed;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            combatTimer = 0f;

            if (!isArmed)
            {
                isArmed = true;
                if (m_ani != null)
                {
                    m_ani.SetTrigger("DrawSword");
                }
            }
            else
            {
                if (m_ani != null)
                {
                    m_ani.SetTrigger("DoAttack");
                }
            }
        }
    }
}
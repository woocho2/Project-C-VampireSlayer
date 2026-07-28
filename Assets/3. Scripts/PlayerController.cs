using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 및 회전 수치 설정")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 5f;

    [Header("전투 설정")]
    public float autoPutTime = 10f;

    private CharacterController m_cc;
    private Animator m_ani;

    private Vector2 moveInput;
    private Vector3 verticalVelocity;

    private bool isGrounded;
    private bool isRunning;

    private bool isArmed = false;
    private float combatTimer = 0f;

    private Transform mainCameraTransform;

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
        if (moveInput.sqrMagnitude <= 0.01f) return Vector3.zero;

        Vector3 cameraForward = mainCameraTransform.forward;
        Vector3 cameraRight = mainCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        return moveDirection * currentSpeed;
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

            if (combatTimer >= autoPutTime)
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
        float currentSpeed = horizontalVelocity.magnitude;

        if (m_ani != null)
        {
            m_ani.SetFloat("MoveSpeed", currentSpeed);
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
        // 개선 1: !IsPlayingAction() 조건을 추가하여 공격/발도/납도 중에는 점프 로직이 무시되도록 설정합니다.
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
        // 개선 2: isGrounded 조건을 추가하여 바닥에 닿아있을 때만 공격 로직이 실행되도록 설정합니다.
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
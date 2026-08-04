using UnityEngine;
using UnityEngine.InputSystem;

// 필수 컴포넌트 강제 부착 어트리뷰트
// 이름 충돌이 해결되었으므로 UnityEngine. 접두사를 생략할 수 있습니다.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(WeaponController))]
public class FieldPlayerController : MonoBehaviour
{
    [Header("데이터 참조")]
    // 싱글톤에서 불러올 플레이어의 통합 데이터를 저장할 변수입니다.
    private PlayerDataSO m_cData;

    [Header("컴포넌트 캐싱")]
    // 유니티 내장 물리 이동 컴포넌트입니다.
    private CharacterController m_cc;
    // 애니메이션 제어 컴포넌트입니다.
    private Animator m_ani;
    // 무기 장착 및 해제를 관리하는 스크립트입니다.
    private WeaponController m_weaponController;

    [Header("상태 변수")]
    private Vector2 moveInput;           // 사용자의 이동 입력값 (X, Y)
    private Vector3 verticalVelocity;    // 중력 및 점프에 의한 수직(Y축) 속도

    private bool isGrounded;             // 바닥에 닿아있는지 여부
    private bool isRunning;              // 달리기 상태 여부
    private bool isArmed = false;        // 무기를 꺼낸(전투) 상태 여부
    private float combatTimer = 0f;      // 전투 상태 유지 시간을 측정하는 타이머

    private Transform mainCameraTransform; // 메인 카메라의 트랜스폼 데이터

    private float currentSpeed = 0f;       // 현재 적용 중인 이동 속도 (가감속 보간용)
    private Vector3 lastMoveDirection = Vector3.zero; // 마지막으로 바라본 이동 방향

    private void Awake()
    {
        // 1. 필요한 컴포넌트들을 게임 시작 시점에 한 번만 찾아서 메모리에 할당(캐싱)합니다.
        m_cc = GetComponent<CharacterController>();
        m_ani = GetComponentInChildren<Animator>();
        m_weaponController = GetComponent<WeaponController>();

        // 2. 씬에 배치된 메인 카메라를 찾아 연동합니다. 카메라 기준으로 이동 방향을 잡기 위함입니다.
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("MainCamera 태그가 설정된 카메라를 찾을 수 없습니다.");
        }

        // 3. 시작 시 검을 집어넣은 상태로 초기화합니다.
        if (m_weaponController != null)
        {
            m_weaponController.PutSword();
        }
    }

    private void Start()
    {
        // 씬 시작 시 GameManager에 등록된 활성 플레이어 데이터를 가져옵니다.
        if (GameManager.Instance != null && GameManager.Instance.playerData != null)
        {
            // GameManager가 들고 있는 데이터를 로컬 변수에 연결합니다.
            m_cData = GameManager.Instance.playerData;
        }
        else
        {
            Debug.LogError("GameManager 또는 PlayerData가 존재하지 않아 이동할 수 없습니다.");
        }
    }

    private void Update()
    {
        // 데이터가 정상적으로 들어오기 전까지는 이동 연산을 수행하지 않고 대기합니다.
        if (m_cData == null) return;

        bool isAction = IsPlayingAction();
        Vector3 horizontalMove = Vector3.zero;

        // 공격 등 특정 액션 중이 아닐 때만 이동 및 회전을 허용합니다.
        if (!isAction)
        {
            horizontalMove = CalculateMovement();
            RotateCharacter(horizontalMove);
        }
        else
        {
            // 액션 중일 때는 목표 속도를 0으로 설정하고, 감속도(deceleration) 대신 가속도(acceleration)를 
            // 사용하여 바닥에 미끄러지지 않고 즉각적으로 멈추도록 브레이크를 겁니다.
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * m_cData.acceleration);
            horizontalMove = lastMoveDirection * currentSpeed;
        }

        // 중력 연산을 수행합니다.
        CalculateGravity();

        // 수평 이동(horizontalMove)과 수직 이동(verticalVelocity)을 합산하여 최종 이동 처리를 합니다.
        Vector3 finalMovement = horizontalMove + verticalVelocity;
        m_cc.Move(finalMovement * Time.deltaTime);

        // 전투 상태 타이머를 갱신하고 애니메이션 파라미터를 업데이트합니다.
        ManageCombatState();
        UpdateAnimation();
    }

    /// <summary>
    /// 현재 재생 중인 애니메이션이 이동을 제한하는 액션인지 판별합니다.
    /// </summary>
    private bool IsPlayingAction()
    {
        if (m_ani != null)
        {
            AnimatorStateInfo stateInfo = m_ani.GetCurrentAnimatorStateInfo(0);

            // 발도, 공격, 납도 상태일 경우 true를 반환하여 이동을 막습니다.
            return stateInfo.IsName("Drawing Sword") ||
                   stateInfo.IsName("Sword Slash") ||
                   stateInfo.IsName("Putting Sword");
        }
        return false;
    }

    /// <summary>
    /// 카메라의 시선을 기준으로 플레이어가 이동할 벡터를 계산합니다.
    /// </summary>
    private Vector3 CalculateMovement()
    {
        float targetSpeed = 0f;

        // 키보드나 패드의 입력값이 존재할 경우
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // 달리기 버튼이 눌렸는지에 따라 목표 속도를 데이터 객체(m_cData)에서 가져와 결정합니다.
            targetSpeed = isRunning ? m_cData.runSpeed : m_cData.walkSpeed;

            // 카메라가 바라보는 정면과 우측 방향 벡터를 구합니다.
            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            // 카메라가 위아래를 보더라도 캐릭터가 땅으로 파고들거나 하늘로 날아가지 않도록 Y축 값을 제거합니다.
            cameraForward.y = 0f;
            cameraRight.y = 0f;

            // Y축을 지웠으므로 찌그러진 벡터의 길이를 다시 1로 정규화(Normalize) 합니다.
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 입력값(WASD)과 카메라의 방향을 곱하여 최종 이동 방향을 설정합니다.
            lastMoveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
            lastMoveDirection.Normalize();
        }

        // 현재 속도(currentSpeed)가 목표 속도(targetSpeed)로 향할 때,
        // 가속 중인지 감속 중인지 판별하여 데이터 객체(m_cData)의 적용 수치를 다르게 줍니다.
        float currentRate = (targetSpeed > currentSpeed) ? m_cData.acceleration : m_cData.deceleration;

        // Lerp(선형 보간)를 이용해 0프레임만에 갑자기 속도가 변하지 않고 부드럽게 가감속 되도록 만듭니다.
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * currentRate);

        return lastMoveDirection * currentSpeed;
    }

    /// <summary>
    /// 캐릭터가 바닥에 서있게 만드는 중력 연산 함수입니다.
    /// </summary>
    private void CalculateGravity()
    {
        // CharacterController가 자체적으로 제공하는 바닥 감지 기능을 사용합니다.
        isGrounded = m_cc.isGrounded;

        // 바닥에 닿아있는데 Y축 속도가 계속 떨어지는 것을 방지하기 위해 최소한의 접지력(-2f)만 남깁니다.
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 공중에 떠 있다면 데이터 객체(m_cData)의 중력값을 계속 더해 아래로 떨어지게 합니다.
        verticalVelocity.y += m_cData.gravity * Time.deltaTime;
    }

    /// <summary>
    /// 입력 방향을 바라보도록 캐릭터를 회전시키는 함수입니다.
    /// </summary>
    private void RotateCharacter(Vector3 moveDirection)
    {
        // 이동하려는 벡터가 존재할 때만 회전을 실행합니다.
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.y = 0f; // 안전을 위해 Y축 기울임 방지

            // 바라봐야 할 방향(Vector3)을 회전 데이터(Quaternion)로 변환합니다.
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // 데이터 객체(m_cData)의 회전 속도에 맞춰 부드럽게 돌아보게(Slerp) 만듭니다.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_cData.rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 공격 후 일정 시간이 지나면 무기를 자동으로 집어넣는 로직입니다.
    /// </summary>
    private void ManageCombatState()
    {
        if (isArmed)
        {
            combatTimer += Time.deltaTime;

            // 데이터 객체(m_cData)에 정의된 전투 유지 시간이 지나면 무기를 넣습니다.
            if (combatTimer >= m_cData.startBattleTime)
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

    /// <summary>
    /// 이동 속도와 땅 접지 상태를 애니메이터에 전달합니다.
    /// </summary>
    private void UpdateAnimation()
    {
        // Y축(중력)을 제외한 순수 수평 이동 벡터만을 계산하여 애니메이션 속도에 반영합니다.
        Vector3 horizontalVelocity = new Vector3(m_cc.velocity.x, 0f, m_cc.velocity.z);
        float speedMagnitude = horizontalVelocity.magnitude;

        if (m_ani != null)
        {
            m_ani.SetFloat("MoveSpeed", speedMagnitude);
            m_ani.SetBool("IsGrounded", isGrounded);
        }
    }

    // ==========================================
    // Input System 이벤트 콜백 함수 영역
    // ==========================================

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        // 데이터가 할당되어 있고, 땅에 닿아있으며, 액션 중이 아닐 때만 점프 공식을 계산합니다.
        if (m_cData != null && value.isPressed && isGrounded && !IsPlayingAction())
        {
            // 물리 공식: v = sqrt(2 * h * g)
            verticalVelocity.y = Mathf.Sqrt(m_cData.jumpHeight * -2f * m_cData.gravity);
        }
    }

    public void OnRun(InputValue value)
    {
        isRunning = value.isPressed;
    }

    public void OnAttack(InputValue value)
    {
        // 공중 공격 기능이 없다면 땅에 있을 때만 공격을 허용합니다.
        if (value.isPressed && isGrounded)
        {
            combatTimer = 0f; // 조작 시 타이머를 초기화하여 무기 넣기를 연기합니다.

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
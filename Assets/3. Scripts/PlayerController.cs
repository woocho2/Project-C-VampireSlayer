using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 및 회전 수치 설정")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Animator animator;

    private Vector2 moveInput;
    private Vector3 verticalVelocity; // Y축(중력 및 점프) 전용 속도 변수
    private bool isGrounded;
    private Transform mainCameraTransform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

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
        // 1. 수평 이동 벡터(X, Z) 연산
        Vector3 horizontalMove = CalculateMovement();

        // 2. 수직 이동 벡터(Y) 연산
        CalculateGravity();

        // 3. 수평과 수직 벡터를 하나로 합성하여 단일 Move() 호출 수행
        // 이렇게 해야 controller.velocity에 수평과 수직 이동 결과가 모두 온전히 저장됩니다.
        Vector3 finalMovement = horizontalMove + verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);

        // 4. 캐릭터 회전 및 애니메이션 갱신
        RotateCharacter(horizontalMove);
        UpdateAnimation();
    }

    private Vector3 CalculateMovement()
    {
        // 입력값이 없으면 이동 벡터 0 반환
        if (moveInput.sqrMagnitude <= 0.01f) return Vector3.zero;

        Vector3 cameraForward = mainCameraTransform.forward;
        Vector3 cameraRight = mainCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // 카메라가 바라보는 방향을 기준으로 최종 수평 방향 벡터 반환
        Vector3 moveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
        return moveDirection * moveSpeed;
    }

    private void CalculateGravity()
    {
        isGrounded = controller.isGrounded;

        // 바닥에 닿아있고 아래로 떨어지는 중이라면 Y축 속도를 일정하게 초기화
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 중력 가속도 누적
        verticalVelocity.y += gravity * Time.deltaTime;
    }

    private void RotateCharacter(Vector3 moveDirection)
    {
        // 수평 이동 입력이 존재할 때만 회전 수행
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // 수직(Y축) 기울어짐을 방지하기 위해 강제로 0으로 고정
            moveDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        // 통합된 단일 Move 호출 덕분에 이제 velocity.x와 velocity.z 값을 제대로 가져옵니다.
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (animator != null)
        {
            animator.SetFloat("MoveSpeed", currentSpeed);
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
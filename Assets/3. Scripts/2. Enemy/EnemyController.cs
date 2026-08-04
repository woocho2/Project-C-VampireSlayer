using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("에너미 데이터")]
    public EnemyDataSO m_eData;

    [Header("대상 설정")]
    public Transform playerTarget;

    private Animator m_ani;
    private NavMeshAgent m_agent;

    private Vector3 startPosition;
    private bool isChasing = false;
    private bool isReturning = false;

    private void Awake()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_ani = GetComponentInChildren<Animator>();
        startPosition = transform.position;

        FindPlayerTarget();
    }

    private void Update()
    {
        if (m_eData == null) return;

        if (playerTarget == null)
        {
            FindPlayerTarget();
        }

        // 1. 복귀 상태 로직 최우선 처리
        if (isReturning)
        {
            ReturnToStartPos();
            UpdateAnimation();
            return; // 복귀 중일 때는 시야 검사나 추적 로직을 무시하고 아래 코드를 실행하지 않습니다.
        }

        // 2. 한계 거리 이탈 검사 (목줄 시스템)
        // 몬스터의 현재 위치와 원래 위치(startPosition)의 거리를 잽니다.
        float distanceFromStart = Vector3.Distance(transform.position, startPosition);
        if (distanceFromStart > m_eData.maxChaseDistance)
        {
            // 한계치를 넘으면 즉시 추적을 멈추고 복귀 상태로 전환합니다.
            isChasing = false;
            isReturning = true;
        }
        else
        {
            // 한계치 안쪽에 있다면 정상적으로 시야 검사를 진행합니다.
            if (playerTarget != null)
            {
                isChasing = CheckFieldOfView();
            }
            else
            {
                isChasing = false;
            }
        }

        // 3. 상태에 따른 행동 실행
        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Wander();
        }

        UpdateAnimation();
    }

    /// <summary>
    /// 플레이어가 몬스터의 시야 범위와 각도 내에 있으며, 장애물에 가려지지 않았는지 검사합니다.
    /// </summary>
    private bool CheckFieldOfView()
    {
        if (playerTarget == null) return false;

        // 1. 거리 검사: 탐지 반경 바깥이면 즉시 false 반환
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > m_eData.detectionRadius) return false;

        // 2. 시야각 검사: 몬스터 정면 벡터와 플레이어 방향 벡터 간의 각도 도출
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        // 도출된 각도가 셋팅된 시야각의 절반(좌/우 기준)보다 크면 시야 밖임
        if (angle > m_eData.viewAngle / 2f) return false;

        // 3. 장애물(Line of Sight) 검사: 눈 위치에서 플레이어를 향해 레이저(Raycast)를 발사
        Vector3 rayOrigin = transform.position + (Vector3.up * m_eData.eyeHeight);
        // 타겟의 발밑이 아닌 타겟의 몸통(Center)을 향하도록 타겟 위치에도 높이를 더해줍니다.
        Vector3 rayDirection = (playerTarget.position + (Vector3.up * 1f)) - rayOrigin;

        // 레이가 무언가에 부딪혔을 때, 그 부딪힌 대상이 플레이어인지 확인합니다.
        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out RaycastHit hit, m_eData.detectionRadius))
        {
            if (hit.transform == playerTarget)
            {
                return true; // 거리, 각도, 장애물 검사를 모두 통과하면 true(시야에 보임)
            }
        }

        // 중간에 벽이나 다른 물체에 레이가 막혔다면 시야에 보이지 않는 것으로 간주합니다.
        return false;
    }

    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void ChasePlayer()
    {
        m_agent.speed = m_eData.chaseSpeed;
        m_agent.SetDestination(playerTarget.position);
    }

    private void Wander()
    {
        m_agent.speed = m_eData.patrolSpeed;

        if (!m_agent.hasPath || m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * m_eData.patrolRadius;
            randomDirection += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, m_eData.patrolRadius, 1))
            {
                m_agent.SetDestination(hit.position);
            }
        }
    }

    private void ReturnToStartPos()
    {
        m_agent.speed = m_eData.patrolSpeed; // 돌아갈 때는 보통 배회 속도로 걸어갑니다.
        m_agent.SetDestination(startPosition);

        // 원래 자리(startPosition)에 거의 도착했는지 검사합니다.
        if (!m_agent.pathPending && m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            // 도착했다면 복귀 상태를 해제하여 다시 배회/시야 탐지를 시작하도록 만듭니다.
            isReturning = false;
        }
    }

    private void UpdateAnimation()
    {
        Vector3 horizontalVelocity = new Vector3(m_agent.velocity.x, 0f, m_agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (m_ani != null)
        {
            m_ani.SetFloat("MoveSpeed", currentSpeed);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                if (m_eData != null)
                {
                    GameManager.Instance.StartBattleTransition(m_eData);
                }
                else
                {
                    Debug.LogError("EnemyController에 EnemyDataSO가 할당되지 않았습니다.");
                }
            }
            else
            {
                Debug.LogError("씬에 GameManager가 존재하지 않습니다.");
            }
        }
    }
}
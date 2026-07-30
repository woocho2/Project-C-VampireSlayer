using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VampireController : MonoBehaviour
{
    [Header("몬스터 데이터")]
    // 이 뱀파이어의 고유 정보(SO)를 인스펙터에서 할당받기 위한 변수입니다.
    public MonsterDataSO MonsterData;

    [Header("이동 및 탐지 설정")]
    public float patrolRadius = 10f;
    public float patrolSpeed = 3f;
    public float chaseSpeed = 8f;
    public float detectionRadius = 10f;

    [Header("대상 설정")]
    public Transform playerTarget;

    private Animator m_ani;
    private NavMeshAgent agent;
    private Vector3 startPosition;
    private bool isChasing = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        m_ani = GetComponentInChildren<Animator>();
        startPosition = transform.position;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            // 씬에서 "Player" 태그를 가진 오브젝트를 찾습니다.
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }

        // 2. 타겟 존재 여부에 따른 상태 결정
        if (playerTarget != null)
        {
            // 타겟이 존재하면 거리를 계산하여 추적 여부를 결정합니다.
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            isChasing = (distanceToPlayer <= detectionRadius);
        }
        else
        {
            // 맵에 플레이어가 아예 없다면 추적을 포기합니다.
            isChasing = false;
        }

        // 3. 상태에 따른 행동 실행 (return으로 강제 종료되지 않으므로 무조건 실행됨)
        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            // 타겟이 없어도 isChasing은 false이므로 배회(Wander) 상태로 정상 진입합니다.
            Wander();
        }

        // 4. 애니메이션 갱신
        UpdateAnimation();
    }

    private void ChasePlayer()
    {
        agent.speed = chaseSpeed;
        agent.SetDestination(playerTarget.position);
    }

    private void Wander()
    {
        agent.speed = patrolSpeed;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void UpdateAnimation()
    {
        Vector3 horizontalVelocity = new Vector3(agent.velocity.x, 0f, agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (m_ani != null)
        {
            m_ani.SetFloat("MoveSpeed", currentSpeed);
            m_ani.SetBool("IsGrounded", true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                // 인스펙터에 SO 데이터가 정상적으로 연결되어 있는지 검사합니다.
                if (MonsterData != null)
                {
                    // 괄호 안에 myMonsterData를 인자로 넣어 GameManager로 전달합니다.
                    GameManager.Instance.StartBattleTransition(MonsterData);
                }
                else
                {
                    // 데이터 할당을 잊었을 경우를 대비하여 콘솔에 에러를 띄웁니다.
                    Debug.LogError("VampireController에 MonsterDataSO가 할당되지 않았습니다.");
                }
            }
            else
            {
                Debug.LogError("씬에 GameManager가 존재하지 않습니다.");
            }
        }
    }
}
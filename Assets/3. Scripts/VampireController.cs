using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VampireController : MonoBehaviour
{
    [Header("이동 및 탐지 설정")]
    public float wanderRadius = 10f;
    public float wanderSpeed = 2f;
    public float chaseSpeed = 4f;
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
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= detectionRadius)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }

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

    private void ChasePlayer()
    {
        agent.speed = chaseSpeed;
        agent.SetDestination(playerTarget.position);
    }

    private void Wander()
    {
        agent.speed = wanderSpeed;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
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
            // 수정된 부분: 직접 SceneManager를 호출하지 않고 GameManager에 처리를 위임합니다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartBattleTransition();
            }
            else
            {
                Debug.LogError("씬에 GameManager가 존재하지 않습니다.");
            }
        }
    }
}
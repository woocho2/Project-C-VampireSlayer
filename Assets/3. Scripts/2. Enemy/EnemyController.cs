using UnityEngine;
using UnityEngine.AI;

// 몬스터 오브젝트에 반드시 필요한 컴포넌트들이 자동으로 추가되도록 강제합니다.
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("에너미 데이터")]
    public EnemyDataSO m_eData; // 몬스터의 능력치와 설정값(이동 속도, 시야각 등)을 담은 데이터 파일

    [Header("대상 설정")]
    public Transform playerTarget; // 몬스터가 추적해야 할 플레이어의 위치 정보

    private Animator m_ani; // 애니메이션을 제어하기 위한 컴포넌트
    private NavMeshAgent m_agent; // 유니티 내비메시를 이용해 길을 찾고 이동하게 만드는 컴포넌트

    private Vector3 startPosition; // 몬스터가 처음 생성되었거나 돌아가야 할 원래 자리
    private bool isChasing = false; // 현재 플레이어를 추적 중인지 여부
    private bool isReturning = false; // 원래 자리로 복귀 중인지 여부

    private void Awake()
    {
        // 컴포넌트와 초기 위치 세팅
        m_agent = GetComponent<NavMeshAgent>();
        m_ani = GetComponentInChildren<Animator>();
        startPosition = transform.position; // 게임 시작 시점의 위치를 원래 자리로 기억

        FindPlayerTarget(); // 게임 내에서 플레이어를 찾아 대상 설정
    }

    private void Update()
    {
        if (m_eData == null) return;

        // 플레이어 정보를 아직 못 찾았다면 계속해서 찾기 시도
        if (playerTarget == null)
        {
            FindPlayerTarget();
        }

        // 1. 복귀 상태 로직 최우선 처리 (원래 자리로 돌아가는 중일 때)
        if (isReturning)
        {
            ReturnToStartPos();
            UpdateAnimation();
            return; // 복귀 중에는 시야 검사나 추적을 하지 않고 아래 코드를 무시합니다.
        }

        // 2. 한계 거리 이탈 검사 (목줄 시스템: 너무 멀리 쫓아왔는지 확인)
        float distanceFromStart = Vector3.Distance(transform.position, startPosition);
        if (distanceFromStart > m_eData.maxChaseDistance)
        {
            // 설정된 최대 추적 거리를 벗어나면 추적을 멈추고 복귀 모드로 전환
            isChasing = false;
            isReturning = true;
        }
        else
        {
            // 한계 거리 안쪽이라면 플레이어가 시야에 들어왔는지 정상적으로 검사
            if (playerTarget != null)
            {
                isChasing = CheckFieldOfView();
            }
            else
            {
                isChasing = false;
            }
        }

        // 3. 상태에 따른 행동 실행 (추적 중이면 플레이어 쫓기, 아니면 배회하기)
        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Wander();
        }

        UpdateAnimation(); // 이동 속도에 맞춰 애니메이션 동기화
    }

    /// <summary>
    /// 플레이어가 몬스터의 시야 범위, 각도 내에 있고 장애물에 가려지지 않았는지 검사합니다.
    /// </summary>
    private bool CheckFieldOfView()
    {
        if (playerTarget == null) return false;

        // 1. 거리 검사: 탐지 반경보다 멀리 있으면 시야에 없는 것으로 처리
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > m_eData.detectionRadius) return false;

        // 2. 시야각 검사: 몬스터가 바라보는 방향과 플레이어 방향 사이의 각도를 계산
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        // 계산된 각도가 설정된 시야각의 절반보다 크면 시야 범위 밖으로 판단
        if (angle > m_eData.viewAngle / 2f) return false;

        // 3. 장애물 검사(Line of Sight): 눈높이에서 플레이어의 몸통을 향해 레이저(Raycast) 발사
        Vector3 rayOrigin = transform.position + (Vector3.up * m_eData.eyeHeight);
        Vector3 rayDirection = (playerTarget.position + (Vector3.up * 1f)) - rayOrigin;

        // 레이저가 무언가에 부딪혔을 때, 그 부딪힌 대상이 플레이어 본인인지 확인
        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out RaycastHit hit, m_eData.detectionRadius))
        {
            if (hit.transform == playerTarget)
            {
                return true; // 거리, 각도, 장애물 검사를 모두 통과하면 플레이어가 보인다고 판단
            }
        }

        // 사이에 벽이나 장애물이 있어 레이저가 가려졌다면 보이지 않는 것으로 처리
        return false;
    }

    // 씬에서 "Player" 태그를 가진 오브젝트를 찾아 플레이어 타겟으로 지정하는 함수
    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    // 플레이어를 추적할 때 실행되는 함수
    private void ChasePlayer()
    {
        m_agent.speed = m_eData.chaseSpeed; // 추적 속도로 변경
        m_agent.SetDestination(playerTarget.position); // 내비게이션 목적지를 플레이어 위치로 설정
    }

    // 평소에 주변을 돌아다니며 배회할 때 실행되는 함수
    private void Wander()
    {
        m_agent.speed = m_eData.patrolSpeed; // 순찰/배회 속도로 변경

        // 목적지에 거의 다 도착했거나 갈 곳이 없다면 새로운 무작위 장소 선정
        if (!m_agent.hasPath || m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * m_eData.patrolRadius;
            randomDirection += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, m_eData.patrolRadius, 1))
            {
                m_agent.SetDestination(hit.position); // 무작위로 뽑힌 이동 가능한 위치로 목적지 설정
            }
        }
    }

    // 최대 추적 거리를 벗어났을 때 원래 자리로 되돌아가는 함수
    private void ReturnToStartPos()
    {
        m_agent.speed = m_eData.patrolSpeed; // 돌아갈 때는 배회 속도로 이동
        m_agent.SetDestination(startPosition); // 목적지를 처음 생성되었던 위치로 설정

        // 원래 자리에 거의 도착했는지 검사
        if (!m_agent.pathPending && m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            isReturning = false; // 복귀 완료 처리하여 다시 평상시 상태로 돌아감
        }
    }

    // 몬스터의 이동 속도를 계산해 애니메이션 파라미터(MoveSpeed)에 전달하는 함수
    private void UpdateAnimation()
    {
        Vector3 horizontalVelocity = new Vector3(m_agent.velocity.x, 0f, m_agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (m_ani != null)
        {
            m_ani.SetFloat("MoveSpeed", currentSpeed);
        }
    }   
}
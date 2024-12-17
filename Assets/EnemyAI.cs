using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public float wanderRadius = 10f;   // �����_���ړ��͈̔�
    public float wanderInterval = 3f; // �����_���ړ��̊Ԋu
    public Transform player;          // �v���C���[��Transform
    public float chaseDistance = 15f; // �v���C���[��ǐՂ��鋗��

    private NavMeshAgent agent;
    private float wanderTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        wanderTimer = wanderInterval;
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogError("Player is not assigned in EnemyAI script!");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // �v���C���[���ǐՔ͈͓��ɂ���ꍇ�͒ǂ�������
        if (distanceToPlayer <= chaseDistance)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // �����_���ړ�
            Wander();
        }

        // NavMesh�O�ɏo�Ȃ����߂̕␳
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Enemy is outside NavMesh! Repositioning...");
            RepositionToNavMesh();
        }
    }

    // �����_���ړ�
    void Wander()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval && agent.isOnNavMesh)
        {
            Vector3 newTarget = GetRandomPoint(transform.position, wanderRadius);
            if (newTarget != Vector3.zero)
            {
                agent.SetDestination(newTarget);
            }
            wanderTimer = 0f;
        }
    }

    // NavMesh���̃����_���ȃ|�C���g���擾
    Vector3 GetRandomPoint(Vector3 center, float radius)
    {
        Vector3 randomPos = center + Random.insideUnitSphere * radius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPos, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }

    // NavMesh��ɍĔz�u
    void RepositionToNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.Warp(hit.position);
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public float wanderRadius = 10f;   // ランダム移動の範囲
    public float wanderInterval = 3f; // ランダム移動の間隔
    public Transform player;          // プレイヤーのTransform
    public float chaseDistance = 15f; // プレイヤーを追跡する距離

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

        // プレイヤーが追跡範囲内にいる場合は追いかける
        if (distanceToPlayer <= chaseDistance)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // ランダム移動
            Wander();
        }

        // NavMesh外に出ないための補正
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Enemy is outside NavMesh! Repositioning...");
            RepositionToNavMesh();
        }
    }

    // ランダム移動
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

    // NavMesh内のランダムなポイントを取得
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

    // NavMesh上に再配置
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

using UnityEngine;
using UnityEngine.AI;
public class ZombieAi : MonoBehaviour
{
    public enum State
    {
        Wander,
        Chase,
        Investigate
    }

    public State currentState = State.Wander;

    [Header("Attack")]
    public float attackRange = 5f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    [Header("References")]
    public Transform player;           

    [Header("Параметры блуждания")]
    public float wanderRadius = 5f;       // Радиус в котором ищем точки
    public float wanderPointDelay = 3f;   // Пауза между сменой точек

    [Header("Параметры чувств")]
    public float sightRange = 12f;        // Дистанция "зрения"
    public float hearingForgetTime = 3f;  // Сколько секунд зомби помнит шум
    private PlayerMovement playerMovement;

    private NavMeshAgent agent;
    private Vector3 lastHeardPosition;
    private float lastHeardTime = -999f;
    private float nextWanderTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        PickNewWanderPoint();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (player == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (player != null && distanceToPlayer <= attackRange)
        {
            print(2);
            TryAttackPlayer();
        }
        print(distanceToPlayer);
        print(player);
        bool canSeePlayer = distanceToPlayer <= sightRange && HasLineOfSight();

        // Выбор состояния
        if (canSeePlayer)
        {
            currentState = State.Chase;
        }
        else if (Time.time - lastHeardTime <= hearingForgetTime)
        {
            currentState = State.Investigate;
        }
        else
        {
            currentState = State.Wander;
        }

        // Поведение по состояниям
        switch (currentState)
        {
            case State.Chase:
                agent.SetDestination(player.position);
                break;

            case State.Investigate:
                agent.SetDestination(lastHeardPosition);

                // Если дошёл до места шума — забываем его
                if (Vector3.Distance(transform.position, lastHeardPosition) < 0.5f)
                {
                    lastHeardTime = -999f;
                }
                break;

            case State.Wander:
                if (Time.time >= nextWanderTime || ReachedDestination())
                {
                    PickNewWanderPoint();
                }
                break;
        }

        // Тут можно потом добавить атаку:
        // if (distanceToPlayer <= attackRange) { ... }
    }

    

    private bool HasLineOfSight()
    {
        // Простая проверка прямой видимости до игрока
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = (player.position - origin).normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, sightRange))
        {
            return hit.transform == player;
        }

        return false;
    }

    private void TryAttackPlayer()
    {
        print(11);
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(attackDamage);
        }
        lastAttackTime = Time.time;
    }

    private bool ReachedDestination()
    {
        if (!agent.hasPath) return true;
        if (agent.pathPending) return false;

        return agent.remainingDistance <= agent.stoppingDistance;
    }


    private void PickNewWanderPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }

        nextWanderTime = Time.time + wanderPointDelay;
    }

    /// <summary>
    /// Вызывается, когда зомби слышит шум.
    /// </summary>
    public void OnHeardNoise(Vector3 noisePosition)
    {
        lastHeardPosition = noisePosition;
        lastHeardTime = Time.time;
    }
}

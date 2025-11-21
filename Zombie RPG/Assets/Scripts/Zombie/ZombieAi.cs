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

    [Header("Health")]
    public float maxHealth = 50f; // Базовое HP
    private float currentHealth;

    [Header("Attack")]
    public float attackRange = 2f; 
    [HideInInspector] public float attackDamage = 20f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime = 0f;

    [Header("References")]
    public Transform player;

    [Header("Параметры блуждания")]
    public float wanderRadius = 10f;
    public float wanderPointDelay = 4f;

    [Header("Параметры чувств")]
    public float sightRange = 15f;
    public float hearingForgetTime = 5f;
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
        currentHealth = maxHealth;
        PickNewWanderPoint();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Атака при близости (независимо от состояния)
        if (distanceToPlayer <= attackRange)
        {
            TryAttackPlayer();
        }

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
                if (Vector3.Distance(transform.position, lastHeardPosition) < 1f)
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
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 target = player.position + Vector3.up * 1f;
        Vector3 direction = (target - origin).normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, sightRange))
        {
            return hit.transform == player;
        }
        return false;
    }

    private void TryAttackPlayer()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(attackDamage);
        }
        lastAttackTime = Time.time;
    }

    private bool ReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    private void PickNewWanderPoint()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius * 2, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        nextWanderTime = Time.time + wanderPointDelay;
    }

    public void OnHeardNoise(Vector3 noisePosition)
    {
        lastHeardPosition = noisePosition;
        lastHeardTime = Time.time;
    }

    // Вызывается при попадании
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Зомби убит!");
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnZombieKilled();
        }
        Destroy(gameObject);
    }
}
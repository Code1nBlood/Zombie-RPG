using UnityEngine;
using UnityEngine.AI;
public class ZombieAi : MonoBehaviour, IEnemy
{
    public static event System.Action OnZombieKilled;
    public enum State
    {
        Wander,
        Chase,
        Investigate
    }

    

    public Animator animator;
    public State currentState = State.Wander;
    private float rotationSpeed = 360f;
    float speedAnimationAttack;
    private float aBL; //attackBaseLength
    private float aCS; //attackClipSpeed
    private float fAS; //finalAttackSpeed
    [Header("Health")]
    public float maxHealth = 50f; // Базовое HP
    public float currentHealth {get;private set;}
    [Header("Attack")]
    public float attackRange = 5f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    [Header("References")]
    public Transform player;           

    [Header("Параметры блуждания")]
    public float wanderRadius = 20f;       // Радиус в котором ищем точки
    public float wanderPointDelay = 4f;   // Пауза между сменой точек

    [Header("Параметры чувств")]
    public float sightRange = 25f;        // Дистанция "зрения"
    public float viewAngle = 120f;  
    public float hearingForgetTime = 3f;  // Сколько секунд зомби помнит шум
    private PlayerMovement playerMovement;

    private NavMeshAgent agent;
    private Vector3 lastHeardPosition;
    private float lastHeardTime = -999f;
    private float nextWanderTime;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.updateRotation = false;
        foreach(var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Punch")
            {
                aBL = clip.length;

                break;
            }
        }
        aCS = animator.speed;
        fAS = aBL/aCS; //получаем сколько длится кадр удара
        agent.autoBraking = false;
    }

    private void Start()
    {
        PickNewWanderPoint();
        Rotate();
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (player == null) return;
        bool canSeePlayer = CanSeePlayer();

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
                bool isAttackInProgress = Time.time < lastAttackTime + fAS;
                if (isAttackInProgress)
                {
                    // Если атака в процессе:
                    // ОСТАНАВЛИВАЕМ NavMeshAgent, чтобы зомби не двигался.
                    if (agent.isActiveAndEnabled)
                    {
                        agent.isStopped = true;
                    }
                }
                else
                {
                    if (agent.isActiveAndEnabled)
                    {
                        agent.isStopped = false;
                    }
                    if(Time.time - lastAttackTime< fAS) break;
                    if (canAttack())
                    {
                        TryAttackPlayer();
                    }
                    agent.SetDestination(player.position);
                }

                Rotate();
                break;

            case State.Investigate:
                agent.SetDestination(lastHeardPosition);
                Rotate();
                if (Vector3.Distance(transform.position, lastHeardPosition) < 0.5f)
                {
                    lastHeardTime = -999f;
                }
                break;

            case State.Wander:
                if (ReachedDestination())
                {
                    agent.ResetPath();

                    if (nextWanderTime <= 0f)
                        nextWanderTime = Time.time + wanderPointDelay;
                }

                if (nextWanderTime > 0f && Time.time >= nextWanderTime)
                {
                    PickNewWanderPoint();
                    nextWanderTime = 0f;
                }

                Rotate();
                break;
        }
        float speed = agent.velocity.magnitude;
        if (ReachedDestination()) speed = 0f;
        animator.SetFloat("Speed", speed);

        float agentSpeed = agent.speed;
        float animSpeed = 0f;
        if (agentSpeed > 0.01f)
        {
            animSpeed = speed / agent.speed;
        }

        animSpeed = Mathf.Clamp(animSpeed, 0f, 2f);

        animator.SetFloat("AnimSpeed", animSpeed);
    }

    private bool CanSeePlayer()
    {
        if(player == null)return false;

        Vector3 toPlayer = player.position-transform.position;
        toPlayer.y=0f;

        float distance = toPlayer.magnitude;
        if(distance>sightRange)return false;
        if(distance<0.001f) return false;

        Vector3 dirToPlayer = toPlayer.normalized;
        Vector3 forward = transform.forward;

        float angle = Vector3.Angle(forward, dirToPlayer);
        if(angle>viewAngle*0.5f) return false;
        return HasLineOfSight();
    }

    private void Rotate()
    {
        Vector3 dir;

        switch (currentState)
        {
            case State.Chase:
                if (player == null) return;
                dir = player.position - transform.position;
                break;

            case State.Investigate:
                dir = lastHeardPosition - transform.position;
                break;

            case State.Wander:
                if (agent.velocity.sqrMagnitude < 0.01f)
                    return;
                dir = agent.velocity;
                break;

            default:
                return;
        }

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();

        Quaternion targetRotate = Quaternion.LookRotation(dir);
        float maxDegrees = rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotate, maxDegrees);
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

    private bool canAttack()
    {
        if(Vector3.Distance(transform.position, player.position)>attackRange) return false;
        // if(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return false;
        if(Time.time-lastAttackTime<attackCooldown)return false;
        var state = animator.GetAnimatorTransitionInfo(0);
        if(state.IsName("Attack"))return false;
        return true;
    }

    private void TryAttackPlayer()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if(animator != null)
        {
            animator.SetTrigger("Attack");
        }
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(attackDamage);
        }
        lastAttackTime = Time.time;
    }

    private bool ReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance) return false;

        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.001f;
    }


    private void PickNewWanderPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    public int ExperienceReward => 10;
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        EnemyEvents.EnemyKilled(this);
        
        if (RoundManager.Instance != null)
            RoundManager.Instance.OnZombieKilled();

        Destroy(gameObject);
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

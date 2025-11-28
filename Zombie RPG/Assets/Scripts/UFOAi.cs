using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UFOFlyingEnemy : MonoBehaviour, IEnemy
{
    [Header("=== Основные параметры ===")]
    public float maxHealth = 100f;
    public float attackDamage = 25f;
    public float attackDistance = 3.5f;
    public float attackCooldown = 2f;
    public float detectionRange = 25f;
    public float idleSpeed = 4f;
    public float chaseSpeed = 8f;

    [Header("=== Парение ===")]
    public float hoverHeight = 6f;
    public float heightSmoothTime = 0.25f;

    [Header("=== Границы карты ===")]
    public Vector2 xBounds = new Vector2(-60, 60);
    public Vector2 zBounds = new Vector2(-60, 60);
    public float boundaryPush = 12f;

    [Header("=== Покачивание корпуса ===")]
    public float rollAmp = 10f;
    public float pitchAmp = 5f;
    public float yawAmp = 18f;
    public float swayFreq = 1f;

    // Ссылки (перетащи в инспекторе!)
    public Transform body;       // UFO_Body
    public Transform facePivot;  // пустышка FacePivot

    private CharacterController cc;
    private Transform player;
    private float health;
    private float lastAttackTime;
    private Vector3 moveVelocity;
    private float heightVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        health = maxHealth;

        // Автопоиск, если не назначил вручную
        if (body == null) body = transform.Find("UFO_Body");
        if (facePivot == null) facePivot = transform.Find("FacePivot");
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // === Выбор поведения ===
        if (dist <= attackDistance)
            Attack();
        else if (dist <= detectionRange)
            Chase();
        else
            Idle();

        KeepInBounds();
        MoveAndHover();
        SwayBody();
        FaceToPlayer();
    }

    void Chase() => moveVelocity = (player.position - transform.position).normalized * chaseSpeed;
    void Idle()  => moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, Time.deltaTime * 2f);

    void Attack()
    {
        moveVelocity = Vector3.zero;
        if (Time.time - lastAttackTime > attackCooldown)
        {
            if (player.TryGetComponent<PlayerMovement>(out var pm))
            {
                pm.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
                // небольшой отскок
                moveVelocity = (transform.position - player.position).normalized * 7f;
            }
        }
    }

    void MoveAndHover()
    {
        // 1. Высота парения
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 30f, LayerMask.GetMask("Ground")))
        {
            float targetY = hit.point.y + hoverHeight;
            float y = Mathf.SmoothDamp(transform.position.y, targetY, ref heightVel, heightSmoothTime);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        // 2. Горизонтальное движение
        Vector3 horizontalMove = moveVelocity * Time.deltaTime;
        horizontalMove.y = 0;
        cc.Move(horizontalMove);

        // 3. Поворот ВСЕГО объекта в сторону движения
        if (moveVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    void SwayBody()
    {
        float t = Time.time * swayFreq;
        float roll  = Mathf.Sin(t * 1.7f) * rollAmp;
        float pitch = Mathf.Sin(t * 1.4f + 0.8f) * pitchAmp;
        float yaw   = Mathf.Sin(t * 0.9f) * yawAmp;

        body.localRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    void FaceToPlayer()
    {
        if (facePivot == null || player == null) return;

        Vector3 worldDirToPlayer = player.position - transform.position;
        worldDirToPlayer.y = 0; 

        if (worldDirToPlayer.sqrMagnitude < 0.1f) return;

        Quaternion targetRot = Quaternion.LookRotation(worldDirToPlayer);
        facePivot.rotation = Quaternion.Slerp(facePivot.rotation, targetRot, 10f * Time.deltaTime);
    }

    void KeepInBounds()
    {
        Vector3 pos = transform.position;
        Vector3 push = Vector3.zero;

        if (pos.x < xBounds.x) push.x = boundaryPush;
        if (pos.x > xBounds.y) push.x = -boundaryPush;
        if (pos.z < zBounds.x) push.z = boundaryPush;
        if (pos.z > zBounds.y) push.z = -boundaryPush;

        moveVelocity += push;
    }
    [SerializeField] private int experienceReward = 50;
    public int ExperienceReward => experienceReward;

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0) Die();
    }

    public void Die()
    {
        EnemyEvents.EnemyKilled(this);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
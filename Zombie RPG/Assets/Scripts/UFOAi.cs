using UnityEngine;
using System.Collections;

public class UFOFlyingEnemy : MonoBehaviour, IEnemy
{
    [Header("Основные параметры ")]
    public float maxHealth = 100f;
    public float attackDamage = 25f;
    public float attackDistance = 10f;  
    public float attackCooldown = 3f;
    public float detectionRange = 25f;
    public float idleSpeed = 4f;
    public float chaseSpeed = 8f;

    [Header("Лазер")]
    public float laserChargeTime = 0.5f;
    public float laserDuration = 0.8f;
    public float laserWidth = 0.15f;
    public Color laserColorStart = Color.red;
    public Color laserColorEnd = new Color(1f, 0.3f, 0f, 1f); 
    [Tooltip("Материал для лазера")]
    public Material laserMaterial;
    
    [Header("Эффекты лазера")]
    public GameObject laserHitEffectPrefab;
    public GameObject laserChargeEffectPrefab;

    [Header("Парение")]
    public float hoverHeight = 6f;
    public float heightSmoothTime = 0.25f;

    [Header("Границы карты")]
    public Vector2 xBounds = new Vector2(-60, 60);
    public Vector2 zBounds = new Vector2(-60, 60);
    public float boundaryPush = 12f;

    [Header("Покачивание корпуса")]
    public float rollAmp = 10f;
    public float pitchAmp = 5f;
    public float yawAmp = 18f;
    public float swayFreq = 1f;

    [Header("Слои")]
    public LayerMask groundLayerMask = 1;

    [Header("Сквозь стены (звук)")]
    public LayerMask wallLayerMask;
    public float wallSoundCooldown = 0.25f;
    public float wallCheckRadius = 2f;        // Радиус проверки стен
    
    private float lastWallSoundTime = -999f;
    private bool wasInsideWall = false;       // Были ли внутри стены в прошлом кадре

    // Ссылки
    public Transform body;
    public Transform facePivot;
    public Transform laserOrigin;

    // Приватные переменные
    private Transform player;
    private float health;
    private float lastAttackTime;
    private Vector3 moveVelocity;
    private float heightVel;
    
    // Лазер
    private LineRenderer laserLine;
    private Light laserLight;
    private bool isAttacking = false;
    private GameObject currentChargeEffect;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        health = maxHealth;

        if (body == null) body = transform.Find("UFO_Body");
        if (facePivot == null) facePivot = transform.Find("FacePivot");
        if (laserOrigin == null) laserOrigin = facePivot;

        DisableAllCollisions();
        SetupLaser();
    }

    void DisableAllCollisions()
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.isTrigger = true;

        var cc = GetComponent<CharacterController>();
        if (cc != null) Destroy(cc);

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void SetupLaser()
    {
        GameObject laserObj = new GameObject("LaserBeam");
        laserObj.transform.SetParent(laserOrigin ?? facePivot);
        laserObj.transform.localPosition = Vector3.zero;

        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth * 0.5f;
        laserLine.useWorldSpace = true;
        laserLine.enabled = false;

        if (laserMaterial != null)
        {
            laserLine.material = laserMaterial;
        }
        else
        {
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(laserColorStart, 0f), 
                new GradientColorKey(laserColorEnd, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.7f, 1f) 
            }
        );
        laserLine.colorGradient = gradient;

        GameObject lightObj = new GameObject("LaserGlow");
        lightObj.transform.SetParent(laserObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        laserLight = lightObj.AddComponent<Light>();
        laserLight.type = LightType.Point;
        laserLight.color = laserColorStart;
        laserLight.intensity = 3f;
        laserLight.range = 5f;
        laserLight.enabled = false;
    }

    #region === ЗВУКИ ===
    private void PlayLaserFireSound()
    {
        AudioManager.Instance?.PlaySFX(SFXType.laserFireSound);
    }

    private void PlayLaserChargeSound()
    {
        AudioManager.Instance?.PlaySFX(SFXType.laserChargeSound);
    }

    private void PlayWallPassSound()
    {
        Debug.Log("🔊 PlayWallPassSound вызван!");
        AudioManager.Instance?.PlaySFX(SFXType.ufoPassWallSound);
    }
    #endregion 

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (!isAttacking)
        {
            if (dist <= attackDistance && CanSeePlayer())
                TryAttack();
            else if (dist <= detectionRange)
                Chase();
            else
                Idle();
        }

        KeepInBounds();
        MoveAndHover();
        SwayBody();
        FaceToPlayer();
        CheckWallPass();
    }

    void CheckWallPass()
    {
        bool isInsideWall = Physics.CheckSphere(
            transform.position, 
            wallCheckRadius, 
            wallLayerMask,
            QueryTriggerInteraction.Ignore 
        );

        if (isInsideWall && !wasInsideWall)
        {
            TryPlayWallSound();
        }

        wasInsideWall = isInsideWall;
    }

    void TryPlayWallSound()
    {
        if (Time.time - lastWallSoundTime < wallSoundCooldown) return;
        
        lastWallSoundTime = Time.time;
        PlayWallPassSound();
    }

    bool CanSeePlayer()
    {
        return true;
    }

    void Chase() => moveVelocity = (player.position - transform.position).normalized * chaseSpeed;
    void Idle() => moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, Time.deltaTime * 2f);

    void TryAttack()
    {
        moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, Time.deltaTime * 5f);

        if (Time.time - lastAttackTime > attackCooldown && !isAttacking)
        {
            StartCoroutine(LaserAttackSequence());
        }
    }

    IEnumerator LaserAttackSequence()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        Transform origin = laserOrigin ?? facePivot;

        PlayLaserChargeSound();

        if (laserChargeEffectPrefab != null)
        {
            currentChargeEffect = Instantiate(laserChargeEffectPrefab, origin.position, Quaternion.identity, origin);
        }

        laserLight.enabled = true;
        float chargeTimer = 0f;
        while (chargeTimer < laserChargeTime)
        {
            chargeTimer += Time.deltaTime;
            float pulse = Mathf.PingPong(chargeTimer * 10f, 1f);
            laserLight.intensity = pulse * 5f;
            laserLight.color = Color.Lerp(Color.white, laserColorStart, pulse);
            yield return null;
        }

        if (currentChargeEffect != null)
            Destroy(currentChargeEffect);

        PlayLaserFireSound();

        if (player != null && player.TryGetComponent<PlayerMovement>(out var pm))
        {
            pm.TakeDamage(attackDamage);
        }

        Vector3 hitPoint = player != null ? player.position + Vector3.up : origin.position + origin.forward * attackDistance;
        if (laserHitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(laserHitEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(hitEffect, 2f);
        }

        laserLine.enabled = true;
        laserLight.intensity = 8f;
        laserLight.range = 8f;

        float laserTimer = 0f;
        while (laserTimer < laserDuration)
        {
            laserTimer += Time.deltaTime;
            float progress = laserTimer / laserDuration;

            Vector3 startPos = origin.position;
            Vector3 endPos = player != null ? player.position + Vector3.up : hitPoint;
            
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, endPos);

            float fade = 1f - progress;
            laserLine.startWidth = laserWidth * fade;
            laserLine.endWidth = laserWidth * 0.5f * fade;
            laserLight.intensity = 8f * fade;
            float jitter = Mathf.Sin(Time.time * 50f) * 0.02f * fade;
            laserLine.startWidth += jitter;

            yield return null;
        }
        laserLine.enabled = false;
        laserLight.enabled = false;
        isAttacking = false;
    }

    void MoveAndHover()
    {
        Vector3 newPos = transform.position;

        if (Physics.Raycast(transform.position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundLayerMask))
        {
            float targetY = hit.point.y + hoverHeight;
            newPos.y = Mathf.SmoothDamp(transform.position.y, targetY, ref heightVel, heightSmoothTime);
        }
        else
        {
            newPos.y = Mathf.SmoothDamp(transform.position.y, hoverHeight, ref heightVel, heightSmoothTime);
        }

        float speedMult = isAttacking ? 0.1f : 1f;
        Vector3 horizontalMove = moveVelocity * speedMult * Time.deltaTime;
        horizontalMove.y = 0;
        newPos.x += horizontalMove.x;
        newPos.z += horizontalMove.z;

        transform.position = newPos;

        if (moveVelocity.sqrMagnitude > 0.1f && !isAttacking)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    void SwayBody()
    {
        if (body == null) return;

        float swayMult = isAttacking ? 0.2f : 1f;

        float t = Time.time * swayFreq;
        float roll = Mathf.Sin(t * 1.7f) * rollAmp * swayMult;
        float pitch = Mathf.Sin(t * 1.4f + 0.8f) * pitchAmp * swayMult;
        float yaw = Mathf.Sin(t * 0.9f) * yawAmp * swayMult;

        body.localRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    void FaceToPlayer()
    {
        if (facePivot == null || player == null) return;

        Vector3 dirToPlayer = player.position - transform.position;
        
        if (isAttacking)
        {
            if (dirToPlayer.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToPlayer.normalized);
                facePivot.rotation = Quaternion.Slerp(facePivot.rotation, targetRot, 15f * Time.deltaTime);
            }
        }
        else
        {
            dirToPlayer.y = 0;
            if (dirToPlayer.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
                facePivot.rotation = Quaternion.Slerp(facePivot.rotation, targetRot, 10f * Time.deltaTime);
            }
        }
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
        StopAllCoroutines();
        if (laserLine != null) laserLine.enabled = false;
        if (laserLight != null) laserLight.enabled = false;
        
        EnemyEvents.EnemyKilled(this);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        float xMin = xBounds.x;
        float xMax = xBounds.y;
        float zMin = zBounds.x;
        float zMax = zBounds.y;

        Vector3 center = new Vector3((xMin + xMax) * 0.5f, transform.position.y, (zMin + zMax) * 0.5f);
        Vector3 size   = new Vector3(Mathf.Abs(xMax - xMin), 0.2f, Mathf.Abs(zMax - zMin)); // тонкая "плашка"

        // заливка + контур
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);

        // Радиус проверки стен
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wallCheckRadius);

        Transform origin = laserOrigin ?? facePivot ?? transform;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(origin.position, origin.forward * attackDistance);
    }
}
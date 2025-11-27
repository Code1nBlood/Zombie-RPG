using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    Noise noiser;
    private float timeSlowing = 1.5f;
    private float multiplierSlowing = 0.35f;
    public CharacterController controller;
    private Coroutine slowCoroutine;
    public float speed = 12f; // Скорость ходьбы
    public float sprintSpeed = 16f; // Скорость бега
    public float gravity = -9.81f;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float jumpHeight = 3f;

    [Header("Health")]
    public float maxHealth = 100f; // Максимальное здоровье
    public float healthRegenRate = 0.5f; // Базовая скорость регенерации HP в секунду
    
    private float currentHealth;
    private float healthRegenModifier = 1f; // Множитель для бустов регенерации HP
    private float maxHealthModifier = 1f; // Множитель для бустов максимального HP

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f; // Скорость расхода стамины при беге
    public float staminaRegenRate = 15f; // Скорость восстановления стамины
    public float minStaminaToSprint = 10f; // Минимальное количество стамины для бега

    [Header("Rolling")]
    public float rollDuration = 0.2f; // Продолжительность деша
    public float rollSpeed = 40f;     // Скорость деша
    public float rollCooldown = 2f;   // Время перезарядки деша

    [Header("Camera Shake & Bob (для динамики)")]
    public Transform cameraToShake; // Назначьте Transform камеры в инспекторе (лучше child-объект камеры для тряски)
    public float walkBobAmount = 0.02f;  // Амплитуда боба при ходьбе
    public float sprintBobAmount = 0.04f; // Амплитуда боба при беге
    public float walkBobSpeed = 10f;     // Скорость боба при ходьбе
    public float sprintBobSpeed = 16f;   // Скорость боба при беге
    public float shakeAmount = 0.015f;   // Дополнительная тряска (Perlin noise)

    private float currentStamina;
    private bool isSprinting = false;
    private bool isRolling = false;
    private bool canRoll = true; // Для кулдауна
    private float rollTimer = 0f;

    private float bobTimer = 0f; // Таймер для боба и тряски

    Vector3 velocity;
    bool isGrounded;
    private Vector3 idleCamPos;

    void Start()
    {
        currentStamina = maxStamina;
        currentHealth = maxHealth;

        if (cameraToShake == null)
        {
            cameraToShake = Camera.main.transform;
        }
        noiser = GetComponent<Noise>();

        idleCamPos = cameraToShake.localPosition;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        if (Input.GetButtonDown("Jump") && isGrounded && !isRolling)
        {
            noiser.makeNoise(2);
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        RegenerateHealth();

        isSprinting = false;
        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift) && isGrounded && !isRolling && currentStamina > minStaminaToSprint)
        {
            noiser.makeNoise(1);
            isSprinting = true;
            currentSpeed = sprintSpeed;

            // Расход стамины при беге
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina < 0) currentStamina = 0;
        }
        else
        {
            // Восстановление стамины
            if (currentStamina < maxStamina && (!Input.GetKey(KeyCode.LeftShift) || !isGrounded || isRolling))
            {
                if (!isSprinting) 
                {
                     currentStamina += staminaRegenRate * Time.deltaTime;
                     if (currentStamina > maxStamina) currentStamina = maxStamina;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && !isRolling && canRoll)
        {
            if (currentStamina >= 20f ) 
            {

                StartCoroutine(Roll());
                noiser.makeNoise(2);
                currentStamina -= 20f; 
                if (currentStamina < 0) currentStamina = 0;
            }
        }

        // Управление кулдауном деша
        if (!canRoll)
        {
            rollTimer += Time.deltaTime;
            if (rollTimer >= rollCooldown)
            {
                canRoll = true;
                rollTimer = 0f;
            }
        }

        float x = 0f;
        float z = 0f;

        if (!isRolling)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
            if(x!=0 || z != 0)
            {
                noiser.makeNoise(0.5f);
            }
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * currentSpeed * Time.deltaTime);
        }
        HandleCameraShakeAndBob(x, z, currentSpeed, isSprinting);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCameraShakeAndBob(float x, float z, float currSpeed, bool sprinting)
    {
        if (cameraToShake == null || !isGrounded || isRolling) return;

        float horizSpeed = new Vector2(x, z).magnitude * currSpeed;
        if (horizSpeed < 0.1f)
        {
            cameraToShake.localPosition = Vector3.Lerp(cameraToShake.localPosition, idleCamPos, 8f * Time.deltaTime);
            return;
        }

        float bobAmount = sprinting ? sprintBobAmount : walkBobAmount;
        float bobSpeed = sprinting ? sprintBobSpeed : walkBobSpeed;

        bobTimer += Time.deltaTime * bobSpeed * (horizSpeed / speed);

        // Head bob (классический боб головы)
        float bobY = Mathf.Sin(bobTimer) * bobAmount;
        float bobX = Mathf.Cos(bobTimer * 2f) * bobAmount * 0.5f;

        // Дополнительная тряска (Perlin noise для реализма)
        float noiseX = (Mathf.PerlinNoise(bobTimer * 3f, 0f) * 2f - 1f) * shakeAmount;
        float noiseY = (Mathf.PerlinNoise(bobTimer * 3f + 50f, 0f) * 2f - 1f) * shakeAmount * 0.6f;

        Vector3 targetOffset = idleCamPos + new Vector3(bobX + noiseX, bobY + noiseY, 0f);

        cameraToShake.localPosition = Vector3.Lerp(cameraToShake.localPosition, targetOffset, 12f * Time.deltaTime);
    }


    //Получить текущие хп
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        slowDown(timeSlowing,multiplierSlowing);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die(); 
        }
    }

    private void slowDown(float time, float value)
    {
        if(slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        slowCoroutine = StartCoroutine(slowDownCoroutine(time,value));
    }

    private IEnumerator slowDownCoroutine(float time, float value)
    {
        float originalSpeed = speed;
        float originalSprintSpeed = sprintSpeed;

        speed*=value;
        sprintSpeed*=value;
        canRoll = false;

        yield return new WaitForSeconds(time);

        speed = originalSpeed;
        sprintSpeed = originalSprintSpeed;
        canRoll=true;

        slowCoroutine = null;

    }
    IEnumerator Roll()
    {
        isRolling = true;
        canRoll = false;

        // Направление деша 
        Vector3 rollDirection = transform.forward;
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(verticalInput) > Mathf.Abs(horizontalInput))
        {
            rollDirection = verticalInput > 0 ? transform.forward : -transform.forward;
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            rollDirection = horizontalInput > 0 ? transform.right : -transform.right;
        }

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            controller.Move(rollDirection.normalized * rollSpeed * Time.deltaTime);
            velocity.y = 0f; 

            elapsed += Time.deltaTime;
            yield return null;
        }

        isRolling = false;
    }

    private void RegenerateHealth()
    {
        // Регенерация HP
        if (currentHealth < maxHealth * maxHealthModifier)
        {
            // Регенерация = Базовая ставка * Множитель регенерации * Время
            currentHealth += healthRegenRate * healthRegenModifier * Time.deltaTime;

            // Ограничение, чтобы не превысить модифицированный максимум
            if (currentHealth > maxHealth * maxHealthModifier)
            {
                currentHealth = maxHealth * maxHealthModifier;
            }
        }
    }

    

    public void ApplyHealthBoost()
    {
        //Увеличение регенерации HP на 10% (множитель становится 1.1)
        healthRegenModifier += 0.1f;

        // 2. Увеличение максимального HP на 30
        maxHealth += 30f;
    }
    
    public void UseHealthPotion()
    {
        float healAmount = (maxHealth * maxHealthModifier) * 0.5f; 
        
        currentHealth += healAmount;
        
        if (currentHealth > maxHealth * maxHealthModifier) 
        {
            currentHealth = maxHealth * maxHealthModifier;
        }
    }


    // функция для получения текущей стамины UI
    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
    }

    public float GetHealthNormalized()
    {
        return currentHealth / (maxHealth * maxHealthModifier);
    }

    private void Die()
    {
        Debug.Log("Игрок мертв!");
        // логика проигрыша: экран смерти

    }

}
using UnityEngine;
using System.Collections; 

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 12f; // Скорость ходьбы
    public float sprintSpeed = 16f; // Скорость бега
    public float gravity = -9.81f;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float jumpHeight = 3f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f; // Скорость расхода стамины при беге
    public float staminaRegenRate = 15f; // Скорость восстановления стамины
    public float minStaminaToSprint = 10f; // Минимальное количество стамины для бега

    [Header("Rolling")]
    public float rollDuration = 0.8f; // Продолжительность подката
    public float rollSpeed = 15f;     // Скорость подката
    public float rollCooldown = 2f;   // Время перезарядки подката

    private float currentStamina;
    private bool isSprinting = false;
    private bool isRolling = false;
    private bool canRoll = true; // Для кулдауна
    private float rollTimer = 0f;

    Vector3 velocity;
    bool isGrounded;

    void Start()
    {
        currentStamina = maxStamina;
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
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        isSprinting = false;
        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift) && isGrounded && !isRolling && currentStamina > minStaminaToSprint)
        {
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
            if (currentStamina >= 20f) 
            {
                StartCoroutine(Roll());
                currentStamina -= 20f; 
                if (currentStamina < 0) currentStamina = 0;
            }
        }

        // Управление кулдауном подката
        if (!canRoll)
        {
            rollTimer += Time.deltaTime;
            if (rollTimer >= rollCooldown)
            {
                canRoll = true;
                rollTimer = 0f;
            }
        }

        if (!isRolling)
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * currentSpeed * Time.deltaTime);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    IEnumerator Roll() 
    {
        isRolling = true;
        canRoll = false; 

        // Направление подката 
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
            controller.Move(rollDirection * rollSpeed * Time.deltaTime);

            Vector3 rollMove = new Vector3(rollDirection.x, 0, rollDirection.z).normalized * rollSpeed * Time.deltaTime;
            controller.Move(rollMove);

            elapsed += Time.deltaTime;
            yield return null; 
        }

        isRolling = false; 
    }


    // функция для получения текущей стамины UI
    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
    }

}
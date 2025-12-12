using UnityEngine;
using System.Collections;

public class GunFireController : MonoBehaviour
{
    // --- ДАННЫЕ И ХАРАКТЕРИСТИКИ (ВМЕСТО WeaponData) ---
    // Настраиваются прямо в Inspector на объекте оружия
    [Header("Характеристики Оружия")]
    public float fireRate = 2f; // Выстрелов в секунду
    public int maxAmmoInClip = 10;
    public float damage = 10f;
    public float range = 100f;
    public float reloadTime = 2.3f; // Время перезарядки (вместо поиска длительности клипа)
    
    [Header("Аудио и Гильзы")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public GameObject shellCasingPrefab; // Префаб гильзы
    public float shellEjectionForce = 3f; // Сила выброса гильзы
    
    // --- Ссылки компонентов и состояния ---
    private Animator animator;
    private AudioSource audioSource; // Используем для звуков
    private int currentAmmo; 
    private float nextFireTime = 0f;
    private bool isReloading = false;
    
    // --- Ссылки сцены ---
    [Header("Ссылки Сцены")]
    public Camera fpsCamera; // Камера, с которой стреляем
    public Transform shellEjectionPoint; // Точка выброса гильзы
    public LayerMask hitMask; // Маска для Raycast
    
    // --- Константы Animator ---
    private const string FireStateName = "Shoot"; 
    private const string ReloadTriggerName = "Reload"; 

    void Awake()
    {
        // Получаем компоненты ОДИН раз в Awake
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>(); 
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();

        if (animator == null) Debug.LogError("Animator не найден!");
        if (audioSource == null) Debug.LogError("AudioSource не найден!");
        
        currentAmmo = maxAmmoInClip; 
    }
    
    // Метод, который вызывается в момент, когда гильза должна вылететь (через Animation Event)
    public void EjectCasing()
    {
        if (shellEjectionPoint == null || shellCasingPrefab == null)
        {
            Debug.LogWarning("Точка выброса или префаб гильзы не назначены!");
            return;
        }

        GameObject shell = Instantiate(
            shellCasingPrefab, 
            shellEjectionPoint.position, 
            shellEjectionPoint.rotation
        );

        ShellEjection shellScript = shell.GetComponent<ShellEjection>();
        if (shellScript != null)
        {
            // Здесь мы передаем силу из нового поля скрипта
            shellScript.Launch(shellEjectionForce);
        }
    }

    void Update()
    {
        // Блокируем стрельбу во время перезарядки
        if (isReloading) return;

        // Логика стрельбы (Fire1 - левая кнопка мыши)
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + 1f / fireRate; 
                Shoot();
            }
            else
            {
                // Автоматическая перезарядка при пустом магазине
                Reload(); 
            }
        }
        
        // Логика ручной перезарядки
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmoInClip)
        {
            Reload();
        }
    }

    private void Shoot()
    {
        if (fpsCamera == null)
        {
            Debug.LogWarning("GunFireController: не назначена fpsCamera!");
            return;
        }
        
        // 1. Анимация
        if (animator != null)
        {
            animator.Play(FireStateName, 0, 0f);
        }
        
        // 2. Звук (PlayOneShot, чтобы не прервать звук перезарядки)
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
        
        // 3. Raycast и патроны
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
        currentAmmo -= 1;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            // 1. ПРОВЕРКА ХЕДШОТА (Ищем HeadshotDetector на КОНКРЕТНОМ коллайдере)
            HeadshotDetector headDetector = hit.collider.GetComponent<HeadshotDetector>();

            if (headDetector != null)
            {
                // Если нашли HeadshotDetector, вызываем его метод 
                // - он сам рассчитает и применит умноженный урон.
                headDetector.ReceiveHit(damage);
            }
            else
            {
                // 2. ПРОВЕРКА ТЕЛА (Ищем ZombieAi на самом объекте или его родителе)
                ZombieAi zombie = hit.collider.GetComponentInParent<ZombieAi>();
                
                if (zombie != null)
                {
                    // Попадание в тело: наносим базовый урон.
                    zombie.TakeDamage(damage);
                    Debug.Log($"Попадание в тело. Оставшееся хп: {zombie.currentHealth}");
                }
            }
        }
    }

    private void Reload()
    {
        if (isReloading || currentAmmo == maxAmmoInClip) return;
        
        isReloading = true;
        
        // 1. Анимация
        if (animator != null)
        {
            animator.SetTrigger(ReloadTriggerName);
        }
        
        // 2. Звук перезарядки
        if (audioSource != null && reloadSound != null)
        {
            // Назначаем клип AudioSource и проигрываем его ОДИН раз
            audioSource.clip = reloadSound;
            audioSource.Play(); 
        }
        
        // 3. Запускаем корутину
        StartCoroutine(WaitForReloadFinish());
    }

    private IEnumerator WaitForReloadFinish()
    {
        // Используем жестко заданное время, чтобы избежать ошибок с GetClipDuration
        yield return new WaitForSeconds(reloadTime); 
        
        // Логика по завершению
        currentAmmo = maxAmmoInClip; 
        isReloading = false; 
        
        // ОСТАНАВЛИВАЕМ AudioSource, чтобы он освободился (если перезарядка была прервана)
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        Debug.Log($"Перезарядка завершена. Патронов: {currentAmmo}");
    }
    
    // Этот метод предотвращает MissingReferenceException, если объект будет уничтожен
    void OnDisable()
    {
        StopAllCoroutines(); 
    }
}
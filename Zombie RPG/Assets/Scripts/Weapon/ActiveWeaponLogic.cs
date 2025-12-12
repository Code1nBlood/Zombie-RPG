using UnityEngine;
using System.Collections;

public class ActiveWeaponLogic : MonoBehaviour
{
    // --- Ссылки, полученные извне ---
    private WeaponData data;
    // Можно хранить ссылку на менеджер, если нужна централизованная логика
    // private WeaponManager manager; 

    // --- Ссылки компонентов ---
    private Animator animator;
    private AudioSource gunShootSource;
    
    // --- Состояния ---
    private bool isReloading = false;
    private float nextFireTime = 0f;
    private int currentAmmo; 


    // --- Константы Animator ---
    // Убедитесь, что эти имена точно соответствуют вашему Animator Controller
    private const string FireStateName = "Shoot"; // Имя состояния выстрела
    private const string ReloadTriggerName = "Reload"; // Имя Trigger для перезарядки
    
    // --- Ссылки из внешнего скрипта Fire ---
    [Header("Внешние ссылки")]
    public Camera fpsCamera; // Камера, с которой стреляем
    public LayerMask hitMask; // Маска для Raycast
    public Noise playerNoise; // Система шума (если есть)
    [Header("Эффекты")]
    public Transform shellEjectionPoint;


    // ====================================================================
    // МЕТОД ИНИЦИАЛИЗАЦИИ: Вызывается WeaponManager
    // ====================================================================
    public void Initialize(WeaponData weaponData, WeaponManager weaponManager)
    {
        data = weaponData;
        // manager = weaponManager; 
        
        // 1. Установка начальных значений
        currentAmmo = data.maxAmmoInClip; 
        
        // 2. Улучшенное получение компонентов на объекте оружия
        // Используем GetComponentIn(Children) на случай, если они на дочернем объекте
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        gunShootSource = GetComponent<AudioSource>(); 
        if (gunShootSource == null) gunShootSource = GetComponentInChildren<AudioSource>();

        if (animator == null) Debug.LogError("Animator не найден на объекте оружия или его детях!", gameObject);
        if (gunShootSource == null) Debug.LogError("AudioSource не найден на объекте оружия или его детях!", gameObject);
        
        // 3. Настройка ресурсов (берем из ScriptableObject)
        if (animator != null && data.weaponAnimatorController != null)
        {
            animator.runtimeAnimatorController = data.weaponAnimatorController;
        }
        
    }

    public void EjectCasing()
    {
        if (shellEjectionPoint == null || data.shellCasingPrefab == null)
        {
            Debug.LogWarning("Не назначена точка выброса или префаб гильзы в данных!");
            return;
        }

        GameObject shell = Instantiate(
            data.shellCasingPrefab, 
            shellEjectionPoint.position, 
            shellEjectionPoint.rotation
        );

        ShellEjection shellScript = shell.GetComponent<ShellEjection>();
        if (shellScript != null)
        {
            shellScript.Launch(data.shellEjectionForce);
        }
        
        // Опционально: можно добавить звук лязга гильзы здесь.
    }

    // ====================================================================
    // ОБНОВЛЕНИЕ ИГРОВОЙ ЛОГИКИ
    // ====================================================================
    void Update()
    {
        // Логика стрельбы
        if (!isReloading && Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                // Устанавливаем таймер следующего выстрела, используя fireRate из WeaponData
                nextFireTime = Time.time + 1f / data.fireRate; 
                Shoot();
            }
            else
            {
                // Автоматическая перезарядка при пустом магазине
                Reload(); 
            }
        }
        
        // Логика ручной перезарядки
        if (!isReloading && Input.GetKeyDown(KeyCode.R) && currentAmmo < data.maxAmmoInClip)
        {
            Reload();
        }
    }

    // ====================================================================
    // МЕТОД СТРЕЛЬБЫ
    // ====================================================================
    private void Shoot()
    {
        if (fpsCamera == null)
        {
            Debug.LogWarning("ActiveWeaponLogic: не назначена fpsCamera!");
            return;
        }
        
        // 1. Анимация (Принудительный перезапуск для быстрой стрельбы)
        if (animator != null)
        {
            animator.Play(FireStateName, 0, 0f);
        }
        
        // 2. Звук
        if (gunShootSource != null && data.fireSound != null)
        {
            // !!! ИСПОЛЬЗУЕМ PlayOneShot, чтобы проиграть звук выстрела, 
            // НЕ МЕНЯЯ ОСНОВНОЙ КЛИП AudioSource. !!!
            gunShootSource.PlayOneShot(data.fireSound); 
        }
        
        // 3. Логика шума
        if (playerNoise != null)
        {
            playerNoise.makeNoise(data.damage); // Можно использовать урон для расчета шума
        }

        // 4. Raycast и урон (используем data.range и data.damage)
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
        currentAmmo -= 1; // Уменьшаем патроны

        if (Physics.Raycast(ray, out RaycastHit hit, data.range, hitMask))
        {
            // Получаем компонент на коллайдере, в который мы попали
            Collider hitCollider = hit.collider;
            
            // 1. ПРОВЕРКА ПОПАДАНИЯ В ГОЛОВУ
            if (hitCollider.TryGetComponent(out HeadshotDetector headDetector))
            {
                // data.damage - это базовый урон из вашего WeaponData
                headDetector.ReceiveHit(data.damage); 
            }
            // 2. ОБЫЧНОЕ ПОПАДАНИЕ (В ТЕЛО)
            else if (hitCollider.TryGetComponent(out ZombieAi zombie))
            {
                // Попадание в тело: передаем базовый урон без множителя.
                zombie.TakeDamage(data.damage);
            }
            // TODO: Здесь можно добавить логику для других объектов (стен, ящиков)
        }
        // TODO: Добавить эффекты выстрела (дульный огонь, гильзы)
    }

    // ====================================================================
    // МЕТОД ПЕРЕЗАРЯДКИ
    // ====================================================================
    private void Reload()
    {
        if (isReloading || currentAmmo == data.maxAmmoInClip) return;
        
        isReloading = true;
        
        // 1. Анимация перезарядки
        if (animator != null)
        {
            animator.SetTrigger(ReloadTriggerName);
        }
        
        // 2. ЗАПУСКАЕМ ЗВУК ПЕРЕЗАРЯДКИ
        if (gunShootSource != null && data.reloadSound != null)
        {
            // Назначаем клип AudioSource и проигрываем его ОДИН раз
            gunShootSource.clip = data.reloadSound;
            gunShootSource.Play(); 
        }
        
        // 3. Запускаем корутину для ожидания
        StartCoroutine(WaitForReloadFinish());
    }

    // Корутина для ожидания окончания анимации перезарядки
    private IEnumerator WaitForReloadFinish()
    {
        // 1. Получаем длительность клипа перезарядки
        float duration = GetClipDuration("Reload"); 
        
        // Ждем точное время, равное длительности клипа
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
        }
        else
        {
            // Если не смогли найти клип, ждем 2 секунды по умолчанию
            Debug.LogWarning("Не удалось найти длительность клипа 'Reload'. Ждем 2 секунды.");
            yield return new WaitForSeconds(2f);
        }
        
        // 2. Логика по завершению
        currentAmmo = data.maxAmmoInClip; 
        isReloading = false; 
        
        // ОСТАНАВЛИВАЕМ AudioSource, чтобы он освободился после перезарядки
        if (gunShootSource != null)
        {
            gunShootSource.Stop();
        }
            
        Debug.Log($"Перезарядка завершена. Патронов: {currentAmmo}");
    }
    
    // Вспомогательный метод для получения длительности клипа
    private float GetClipDuration(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) // Имя клипа должно быть точным
            {
                return clip.length;
            }
        }
        return 0f;
    }
    void OnDisable()
    {
        // Это вызывается перед уничтожением объекта.
        // Останавливает все корутины (например, WaitForReloadFinish),
        // предотвращая MissingReferenceException.
        StopAllCoroutines(); 
    }
}
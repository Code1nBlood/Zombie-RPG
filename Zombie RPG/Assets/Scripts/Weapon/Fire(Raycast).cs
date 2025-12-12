
using UnityEngine;

public class Fire : MonoBehaviour
{
    private int amountAmmo;
    private int maxAmmo;
    Animator animator;
    public AudioSource gunShoot;
    private const string fireTrigger = "Shoot";
    private bool Reloading = false;
    private const string reloadTrigger = "Reload";
    private float animationDuration;
    [Header("Шум выстрела")]
    public Noise playerNoise;           
    public float gunshotNoisePower = 3f; //  (3f = в 3 раза громче базы)

    [Header("Стрельба")]
    public float damage = 20f;   
    public float range = 50f;       // Дистанция луча
    public float fireRate = 2f;     // Выстрелов в секунду

    [Header("Ссылки")]
    public Camera fpsCamera;  
    public LayerMask hitMask;       // По каким слоям вообще можно стрелять

    private float nextFireTime = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Reloading)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Reload"))
            {
                if (!animator.IsInTransition(0))
                {
                    amountAmmo = maxAmmo;
                    Reloading = false;
                }
            }
        }
        if (!Reloading && Input.GetButton("Fire1") && Time.time >= nextFireTime && amountAmmo>0)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
        else if (!Reloading && amountAmmo == 0 || Input.GetKeyDown("r"))
        {
            Reload();
        }

    }

    private void Shoot()
    {
        if (fpsCamera == null)
        {
            Debug.LogWarning("Fire: не назначена fpsCamera");
            return;
        }
        if (playerNoise != null)
        {
            playerNoise.makeNoise(gunshotNoisePower);
        }

        animator.Play(fireTrigger, 0, 0f);
        if (gunShoot != null)
        {
            if (gunShoot.clip != null)
            {
                gunShoot.Play();
            }
        }

        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
        amountAmmo-=1;
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            print("Попадание");
            ZombieAi zombie = hit.collider.GetComponentInParent<ZombieAi>();
            print($"Попадание: Оставшееся хп {zombie.currentHealth}");
            if (zombie != null)
            {
                zombie.TakeDamage(damage);
            }

            // Тут потом добавить эффекты
        }
    }
    private void Reload()
    {
        if (!Reloading)
        {
            Reloading = true;
            animator.SetTrigger("Reload");
        }
    }
}

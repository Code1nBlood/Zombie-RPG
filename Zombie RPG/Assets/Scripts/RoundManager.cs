using UnityEngine;
using System.Collections;
using System;
using Unity.VisualScripting;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.SceneManagement;


public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Настройки раундов")]
    public float roundDuration = 180f; 
    public float breakDuration = 20f;  
    [Header("Префаб и спавн")]
    [SerializeField] private string spawnTag = "ZombieSpawn";
    [SerializeField] private string zombiePrefabPath = "Zombie_Male";
    private GameObject zombiePrefab;
    private Transform[] spawnPoints;

    [Header("Параметры зомби (база + прирост за раунд)")]
    [SerializeField] private float baseZombieSpeed = 2f;           // Базовая скорость 
    [SerializeField] private float speedIncrementPerRound = 0.3f;  // +0.3 за раунд
    [SerializeField] private float baseZombieDamage = 20f;       
    [SerializeField] private float damageIncrementPerRound = 5f;  
    [SerializeField] private int baseZombiesPerRound = 9;        
    [SerializeField] private int zombiesIncrementPerRound = 8;    

    [Header("Спавн-ритм")]
    [SerializeField] private float baseSpawnInterval = 2f;         // Интервал спавна (сек)
    [SerializeField] private float spawnIntervalDecrement = 0.1f;  // Уменьшение интервала за раунд

    // Состояние
    private int currentRound = 0;
    private float roundTimer;
    private float breakTimer;
    private bool isRoundActive = false;
    private bool isBreakActive = false;
    private int zombiesSpawnedThisRound = 0;
    private int zombiesAliveThisRound = 0; // Для досрочного конца раунда
    private Coroutine spawnCoroutine;
    private float currentZombieSpeed;
    private float currentZombieDamage;
    private float currentSpawnInterval;
    private int totalZombiesThisRound;

    // События для UI/саунд/эффектов
    public static event Action<int> OnRoundStart;
    public static event Action<int> OnRoundEnd;
    public static event Action OnBreakStart;
    public static event Action<float> OnTimerUpdate;
    public static event Action<int> OnRoundUpdate;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            // === ПОЛНЫЙ РЕСЕТ ПРИ ЗАХОДЕ В ИГРУ ===
            FindZombiePrefabAndSpawns();
            ResetRoundManager();
            StartNextRound();
            Debug.Log("RoundManager: Игра перезапущена!");
        }
        else if (scene.name == "MainMenu")
        {
            // Остановка всего в главном меню
            StopAllCoroutines();
            isRoundActive = false;
            isBreakActive = false;
            Debug.Log("RoundManager: Остановлен в главном меню");
        }
    }

    private void FindZombiePrefabAndSpawns()
    {
        zombiePrefab = Resources.Load<GameObject>(zombiePrefabPath);
        if (zombiePrefab == null)
        {
            Debug.LogError($"RoundManager: Префаб не найден по пути Resources/{zombiePrefabPath}!");
            return;
        }

        GameObject[] spawnGOs = GameObject.FindGameObjectsWithTag(spawnTag);
        spawnPoints = spawnGOs.Select(go => go.transform).ToArray();

        if (spawnPoints.Length == 0)
        {
            Debug.LogError($"RoundManager: Не найдено объектов с тегом '{spawnTag}'!");
        }
        else
        {
            Debug.Log($"RoundManager: Найдено {spawnPoints.Length} точек спавна с тегом '{spawnTag}'");
        }
    }

    private void ResetRoundManager()
    {
        StopAllCoroutines();
        currentRound = 0;
        roundTimer = 0f;
        breakTimer = 0f;
        isRoundActive = false;
        isBreakActive = false;
        zombiesSpawnedThisRound = 0;
        zombiesAliveThisRound = 0;
        spawnCoroutine = null;
        currentZombieSpeed = 0f;
        currentZombieDamage = 0f;
        currentSpawnInterval = 0f;
        totalZombiesThisRound = 0;
    }

    void Update()
    {
        OnRoundUpdate?.Invoke(currentRound);
    }

    void StartNextRound()
    {
        currentRound++;
        StartRound();
    }

    void StartRound()
    {
        isRoundActive = true;
        zombiesSpawnedThisRound = 0;
        zombiesAliveThisRound = 0;

        // Расчёт параметров для раунда
        currentZombieSpeed = baseZombieSpeed + (currentRound - 1) * speedIncrementPerRound;
        currentZombieDamage = baseZombieDamage + (currentRound - 1) * damageIncrementPerRound;
        totalZombiesThisRound = baseZombiesPerRound + (currentRound - 1) * zombiesIncrementPerRound;
        currentSpawnInterval = Mathf.Max(0.5f, baseSpawnInterval - (currentRound - 1) * spawnIntervalDecrement);

        OnRoundStart?.Invoke(currentRound);
        Debug.Log($"Раунд {currentRound} начался! Цель: {totalZombiesThisRound} зомби, Скорость: {currentZombieSpeed:F1}, Урон: {currentZombieDamage}, Интервал: {currentSpawnInterval:F1}с");

        StartCoroutine(RoundTimer());
    }

    IEnumerator RoundTimer()
    {
        roundTimer = roundDuration;
        spawnCoroutine = StartCoroutine(SpawnZombiesCoroutine());

        while (roundTimer > 0f)
        {
            if (!isRoundActive) yield break; 
            roundTimer -= Time.deltaTime;
            OnTimerUpdate?.Invoke(roundTimer);
            yield return null;
        }

        EndRound();
    }

    void EndRound()
    {
        isRoundActive = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        OnRoundEnd?.Invoke(currentRound);
        Debug.Log($"Раунд {currentRound} завершён! Перерыв {breakDuration}с");

        StartCoroutine(BreakTimer());
    }

    IEnumerator BreakTimer()
    {
        isBreakActive = true;
        OnBreakStart?.Invoke();
        breakTimer = breakDuration;

        while (breakTimer > 0f)
        {
            breakTimer -= Time.deltaTime;
            OnTimerUpdate?.Invoke(breakTimer);
            yield return null;
        }

        isBreakActive = false;
        StartNextRound();
    }

    IEnumerator SpawnZombiesCoroutine()
    {
        while (zombiesSpawnedThisRound < totalZombiesThisRound && isRoundActive)
        {
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            GameObject newZombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);

            // Настройка зомби под раунд
            ZombieAi zombieAi = newZombie.GetComponent<ZombieAi>();
            if (zombieAi != null)
            {
                zombieAi.attackDamage = currentZombieDamage; // Урон
            }

            NavMeshAgent agent = newZombie.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = currentZombieSpeed; // Скорость
            }

            zombiesSpawnedThisRound++;
            zombiesAliveThisRound++; // Счётчик живых

            yield return new WaitForSeconds(currentSpawnInterval);
        }
    }

    public void OnZombieKilled()
    {
        zombiesAliveThisRound--;
        if (isRoundActive && zombiesAliveThisRound <= 0)
        {
            EndRound(); 
            Debug.Log($"Все зомби убиты досрочно! Раунд {currentRound} завершён.");
        }
    }

    public void SkipToNextRound()
    {
        if (isRoundActive) EndRound();
        else if (isBreakActive) StartCoroutine(BreakTimer());
    }

    public int GetCurrentRound() => currentRound;
    public bool IsRoundActive() => isRoundActive;
    public bool IsBreakActive() => isBreakActive;
    public float GetTimeLeft() => isRoundActive ? roundTimer : (isBreakActive ? breakTimer : 0f);
}
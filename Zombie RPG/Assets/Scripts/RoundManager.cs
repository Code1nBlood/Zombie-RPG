using UnityEngine;
using System.Collections;
using System;
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
    [SerializeField] private float baseZombieSpeed = 2f;
    [SerializeField] private float speedIncrementPerRound = 0.3f;
    [SerializeField] private float baseZombieDamage = 20f;
    [SerializeField] private float damageIncrementPerRound = 5f;
    [SerializeField] private int baseZombiesPerRound = 9;
    [SerializeField] private int zombiesIncrementPerRound = 8;

    [Header("Спавн-ритм")]
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private float spawnIntervalDecrement = 0.1f;

    private int currentRound = 0;
    private float roundTimer;
    private float breakTimer;
    private bool isRoundActive = false;
    private bool isBreakActive = false;
    private int zombiesSpawnedThisRound = 0;
    private int zombiesAliveThisRound = 0;
    private Coroutine spawnCoroutine;
    private float currentZombieSpeed;
    private float currentZombieDamage;
    private float currentSpawnInterval;
    private int totalZombiesThisRound;

    //Общая статистика за игру ===
    private int totalZombiesKilled = 0;
    private int highestRoundReached = 0;

    // Свойства для статистики
    public int TotalZombiesKilled => totalZombiesKilled;
    public int CurrentRound => currentRound;
    public int HighestRoundReached => highestRoundReached;

    // События
    public static event Action<int> OnRoundStart;
    public static event Action<int> OnRoundEnd;
    public static event Action OnBreakStart;
    public static event Action<float> OnTimerUpdate;
    public static event Action<int> OnRoundUpdate;
    public static event Action<int> OnZombieKilledEvent; 

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
            FindZombiePrefabAndSpawns();
            ResetRoundManager();
            StartNextRound();
            Debug.Log("RoundManager: Игра перезапущена!");
        }
        else if (scene.name == "MainMenu")
        {
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
            Debug.Log($"RoundManager: Найдено {spawnPoints.Length} точек спавна");
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

        totalZombiesKilled = 0;
        highestRoundReached = 0;
    }

    void Update()
    {
        OnRoundUpdate?.Invoke(currentRound);
    }

    void StartNextRound()
    {
        currentRound++;
        
        if (currentRound > highestRoundReached)
        {
            highestRoundReached = currentRound;
        }
        
        StartRound();
    }

    void StartRound()
    {
        isRoundActive = true;
        zombiesSpawnedThisRound = 0;
        zombiesAliveThisRound = 0;

        currentZombieSpeed = baseZombieSpeed + (currentRound - 1) * speedIncrementPerRound;
        currentZombieDamage = baseZombieDamage + (currentRound - 1) * damageIncrementPerRound;
        totalZombiesThisRound = baseZombiesPerRound + (currentRound - 1) * zombiesIncrementPerRound;
        currentSpawnInterval = Mathf.Max(0.5f, baseSpawnInterval - (currentRound - 1) * spawnIntervalDecrement);

        OnRoundStart?.Invoke(currentRound);
        Debug.Log($"Раунд {currentRound} начался! Зомби: {totalZombiesThisRound}, Скорость: {currentZombieSpeed:F1}, Урон: {currentZombieDamage}");

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
        Debug.Log($"Раунд {currentRound} завершён! Всего убито за игру: {totalZombiesKilled}");

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

            ZombieAi zombieAi = newZombie.GetComponent<ZombieAi>();
            if (zombieAi != null)
            {
                zombieAi.attackDamage = currentZombieDamage;
            }

            NavMeshAgent agent = newZombie.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = currentZombieSpeed;
            }

            zombiesSpawnedThisRound++;
            zombiesAliveThisRound++;

            yield return new WaitForSeconds(currentSpawnInterval);
        }
    }

    // Вызывается когда зомби убит
    public void OnZombieKilled()
    {
        zombiesAliveThisRound--;
        totalZombiesKilled++;

        OnZombieKilledEvent?.Invoke(totalZombiesKilled);

        Debug.Log($"Зомби убит! Всего: {totalZombiesKilled}, Осталось в раунде: {zombiesAliveThisRound}");

        if (isRoundActive && zombiesAliveThisRound <= 0 && zombiesSpawnedThisRound >= totalZombiesThisRound)
        {
            EndRound();
            Debug.Log($"Все зомби убиты досрочно! Раунд {currentRound} завершён.");
        }
    }

    // Остановить раунды (вызывается при смерти игрока)
    public void StopRounds()
    {
        isRoundActive = false;
        isBreakActive = false;
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        StopAllCoroutines();
        Debug.Log($"RoundManager: Раунды остановлены. Финальная статистика - Убито: {totalZombiesKilled}, Раунд: {currentRound}");
    }


    //статистика для экрана смерти
    public (int zombiesKilled, int roundsSurvived) GetDeathStats()
    {
        int survivedRounds = isBreakActive ? currentRound : Mathf.Max(0, currentRound - 1);
        return (totalZombiesKilled, survivedRounds);
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
using UnityEngine;
using UnityEngine.Events;

public class ExperienceSystem : MonoBehaviour
{
    [Header("Настройки опыта")]
    public int baseExperiencePerZombie = 10; // Базовое количество очков за зомби
    public float experienceMultiplierPerRound = 0.5f; // Прирост очков за раунд
    
    [Header("События")]
    public UnityEvent<int> OnExperienceGained;
    public UnityEvent<int> OnLevelUp;
    
    private int currentExperience;
    private int currentLevel = 1;
    private float experienceMultiplier = 1f; // Множитель для бустов
    
    // Для UI
    [HideInInspector] public int experienceToNextLevel = 100;

    private void Start()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.OnRoundStart += OnRoundStarted;
            ZombieAi.OnZombieKilled += OnZombieKilled;
        }
        
        // Инициализация
        UpdateExperienceToNextLevel();
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.OnRoundStart -= OnRoundStarted;
            ZombieAi.OnZombieKilled -= OnZombieKilled;
        }
    }

    private void OnZombieKilled()
    {
        if (!RoundManager.Instance.IsRoundActive()) return;
        
        int currentRound = RoundManager.Instance.GetCurrentRound();
        int experienceForKill = CalculateExperienceForKill(currentRound);
        
        AddExperience(experienceForKill);
    }

    private int CalculateExperienceForKill(int roundNumber)
    {
        // Базовый опыт + прирост за раунд + бонус за множитель
        return Mathf.RoundToInt((baseExperiencePerZombie + (roundNumber - 1) * experienceMultiplierPerRound) * experienceMultiplier);
    }

    public void AddExperience(int amount)
    {
        currentExperience += amount;
        OnExperienceGained?.Invoke(amount);
        
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        if (currentExperience >= experienceToNextLevel)
        {
            currentLevel++;
            currentExperience -= experienceToNextLevel;
            OnLevelUp?.Invoke(currentLevel);
            
            UpdateExperienceToNextLevel();
        }
    }

    private void UpdateExperienceToNextLevel()
    {
        // Формула для расчета опыта до следующего уровня
        experienceToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(1.2f, currentLevel - 1));
    }

    private void OnRoundStarted(int roundNumber)
    {
        // Можно добавить логику для начисления бонусного опыта за начало раунда
    }

    // Для бустов опыта
    public void ApplyExperienceBoost(float multiplier, int durationInRounds)
    {
        // Запускаем корутину для сброса буста после окончания раундов
        StartCoroutine(ExperienceBoostRoutine(multiplier, durationInRounds));
    }

    private System.Collections.IEnumerator ExperienceBoostRoutine(float multiplier, int durationInRounds)
    {
        float originalMultiplier = experienceMultiplier;
        experienceMultiplier = multiplier;
        
        int currentRound = RoundManager.Instance.GetCurrentRound();
        int targetRound = currentRound + durationInRounds;
        
        while (RoundManager.Instance.GetCurrentRound() < targetRound)
        {
            yield return new WaitUntil(() => !RoundManager.Instance.IsRoundActive());
        }
        
        experienceMultiplier = originalMultiplier;
    }

    // Геттеры для UI
    public int GetCurrentExperience() => currentExperience;
    public int GetCurrentLevel() => currentLevel;
    public float GetExperienceProgress() => (float)currentExperience / experienceToNextLevel;
}
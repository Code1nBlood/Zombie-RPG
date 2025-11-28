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

    private void OnEnable()
    {
        EnemyEvents.OnEnemyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyEvents.OnEnemyKilled -= OnEnemyKilled;
    }

    private void OnEnemyKilled(IEnemy enemy)
    {
        if (enemy == null) return;
        if (!RoundManager.Instance?.IsRoundActive() ?? false) return;

        int round = RoundManager.Instance.GetCurrentRound();
        float roundBonus = (round - 1) * experienceMultiplierPerRound;
        int reward = Mathf.RoundToInt(enemy.ExperienceReward * (1f + roundBonus) * experienceMultiplier);

        AddExperience(reward);
    }

    public void AddExperience(int amount)
    {
        currentExperience += amount;
        OnExperienceGained?.Invoke(amount);
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentExperience >= experienceToNextLevel)
        {
            currentLevel++;
            currentExperience -= experienceToNextLevel;
            OnLevelUp?.Invoke(currentLevel);
            UpdateExperienceToNextLevel();
        }
    }

    private void UpdateExperienceToNextLevel()
    {
        experienceToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(1.2f, currentLevel - 1));
    }

        // Новый метод — безопасно меняет множитель
    public void SetExperienceMultiplier(float multiplier)
    {
        experienceMultiplier = multiplier;
        Debug.Log($"[ExperienceSystem] Множитель опыта изменён на: x{multiplier}");
    }



    // Геттеры для UI
    public int GetCurrentExperience() => currentExperience;
    public int GetCurrentLevel() => currentLevel;
    public float GetExperienceProgress() => (float)currentExperience / experienceToNextLevel;
}
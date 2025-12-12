using UnityEngine;

// Этот скрипт должен быть на коллайдере головы
public class HeadshotDetector : MonoBehaviour
{
    // Множитель урона только для головы (например, 3.0x)
    [Tooltip("Множитель базового урона при попадании в голову.")]
    public float headshotMultiplier = 3.0f; 

    // --- НОВОЕ ПОЛЕ ДЛЯ ЗВУКА ---
    [Header("Аудио")]
    [Tooltip("Звуковой клип, который проигрывается при хедшоте.")]
    public AudioClip headshotSound; 

    private ZombieAi parentZombie;
    
    private void Awake()
    {
        // Ищем компонент ZombieAi на родительском объекте
        parentZombie = GetComponentInParent<ZombieAi>();
        if (parentZombie == null)
        {
            Debug.LogError("HeadshotDetector не смог найти компонент ZombieAi на родительских объектах!");
        }
    }
    
    /// <summary>
    /// Этот метод вызывается из скрипта стрельбы, когда Raycast попадает в голову.
    /// </summary>
    /// <param name="baseDamage">Базовый урон, который наносит оружие.</param>
    public void ReceiveHit(float baseDamage)
    {
        // Рассчитываем итоговый урон
        float finalDamage = baseDamage * headshotMultiplier;
        
        // 1. ВОСПРОИЗВЕДЕНИЕ ЗВУКА ХЕДШОТА
        if (headshotSound != null)
        {
            // PlayClipAtPoint проигрывает клип в указанной позиции (позиция коллайдера головы) 
            // и с указанной громкостью (1.0f - полная громкость).
            AudioSource.PlayClipAtPoint(headshotSound, transform.position, 1.0f); 
        }

        // 2. Передаем итоговый урон основному скрипту Зомби
        if (parentZombie != null)
        {
            parentZombie.TakeDamage(finalDamage);
            Debug.Log($"Хедшот! Нанесено урона: {finalDamage}");
        }
    }
}
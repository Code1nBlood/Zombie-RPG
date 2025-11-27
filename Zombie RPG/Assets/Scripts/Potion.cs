using UnityEngine;

[CreateAssetMenu(fileName = "New Potion", menuName = "Inventory/Potion")]
public class Potion : ScriptableObject, IEffectProvider
{
    public string potionName;
    public string description;
    public PotionEffectType effectType;

    [Header("UI")]
    [TextArea] public string effectDescription = "Восстанавливает 50% здоровья";

    [Header("Параметры эффекта")]
    public float duration = 5f;
    public float healPercent = 0.5f;
    public float speedMultiplier = 1.5f;

    public Sprite icon;
    public Color color = Color.white;

    public enum PotionEffectType { Health, Speed, PoisonImmunity, Typhoon, Invincibility }

    // Сам создаёт свой эффект — 100% DIP и Open/Closed
    public IEffect CreateEffect()
    {
        return effectType switch
        {
            PotionEffectType.Health => new HealthPotionEffect(healPercent, 0f),
            PotionEffectType.Speed => new SpeedBoostEffect(speedMultiplier, duration),
            PotionEffectType.Invincibility => new InvincibilityEffect(duration),
            _ => null
        };
    }
}
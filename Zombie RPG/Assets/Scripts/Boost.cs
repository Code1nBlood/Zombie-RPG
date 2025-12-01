using UnityEngine;

[CreateAssetMenu(fileName = "New Boost", menuName = "Inventory/Boost")]
public class Boost : ScriptableObject, IEffectProvider
{
    public string boostName;
    public string description;
    public BoostEffectType effectType;

    [Header("UI")]
    [TextArea] public string effectDescription = "Удваивает получаемый опыт";

    [Header("Параметры эффекта")]
    public int experienceMultiplier = 2;

    public int rounds = 3;

    public Sprite icon;
    public Color color = Color.white;

    public enum BoostEffectType { Experience, HealthRegeneration }

    public IEffect CreateEffect()
    {
        return effectType switch
        {
            BoostEffectType.Experience => new ExperienceBoostEffect(rounds, experienceMultiplier),
            BoostEffectType.HealthRegeneration => new HealthRegenerationBoost(rounds),
            _ => null
        };
    }
}
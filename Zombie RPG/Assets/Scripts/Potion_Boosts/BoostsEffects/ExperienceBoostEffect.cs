using UnityEngine;

public class ExperienceBoostEffect : RoundBasedEffect
{
    private readonly float _multiplier;

    public ExperienceBoostEffect(float multiplier, int rounds)
    {
        Name = $"Опыт x{multiplier}";
        _multiplier = multiplier;
        RoundsRemaining = rounds;
    }

    public override void Apply(GameObject target)
    {
        base.Apply(target);

        var expSystem = Object.FindAnyObjectByType<ExperienceSystem>();
        if (expSystem != null)
        {
            // Используем внутреннее поле напрямую (или через метод — ниже)
            expSystem.SetExperienceMultiplier(_multiplier);
        }
    }

    public override void Remove(GameObject target)
    {
        var expSystem = Object.FindAnyObjectByType<ExperienceSystem>();
        if (expSystem != null)
        {
            expSystem.SetExperienceMultiplier(1f); // сбрасываем
        }
    }

    // Если хочешь — можно вызвать при окончании раунда
    public override void OnRoundEnd(GameObject target)
    {
        RoundsRemaining--;
        if (RoundsRemaining <= 0)
        {
            Remove(target);
        }
    }
}
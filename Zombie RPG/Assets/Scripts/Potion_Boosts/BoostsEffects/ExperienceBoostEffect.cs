using UnityEngine;

public class ExperienceBoostEffect : RoundBasedEffect
{
    private float _expMultiplier;
    private ExperienceSystem _expSystem;
    
    public ExperienceBoostEffect(float expMultiplier)
    {
        Name = "Буст опыта";
        _expMultiplier = expMultiplier;
    }
    
    public override void Apply(GameObject target)
    {
        base.Apply(target);
        _expSystem = target.GetComponent<ExperienceSystem>();
        
        if (_expSystem != null)
        {
            _expSystem.ApplyExperienceBoost(_expMultiplier, RoundsRemaining);
        }
    }
    
    public override void Remove(GameObject target)
    {
        // Очистка не нужна, так как ExperienceSystem сама управляет своим множителем
    }
    
    public override void OnRoundEnd(GameObject target)
    {
        base.OnRoundEnd(target);
    }
}
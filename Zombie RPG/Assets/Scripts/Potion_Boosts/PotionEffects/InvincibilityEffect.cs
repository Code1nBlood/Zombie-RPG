using UnityEngine;

public class InvincibilityEffect : TimedEffect
{
    private PlayerMovement _playerMovement;
    private float _originalHealthRegen;
    
    public InvincibilityEffect(float duration)
    {
        Name = "Бессмертие";
        Duration = duration;
    }
    
    public override void Apply(GameObject target)
    {
        base.Apply(target);
        _playerMovement = target.GetComponent<PlayerMovement>();
        
        if (_playerMovement != null)
        {
            _originalHealthRegen = _playerMovement.healthRegenModifier;
            _playerMovement.healthRegenModifier = 10f; // Максимальная регенерация
        }
    }
    
    public override void Remove(GameObject target)
    {
        if (_playerMovement != null)
        {
            _playerMovement.healthRegenModifier = _originalHealthRegen;
        }
    }
}
using UnityEngine;

public class HealthRegenerationBoost : RoundBasedEffect
{
    private float _regenBoost;
    private float _originalRegen;
    private PlayerMovement _playerMovement;
    
    public HealthRegenerationBoost(float regenBoost)
    {
        Name = "Регенерация здоровья";
        _regenBoost = regenBoost;
    }
    
    public override void Apply(GameObject target)
    {
        base.Apply(target);
        _playerMovement = target.GetComponent<PlayerMovement>();
        
        if (_playerMovement != null)
        {
            _originalRegen = _playerMovement.healthRegenRate;
            _playerMovement.healthRegenRate += _regenBoost;
        }
    }
    
    public override void Remove(GameObject target)
    {
        if (_playerMovement != null)
        {
            _playerMovement.healthRegenRate = _originalRegen;
        }
    }
}
using UnityEngine;

public class HealthPotionEffect : TimedEffect
{
    private float _healPercent;
    private float _maxHealth;
    private PlayerMovement _playerMovement;
    
    public HealthPotionEffect(float healPercent, float duration)
    {
        Name = "Восстановление здоровья";
        _healPercent = healPercent;
        Duration = duration;
    }
    
    public override void Apply(GameObject target)
    {
        base.Apply(target);
        _playerMovement = target.GetComponent<PlayerMovement>();
        
        if (_playerMovement != null)
        {
            _maxHealth = _playerMovement.maxHealth * _playerMovement.maxHealthModifier;
            float healAmount = _maxHealth * _healPercent;
            _playerMovement.currentHealth += healAmount;
            
            if (_playerMovement.currentHealth > _maxHealth)
                _playerMovement.currentHealth = _maxHealth;
        }
    }
    
    public override void Remove(GameObject target) { /* Не требуется отмена */ }
}
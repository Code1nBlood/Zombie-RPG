using UnityEngine;

public class SpeedBoostEffect : TimedEffect
{
    private float _speedMultiplier;
    private float _originalSpeed;
    private float _originalSprintSpeed;
    private PlayerMovement _playerMovement;
    
    public SpeedBoostEffect(float speedMultiplier, float duration)
    {
        Name = "Ускорение";
        _speedMultiplier = speedMultiplier;
        Duration = duration;
    }
    
    public override void Apply(GameObject target)
    {
        base.Apply(target);
        _playerMovement = target.GetComponent<PlayerMovement>();
        
        if (_playerMovement != null)
        {
            _originalSpeed = _playerMovement.speed;
            _originalSprintSpeed = _playerMovement.sprintSpeed;
            
            _playerMovement.speed *= _speedMultiplier;
            _playerMovement.sprintSpeed *= _speedMultiplier;
        }
    }
    
    public override void Remove(GameObject target)
    {
        if (_playerMovement != null)
        {
            _playerMovement.speed = _originalSpeed;
            _playerMovement.sprintSpeed = _originalSprintSpeed;
        }
    }
}
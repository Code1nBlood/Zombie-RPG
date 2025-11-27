using UnityEngine;

public abstract class TimedEffect : BaseEffect, ITimedEffect
{
    public float Duration { get; protected set; }
    public bool IsActive { get; protected set; }
    private float _elapsedTime;
    
    public void Tick(GameObject target, float deltaTime)
    {
        if (!IsActive) return;
        
        _elapsedTime += deltaTime;
        if (_elapsedTime >= Duration)
        {
            Remove(target);
            IsActive = false;
        }
    }
    
    public override void Apply(GameObject target)
    {
        base.Apply(target);
        IsActive = true;
        _elapsedTime = 0f;
    }
}
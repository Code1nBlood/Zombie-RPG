using UnityEngine;

public interface ITimedEffect : IEffect
{
    float Duration { get; }
    bool IsActive { get; }
    void Tick(GameObject target, float deltaTime);
}
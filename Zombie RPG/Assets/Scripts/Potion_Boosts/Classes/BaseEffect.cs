using UnityEngine;

public abstract class BaseEffect : IEffect
{
    public string Name { get; protected set; }
    protected GameObject _target;
    
    public virtual void Apply(GameObject target)
    {
        _target = target;
    }
    
    public virtual void Remove(GameObject target)
    {
        _target = null;
    }
}
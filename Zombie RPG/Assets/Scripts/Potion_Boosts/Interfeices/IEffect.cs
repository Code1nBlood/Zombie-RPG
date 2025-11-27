using UnityEngine;

public interface IEffect
{
    string Name { get; }
    void Apply(GameObject target);
    void Remove(GameObject target);
}

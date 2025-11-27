using UnityEngine;

public abstract class RoundBasedEffect : BaseEffect, IRoundBasedEffect
{
    public int RoundsRemaining { get; set; }
    
    public virtual void OnRoundEnd(GameObject target)
    {
        RoundsRemaining--;
        if (RoundsRemaining <= 0)
        {
            Remove(target);
        }
    }
}
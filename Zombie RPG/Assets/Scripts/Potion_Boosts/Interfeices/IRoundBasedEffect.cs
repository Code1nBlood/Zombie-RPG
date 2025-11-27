using UnityEngine;

public interface IRoundBasedEffect : IEffect
{
    int RoundsRemaining { get; set; }
    void OnRoundEnd(GameObject target);
}
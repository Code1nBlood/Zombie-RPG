using UnityEngine;

using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Gameplay/Effect System")]
public class EffectSystem : MonoBehaviour
{
    private readonly List<IEffect> _activeEffects = new List<IEffect>();
    private readonly List<ITimedEffect> _timedEffects = new List<ITimedEffect>();
    private readonly List<IRoundBasedEffect> _roundEffects = new List<IRoundBasedEffect>();

    public IReadOnlyList<IEffect> ActiveEffects => _activeEffects;

    // Применяет эффект (если он уже есть — не дублирует, если не разрешено)
    public void ApplyEffect(IEffect effect, GameObject target)
    {
        if (effect == null) return;

        if (_activeEffects.Any(e => e.GetType() == effect.GetType()))
        {
            Debug.Log($"[EffectSystem] Эффект {effect.Name} уже активен — стак не разрешён");
            return;
        }

        effect.Apply(target);

        _activeEffects.Add(effect);

        if (effect is ITimedEffect timed)
            _timedEffects.Add(timed);

        if (effect is IRoundBasedEffect roundBased)
            _roundEffects.Add(roundBased);

        Debug.Log($"[EffectSystem] Применён эффект: {effect.Name}");
    }

    // Удаляет эффект вручную (например, по кнопке)
    public void RemoveEffect(IEffect effect, GameObject target)
    {
        if (effect == null || !_activeEffects.Contains(effect)) return;

        effect.Remove(target);

        _activeEffects.Remove(effect);
        if (effect is ITimedEffect t) _timedEffects.Remove(t);
        if (effect is IRoundBasedEffect r) _roundEffects.Remove(r);

        Debug.Log($"[EffectSystem] Удалён эффект: {effect.Name}");
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = _timedEffects.Count - 1; i >= 0; i--)
        {
            var effect = _timedEffects[i];
            if (effect.IsActive)
            {
                effect.Tick(gameObject, deltaTime); // или передавай player
            }

            if (!effect.IsActive)
            {
                _timedEffects.RemoveAt(i);
                _activeEffects.Remove(effect);
            }
        }
    }

    // Вызывай это из GameManager, RoundManager и т.д.
    public void OnRoundEnd()
    {
        for (int i = _roundEffects.Count - 1; i >= 0; i--)
        {
            var effect = _roundEffects[i];
            effect.OnRoundEnd(gameObject);

            if (effect.RoundsRemaining <= 0)
            {
                effect.Remove(gameObject);
                _roundEffects.RemoveAt(i);
                _activeEffects.Remove(effect);
            }
        }
    }

    // Очистка при смерти/рестарте
    public void ClearAllEffects()
    {
        foreach (var effect in _activeEffects.ToList())
        {
            effect.Remove(gameObject);
        }
        _activeEffects.Clear();
        _timedEffects.Clear();
        _roundEffects.Clear();
    }
}
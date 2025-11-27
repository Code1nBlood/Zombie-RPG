using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(EffectSystem))]
public class EffectApplicator : MonoBehaviour
{
    [SerializeField] private GameObject player; // не обязательно заполнять вручную
    private EffectSystem _effectSystem;

    private void Awake()
    {
        _effectSystem = GetComponent<EffectSystem>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Когда зашла игровая сцена — ищем игрока
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log($"[EffectApplicator] Игрок найден: {player.name}");
            }
        }
    }

    public void ApplyPotion(Potion potion)
    {
        if (potion == null) return;
        if (player == null)
        {
            Debug.LogWarning("Попытка применить зелье, но игрок ещё не загружен!");
            return;
        }

        IEffect effect = potion.CreateEffect();
        if (effect != null)
        {
            _effectSystem.ApplyEffect(effect, player);
            Debug.Log($"Применено зелье: {potion.potionName}");
        }
    }

    public void ApplyBoost(Boost boost)
    {
        if (boost == null || player == null) return;

        IEffect effect = boost.CreateEffect();
        if (effect != null)
        {
            _effectSystem.ApplyEffect(effect, player);
            Debug.Log($"Активирован буст: {boost.boostName}");
        }
    }
}
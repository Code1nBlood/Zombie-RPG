using UnityEngine;
using System.Linq;

[DefaultExecutionOrder(-100)] // чтобы проснулся раньше всех
public class InventoryManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private EffectApplicator effectApplicator;

    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (effectApplicator == null)
            effectApplicator = FindAnyObjectByType<EffectApplicator>();

        if (effectApplicator == null)
            Debug.LogError("[InventoryManager] EffectApplicator не найден! Зелья и бусты НЕ будут работать!");
    }

    private void Start()
    {
        // При старте матча — сразу применяем все назначенные бусты
        ApplyAllAssignedBoosts();
    }

    // ====================================================================
    // ПОЛУЧЕНИЕ ДАННЫХ ИЗ СЛОТОВ
    // ====================================================================

    /// <summary>
    /// Возвращает зелье из указанного слота (0 или 1)
    /// </summary>
    public Potion GetPotionInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 1) return null;

        var name = InventoryData.Instance.currentData.potionSlotNames[slotIndex];
        if (string.IsNullOrEmpty(name)) return null;

        return InventoryData.Instance.GetPotionByName(name);
    }

    /// <summary>
    /// Использовать зелье из слота (например, по нажатию кнопки Q/E)
    /// </summary>
    public bool UsePotionFromSlot(int slotIndex)
    {
        var potion = GetPotionInSlot(slotIndex);
        if (potion == null) return false;

        effectApplicator?.ApplyPotion(potion);

        // Очищаем слот
        InventoryData.Instance.currentData.potionSlotNames[slotIndex] = null;

        // Уведомляем UI в игре (если оно есть)
        OnPotionUsed?.Invoke(slotIndex);

        return true;
    }

    /// <summary>
    /// Получить все активные бусты (включая оставшиеся раунды)
    /// </summary>
    public Boost[] GetActiveBoosts()
    {
        var boosts = new Boost[3];
        for (int i = 0; i < 3; i++)
        {
            var name = InventoryData.Instance.currentData.boostSlotNames[i];
            if (!string.IsNullOrEmpty(name))
                boosts[i] = InventoryData.Instance.GetBoostByName(name);
        }
        return boosts.Where(b => b != null).ToArray();
    }

    /// <summary>
    /// Сколько раундов осталось у буста в слоте
    /// </summary>
    public int GetBoostRoundsRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2) return 0;
        return InventoryData.Instance.currentData.boostMatchesRemaining[slotIndex];
    }

    // ====================================================================
    // ПРИМЕНЕНИЕ БУСТОВ
    // ====================================================================

    /// <summary>
    /// Вызывается в начале каждого нового раунда (или матча)
    /// </summary>
    public void ApplyAllAssignedBoosts()
    {
        for (int i = 0; i < 3; i++)
        {
            var boostName = InventoryData.Instance.currentData.boostSlotNames[i];
            if (string.IsNullOrEmpty(boostName)) continue;

            var boost = InventoryData.Instance.GetBoostByName(boostName);
            if (boost != null)
            {
                effectApplicator?.ApplyBoost(boost);
            }
        }
    }

    /// <summary>
    /// Уменьшить счётчик раундов у всех активных бустов
    /// Вызывается в конце каждого раунда
    /// </summary>
    public void DecrementBoostRounds()
    {
        bool anyBoostExpired = false;

        for (int i = 0; i < 3; i++)
        {
            if (InventoryData.Instance.currentData.boostMatchesRemaining[i] > 0)
            {
                InventoryData.Instance.currentData.boostMatchesRemaining[i]--;

                if (InventoryData.Instance.currentData.boostMatchesRemaining[i] <= 0)
                {
                    InventoryData.Instance.currentData.boostSlotNames[i] = null;
                    anyBoostExpired = true;
                    OnBoostExpired?.Invoke(i);
                }
            }
        }

        if (anyBoostExpired)
            OnBoostsChanged?.Invoke();
    }

    // ====================================================================
    // СБРОС ПО ОКОНЧАНИИ МАТЧА
    // ====================================================================

    /// <summary>
    /// Вызывать после окончания матча — очищает одноразовые слоты
    /// </summary>
    public void ResetAfterMatch()
    {
        // Зелья уже потрачены — они остаются пустыми
        // Бусты с 0 раундов уже очищены
        // Можно дополнительно очистить всё, если нужно:
        // for (int i = 0; i < 3; i++) InventoryData.Instance.currentData.boostSlotNames[i] = null;

        OnMatchEnded?.Invoke();
    }

    // ====================================================================
    // СОБЫТИЯ ДЛЯ UI В ИГРЕ (опционально)
    // ====================================================================

    public System.Action<int> OnPotionUsed;           // slotIndex
    public System.Action<int> OnBoostExpired;         // slotIndex
    public System.Action OnBoostsChanged;             // любой буст изменился
    public System.Action OnMatchEnded;
}
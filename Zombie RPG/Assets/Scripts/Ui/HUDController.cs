using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument document;

    [Header("Player References")]
    public PlayerMovement playerMovement;

    private ExperienceSystem expSystem;
    private RoundManager roundManager;

    // UI Elements
    private VisualElement healthBar;
    private VisualElement staminaBar;
    private VisualElement boostsPanel;
    private Label roundLabel;
    private Label timerLabel;
    private VisualElement experienceBar;
    private Label experienceLabel;

    private readonly VisualElement[] potionSlotsUI = new VisualElement[2];

    [SerializeField] private Texture2D defaultSlotBackground;

    private void Awake()
    {
        // Singleton protection
        var other = FindObjectsByType<HUDController>(FindObjectsSortMode.None);
        if (other.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        var root = document.rootVisualElement;

        // Находим элементы
        healthBar = root.Q<VisualElement>("HealthBar");
        staminaBar = root.Q<VisualElement>("StaminaBar");
        boostsPanel = root.Q<VisualElement>("BoostsPanel");
        roundLabel = root.Q<Label>("RoundLabel");
        timerLabel = root.Q<Label>("TimerLabel");
        experienceBar = root.Q<VisualElement>("ExperienceBar");
        experienceLabel = root.Q<Label>("ExperienceLabel");

        // Находим слоты зелий (1 и 2)
        var slot1 = root.Q<VisualElement>("Slot1");
        var slot2 = root.Q<VisualElement>("Slot2");
        if (slot1 != null) potionSlotsUI[0] = slot1;
        if (slot2 != null) potionSlotsUI[1] = slot2;

        // Подписываемся на опыт
        expSystem = FindFirstObjectByType<ExperienceSystem>();
        if (expSystem != null)
        {
            expSystem.OnExperienceGained.AddListener(OnExperienceGained);
            expSystem.OnLevelUp.AddListener(OnLevelUp);
        }

        roundManager = RoundManager.Instance;

        // Инициализация UI слотов зелий
        UpdateAllPotionSlots();
        UpdateBoostsDisplay();

        // Подписываемся на события нового InventoryManager
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPotionUsed += OnPotionUsedInSlot;
            InventoryManager.Instance.OnBoostExpired += OnBoostExpired;
            InventoryManager.Instance.OnBoostsChanged += UpdateBoostsDisplay;
            InventoryManager.Instance.OnMatchEnded += OnMatchEnded;
        }

        // При старте матча — сразу обновляем всё
        Invoke(nameof(DelayedInit), 0.1f);
    }

    private void DelayedInit()
    {
        UpdateAllPotionSlots();
        UpdateBoostsDisplay();
    }

    private void Update()
    {
        if (playerMovement != null)
        {
            UpdateHealthBar();
            UpdateStaminaBar();
        }

        if (roundManager != null && roundLabel != null && timerLabel != null)
        {
            roundLabel.text = $"Раунд: {roundManager.GetCurrentRound()}";

            float timeLeft = roundManager.GetTimeLeft();
            string prefix = roundManager.IsBreakActive() ? "Перерыв: " : "";
            timerLabel.text = prefix + FormatTime(timeLeft);
        }

        HandleHotbarInput();
    }

    private void HandleHotbarInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Q))
            UsePotionInSlot(0);

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.E))
            UsePotionInSlot(1);
    }

    private void UsePotionInSlot(int slotIndex)
    {
        InventoryManager.Instance?.UsePotionFromSlot(slotIndex);
    }

    // ====================================================================
    // СОБЫТИЯ ОТ INVENTORY MANAGER
    // ====================================================================

    private void OnPotionUsedInSlot(int slotIndex)
    {
        UpdatePotionSlotVisual(slotIndex);
    }

    private void OnBoostExpired(int slotIndex)
    {
        UpdateBoostsDisplay();
    }

    private void OnMatchEnded()
    {
        // Можно анимировать исчезновение бустов или сброс UI
        UpdateBoostsDisplay();
    }

    // ====================================================================
    // ОБНОВЛЕНИЕ UI
    // ====================================================================

    private void UpdateAllPotionSlots()
    {
        for (int i = 0; i < 2; i++)
            UpdatePotionSlotVisual(i);
    }

    private void UpdatePotionSlotVisual(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 2 || potionSlotsUI[slotIndex] == null) return;

        var slot = potionSlotsUI[slotIndex];
        slot.Clear();

        var potion = InventoryManager.Instance?.GetPotionInSlot(slotIndex);

        if (potion != null && potion.icon != null)
        {
            slot.style.backgroundImage = new StyleBackground(potion.icon);
            slot.style.backgroundColor = Color.clear;
        }
        else
        {
            // Пустой слот
            slot.style.backgroundImage = defaultSlotBackground != null
                ? new StyleBackground(defaultSlotBackground)
                : new StyleBackground((Texture2D)null);

            slot.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.7f);

            var label = new Label((slotIndex + 1).ToString())
            {
                style =
                {
                    fontSize = 20,
                    color = Color.white,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    alignSelf = Align.Center
                }
            };
            slot.Add(label);
        }
    }

    private void UpdateBoostsDisplay()
    {
        if (boostsPanel == null) return;

        boostsPanel.Clear();

        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < 3; i++)
        {
            var boostName = InventoryData.Instance.currentData.boostSlotNames[i];
            if (string.IsNullOrEmpty(boostName)) continue;

            var boost = InventoryData.Instance.GetBoostByName(boostName);
            if (boost == null) continue;

            int remaining = InventoryManager.Instance.GetBoostRoundsRemaining(i);
            if (remaining <= 0) continue;

            var container = new VisualElement();
            container.AddToClassList("boost-item");

            var icon = new VisualElement();
            icon.AddToClassList("boost-icon");

            if (boost.icon != null)
                icon.style.backgroundImage = new StyleBackground(boost.icon);
            else
                icon.style.backgroundColor = boost.color;

            var label = new Label($"{remaining}")
            {
                style =
                {
                    fontSize = 12,
                    color = Color.white,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginTop = -6
                }
            };

            container.Add(icon);
            container.Add(label);
            boostsPanel.Add(container);
        }
    }

    // ====================================================================
    // ОПЫТ
    // ====================================================================

    private void OnExperienceGained(int amount) => UpdateExperienceUI();
    private void OnLevelUp(int newLevel) => UpdateExperienceUI();

    private void UpdateExperienceUI()
    {
        if (expSystem == null || experienceBar == null || experienceLabel == null) return;

        float progress = expSystem.GetExperienceProgress();
        experienceBar.style.width = Length.Percent(progress * 100f);

        int level = expSystem.GetCurrentLevel();
        int current = expSystem.GetCurrentExperience();
        int toNext = expSystem.experienceToNextLevel;

        experienceLabel.text = $"Lv.{level} ({current}/{toNext})";
    }

    // ====================================================================
    // ЗДОРОВЬЕ И СТАМИНА
    // ====================================================================

    private void UpdateHealthBar()
    {
        if (healthBar == null || playerMovement == null) return;
        float hp = playerMovement.GetHealthNormalized();
        healthBar.style.width = Length.Percent(hp * 100f);
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar == null || playerMovement == null) return;
        float st = playerMovement.GetStaminaNormalized();
        staminaBar.style.width = Length.Percent(st * 100f);
    }

    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:D2}:{seconds:D2}";
    }

    // ====================================================================
    // ПУБЛИЧНЫЕ МЕТОДЫ
    // ====================================================================

    public void HideHUD() => document.rootVisualElement.style.display = DisplayStyle.None;
    public void ShowHUD() => document.rootVisualElement.style.display = DisplayStyle.Flex;

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPotionUsed -= OnPotionUsedInSlot;
            InventoryManager.Instance.OnBoostExpired -= OnBoostExpired;
            InventoryManager.Instance.OnBoostsChanged -= UpdateBoostsDisplay;
            InventoryManager.Instance.OnMatchEnded -= OnMatchEnded;
        }

        if (expSystem != null)
        {
            expSystem.OnExperienceGained.RemoveListener(OnExperienceGained);
            expSystem.OnLevelUp.RemoveListener(OnLevelUp);
        }
    }
}
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    public UIDocument document;

    public PlayerMovement playerMovement;
    

    private VisualElement healthBar;
    private VisualElement staminaBar;
    private VisualElement boostsPanel;

    private readonly VisualElement[] hotbarSlots = new VisualElement[5];
    private readonly VisualElement[] potionSlotsUI = new VisualElement[2];
    
    [SerializeField] private Texture2D defaultSlotIcon;

    void Start()
    {
        VisualElement root = document.rootVisualElement;
        healthBar = root.Q<VisualElement>("HealthBar");
        staminaBar = root.Q<VisualElement>("StaminaBar");
        boostsPanel = root.Q<VisualElement>("BoostsPanel");

        InitializeHotbar(root);

        if (InventoryManager.Instance != null) 
        {
            // Проверяем, есть ли уже назначенные зелья 
            Potion[] assignedPotions = InventoryManager.Instance.GetAssignedPotions();
            for (int i = 0; i < assignedPotions.Length; i++)
            {
                // Если assignedPotions[i] == null, вызывается логика пустого слота (с номером).
                OnPotionAssignedToSlot(assignedPotions[i], i);
            }
            UpdateBoostsDisplay();

            InventoryManager.Instance.OnPotionAssigned += OnPotionAssignedToSlot;
            InventoryManager.Instance.OnBoostAssigned += OnBoostAssignedToSlot;
        }
        else
        {
            Debug.LogError("HUDController: Inventory Manager Instance is missing! Make sure the Singleton setup is correct.");
        }

        if (playerMovement == null)
        {
             playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            Debug.LogError("HUDController: Player Movement reference is missing!");
        }
    }

    private void InitializeHotbar(VisualElement root)
    {
        VisualElement hotbar = root.Q<VisualElement>("Hotbar");
        if (hotbar != null)
        {
            // Получаем все 5 слотов из UXML
            for (int i = 0; i < 5; i++)
            {
                hotbarSlots[i] = hotbar.Q<VisualElement>($"Slot{i + 1}");
                UpdateSlotVisual(hotbarSlots[i], null); // Изначально пустые
            }
            
            // Первые 2 слота - это слоты зелий
            potionSlotsUI[0] = hotbarSlots[0];
            potionSlotsUI[1] = hotbarSlots[1];
        }
    }

    void Update()
    {
        if (playerMovement != null)
        {
            UpdateHealthBar();
            UpdateStaminaBar();
        }
        
        HandleHotbarInput();
    }
    
    private void HandleHotbarInput()
    {
        // Клавиша '1' для первого слота зелий (индекс 0)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UsePotionInHotbarSlot(0);
        }
        
        // Клавиша '2' для второго слота зелий (индекс 1)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UsePotionInHotbarSlot(1);
        }
    }

    private void UsePotionInHotbarSlot(int slotIndex)
    {
        if (InventoryManager.Instance != null && slotIndex < potionSlotsUI.Length)
        {
            InventoryManager.Instance.UsePotionFromSlot(slotIndex);
        }
    }

    // --- ОБРАБОТКА СОБЫТИЙ ---

    // Вызывается, когда зелье назначено/удалено в InventoryManager
    private void OnPotionAssignedToSlot(Potion potion, int index)
    {
        if (index < potionSlotsUI.Length && index >= 0)
        {
            UpdateSlotVisual(potionSlotsUI[index], potion);
        }
    }
    
    private void OnBoostAssignedToSlot(Boost boost, int index)
    {
        UpdateBoostsDisplay();
    }

    // --- ВИЗУАЛЬНОЕ ОБНОВЛЕНИЕ ---

    private void UpdateSlotVisual(VisualElement slotElement, Potion potion)
    {
        if (slotElement == null) return;

        slotElement.Clear();
        slotElement.style.backgroundImage = new StyleBackground((Texture2D)null);

        // --- DEBUG LOG 1: Проверка наличия предмета ---
        Debug.Log($"[HUD Debug 1/4] Обновление слота {slotElement.name}. Зелье: {(potion != null ? potion.potionName : "NULL (Пустой Слот)")}");

        if (potion != null)
        {
            if (potion.icon != null)
            {

                if (potion.icon.texture != null)
                {

                    slotElement.style.backgroundImage = new StyleBackground(potion.icon);
                    slotElement.style.backgroundColor = Color.clear;

                    if (slotElement.resolvedStyle.backgroundImage.sprite != null)
                    {
                        Debug.Log($"[HUD Debug SUCCESS] Иконка УСПЕШНО установлена на {slotElement.name}.");
                    }
                    else
                    {
                        Debug.LogError($"[HUD Debug FAIL] Иконка не отобразилась, хотя все данные верны. Конфликты.");
                    }

                }
                else
                {
                    // --- DEBUG LOG ОШИБКА ИМПОРТА ---
                    Debug.LogError($"[HUD Debug ОШИБКА ИМПОРТА] Зелье: {potion.potionName} имеет ссылку на Sprite, НО его базовая текстура равна NULL. **ПРОВЕРЬТЕ НАСТРОЙКИ ТЕКСТУРЫ** (Texture Type должен быть Sprite (2D and UI)).");
                    slotElement.style.backgroundColor = potion.color;
                }
            }
            else
            {
                // --- DEBUG LOG 4: Ссылка на Иконку NULL ---
                Debug.LogWarning($"[HUD Debug ПРЕДУПРЕЖДЕНИЕ] Зелье: {potion.potionName} в слоте, но поле 'icon' в ScriptableObject ПУСТОЕ (NULL).");
                // Запасной вариант: цвет
                slotElement.style.backgroundColor = potion.color;
            }
        }
        else
        {

            slotElement.style.backgroundImage = defaultSlotIcon != null
                ? new StyleBackground(defaultSlotIcon)
                : new StyleBackground((Texture2D)null);

            slotElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            string slotNumber = "";
            if (slotElement == hotbarSlots[0]) slotNumber = "1";
            else if (slotElement == hotbarSlots[1]) slotNumber = "2";

            if (!string.IsNullOrEmpty(slotNumber))
            {
                slotElement.Add(new Label(slotNumber)
                {
                    style = {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = Color.white,
                    fontSize = 20,
                    alignSelf = Align.Center
                }
                });
            }
        }
    }


    private void UpdateBoostsDisplay()
    {
        if (boostsPanel == null || InventoryManager.Instance == null) return;

        boostsPanel.Clear();
        
        Boost[] assignedBoosts = InventoryManager.Instance.GetAssignedBoosts();
        
        for (int i = 0; i < assignedBoosts.Length; i++)
        {
            Boost boost = assignedBoosts[i];
            
            if (boost != null)
            {
                int remainingMatches = InventoryManager.Instance.GetBoostMatchesRemaining(i); 
                
                if (remainingMatches <= 0) continue; 
                
                VisualElement boostContainer = new VisualElement();
                boostContainer.AddToClassList("active-boost-container"); 

                VisualElement iconElement = new VisualElement();
                iconElement.AddToClassList("boost-icon");

                if (boost.icon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(boost.icon);
                    iconElement.style.backgroundColor = Color.clear;
                }
                else
                {
                    iconElement.style.backgroundColor = boost.color; 
                }
                
                Label durationLabel = new Label 
                {
                    text = $"Осталось матчей: {remainingMatches}",
                };
                durationLabel.AddToClassList("boost-duration-label");
                
                boostContainer.Add(iconElement);
                boostContainer.Add(durationLabel);
                boostsPanel.Add(boostContainer);
            }
        }
    }

    private void UpdateHealthBar()
    {
        float normalizedHealth = playerMovement.GetHealthNormalized();

        healthBar.style.width = new StyleLength(new Length(normalizedHealth * 100, LengthUnit.Percent));
    }

    private void UpdateStaminaBar()
    {
        float normalizedStamina = playerMovement.GetStaminaNormalized();

        staminaBar.style.width = new StyleLength(new Length(normalizedStamina * 100, LengthUnit.Percent));
    }
}
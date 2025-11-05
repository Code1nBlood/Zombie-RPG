using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    public UIDocument document;

    public PlayerMovement playerMovement;
    

    private VisualElement healthBar;
    private VisualElement staminaBar;

    private readonly VisualElement[] hotbarSlots = new VisualElement[5];
    private readonly VisualElement[] potionSlotsUI = new VisualElement[2];
    
    [SerializeField] private Texture2D defaultSlotIcon;

    void Start()
    {
        VisualElement root = document.rootVisualElement;
        healthBar = root.Q<VisualElement>("HealthBar");
        staminaBar = root.Q<VisualElement>("StaminaBar");

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
            InventoryManager.Instance.OnPotionAssigned += OnPotionAssignedToSlot;
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
        
        // Клавиши 3, 4, 5 пока не используются для зелий
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

    // --- ВИЗУАЛЬНОЕ ОБНОВЛЕНИЕ ---

    private void UpdateSlotVisual(VisualElement slotElement, Potion potion)
{
    if (slotElement == null) return;
    
    // Очищаем предыдущее содержимое и сбрасываем фон
    slotElement.Clear(); 
    slotElement.style.backgroundImage = new StyleBackground((Texture2D)null);
    
    // --- DEBUG LOG 1: Проверка наличия предмета ---
    Debug.Log($"[HUD Debug 1/4] Обновление слота {slotElement.name}. Зелье: {(potion != null ? potion.potionName : "NULL (Пустой Слот)")}");

    if (potion != null)
    {
        // 1. Проверяем наличие Sprite
        if (potion.icon != null)
        {
            // --- DEBUG LOG 2: Спрайт найден ---
            Debug.Log($"[HUD Debug 2/4] Зелье: {potion.potionName}. Спрайт найден: {potion.icon.name}.");

            // Проверка: Спрайт должен иметь рабочую текстуру
            if (potion.icon.texture != null)
            {
                // --- DEBUG LOG 3: Текстура рабочая ---
                Debug.Log($"[HUD Debug 3/4] Текстура спрайта рабочая. Попытка установки фона.");
                
                // Установка Sprite как фона
                slotElement.style.backgroundImage = new StyleBackground(potion.icon);
                slotElement.style.backgroundColor = Color.clear;
                
                // --- DEBUG LOG 4: ЗАКЛЮЧИТЕЛЬНАЯ ПРОВЕРКА ---
                // Проверяем, удалось ли UI Toolkit применить фон.
                if (slotElement.resolvedStyle.backgroundImage.sprite != null)
                {
                     Debug.Log($"[HUD Debug SUCCESS] Иконка УСПЕШНО установлена на {slotElement.name}.");
                }
                else
                {
                     // Это срабатывает, если все данные есть, но UI Toolkit отказывается рисовать.
                     Debug.LogError($"[HUD Debug FAIL] Иконка не отобразилась, хотя все данные верны. ВЕРСИЯ UNITY или CSS конфликты.");
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
        // ... (логика для пустого слота с цифрой)
        
        // Фон для пустого слота
        slotElement.style.backgroundImage = defaultSlotIcon != null 
            ? new StyleBackground(defaultSlotIcon) 
            : new StyleBackground((Texture2D)null);
            
        slotElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        
        // Добавляем цифру в качестве дочернего элемента
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
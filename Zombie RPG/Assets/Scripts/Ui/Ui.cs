using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;

public class Ui : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset contractsUxml;
    [SerializeField] private VisualTreeAsset contractCardUxml;

    private VisualElement mainMenu;
    private VisualElement contractsWindow;
    private UIDocument uiDocument;

    private Label freeRefreshLabel;
    private Button paidRefreshButton;
    private Label currencyLabel;
    
    private bool isMenuOpen = false;

    private MainMenuInventoryUI inventoryUI;  // Прямая ссылка на наш UI инвентаря

    private void Start()
    {
        if (ContractManager.Instance == null)
        {
            var go = new GameObject("ContractManager_Global");
            go.AddComponent<ContractManager>();
        }
    }

    private void Update()
    {
        // Обновляем таймер каждый кадр, только если меню открыто
        if (isMenuOpen && freeRefreshLabel != null)
        {
            UpdateTimerDisplay();
        }
        UpdateHeaderInfo();
    }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        inventoryUI = FindFirstObjectByType<MainMenuInventoryUI>();

        // Защита от дубликатов
        var others = FindObjectsByType<Ui>(FindObjectsSortMode.None);
        if (others.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        uiDocument.rootVisualElement.Clear();

        mainMenu = mainMenuUxml.Instantiate();
        mainMenu.style.flexGrow = 1;
        uiDocument.rootVisualElement.Add(mainMenu);

        SetupMainMenuButtons();
    }

    private void SetupMainMenuButtons()
    {
        var btnInventory = mainMenu.Q<Button>("btnInventory");
        if (btnInventory != null)
            btnInventory.clicked += OpenInventory;

        var btnContracts = mainMenu.Q<Button>("btnContracts");
        if (btnContracts != null)
            btnContracts.clicked += OpenContracts;

        var btnPlay = mainMenu.Q<Button>("btnPlay");
        if (btnPlay != null)
            btnPlay.clicked += StartGame;
    }

    // ====================================================================
    // ИНВЕНТАРЬ
    // ====================================================================

    private void OpenInventory()
    {
        if (inventoryUI != null)
        {
            // Скрываем главное меню
            mainMenu.style.display = DisplayStyle.None;
            inventoryUI.ShowInventory();

            // Подписываемся на закрытие инвентаря (один раз!)
            inventoryUI.GetType().GetMethod("OnInventoryClosed")?.Invoke(inventoryUI, null);
            // Лучше — через публичное событие (рекомендую добавить в MainMenuInventoryUI)
            // Но пока сделаем через коллбэк:
            StartCoroutine(WaitForInventoryClose());
        }
        else
        {
            Debug.LogError("MainMenuInventoryUI не найден на сцене! Добавьте объект с этим скриптом.");
        }
    }

    // Вариант A: Через корутину (работает сразу)
    private System.Collections.IEnumerator WaitForInventoryClose()
    {
        // Ждём, пока инвентарь не исчезнет из root
        while (uiDocument.rootVisualElement.childCount > 0 &&
               uiDocument.rootVisualElement[0].name.Contains("Inventory")) // или проверка по классу
        {
            yield return null;
        }

        // Инвентарь закрыт → возвращаемся в главное меню
        ShowMainMenu();
    }
    #region КОНТРАКТЫ

    // ====================================================================
    // КОНТРАКТЫ
    // ====================================================================

    private void OpenContracts()
    {
        if (contractsWindow == null)
            CreateContractsWindow();

        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(contractsWindow);
        isMenuOpen = true;
    }

    private void CloseContracts()
    {
        uiDocument.rootVisualElement.Clear();
        ShowMainMenu();
        isMenuOpen = false;
    }

    private void CreateContractsWindow()
    {
        contractsWindow = contractsUxml.Instantiate();
        contractsWindow.style.flexGrow = 1;

        var closeButton = contractsWindow.Q<Button>("closeButton");
        closeButton.clicked += CloseContracts;

        paidRefreshButton = contractsWindow.Q<Button>("paidRefreshButton");
        paidRefreshButton.clicked += OnRefreshClicked;

        freeRefreshLabel = contractsWindow.Q<Label>("freeRefreshLabel");
        currencyLabel = contractsWindow.Q<Label>("currencyLabel");

        ContractManager.Instance.OnShopRefreshed += PopulateContractCards;
    }

    private void OnRefreshClicked()
    {
        var timeRemaining = ContractManager.Instance.GetTimeUntilFreeRefresh();

        if (timeRemaining <= TimeSpan.Zero)
        {
            ContractManager.Instance.TryRefreshShop(false);
        }
        else
        {
            // обновить платно
            bool success = ContractManager.Instance.TryRefreshShop(true);
            if (!success)
            {
                Debug.Log("Недостаточно денег для обновления!");
            }
        }
        UpdateHeaderInfo(); 
    }

    private void UpdateTimerDisplay()
    {
        var timeSpan = ContractManager.Instance.GetTimeUntilFreeRefresh();

        if (timeSpan <= TimeSpan.Zero)
        {
            freeRefreshLabel.text = "Бесплатное обновление: ДОСТУПНО";
            freeRefreshLabel.style.color = new StyleColor(Color.green);
            
            paidRefreshButton.text = "ОБНОВИТЬ БЕСПЛАТНО";
            paidRefreshButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f)); // Зеленый
        }
        else
        {
            freeRefreshLabel.text = $"Бесплатное обновление через: {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            freeRefreshLabel.style.color = new StyleColor(Color.white);
            
            paidRefreshButton.text = "ОБНОВИТЬ ЗА 100 КК";
            paidRefreshButton.style.backgroundColor = new StyleColor(new Color(1f, 0.4f, 0.2f)); // Оранжевый
        }
    }

     private void UpdateHeaderInfo()
    {
        if(currencyLabel != null && InventoryData.Instance != null)
        {
            currencyLabel.text = $"{InventoryData.Instance.ContractCoins} KK";
        }
    }

    private void PopulateContractCards()
    {
        var container = contractsWindow.Q<VisualElement>("contractsContainer");
        if (container == null) return;

        container.Clear();

        var allContracts = ContractManager.Instance.GetCurrentShopContracts();
        var activeContract = ContractManager.Instance.ActiveContract;
        int playerMoney = InventoryData.Instance.ContractCoins;

        foreach (var c in allContracts)
        {
            if (ContractManager.Instance.IsContractCompleted(c.Id))
                continue;

            var card = contractCardUxml.Instantiate();
            card.Q<Label>("contractName").text = c.Name;
            card.Q<Label>("contractDescription").text = c.Description;
            card.Q<Label>("contractReward").text = c.RewardText;

            var buyButton = card.Q<Button>("buyButton");

            // 2. ЛОГИКА "ВЫПОЛНЯЕТСЯ"
            if (activeContract != null && activeContract.Id == c.Id)
            {
                buyButton.text = "ВЫПОЛНЯЕТСЯ";
                buyButton.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0f)); 
                buyButton.style.color = new StyleColor(Color.black);
                buyButton.SetEnabled(false); 
            }
            // 3. ЛОГИКА "НЕДОСТАТОЧНО ДЕНЕГ"
            else if (playerMoney < c.Cost)
            {
                buyButton.text = "НЕДОСТАТОЧНО КК";
                // Красный цвет
                buyButton.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
                buyButton.style.color = new StyleColor(Color.white);
                buyButton.SetEnabled(false); // Кнопка неактивна
            }
            // 4. ОБЫЧНОЕ СОСТОЯНИЕ "КУПИТЬ"
            else
            {
                buyButton.text = $"КУПИТЬ ЗА {c.Cost} КК";
                // Зеленоватый цвет
                buyButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.2f)); 
                buyButton.style.color = new StyleColor(Color.white);

                buyButton.clicked += () => 
                {
                    if(InventoryData.Instance.TrySpendMoney(c.Cost))
                    {
                        ContractManager.Instance.SetActiveContract(c);
                        PopulateContractCards(); // Перерисовать кнопки
                        UpdateHeaderInfo(); // Перерисовать баланс наверху
                    }
                };
            }

            container.Add(card);
        }
    }

    private void OnDestroy()
    {
        if (ContractManager.Instance != null) 
            ContractManager.Instance.OnShopRefreshed -= PopulateContractCards;
    }

    #endregion


    // ====================================================================
    // ЗАПУСК ИГРЫ
    // ====================================================================

    private void StartGame()
    {
        // Убедимся, что InventoryData существует
        if (InventoryData.Instance == null)
        {
            var go = new GameObject("InventoryData_Persistent");
            var data = go.AddComponent<InventoryData>();
            DontDestroyOnLoad(go);
        }

        SceneManager.LoadScene("Scenes/SampleScene");
    }

    // ====================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ====================================================================

    public void ShowHUD() => uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    public void HideHUD() => uiDocument.rootVisualElement.style.display = DisplayStyle.None;
}
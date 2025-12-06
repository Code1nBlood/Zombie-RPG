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
        AudioManager.Instance.LoadVolumeSettings();
        AudioManager.Instance.PlayMusic(MusicType.MainMenu);
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
        {
            btnInventory.clicked += () =>
            {
                PlayClickSound();
                OpenInventory();
            };
            AddHoverSound(btnInventory);
        }

        var btnContracts = mainMenu.Q<Button>("btnContracts");
        if (btnContracts != null)
        {
            btnContracts.clicked += () =>
            {
                PlayClickSound();
                OpenContracts();
            };
            AddHoverSound(btnContracts);
        }

        var btnPlay = mainMenu.Q<Button>("btnPlay");
        if (btnPlay != null)
        {
            btnPlay.clicked += () =>
            {
                PlayClickSound();
                StartGame();
            };
            AddHoverSound(btnPlay);
        }
    }

    #region === ЗВУКИ ===

    private void PlayClickSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.ButtonClick);
    }

    private void PlayHoverSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.ButtonHover, 0.5f);
    }

    private void PlaySuccessSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.Success);
    }

    private void PlayFailSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.Fail);
    }

    private void PlayOpenSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.MenuOpen);
    }

    private void PlayCloseSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.MenuClose);
    }

    private void PlayPurchaseSound()
    {
        AudioManager.Instance.PlaySFX(SFXType.Purchase);
    }

    // Добавляет звук при наведении на кнопку

    private void AddHoverSound(Button button)
    {
        button.RegisterCallback<MouseEnterEvent>(evt => PlayHoverSound());
    }

    #endregion


    // ====================================================================
    // ИНВЕНТАРЬ
    // ====================================================================

    private void OpenInventory()
    {
        PlayOpenSound();
    
    if (inventoryUI != null)
    {
        mainMenu.style.display = DisplayStyle.None;
        inventoryUI.OnClosed += OnInventoryClosed;
        inventoryUI.ShowInventory();
    }
}

    private void OnInventoryClosed()
    {
        PlayCloseSound();
        inventoryUI.OnClosed -= OnInventoryClosed;
        ShowMainMenu();
    }
    #region КОНТРАКТЫ

    // ====================================================================
    // КОНТРАКТЫ
    // ====================================================================

    private void OpenContracts()
    {
        PlayOpenSound();
        if (contractsWindow == null)
            CreateContractsWindow();

        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(contractsWindow);
        isMenuOpen = true;
    }

    private void CloseContracts()
    {
        PlayCloseSound();
        uiDocument.rootVisualElement.Clear();
        ShowMainMenu();
        isMenuOpen = false;
    }

    private void CreateContractsWindow()
    {
        contractsWindow = contractsUxml.Instantiate();
        contractsWindow.style.flexGrow = 1;

        var closeButton = contractsWindow.Q<Button>("closeButton");
        closeButton.clicked += () =>
        {
            PlayClickSound();
            CloseContracts();
        };
        AddHoverSound(closeButton);

        paidRefreshButton = contractsWindow.Q<Button>("paidRefreshButton");
        paidRefreshButton.clicked += OnRefreshClicked;
        AddHoverSound(paidRefreshButton);

        freeRefreshLabel = contractsWindow.Q<Label>("freeRefreshLabel");
        currencyLabel = contractsWindow.Q<Label>("currencyLabel");

        ContractManager.Instance.OnShopRefreshed += PopulateContractCards;
    }

    private void OnRefreshClicked()
    {
        PlayClickSound();
        var timeRemaining = ContractManager.Instance.GetTimeUntilFreeRefresh();

        if (timeRemaining <= TimeSpan.Zero)
        {
            ContractManager.Instance.TryRefreshShop(false);
            PlaySuccessSound();
        }
        else
        {
            // обновить платно
            bool success = ContractManager.Instance.TryRefreshShop(true);
            if (success)
            {
                PlayPurchaseSound();
            }
            else
            {
                PlayFailSound();
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
            AddHoverSound(buyButton);

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
                    PlayPurchaseSound();
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
        AudioManager.Instance.PlaySFX(SFXType.GameStart);
        AudioManager.Instance.StopMusic(1f);
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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Ui : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset contractsUxml;
    [SerializeField] private VisualTreeAsset contractCardUxml;

    private VisualElement mainMenu;
    private VisualElement contractsWindow;
    private UIDocument uiDocument;

    private MainMenuInventoryUI inventoryUI;  // Прямая ссылка на наш UI инвентаря

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

    // ====================================================================
    // КОНТРАКТЫ
    // ====================================================================

    private void OpenContracts()
    {
        if (contractsWindow == null)
            CreateContractsWindow();

        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(contractsWindow);
    }

    private void CreateContractsWindow()
    {
        contractsWindow = contractsUxml.Instantiate();
        contractsWindow.style.flexGrow = 1;

        var closeButton = contractsWindow.Q<Button>("closeButton");
        if (closeButton != null)
            closeButton.clicked += () => ShowMainMenu();

        var paidRefreshButton = contractsWindow.Q<Button>("paidRefreshButton");
        if (paidRefreshButton != null)
            paidRefreshButton.clicked += OnPaidRefreshClicked;

        PopulateContractCards();
    }

    private void OnPaidRefreshClicked()
    {
        Debug.Log("Обновление контрактов за 100 КК");
        PopulateContractCards(); // можно добавить анимацию
    }

    private void PopulateContractCards()
    {
        var container = contractsWindow.Q<VisualElement>("contractsContainer");
        if (container == null) return;

        container.Clear();

        var contracts = new[]
        {
            new { Name = "Точность решает всё", Desc = "Убить 10 зомби в голову", Reward = "Убийца", Cost = 50 },
            new { Name = "Цепляй комбо!", Desc = "Убить 3 зомби подряд без промаха", Reward = "Стрелок", Cost = 80 },
            new { Name = "На грани жизни и смерти", Desc = "Пережить 5 раз с HP ≤ 10", Reward = "Выживальщик", Cost = 120 }
        };

        foreach (var c in contracts)
        {
            var card = contractCardUxml.Instantiate();

            card.Q<Label>("contractName").text = c.Name;
            card.Q<Label>("contractDescription").text = c.Desc;
            card.Q<Label>("contractReward").text = c.Reward;

            var buyButton = card.Q<Button>("buyButton");
            buyButton.text = $"КУПИТЬ ЗА {c.Cost} КК";
            buyButton.clicked += () => Debug.Log($"Куплен контракт: {c.Name}");

            container.Add(card);
        }
    }

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
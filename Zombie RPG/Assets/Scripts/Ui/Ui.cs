using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Ui : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset contractsUxml;
    [SerializeField] private VisualTreeAsset contractCardUxml;

    [Header("Dependencies")]
    [SerializeField] private InventoryManager inventoryManager;

    private VisualElement mainMenu;
    private VisualElement contractsWindow;
    private UIDocument uiDocument;
    private Button paidRefreshButton;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        
        // Subscribe to inventory events
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryClosed += ShowMainMenu;
        }

        ShowMainMenu();
    }

    void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryClosed -= ShowMainMenu;
        }
    }

    private void ShowMainMenu()
    {
        uiDocument.rootVisualElement.Clear();

        mainMenu = mainMenuUxml.Instantiate();
        uiDocument.rootVisualElement.Add(mainMenu);

        // Setup main menu buttons
        var btnInventory = mainMenu.Q<Button>("btnInventory");
        if (btnInventory != null)
            btnInventory.clicked += OpenInventory;

        var btnContracts = mainMenu.Q<Button>("btnContracts");
        if (btnContracts != null)
            btnContracts.clicked += OpenContracts;

        var btnScene1 = mainMenu.Q<Button>("btnPlay");
        if (btnScene1 != null)
            btnScene1.clicked += OpenScene1;
    }

    private void OpenInventory()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OpenInventory();
        }
    }

    private void OpenScene1()
    {
        SceneManager.LoadScene("Scenes/SampleScene");
    }

    private void OpenContracts()
    {
        if (contractsWindow == null)
        {
            contractsWindow = contractsUxml.Instantiate();
            contractsWindow.style.flexGrow = 1;

            var closeButton = contractsWindow.Q<Button>("closeButton");
            if (closeButton != null)
                closeButton.clicked += CloseContracts;

            paidRefreshButton = contractsWindow.Q<Button>("paidRefreshButton");
            if (paidRefreshButton != null)
                paidRefreshButton.clicked += OnPaidRefreshClicked;

            PopulateContractCards();
        }

        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(contractsWindow);
    }

    private void CloseContracts()
    {
        ShowMainMenu();
    }
    
    private void OnPaidRefreshClicked()
    {
        Debug.Log("Пользователь нажал 'Обновить за 100 контракт-койнов'");
    }

    private void PopulateContractCards()
    {
        var container = contractsWindow.Q<VisualElement>("contractsContainer");
        container.Clear();

        var contracts = new[]
        {
            new { Name = "Убить 10 зомби в голову", Desc = "Точность решает всё.", Reward = "Убийца", Cost = 50 },
            new { Name = "Убить 3 зомби подряд без промаха", Desc = "Цепляй комбо!", Reward = "Стрелок", Cost = 80 },
            new { Name = "Пережить 5 раз с HP ≤ 10", Desc = "На грани жизни и смерти.", Reward = "Выживальщик", Cost = 120 }
        };

        foreach (var c in contracts)
        {
            var card = contractCardUxml.Instantiate();
            
            card.style.width = 250;
            card.style.height = 400;
            card.style.marginRight = 10;
            card.style.marginLeft = 10;
            card.style.flexGrow = 0;
            card.style.flexShrink = 0;

            var nameLabel = card.Q<Label>("contractName");
            var descLabel = card.Q<Label>("contractDescription");
            var rewardLabel = card.Q<Label>("contractReward");
            var buyButton = card.Q<Button>("buyButton");

            nameLabel.text = c.Name;
            descLabel.text = c.Desc;
            rewardLabel.text = c.Reward;
            buyButton.text = $"КУПИТЬ ЗА {c.Cost} КК";

            buyButton.clicked += () =>
            {
                Debug.Log($"Покупка контракта: {c.Name}");
            };

            container.Add(card);
        }
    }
}
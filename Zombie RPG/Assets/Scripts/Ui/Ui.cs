using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Ui : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset inventoryUxml;
    [SerializeField] private VisualTreeAsset contractsUxml;
    [SerializeField] private VisualTreeAsset contractCardUxml;

    private VisualElement mainMenu;
    private VisualElement inventoryWindow;
    private UIDocument uiDocument;
    private VisualElement contractsWindow;
    private Button paidRefreshButton; 

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        uiDocument.rootVisualElement.Clear();

        // главное меню
        mainMenu = mainMenuUxml.Instantiate();
        uiDocument.rootVisualElement.Add(mainMenu);

        var btnInventory = mainMenu.Q<Button>("btnInventory");
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
        if (inventoryWindow == null)
        {
            inventoryWindow = inventoryUxml.Instantiate();
            inventoryWindow.style.flexGrow = 1;

            var closeButton = inventoryWindow.Q<Button>("CloseButton");
            if (closeButton != null)
                closeButton.clicked += CloseInventory;
        }

        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(inventoryWindow);
    }

    private void CloseInventory()
    {
        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(mainMenu);
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

        // 🔥 Привязываем кнопку платного обновления
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
        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(mainMenu);
    }
    
    private void OnPaidRefreshClicked()
{
    Debug.Log("Пользователь нажал 'Обновить за 100 контракт-койнов'");

}

    private void PopulateContractCards()
{
    var container = contractsWindow.Q<VisualElement>("contractsContainer");

    container.Clear();

    // данные контрактов 
    var contracts = new[]
    {
        new { Name = "Убить 10 зомби в голову", Desc = "Точность решает всё.", Reward = "Убийца", Cost = 50 },
        new { Name = "Убить 3 зомби подряд без промаха", Desc = "Цепляй комбо!", Reward = "Стрелок", Cost = 80 },
        new { Name = "Пережить 5 раз с HP ≤ 10", Desc = "На грани жизни и смерти.", Reward = "Выживальщик", Cost = 120 }
    };

    foreach (var c in contracts)
    {
        // создание копии UI-элементов через Instantiate из VisualTreeAsset
        var card = contractCardUxml.Instantiate();
        
        card.style.width = new StyleLength(250);
        card.style.height = new StyleLength(400);
        card.style.marginRight = new StyleLength(10); 
        card.style.marginLeft = new StyleLength(10); 
        
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
            // Здесь — логика покупки (проверка КК, активация и т.д.)
        };

        container.Add(card);
    }
}
}

using UnityEngine;
using UnityEngine.UIElements;

public class Ui : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset inventoryUxml;

    private VisualElement mainMenu;
    private VisualElement inventoryWindow;
    private UIDocument uiDocument;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        uiDocument.rootVisualElement.Clear();

        // главное меню
        mainMenu = mainMenuUxml.Instantiate();
        uiDocument.rootVisualElement.Add(mainMenu);

        var btnInventory = mainMenu.Q<Button>("btnInventory");
        btnInventory.clicked += OpenInventory;
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
}

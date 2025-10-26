using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;


public class InventoryManager : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset inventoryUxml;
    
    [Header("Potion Settings")]
    [SerializeField] private List<Potion> allPotions = new List<Potion>();
    [SerializeField] private VisualTreeAsset potionElementTemplate;

    private UIDocument uiDocument;
    private VisualElement inventoryWindow;
    private List<Potion> collectedPotions = new List<Potion>();
    private ListView potionListView;
    private ListView boostListView;

    // Drag & Drop system
    private VisualElement dragPreview;
    private Potion draggedPotion;

    private bool isDragging = false;

    // Quick slots
    private VisualElement[] quickSlots = new VisualElement[2];
    private Potion[] quickSlotPotions = new Potion[2];

    // Events
    public System.Action OnInventoryClosed { get; set; }
    public System.Action<Potion, int> OnPotionAssignedToQuickSlot { get; set; }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    public void OpenInventory()
    {
        if (inventoryWindow == null)
        {
            CreateInventoryWindow();
        }

        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(inventoryWindow);
        RefreshInventoryDisplay();
    }

    private void CreateInventoryWindow()
    {
        inventoryWindow = inventoryUxml.Instantiate();
        inventoryWindow.style.flexGrow = 1;

        // Setup close button
        var closeButton = inventoryWindow.Q<Button>("CloseButton");
        if (closeButton != null)
            closeButton.clicked += CloseInventory;

        InitializeQuickSlots();
        InitializeInventoryLists();
        SetupDragAndDrop();
    }

    private void InitializeQuickSlots()
    {
        var boostSlots = inventoryWindow.Query<VisualElement>(className: "potion-slot").ToList();
        for (int i = 0; i < Mathf.Min(quickSlots.Length, boostSlots.Count); i++)
        {
            quickSlots[i] = boostSlots[i];
            quickSlots[i].userData = i; // Store slot index
            UpdateQuickSlotVisual(i);
        }
    }

    private void InitializeInventoryLists()
    {
        // Setup Potion ListView
        potionListView = inventoryWindow.Q<ListView>("PotionList");
        if (potionListView != null)
        {
            potionListView.makeItem = MakePotionItem;
            potionListView.bindItem = BindPotionItem;
            potionListView.itemsSource = collectedPotions;
            potionListView.selectionType = SelectionType.None;
            potionListView.fixedItemHeight = 80;
            potionListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
        }

        // Setup Boost ListView
        boostListView = inventoryWindow.Q<ListView>("BoostList");
        if (boostListView != null)
        {
            boostListView.makeItem = MakeBoostItem;
            boostListView.bindItem = BindBoostItem;
            boostListView.itemsSource = new List<object>();
        }

        StartCoroutine(AddTestPotions());
    }

    private void SetupDragAndDrop()
    {
        // Register drag events on quick slots
        foreach (var slot in quickSlots)
        {
            if (slot != null)
            {
                slot.RegisterCallback<PointerDownEvent>(OnQuickSlotPointerDown);
                slot.RegisterCallback<PointerUpEvent>(OnQuickSlotPointerUp);
            }
        }

        // Register global mouse events for drag handling
        inventoryWindow.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        inventoryWindow.RegisterCallback<PointerUpEvent>(OnPointerUpGlobal);
    }

    private VisualElement MakePotionItem()
    {
        var element = new VisualElement();
        element.style.flexDirection = FlexDirection.Row;
        element.style.alignItems = Align.Center;
        element.style.paddingTop = 5;
        element.style.paddingBottom = 5;
        element.style.paddingLeft = 10;
        element.style.paddingRight = 10;
        return element;
    }

    private void BindPotionItem(VisualElement element, int index)
    {
        element.Clear();
        
        var potion = collectedPotions[index];
        
        // Icon with drag capability
        var icon = new VisualElement();
        icon.style.width = 40;
        icon.style.height = 40;
        icon.style.marginRight = 10;
        icon.userData = potion; // Store potion reference

        if (potion.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(potion.icon);
            icon.style.backgroundColor = Color.clear; // чтобы не мешал цвет
        }
        else
        {
            icon.style.backgroundColor = potion.color;
        }
        
        element.userData = potion; // храним данные на элементе
        element.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, potion, element));
        
        element.Add(icon);
        
        // Info
        var infoContainer = new VisualElement();
        infoContainer.style.flexDirection = FlexDirection.Column;
        
        var nameLabel = new Label(potion.potionName);
        nameLabel.style.fontSize = 14;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.color = Color.white;
        
        var effectLabel = new Label(potion.effect);
        effectLabel.style.fontSize = 11;
        effectLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
        
        var durationLabel = new Label($"Длительность: {potion.duration}");
        durationLabel.style.fontSize = 10;
        durationLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        
        infoContainer.Add(nameLabel);
        infoContainer.Add(effectLabel);
        infoContainer.Add(durationLabel);
        element.Add(infoContainer);
    }

    private void StartDrag(PointerDownEvent evt, Potion potion, VisualElement sourceElement)
    {
        if (isDragging) return;

        isDragging = true;
        draggedPotion = potion;

        dragPreview = new VisualElement();
        dragPreview.style.position = Position.Absolute;
        dragPreview.style.width = 50;
        dragPreview.style.height = 50;
        dragPreview.style.backgroundColor = potion.color;
        dragPreview.style.opacity = 0.8f;
        dragPreview.pickingMode = PickingMode.Ignore;

        // Set initial position
        UpdateDragPosition(evt.position);
        uiDocument.rootVisualElement.Add(dragPreview);

        sourceElement.AddToClassList("dragging");
    }

    private void StopDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        
        // Remove drag preview
        if (dragPreview != null)
        {
            uiDocument.rootVisualElement.Remove(dragPreview);
            dragPreview = null;
        }

        // Remove dragging class from all elements
        var draggingElements = inventoryWindow.Query<VisualElement>(className: "dragging").ToList();
        foreach (var element in draggingElements)
        {
            element.RemoveFromClassList("dragging");
        }

        draggedPotion = null;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging || dragPreview == null) return;

        UpdateDragPosition(evt.position);
    }

    private void OnPointerUpGlobal(PointerUpEvent evt)
    {
        if (!isDragging) return;

        // Check if dropped on a quick slot
        var quickSlot = GetQuickSlotAtPosition(evt.position);
        if (quickSlot != null && draggedPotion != null)
        {
            int slotIndex = (int)quickSlot.userData;
            AssignPotionToQuickSlot(draggedPotion, slotIndex);
        }

        StopDrag();
    }

    private void OnQuickSlotPointerDown(PointerDownEvent evt)
{
    var slot = evt.currentTarget as VisualElement;
    int slotIndex = (int)slot.userData;
    if (quickSlotPotions[slotIndex] != null)
    {
        var potion = quickSlotPotions[slotIndex];
        collectedPotions.Add(potion);
        RefreshInventoryDisplay();

        quickSlotPotions[slotIndex] = null;
        UpdateQuickSlotVisual(slotIndex);

        StartDrag(evt, potion, slot);
    }
}

    private void OnQuickSlotPointerUp(PointerUpEvent evt)
    {
        if (!isDragging) return;

        var slot = evt.currentTarget as VisualElement;
        int slotIndex = (int)slot.userData;
        
        if (draggedPotion != null)
        {
            AssignPotionToQuickSlot(draggedPotion, slotIndex);
        }

        StopDrag();
    }

    private VisualElement GetQuickSlotAtPosition(Vector2 position)
    {
        foreach (var slot in quickSlots)
        {
            if (slot != null && slot.worldBound.Contains(position))
            {
                return slot;
            }
        }
        return null;
    }

    private void UpdateDragPosition(Vector2 position)
    {
        if (dragPreview != null)
        {
            dragPreview.style.left = position.x - 25;
            dragPreview.style.top = position.y - 25;
        }
    }

    private void AssignPotionToQuickSlot(Potion potion, int slotIndex)
{
    if (slotIndex < 0 || slotIndex >= quickSlotPotions.Length) return;

    for (int i = 0; i < quickSlotPotions.Length; i++)
    {
        if (quickSlotPotions[i] == potion)
        {
            quickSlotPotions[i] = null;
            UpdateQuickSlotVisual(i);
        }
    }

    if (collectedPotions.Contains(potion))
    {
        collectedPotions.Remove(potion);
        RefreshInventoryDisplay();
    }

    quickSlotPotions[slotIndex] = potion;
    UpdateQuickSlotVisual(slotIndex);
    OnPotionAssignedToQuickSlot?.Invoke(potion, slotIndex);
    Debug.Log($"Зелье {potion.potionName} назначено на слот {slotIndex + 1}");
}

    private void UpdateQuickSlotVisual(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlots.Length || quickSlots[slotIndex] == null) return;

        var slot = quickSlots[slotIndex];
        slot.Clear();

        var potion = quickSlotPotions[slotIndex];
        if (potion != null)
        {
            var icon = new VisualElement();
            icon.style.width = slot.contentRect.width;
            icon.style.height = slot.contentRect.height;
            icon.style.backgroundColor = potion.color;
            
            if (potion.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(potion.icon);
            }
            
            var nameLabel = new Label(potion.potionName);
            nameLabel.style.fontSize = 8;
            nameLabel.style.unityTextAlign = TextAnchor.LowerCenter;
            nameLabel.style.color = Color.white;
            nameLabel.style.position = Position.Absolute;
            nameLabel.style.bottom = 0;
            nameLabel.style.width = Length.Percent(100);
            nameLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            
            icon.Add(nameLabel);
            slot.Add(icon);
        }
        else
        {
            // Empty slot visual
            var placeholder = new Label($"Слот {slotIndex + 1}");
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.color = new Color(1, 1, 1, 0.5f);
            slot.Add(placeholder);
        }
    }

    private VisualElement MakeBoostItem()
    {
        return new VisualElement();
    }

    private void BindBoostItem(VisualElement element, int index)
    {
        // Implementation for boosts
    }

    public void AddPotion(Potion potion)
    {
        collectedPotions.Add(potion);
        RefreshInventoryDisplay();
    }

    public Potion GetPotionInQuickSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < quickSlotPotions.Length)
        {
            return quickSlotPotions[slotIndex];
        }
        return null;
    }

    public void UsePotionFromQuickSlot(int slotIndex)
    {
        var potion = GetPotionInQuickSlot(slotIndex);
        if (potion != null)
        {
            // Apply effect
            var effectApplier = Object.FindAnyObjectByType<PotionEffectApplier>();
            if (effectApplier != null)
            {
                effectApplier.ApplyPotionEffect(potion);
            }
            
            // Remove from quick slot
            quickSlotPotions[slotIndex] = null;
            UpdateQuickSlotVisual(slotIndex);
            
            Debug.Log($"Использовано зелье: {potion.potionName} из слота {slotIndex + 1}");
        }
    }

    private void RefreshInventoryDisplay()
    {
        potionListView?.Rebuild();
        boostListView?.Rebuild();
    }

    private IEnumerator AddTestPotions()
    {
        yield return new WaitForEndOfFrame();
        
        foreach (var potion in allPotions)
        {
            AddPotion(potion);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void CloseInventory()
    {
        StopDrag(); // Clean up any ongoing drag
        uiDocument.rootVisualElement.Clear();
        OnInventoryClosed?.Invoke();
    }
}
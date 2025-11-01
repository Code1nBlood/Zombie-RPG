using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset inventoryUxml;

    [Header("Potions")]
    [SerializeField] private List<Potion> allPotions = new List<Potion>();
    private List<Potion> collectedPotions = new List<Potion>();
    private ListView potionListView;
    private VisualElement[] potionQuickSlots = new VisualElement[2];
    private Potion[] potionSlotItems = new Potion[2];

    [Header("Boosts")]
    [SerializeField] private List<Boost> allBoosts = new List<Boost>();
    private List<Boost> collectedBoosts = new List<Boost>();
    private ListView boostListView;
    private VisualElement[] boostQuickSlots = new VisualElement[3]; 
    private Boost[] boostSlotItems = new Boost[3];

    private UIDocument uiDocument;
    private VisualElement inventoryWindow;

    private VisualElement dragPreview;
    private object draggedItem; 
    private bool isDragging = false;

    // Events
    public System.Action OnInventoryClosed { get; set; }
    public System.Action<Potion, int> OnPotionAssigned { get; set; }
    public System.Action<Boost, int> OnBoostAssigned { get; set; }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    public void OpenInventory()
    {
        if (inventoryWindow == null)
            CreateInventoryWindow();
        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(inventoryWindow);
        RefreshInventoryDisplay();
    }

    private void CreateInventoryWindow()
    {
        inventoryWindow = inventoryUxml.Instantiate();
        inventoryWindow.style.flexGrow = 1;

        var closeButton = inventoryWindow.Q<Button>("CloseButton");
        if (closeButton != null)
            closeButton.clicked += CloseInventory;

        InitializeQuickSlots();
        InitializeInventoryLists();
        SetupDragAndDrop();
    }

    private void InitializeQuickSlots()
    {
        // Potion slots (2 шт)
        var potionSlots = inventoryWindow.Query<VisualElement>(className: "potion-slot").ToList();
        for (int i = 0; i < Mathf.Min(potionQuickSlots.Length, potionSlots.Count); i++)
        {
            potionQuickSlots[i] = potionSlots[i];
            potionQuickSlots[i].userData = (object)i;
            UpdatePotionSlotVisual(i);
        }

        // Boost slots (3 шт)
        var boostSlots = inventoryWindow.Query<VisualElement>(className: "boost-slot").ToList();
        for (int i = 0; i < Mathf.Min(boostQuickSlots.Length, boostSlots.Count); i++)
        {
            boostQuickSlots[i] = boostSlots[i];
            boostQuickSlots[i].userData = (object)i;
            UpdateBoostSlotVisual(i);
        }
    }

    private void InitializeInventoryLists()
    {
        // Potions
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

        // Boosts
        boostListView = inventoryWindow.Q<ListView>("BoostList");
        if (boostListView != null)
        {
            boostListView.makeItem = MakeBoostItem;
            boostListView.bindItem = BindBoostItem;
            boostListView.itemsSource = collectedBoosts;
            boostListView.selectionType = SelectionType.None;
            boostListView.fixedItemHeight = 80;
            boostListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
        }

        StartCoroutine(AddTestItems());
    }

    private IEnumerator AddTestItems()
    {
        yield return new WaitForEndOfFrame();
        foreach (var p in allPotions) collectedPotions.Add(p);
        foreach (var b in allBoosts) collectedBoosts.Add(b);
        RefreshInventoryDisplay();
    }

    private void SetupDragAndDrop()
    {
        // Potion slots
        foreach (var slot in potionQuickSlots)
            if (slot != null)
            {
                slot.RegisterCallback<PointerDownEvent>(OnPotionSlotPointerDown);
                slot.RegisterCallback<PointerUpEvent>(OnPotionSlotPointerUp);
            }

        // Boost slots
        foreach (var slot in boostQuickSlots)
            if (slot != null)
            {
                slot.RegisterCallback<PointerDownEvent>(OnBoostSlotPointerDown);
                slot.RegisterCallback<PointerUpEvent>(OnBoostSlotPointerUp);
            }

        inventoryWindow.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        inventoryWindow.RegisterCallback<PointerUpEvent>(OnPointerUpGlobal);
    }

    // === POTIONS ===
    private VisualElement MakePotionItem() => CreateItemElement();
    private void BindPotionItem(VisualElement el, int idx)
    {
        var p = collectedPotions[idx];
        BindItem(el, p.potionName, p.effect, p.duration, p.icon, p.color, p);
        el.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, p, el));
    }

    // === BOOSTS ===
    private VisualElement MakeBoostItem() => CreateItemElement();
    private void BindBoostItem(VisualElement el, int idx)
    {
        var b = collectedBoosts[idx];
        BindItem(el, b.boostName, b.effect, b.duration, b.icon, b.color, b);
        el.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, b, el));
    }

    private VisualElement CreateItemElement()
    {
        var el = new VisualElement();
        el.style.flexDirection = FlexDirection.Row;
        el.style.alignItems = Align.Center;
        el.style.paddingTop = 5;
        el.style.paddingBottom = 5;
        el.style.paddingLeft = 10;
        el.style.paddingRight = 10;
        return el;
    }

    private void BindItem(VisualElement el, string name, string effect, string duration, Sprite icon, Color color, object userData)
    {
        el.Clear();
        el.userData = userData;

        var iconEl = new VisualElement();
        iconEl.style.width = 40;
        iconEl.style.height = 40;
        iconEl.style.marginRight = 10;
        if (icon != null)
        {
            iconEl.style.backgroundImage = new StyleBackground(icon);
            iconEl.style.backgroundColor = Color.clear;
        }
        else
        {
            iconEl.style.backgroundColor = color;
        }

        var info = new VisualElement { style = { flexDirection = FlexDirection.Column } };
        info.Add(new Label(name) { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, color = Color.white } });
        info.Add(new Label(effect) { style = { fontSize = 11, color = new Color(0.8f, 0.8f, 0.8f) } });
        info.Add(new Label($"Длительность: {duration}") { style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f) } });

        el.Add(iconEl);
        el.Add(info);
    }

    // === DRAG & DROP ===
    private void StartDrag(PointerDownEvent evt, object item, VisualElement source)
    {
        if (isDragging) return;
        isDragging = true;
        draggedItem = item;

        dragPreview = new VisualElement();
        dragPreview.style.position = Position.Absolute;
        dragPreview.style.width = 50;
        dragPreview.style.height = 50;
        dragPreview.style.opacity = 0.8f;
        dragPreview.pickingMode = PickingMode.Ignore;

        Color color = Color.white;
        Sprite icon = null;

        if (item is Potion p)
        {
            color = p.color;
            icon = p.icon;
        }
        else if (item is Boost b)
        {
            color = b.color;
            icon = b.icon;
        }

        if (icon != null)
            dragPreview.style.backgroundImage = new StyleBackground(icon);
        else
            dragPreview.style.backgroundColor = color;

        UpdateDragPosition(evt.position);
        uiDocument.rootVisualElement.Add(dragPreview);
        source.AddToClassList("dragging");
    }

    private void StopDrag()
    {
        if (!isDragging) return;
        isDragging = false;
        if (dragPreview != null)
        {
            uiDocument.rootVisualElement.Remove(dragPreview);
            dragPreview = null;
        }
        foreach (var el in inventoryWindow.Query<VisualElement>(className: "dragging").ToList())
            el.RemoveFromClassList("dragging");
        draggedItem = null;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isDragging && dragPreview != null)
            UpdateDragPosition(evt.position);
    }

    private void OnPointerUpGlobal(PointerUpEvent evt)
    {
        if (!isDragging) return;

        // Проверяем, куда сбросили
        if (draggedItem is Potion p)
        {
            var slot = GetPotionSlotAt(evt.position);
            if (slot != null)
            {
                int idx = (int)slot.userData;
                AssignPotionToSlot(p, idx);
            }
        }
        else if (draggedItem is Boost b)
        {
            var slot = GetBoostSlotAt(evt.position);
            if (slot != null)
            {
                int idx = (int)slot.userData;
                AssignBoostToSlot(b, idx);
            }
        }

        StopDrag();
    }

    // === POTION SLOTS ===
    private void OnPotionSlotPointerDown(PointerDownEvent evt)
    {
        var slot = evt.currentTarget as VisualElement;
        int idx = (int)slot.userData;
        if (potionSlotItems[idx] != null)
        {
            var p = potionSlotItems[idx];
            collectedPotions.Add(p);
            RefreshInventoryDisplay();
            potionSlotItems[idx] = null;
            UpdatePotionSlotVisual(idx);
            StartDrag(evt, p, slot);
        }
    }

    private void OnPotionSlotPointerUp(PointerUpEvent evt)
    {
        if (!isDragging || draggedItem is not Potion p) return;
        int idx = (int)(evt.currentTarget as VisualElement).userData;
        AssignPotionToSlot(p, idx);
        StopDrag();
    }

    private VisualElement GetPotionSlotAt(Vector2 pos)
    {
        return potionQuickSlots.FirstOrDefault(s => s != null && s.worldBound.Contains(pos));
    }

    private void AssignPotionToSlot(Potion p, int idx)
    {
        if (idx < 0 || idx >= potionSlotItems.Length) return;

        if (potionSlotItems[idx] != null && potionSlotItems[idx] != p)
        {
            collectedPotions.Add(potionSlotItems[idx]);
        }

        // Убираем из других слотов
        for (int i = 0; i < potionSlotItems.Length; i++)
            if (potionSlotItems[i] == p)
            {
                potionSlotItems[i] = null;
                UpdatePotionSlotVisual(i);
            }

        // Убираем из инвентаря
        if (collectedPotions.Remove(p))
            RefreshInventoryDisplay();

        potionSlotItems[idx] = p;
        UpdatePotionSlotVisual(idx);
        OnPotionAssigned?.Invoke(p, idx);
        RefreshInventoryDisplay();
    }

    private void UpdatePotionSlotVisual(int idx)
    {
        if (idx < 0 || idx >= potionQuickSlots.Length || potionQuickSlots[idx] == null) return;
        var slot = potionQuickSlots[idx];
        slot.Clear();
        var p = potionSlotItems[idx];
        if (p != null)
        {
            var icon = CreateSlotIcon(p.icon, p.color, p.potionName);
            slot.Add(icon);
        }
        else
        {
            slot.Add(new Label($"Слот {idx + 1}") { style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(1,1,1,0.5f) } });
        }
    }

    // === BOOST SLOTS ===
    private void OnBoostSlotPointerDown(PointerDownEvent evt)
    {
        var slot = evt.currentTarget as VisualElement;
        int idx = (int)slot.userData;
        if (boostSlotItems[idx] != null)
        {
            var b = boostSlotItems[idx];
            collectedBoosts.Add(b);
            RefreshInventoryDisplay();
            boostSlotItems[idx] = null;
            UpdateBoostSlotVisual(idx);
            StartDrag(evt, b, slot);
        }
    }

    private void OnBoostSlotPointerUp(PointerUpEvent evt)
    {
        if (!isDragging || draggedItem is not Boost b) return;
        int idx = (int)(evt.currentTarget as VisualElement).userData;
        AssignBoostToSlot(b, idx);
        StopDrag();
    }

    private VisualElement GetBoostSlotAt(Vector2 pos)
    {
        return boostQuickSlots.FirstOrDefault(s => s != null && s.worldBound.Contains(pos));
    }

    private void AssignBoostToSlot(Boost b, int idx)
    {
        if (idx < 0 || idx >= boostSlotItems.Length) return;

        if (boostSlotItems[idx] != null && boostSlotItems[idx] != b)
        {
            collectedBoosts.Add(boostSlotItems[idx]);
        }

        for (int i = 0; i < boostSlotItems.Length; i++)
            if (boostSlotItems[i] == b)
            {
                boostSlotItems[i] = null;
                UpdateBoostSlotVisual(i);
            }

        if (collectedBoosts.Remove(b))
            RefreshInventoryDisplay();

        boostSlotItems[idx] = b;
        UpdateBoostSlotVisual(idx);
        OnBoostAssigned?.Invoke(b, idx);
        RefreshInventoryDisplay();
    }

    private void UpdateBoostSlotVisual(int idx)
    {
        if (idx < 0 || idx >= boostQuickSlots.Length || boostQuickSlots[idx] == null) return;
        var slot = boostQuickSlots[idx];
        slot.Clear();
        var b = boostSlotItems[idx];
        if (b != null)
        {
            var icon = CreateSlotIcon(b.icon, b.color, b.boostName);
            slot.Add(icon);
        }
        else
        {
            slot.Add(new Label($"Слот {idx + 1}") { style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(1,1,1,0.5f) } });
        }
    }

    private VisualElement CreateSlotIcon(Sprite icon, Color color, string name)
    {
        var iconEl = new VisualElement();
        iconEl.style.width = Length.Percent(100);
        iconEl.style.height = Length.Percent(100);
        if (icon != null)
            iconEl.style.backgroundImage = new StyleBackground(icon);
        else
            iconEl.style.backgroundColor = color;

        var label = new Label(name);
        label.style.fontSize = 8;
        label.style.unityTextAlign = TextAnchor.LowerCenter;
        label.style.color = Color.white;
        label.style.position = Position.Absolute;
        label.style.bottom = 0;
        label.style.width = Length.Percent(100);
        label.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        iconEl.Add(label);
        return iconEl;
    }

    private void UpdateDragPosition(Vector2 pos)
    {
        if (dragPreview != null)
        {
            dragPreview.style.left = pos.x - 25;
            dragPreview.style.top = pos.y - 25;
        }
    }

    private void RefreshInventoryDisplay()
    {
        potionListView?.Rebuild();
        boostListView?.Rebuild();
    }

    private void CloseInventory()
    {
        StopDrag();
        uiDocument.rootVisualElement.Clear();
        OnInventoryClosed?.Invoke();
    }
}
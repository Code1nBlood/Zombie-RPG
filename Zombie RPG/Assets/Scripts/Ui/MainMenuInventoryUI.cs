using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class MainMenuInventoryUI : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset inventoryUxml;

    private UIDocument uiDocument;
    private VisualElement inventoryWindow;
    private ListView potionListView;
    private ListView boostListView;

    private VisualElement[] potionQuickSlots = new VisualElement[2];
    private VisualElement[] boostQuickSlots = new VisualElement[3];

    // Drag-and-drop
    private object draggedItem;
    private VisualElement draggedSource;
    private bool isDragging;
    private VisualElement dragPreview;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        EnsureInventoryDataExists();
    }

    public void ShowInventory()
    {
        if (inventoryWindow == null) CreateInventoryWindow();
        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(inventoryWindow);
        RefreshInventoryDisplay();
    }

    private void CreateInventoryWindow()
    {
        inventoryWindow = inventoryUxml.Instantiate();
        inventoryWindow.style.flexGrow = 1;

        var closeBtn = inventoryWindow.Q<Button>("CloseButton");
        if (closeBtn != null) closeBtn.clicked += CloseInventory;

        InitializeQuickSlots();
        InitializeLists();
        SetupDragAndDrop();
    }

    private void InitializeQuickSlots()
    {
        var potSlots = inventoryWindow.Query<VisualElement>(className: "potion-slot").ToList();
        for (int i = 0; i < 2 && i < potSlots.Count; i++)
        {
            potionQuickSlots[i] = potSlots[i];
            potionQuickSlots[i].userData = i;
        }

        var bstSlots = inventoryWindow.Query<VisualElement>(className: "boost-slot").ToList();
        for (int i = 0; i < 3 && i < bstSlots.Count; i++)
        {
            boostQuickSlots[i] = bstSlots[i];
            boostQuickSlots[i].userData = i;
        }
    }

    private void InitializeLists()
    {
        potionListView = inventoryWindow.Q<ListView>("PotionList");
        boostListView = inventoryWindow.Q<ListView>("BoostList");

        if (potionListView != null)
        {
            potionListView.makeItem = MakeListItem;
            potionListView.bindItem = (el, i) => BindListItem(el, GetCollectedPotions()[i]);
            potionListView.selectionType = SelectionType.None;
            potionListView.fixedItemHeight = 80;
        }

        if (boostListView != null)
        {
            boostListView.makeItem = MakeListItem;
            boostListView.bindItem = (el, i) => BindListItem(el, GetCollectedBoosts()[i]);
            boostListView.selectionType = SelectionType.None;
            boostListView.fixedItemHeight = 80;
        }
    }

    private VisualElement MakeListItem()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = row.style.paddingBottom = 5;
        row.style.paddingLeft = row.style.paddingRight = 10;

        var icon = new VisualElement { style = { width = 40, height = 40, marginRight = 10 } };
        var info = new VisualElement { style = { flexDirection = FlexDirection.Column } };
        row.Add(icon);
        row.Add(info);
        return row;
    }

    private void BindListItem(VisualElement el, object item)
    {
        el.userData = item;
        el.Clear();

        var iconEl = new VisualElement { style = { width = 40, height = 40, marginRight = 10 } };
        var info = new VisualElement { style = { flexDirection = FlexDirection.Column } };

        if (item is Potion p)
        {
            if (p.icon) iconEl.style.backgroundImage = new StyleBackground(p.icon);
            else iconEl.style.backgroundColor = p.color;

            info.Add(new Label(p.potionName) { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, color = Color.white } });
            info.Add(new Label(p.effectDescription) { style = { fontSize = 11, color = new Color(0.8f, 0.8f, 0.8f) } });
            info.Add(new Label($"Длительность: {(p.duration > 0 ? $"{p.duration} сек" : "Мгновенно")}") 
            { style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f) } });

            el.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, p, el));
        }
        else if (item is Boost b)
        {
            if (b.icon) iconEl.style.backgroundImage = new StyleBackground(b.icon);
            else iconEl.style.backgroundColor = b.color;

            info.Add(new Label(b.boostName) { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, color = Color.white } });
            info.Add(new Label(b.effectDescription) { style = { fontSize = 11, color = new Color(0.8f, 0.8f, 0.8f) } });
            info.Add(new Label($"Длительность: {b.rounds} раунда(ов)") 
            { style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f) } });

            el.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, b, el));
        }

        el.Add(iconEl);
        el.Add(info);
    }

    private void SetupDragAndDrop()
    {
        // Тащим из слотов
        foreach (var s in potionQuickSlots) if (s != null) s.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
        foreach (var s in boostQuickSlots) if (s != null) s.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);

        // Глобальные события
        inventoryWindow.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        inventoryWindow.RegisterCallback<PointerUpEvent>(OnPointerUpGlobal, TrickleDown.TrickleDown);
    }

    #region Drag & Drop

    private void StartDrag(PointerDownEvent evt, object item, VisualElement source)
    {
        if (isDragging || item == null) return;

        isDragging = true;
        draggedItem = item;
        draggedSource = source;

        dragPreview = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                width = 50,
                height = 50,
                opacity = 0.8f
            },
            pickingMode = PickingMode.Ignore   // Это работает — свойство у VisualElement, а не у style!
        };

        Sprite icon = null; 
        Color color = Color.white;

        if (item is Potion p) { icon = p.icon; color = p.color; }
        else if (item is Boost b) { icon = b.icon; color = b.color; }

        if (icon != null)
            dragPreview.style.backgroundImage = new StyleBackground(icon);
        else
            dragPreview.style.backgroundColor = color;

        UpdateDragPosition(evt.position);
        inventoryWindow.Add(dragPreview);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isDragging) UpdateDragPosition(evt.position);
    }

    private void UpdateDragPosition(Vector2 pos)
    {
        if (dragPreview != null)
        {
            dragPreview.style.left = pos.x - 25;
            dragPreview.style.top = pos.y - 25;
        }
    }

    private void OnPointerUpGlobal(PointerUpEvent evt)
    {
        if (!isDragging) return;

        bool droppedOnSlot = IsOverAnySlot(evt.position);

        if (!droppedOnSlot)
        {
            // Отпустили вне слотов → возвращаем в инвентарь
            ReturnDraggedItemToInventory();
        }
        else
        {
            // Отпустили над слотом → пытаемся положить
            TryAssignToSlot(evt.position);
        }

        StopDrag();
    }

    private bool IsOverAnySlot(Vector2 pos)
    {
        foreach (var s in potionQuickSlots)
            if (s != null && s.worldBound.Contains(pos)) return true;
        foreach (var s in boostQuickSlots)
            if (s != null && s.worldBound.Contains(pos)) return true;
        return false;
    }

    private void TryAssignToSlot(Vector2 pos)
    {
        // Potion slots
        for (int i = 0; i < potionQuickSlots.Length; i++)
        {
            if (potionQuickSlots[i]?.worldBound.Contains(pos) == true && draggedItem is Potion p)
            {
                InventoryData.Instance.currentData.collectedPotionNames.Remove(p.potionName);

                // Убираем это зелье из других слотов
                for (int j = 0; j < 2; j++)
                    if (InventoryData.Instance.currentData.potionSlotNames[j] == p.potionName)
                        InventoryData.Instance.currentData.potionSlotNames[j] = null;

                InventoryData.Instance.currentData.potionSlotNames[i] = p.potionName;
                RefreshInventoryDisplay();
                return;
            }
        }

        // Boost slots
        for (int i = 0; i < boostQuickSlots.Length; i++)
        {
            if (boostQuickSlots[i]?.worldBound.Contains(pos) == true && draggedItem is Boost b)
            {
                InventoryData.Instance.currentData.collectedBoostNames.Remove(b.boostName);

                for (int j = 0; j < 3; j++)
                    if (InventoryData.Instance.currentData.boostSlotNames[j] == b.boostName)
                    {
                        InventoryData.Instance.currentData.boostSlotNames[j] = null;
                        InventoryData.Instance.currentData.boostMatchesRemaining[j] = 0;
                    }

                InventoryData.Instance.currentData.boostSlotNames[i] = b.boostName;
                InventoryData.Instance.currentData.boostMatchesRemaining[i] = 3;
                RefreshInventoryDisplay();
                return;
            }
        }
    }

    private void ReturnDraggedItemToInventory()
    {
        if (draggedItem is Potion p)
        {
            if (!InventoryData.Instance.currentData.collectedPotionNames.Contains(p.potionName))
                InventoryData.Instance.currentData.collectedPotionNames.Add(p.potionName);

            for (int i = 0; i < 2; i++)
                if (InventoryData.Instance.currentData.potionSlotNames[i] == p.potionName)
                    InventoryData.Instance.currentData.potionSlotNames[i] = null;
        }
        else if (draggedItem is Boost b)
        {
            if (!InventoryData.Instance.currentData.collectedBoostNames.Contains(b.boostName))
                InventoryData.Instance.currentData.collectedBoostNames.Add(b.boostName);

            for (int i = 0; i < 3; i++)
                if (InventoryData.Instance.currentData.boostSlotNames[i] == b.boostName)
                {
                    InventoryData.Instance.currentData.boostSlotNames[i] = null;
                    InventoryData.Instance.currentData.boostMatchesRemaining[i] = 0;
                }
        }

        RefreshInventoryDisplay();
    }

    private void OnSlotPointerDown(PointerDownEvent evt)
    {
        VisualElement slot = evt.currentTarget as VisualElement;
        int index = (int)slot.userData;

        object item = null;

        if (potionQuickSlots.Contains(slot))
        {
            string name = InventoryData.Instance.currentData.potionSlotNames[index];
            if (!string.IsNullOrEmpty(name))
                item = InventoryData.Instance.GetPotionByName(name);
        }
        else if (boostQuickSlots.Contains(slot))
        {
            string name = InventoryData.Instance.currentData.boostSlotNames[index];
            if (!string.IsNullOrEmpty(name))
                item = InventoryData.Instance.GetBoostByName(name);
        }

        if (item != null)
        {
            evt.StopPropagation();
            StartDrag(evt, item, slot);
        }
    }

    private void StopDrag()
    {
        isDragging = false;
        draggedItem = null;
        draggedSource = null;
        if (dragPreview != null)
        {
            inventoryWindow.Remove(dragPreview);
            dragPreview = null;
        }
    }
    #endregion

    private void RefreshInventoryDisplay()
    {
        // Просто пересоздаём источники и Rebuild — это надёжнее всего в старых версиях
        if (potionListView != null)
        {
            potionListView.itemsSource = GetCollectedPotions();
            potionListView.Rebuild();
        }
        if (boostListView != null)
        {
            boostListView.itemsSource = GetCollectedBoosts();
            boostListView.Rebuild();
        }

        for (int i = 0; i < 2; i++) UpdatePotionSlotVisual(i);
        for (int i = 0; i < 3; i++) UpdateBoostSlotVisual(i);
    }

    private List<Potion> GetCollectedPotions() =>
        InventoryData.Instance.currentData.collectedPotionNames
            .Select(name => InventoryData.Instance.GetPotionByName(name))
            .Where(p => p != null)
            .ToList();

    private List<Boost> GetCollectedBoosts() =>
        InventoryData.Instance.currentData.collectedBoostNames
            .Select(name => InventoryData.Instance.GetBoostByName(name))
            .Where(b => b != null)
            .ToList();

    private void UpdatePotionSlotVisual(int i)
    {
        var slot = potionQuickSlots[i];
        if (slot == null) return;
        slot.Clear();

        var name = InventoryData.Instance.currentData.potionSlotNames[i];
        if (!string.IsNullOrEmpty(name))
        {
            var p = InventoryData.Instance.GetPotionByName(name);
            if (p) slot.Add(CreateSlotIcon(p.icon, p.color, p.potionName));
        }
        else
        {
            slot.Add(new Label($"Слот {i + 1}") 
            { 
                style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(1,1,1,0.5f), fontSize = 12 } 
            });
        }
    }

    private void UpdateBoostSlotVisual(int i)
    {
        var slot = boostQuickSlots[i];
        if (slot == null) return;
        slot.Clear();

        var name = InventoryData.Instance.currentData.boostSlotNames[i];
        if (!string.IsNullOrEmpty(name))
        {
            var b = InventoryData.Instance.GetBoostByName(name);
            if (b) slot.Add(CreateSlotIcon(b.icon, b.color, b.boostName));
        }
        else
        {
            slot.Add(new Label($"Слот {i + 1}") 
            { 
                style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(1,1,1,0.5f), fontSize = 12 } 
            });
        }
    }

    private VisualElement CreateSlotIcon(Sprite icon, Color color, string text)
    {
        var el = new VisualElement();
        el.style.width = Length.Percent(100);
        el.style.height = Length.Percent(100);

        if (icon != null)
            el.style.backgroundImage = new StyleBackground(icon);
        else
            el.style.backgroundColor = color;

        var label = new Label(text);
        label.style.fontSize = 8;
        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.LowerCenter;
        label.style.position = Position.Absolute;
        label.style.bottom = 0;
        label.style.left = 0;
        label.style.right = 0;
        label.style.backgroundColor = new Color(0, 0, 0, 0.7f);

        el.Add(label);
        return el;
    }

    private void CloseInventory()
    {
        uiDocument.rootVisualElement.Clear();
        FindAnyObjectByType<Ui>()?.ShowMainMenu();
    }

    private void EnsureInventoryDataExists()
    {
        if (InventoryData.Instance != null) return;
        var prefab = Resources.Load<GameObject>("InventoryDataPrefab");
        if (prefab) Instantiate(prefab);
    }
}
#if UNITY_EDITOR && ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class ItemCatalogOdinWindow : OdinEditorWindow
{
    private const string DefaultItemConfigFolderPath = "Assets/Art/Data/InventoryConfig/Item";
    private const float ToolbarButtonHeight = 28f;
    private const float CardWidth = 160f;
    private const float CardHeight = 188f;
    private const float CardSpacing = 12f;
    private const float CardPadding = 12f;

    private enum ItemCategoryTab
    {
        All,
        Weapon,
        Helmet,
        Armor,
        Ammo,
        Medical,
        ChestRig,
        Backpack,
        Collection
    }

    private readonly Dictionary<int, SOItemConfig> itemAssetsById = new Dictionary<int, SOItemConfig>();

    private Vector2 scrollPosition;
    private ItemCategoryTab activeCategoryTab = ItemCategoryTab.All;
    private int selectedItemId;
    private string statusMessage = "Ready.";
    private string itemConfigFolderPath = DefaultItemConfigFolderPath;

    [MenuItem("Tools/GreyZoneOps/Inventory/Item Catalog Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<ItemCatalogOdinWindow>();
        window.titleContent = new GUIContent("Item Config Editor");
        window.minSize = new Vector2(900f, 620f);
        window.Show();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshItemAssets();
    }

    [OnInspectorGUI, PropertyOrder(-100)]
    private void DrawWindow()
    {
        DrawCategoryTabs();
        GUILayout.Space(8f);
        DrawToolbar();
        GUILayout.Space(10f);
        DrawCardGrid();
        GUILayout.Space(10f);
        DrawStatusBar();
    }

    private void DrawCategoryTabs()
    {
        int currentIndex = (int)activeCategoryTab;
        int nextIndex = GUILayout.Toolbar(currentIndex, BuildCategoryTabLabels());
        if (nextIndex == currentIndex)
        {
            return;
        }

        activeCategoryTab = (ItemCategoryTab)nextIndex;
        selectedItemId = GetFirstVisibleItemId();
        Repaint();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Item", GUILayout.Height(ToolbarButtonHeight), GUILayout.Width(140f)))
                {
                    OpenCreateWindow();
                }

                if (GUILayout.Button("Refresh", GUILayout.Height(ToolbarButtonHeight), GUILayout.Width(120f)))
                {
                    RefreshItemAssets(selectedItemId);
                }

                using (new EditorGUI.DisabledScope(!TryGetSelectedVisibleItem(out _)))
                {
                    if (GUILayout.Button("Ping Selected", GUILayout.Height(ToolbarButtonHeight), GUILayout.Width(140f)))
                    {
                        if (TryGetSelectedVisibleItem(out var selected))
                        {
                            EditorGUIUtility.PingObject(selected);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"Visible {GetVisibleItemCount()} / Total {itemAssetsById.Count}", EditorStyles.miniBoldLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Folder", GUILayout.Width(44f));
                EditorGUILayout.SelectableLabel(itemConfigFolderPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
    }

    private void DrawCardGrid()
    {
        List<SOItemConfig> visibleItems = GetVisibleItemsSorted();
        if (visibleItems.Count == 0)
        {
            EditorGUILayout.HelpBox($"No items in {GetActiveCategoryTabLabel()} tab.", MessageType.Info);
            return;
        }

        float availableWidth = Mathf.Max(320f, position.width - 24f);
        int columnCount = Mathf.Max(1, Mathf.FloorToInt((availableWidth + CardSpacing - CardPadding * 2f) / (CardWidth + CardSpacing)));
        int rowCount = Mathf.CeilToInt(visibleItems.Count / (float)columnCount);
        float contentHeight = CardPadding * 2f + rowCount * CardHeight + Mathf.Max(0, rowCount - 1) * CardSpacing;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        Rect gridRect = GUILayoutUtility.GetRect(availableWidth, contentHeight, GUILayout.ExpandWidth(true));

        for (int i = 0; i < visibleItems.Count; i++)
        {
            int row = i / columnCount;
            int column = i % columnCount;
            Rect cardRect = new Rect(
                gridRect.x + CardPadding + column * (CardWidth + CardSpacing),
                gridRect.y + CardPadding + row * (CardHeight + CardSpacing),
                CardWidth,
                CardHeight);

            DrawItemCard(cardRect, visibleItems[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawItemCard(Rect rect, SOItemConfig item)
    {
        if (item == null)
        {
            return;
        }

        Event evt = Event.current;
        bool isSelected = item.Id == selectedItemId;
        Color background = isSelected
            ? new Color(0.22f, 0.42f, 0.72f, 0.35f)
            : new Color(0.18f, 0.18f, 0.18f, 1f);
        Color border = isSelected
            ? new Color(0.35f, 0.62f, 0.96f, 1f)
            : new Color(0.30f, 0.30f, 0.30f, 1f);

        EditorGUI.DrawRect(rect, background);
        DrawBorder(rect, border);

        if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
        {
            selectedItemId = item.Id;
            Repaint();

            if (evt.button == 0 && evt.clickCount >= 2)
            {
                OpenEditWindow(item);
            }

            evt.Use();
        }

        Rect iconRect = new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 92f);
        DrawCardIcon(iconRect, item.Icon);

        Rect idRect = new Rect(rect.x + 12f, rect.y + 114f, rect.width - 24f, 20f);
        EditorGUI.LabelField(idRect, $"ID {item.Id}", EditorStyles.boldLabel);

        Rect nameRect = new Rect(rect.x + 12f, rect.y + 138f, rect.width - 24f, 38f);
        GUI.Label(nameRect, string.IsNullOrWhiteSpace(item.Name) ? "<Unnamed Item>" : item.Name, BuildWrappedLabelStyle());
    }

    private void DrawCardIcon(Rect rect, Sprite icon)
    {
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));
        DrawBorder(rect, new Color(0.24f, 0.24f, 0.24f, 1f));

        Texture preview = null;
        if (icon != null)
        {
            preview = AssetPreview.GetAssetPreview(icon);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(icon);
            }
        }

        if (preview != null)
        {
            GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            return;
        }

        GUI.Label(rect, "No Icon", BuildCenteredMiniLabelStyle());
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.HelpBox(statusMessage, MessageType.None);
    }

    private void OpenCreateWindow()
    {
        ItemCategory? presetCategory = null;
        if (TryMapTabToCategory(activeCategoryTab, out var category))
        {
            presetCategory = category;
        }

        ItemConfigDetailWindow.OpenForCreate(itemConfigFolderPath, presetCategory, HandlePopupSavedOrDeleted);
    }

    private void OpenEditWindow(SOItemConfig asset)
    {
        if (asset == null)
        {
            return;
        }

        ItemConfigDetailWindow.OpenForEdit(asset, itemConfigFolderPath, HandlePopupSavedOrDeleted);
    }

    private void HandlePopupSavedOrDeleted(int preferredItemId, string message)
    {
        RefreshItemAssets(preferredItemId);
        if (!string.IsNullOrWhiteSpace(message))
        {
            SetStatus(message);
        }
    }

    private void RefreshItemAssets(int preferredItemId = 0)
    {
        itemAssetsById.Clear();

        string[] guids = AssetDatabase.FindAssets("t:SOItemConfig");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SOItemConfig asset = AssetDatabase.LoadAssetAtPath<SOItemConfig>(path);
            if (asset == null || asset.Id <= 0)
            {
                continue;
            }

            if (!itemAssetsById.ContainsKey(asset.Id))
            {
                itemAssetsById.Add(asset.Id, asset);
            }
        }

        if (preferredItemId > 0 && itemAssetsById.TryGetValue(preferredItemId, out var preferred) && IsVisibleInCurrentTab(preferred))
        {
            selectedItemId = preferredItemId;
        }
        else if (!TryGetSelectedVisibleItem(out _))
        {
            selectedItemId = GetFirstVisibleItemId();
        }

        SetStatus($"Loaded {GetVisibleItemCount()} items in {GetActiveCategoryTabLabel()} tab. Total={itemAssetsById.Count}.");
    }

    private bool TryGetSelectedVisibleItem(out SOItemConfig selected)
    {
        selected = null;
        if (!itemAssetsById.TryGetValue(selectedItemId, out var asset) || !IsVisibleInCurrentTab(asset))
        {
            return false;
        }

        selected = asset;
        return selected != null;
    }

    private int GetFirstVisibleItemId()
    {
        int bestId = 0;
        foreach (SOItemConfig item in GetVisibleItemsSorted())
        {
            if (bestId == 0 || item.Id < bestId)
            {
                bestId = item.Id;
            }
        }

        return bestId;
    }

    private int GetVisibleItemCount()
    {
        int count = 0;
        foreach (var pair in itemAssetsById)
        {
            if (IsVisibleInCurrentTab(pair.Value))
            {
                count++;
            }
        }

        return count;
    }

    private List<SOItemConfig> GetVisibleItemsSorted()
    {
        var items = new List<SOItemConfig>();
        foreach (var pair in itemAssetsById)
        {
            if (IsVisibleInCurrentTab(pair.Value))
            {
                items.Add(pair.Value);
            }
        }

        items.Sort((a, b) => a.Id.CompareTo(b.Id));
        return items;
    }

    private bool IsVisibleInCurrentTab(SOItemConfig asset)
    {
        if (asset == null)
        {
            return false;
        }

        if (!TryMapTabToCategory(activeCategoryTab, out var category))
        {
            return true;
        }

        return asset.Category == category;
    }

    private string[] BuildCategoryTabLabels()
    {
        return new[]
        {
            BuildCategoryTabLabel(ItemCategoryTab.All, "All"),
            BuildCategoryTabLabel(ItemCategoryTab.Weapon, "Weapon"),
            BuildCategoryTabLabel(ItemCategoryTab.Helmet, "Helmet"),
            BuildCategoryTabLabel(ItemCategoryTab.Armor, "Armor"),
            BuildCategoryTabLabel(ItemCategoryTab.Ammo, "Ammo"),
            BuildCategoryTabLabel(ItemCategoryTab.Medical, "Medical"),
            BuildCategoryTabLabel(ItemCategoryTab.ChestRig, "ChestRig"),
            BuildCategoryTabLabel(ItemCategoryTab.Backpack, "Backpack"),
            BuildCategoryTabLabel(ItemCategoryTab.Collection, "Collection")
        };
    }

    private string BuildCategoryTabLabel(ItemCategoryTab tab, string label)
    {
        return $"{label} ({GetCategoryItemCount(tab)})";
    }

    private int GetCategoryItemCount(ItemCategoryTab tab)
    {
        int count = 0;
        foreach (var pair in itemAssetsById)
        {
            SOItemConfig asset = pair.Value;
            if (asset == null)
            {
                continue;
            }

            if (tab == ItemCategoryTab.All)
            {
                count++;
                continue;
            }

            if (TryMapTabToCategory(tab, out var category) && asset.Category == category)
            {
                count++;
            }
        }

        return count;
    }

    private string GetActiveCategoryTabLabel()
    {
        switch (activeCategoryTab)
        {
            case ItemCategoryTab.Weapon:
                return "Weapon";
            case ItemCategoryTab.Helmet:
                return "Helmet";
            case ItemCategoryTab.Armor:
                return "Armor";
            case ItemCategoryTab.Ammo:
                return "Ammo";
            case ItemCategoryTab.Medical:
                return "Medical";
            case ItemCategoryTab.ChestRig:
                return "ChestRig";
            case ItemCategoryTab.Backpack:
                return "Backpack";
            case ItemCategoryTab.Collection:
                return "Collection";
            default:
                return "All";
        }
    }

    private static bool TryMapTabToCategory(ItemCategoryTab tab, out ItemCategory category)
    {
        switch (tab)
        {
            case ItemCategoryTab.Weapon:
                category = ItemCategory.Weapon;
                return true;
            case ItemCategoryTab.Helmet:
                category = ItemCategory.helmet;
                return true;
            case ItemCategoryTab.Armor:
                category = ItemCategory.Armor;
                return true;
            case ItemCategoryTab.Ammo:
                category = ItemCategory.Ammo;
                return true;
            case ItemCategoryTab.Medical:
                category = ItemCategory.Medical;
                return true;
            case ItemCategoryTab.ChestRig:
                category = ItemCategory.ChestRig;
                return true;
            case ItemCategoryTab.Backpack:
                category = ItemCategory.Backpack;
                return true;
            case ItemCategoryTab.Collection:
                category = ItemCategory.Collection;
                return true;
            default:
                category = default;
                return false;
        }
    }

    private static GUIStyle BuildWrappedLabelStyle()
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            fontSize = 12
        };
        return style;
    }

    private static GUIStyle BuildCenteredMiniLabelStyle()
    {
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
        return style;
    }

    private static void DrawBorder(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
    }

    private void SetStatus(string message)
    {
        statusMessage = $"{DateTime.Now:HH:mm:ss} {message}";
        Repaint();
    }
}
#endif

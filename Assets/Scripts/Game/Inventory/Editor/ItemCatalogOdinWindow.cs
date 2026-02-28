#if UNITY_EDITOR && ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class ItemCatalogOdinWindow : OdinEditorWindow
{
    private const string DefaultCatalogAssetPath = "Assets/Art/Data/InventoryConfig/SOItemCatalog.asset";
    private const string GameConfigAssetPath = "Assets/Resources/SOGameConfig.asset";
    private const string WeaponConfigFolderPath = "Assets/Art/Data/WeaponConfig";
    private const string WeaponConfigNamePrefix = "Weapon_";

    [Serializable]
    private class WeaponConfigDraft
    {
        [LabelText("Weapon Name")]
        public string WeaponName;

        [LabelText("Weapon Type")]
        public WeaponType WeaponType = WeaponType.Firearm;

        [LabelText("Description")]
        [MultiLineProperty(3)]
        public string Discription;

        [LabelText("Weapon Prefab")]
        public GameObject WeaponPrefab;

        [LabelText("Move Speed Multiplier")]
        [MinValue(0f)]
        public float MoveSpeedMultiplier = 1f;

        [LabelText("Run Speed Multiplier")]
        [MinValue(0f)]
        public float RunSpeedMultiplier = 1f;

        [LabelText("Impact Effect")]
        public GameObject ImpactEffect;
    }

    [MenuItem("Tools/GreyZoneOps/Inventory/Item Catalog Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<ItemCatalogOdinWindow>();
        window.titleContent = new GUIContent("Item Catalog");
        window.minSize = new Vector2(760f, 600f);
        window.Show();
    }

    [BoxGroup("Catalog"), LabelText("Item Catalog"), OnValueChanged(nameof(OnCatalogChanged))]
    [SerializeField]
    private SOItemCatalog itemCatalog;

    [BoxGroup("Catalog"), ReadOnly, ShowInInspector, LabelText("Entry Count")]
    private int EntryCount => itemCatalog?.Entries?.Count ?? 0;

    [BoxGroup("Catalog"), HorizontalGroup("Catalog/Actions"), Button("Find Default Catalog")]
    private void FindDefaultCatalog()
    {
        itemCatalog = ResolveDefaultCatalog();
        OnCatalogChanged();
    }

    [BoxGroup("Catalog"), HorizontalGroup("Catalog/Actions"), EnableIf(nameof(HasCatalog)), Button("Ping Catalog")]
    private void PingCatalog()
    {
        EditorGUIUtility.PingObject(itemCatalog);
    }

    [TitleGroup("Modify Existing Item"), LabelText("Selected Item"), ValueDropdown(nameof(GetEntryIdOptions))]
    [OnValueChanged(nameof(OnSelectedItemChanged))]
    [SerializeField]
    private int selectedItemId;

    [TitleGroup("Modify Existing Item"), ShowIf(nameof(HasSelectedEntry)), InlineProperty, HideLabel]
    [SerializeField]
    private ItemCatalogEntry selectedItemDraft = CreateDefaultItemDraft();

    [TitleGroup("Modify Existing Item"), ShowIf(nameof(ShowSelectedWeaponConfig)), LabelText("Sync Weapon Config")]
    [SerializeField]
    private bool syncWeaponConfigOnApply = true;

    [TitleGroup("Modify Existing Item"), ShowIf(nameof(ShowSelectedWeaponConfig)), InlineProperty, HideLabel]
    [SerializeField]
    private WeaponConfigDraft selectedWeaponDraft = CreateDefaultWeaponDraft(string.Empty);

    [TitleGroup("Modify Existing Item"), ShowIf(nameof(ShowSelectedWeaponConfig))]
    [ReadOnly, ShowInInspector, LabelText("Current Weapon Asset")]
    private SOWeaponConfigBase SelectedWeaponAsset => FindWeaponConfigAsset(selectedItemDraft?.Id ?? 0);

    [TitleGroup("Modify Existing Item"), HorizontalGroup("Modify Existing Item/Actions"), EnableIf(nameof(HasSelectedEntry))]
    [Button(ButtonSizes.Medium)]
    private void ReloadSelected()
    {
        LoadSelectedDraftFromCatalog();
        SetStatus("Reloaded selected item from catalog.");
    }

    [TitleGroup("Modify Existing Item"), HorizontalGroup("Modify Existing Item/Actions"), EnableIf(nameof(HasSelectedEntry))]
    [Button(ButtonSizes.Medium)]
    private void ApplyChanges()
    {
        if (!TryGetEntryById(selectedItemId, out var target))
        {
            SetStatus($"Cannot apply changes. Item id={selectedItemId} does not exist.");
            return;
        }

        if (!ValidateDraft(selectedItemDraft, target))
        {
            return;
        }

        CopyItemEntry(selectedItemDraft, target);
        if (syncWeaponConfigOnApply && target.Category == ItemCategory.Weapon)
        {
            UpsertWeaponConfig(target, selectedWeaponDraft);
        }

        selectedItemId = target.Id;
        SaveCatalog($"Updated item id={target.Id}.");
        LoadSelectedDraftFromCatalog();
    }

    [TitleGroup("Modify Existing Item"), HorizontalGroup("Modify Existing Item/Actions"), EnableIf(nameof(HasSelectedEntry))]
    [Button(ButtonSizes.Medium)]
    private void DeleteSelected()
    {
        if (!TryGetEntryById(selectedItemId, out var target))
        {
            SetStatus($"Cannot delete item. Item id={selectedItemId} does not exist.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Delete Item", $"Delete item [{target.Id}] {target.Name}?", "Delete", "Cancel"))
        {
            return;
        }

        itemCatalog.Entries.Remove(target);
        SaveCatalog($"Deleted item id={target.Id}.");

        selectedItemId = 0;
        SyncSelectionAfterCatalogChanged();
    }

    [TitleGroup("Create New Item"), InlineProperty, HideLabel]
    [SerializeField]
    private ItemCatalogEntry newItemDraft = CreateDefaultItemDraft();

    [TitleGroup("Create New Item"), ShowIf(nameof(ShowCreateWeaponConfig)), LabelText("Create/Update Weapon Config")]
    [SerializeField]
    private bool createWeaponConfigForNewItem = true;

    [TitleGroup("Create New Item"), ShowIf(nameof(ShowCreateWeaponConfig)), InlineProperty, HideLabel]
    [SerializeField]
    private WeaponConfigDraft newWeaponDraft = CreateDefaultWeaponDraft(string.Empty);

    [TitleGroup("Create New Item"), HorizontalGroup("Create New Item/Actions"), EnableIf(nameof(HasCatalog))]
    [Button(ButtonSizes.Medium)]
    private void SuggestNextId()
    {
        newItemDraft.Id = GenerateNextItemId();
        SetStatus($"Suggested next item id: {newItemDraft.Id}");
    }

    [TitleGroup("Create New Item"), HorizontalGroup("Create New Item/Actions"), EnableIf(nameof(HasCatalog))]
    [Button(ButtonSizes.Medium)]
    private void CreateItem()
    {
        if (itemCatalog == null)
        {
            SetStatus("Cannot create item. Item catalog is null.");
            return;
        }

        if (!ValidateDraft(newItemDraft, null))
        {
            return;
        }

        var entry = CreateDefaultItemDraft();
        CopyItemEntry(newItemDraft, entry);
        itemCatalog.Entries.Add(entry);

        if (createWeaponConfigForNewItem && entry.Category == ItemCategory.Weapon)
        {
            UpsertWeaponConfig(entry, newWeaponDraft);
        }

        SaveCatalog($"Created item id={entry.Id}.");

        selectedItemId = entry.Id;
        LoadSelectedDraftFromCatalog();

        newItemDraft = CreateDefaultItemDraft();
        CopyItemEntry(entry, newItemDraft);
        newItemDraft.Id = GenerateNextItemId();
    }

    [PropertyOrder(99), ShowInInspector, ReadOnly, MultiLineProperty(3), LabelText("Status")]
    [SerializeField]
    private string statusMessage = "Ready.";

    private bool HasCatalog => itemCatalog != null;

    private bool HasSelectedEntry => TryGetEntryById(selectedItemId, out _);

    private bool ShowSelectedWeaponConfig => selectedItemDraft != null && selectedItemDraft.Category == ItemCategory.Weapon;

    private bool ShowCreateWeaponConfig => newItemDraft != null && newItemDraft.Category == ItemCategory.Weapon;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (selectedItemDraft == null)
        {
            selectedItemDraft = CreateDefaultItemDraft();
        }

        if (newItemDraft == null)
        {
            newItemDraft = CreateDefaultItemDraft();
        }

        if (selectedWeaponDraft == null)
        {
            selectedWeaponDraft = CreateDefaultWeaponDraft(string.Empty);
        }

        if (newWeaponDraft == null)
        {
            newWeaponDraft = CreateDefaultWeaponDraft(string.Empty);
        }

        if (itemCatalog == null)
        {
            itemCatalog = ResolveDefaultCatalog();
        }

        OnCatalogChanged();
    }

    private void OnCatalogChanged()
    {
        if (itemCatalog == null)
        {
            SetStatus("Item catalog is null. Assign one or click Find Default Catalog.");
            return;
        }

        if (itemCatalog.Entries == null)
        {
            itemCatalog.Entries = new List<ItemCatalogEntry>();
            EditorUtility.SetDirty(itemCatalog);
        }

        SyncSelectionAfterCatalogChanged();
        if (newItemDraft != null && newItemDraft.Id <= 0)
        {
            newItemDraft.Id = GenerateNextItemId();
        }

        SetStatus($"Loaded catalog: {itemCatalog.name}, entries={itemCatalog.Entries.Count}");
    }

    private void OnSelectedItemChanged()
    {
        LoadSelectedDraftFromCatalog();
    }

    private void SyncSelectionAfterCatalogChanged()
    {
        if (itemCatalog == null || itemCatalog.Entries == null || itemCatalog.Entries.Count == 0)
        {
            selectedItemId = 0;
            selectedItemDraft = CreateDefaultItemDraft();
            selectedWeaponDraft = CreateDefaultWeaponDraft(string.Empty);
            return;
        }

        if (!TryGetEntryById(selectedItemId, out _))
        {
            var first = GetFirstValidEntry();
            selectedItemId = first != null ? first.Id : 0;
        }

        LoadSelectedDraftFromCatalog();
    }

    private void LoadSelectedDraftFromCatalog()
    {
        if (!TryGetEntryById(selectedItemId, out var source))
        {
            return;
        }

        if (selectedItemDraft == null)
        {
            selectedItemDraft = CreateDefaultItemDraft();
        }

        CopyItemEntry(source, selectedItemDraft);
        selectedWeaponDraft = CreateWeaponDraftFromAsset(source.Id, source.Name);
    }

    private IEnumerable<ValueDropdownItem<int>> GetEntryIdOptions()
    {
        if (itemCatalog == null || itemCatalog.Entries == null || itemCatalog.Entries.Count == 0)
        {
            return new[] { new ValueDropdownItem<int>("No Item", 0) };
        }

        var sorted = new List<ItemCatalogEntry>();
        for (int i = 0; i < itemCatalog.Entries.Count; i++)
        {
            var entry = itemCatalog.Entries[i];
            if (entry != null)
            {
                sorted.Add(entry);
            }
        }

        sorted.Sort((a, b) => a.Id.CompareTo(b.Id));
        var options = new List<ValueDropdownItem<int>>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            var entry = sorted[i];
            options.Add(new ValueDropdownItem<int>($"[{entry.Id}] {entry.Name}", entry.Id));
        }

        return options;
    }

    private ItemCatalogEntry GetFirstValidEntry()
    {
        if (itemCatalog?.Entries == null)
        {
            return null;
        }

        for (int i = 0; i < itemCatalog.Entries.Count; i++)
        {
            if (itemCatalog.Entries[i] != null)
            {
                return itemCatalog.Entries[i];
            }
        }

        return null;
    }

    private bool TryGetEntryById(int itemId, out ItemCatalogEntry entry)
    {
        entry = null;
        if (itemCatalog?.Entries == null || itemId <= 0)
        {
            return false;
        }

        for (int i = 0; i < itemCatalog.Entries.Count; i++)
        {
            var candidate = itemCatalog.Entries[i];
            if (candidate != null && candidate.Id == itemId)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }

    private bool ValidateDraft(ItemCatalogEntry draft, ItemCatalogEntry editingTarget)
    {
        if (itemCatalog == null)
        {
            SetStatus("Validation failed: Item catalog is null.");
            return false;
        }

        if (draft == null)
        {
            SetStatus("Validation failed: Draft is null.");
            return false;
        }

        if (draft.Id <= 0)
        {
            SetStatus("Validation failed: Item Id must be greater than 0.");
            return false;
        }

        if (draft.Size.x <= 0 || draft.Size.y <= 0)
        {
            SetStatus("Validation failed: Item size must be at least 1x1.");
            return false;
        }

        if (draft.MaxStack <= 0)
        {
            SetStatus("Validation failed: MaxStack must be greater than 0.");
            return false;
        }

        if (HasDuplicateId(draft.Id, editingTarget))
        {
            SetStatus($"Validation failed: duplicate Item Id {draft.Id}.");
            return false;
        }

        return true;
    }

    private bool HasDuplicateId(int itemId, ItemCatalogEntry except)
    {
        if (itemCatalog?.Entries == null)
        {
            return false;
        }

        for (int i = 0; i < itemCatalog.Entries.Count; i++)
        {
            var entry = itemCatalog.Entries[i];
            if (entry == null || entry == except)
            {
                continue;
            }

            if (entry.Id == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private int GenerateNextItemId()
    {
        int maxId = 0;
        if (itemCatalog?.Entries != null)
        {
            for (int i = 0; i < itemCatalog.Entries.Count; i++)
            {
                var entry = itemCatalog.Entries[i];
                if (entry != null && entry.Id > maxId)
                {
                    maxId = entry.Id;
                }
            }
        }

        return maxId + 1;
    }

    private void SaveCatalog(string message)
    {
        if (itemCatalog == null)
        {
            SetStatus("Save skipped: Item catalog is null.");
            return;
        }

        itemCatalog.RebuildEntryIndex();
        EditorUtility.SetDirty(itemCatalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SetStatus(message);
    }

    private void UpsertWeaponConfig(ItemCatalogEntry itemEntry, WeaponConfigDraft draft)
    {
        if (itemEntry == null || itemEntry.Id <= 0)
        {
            return;
        }

        if (draft == null)
        {
            draft = CreateDefaultWeaponDraft(itemEntry.Name);
        }

        EnsureFolderExists(WeaponConfigFolderPath);

        var desiredPath = BuildWeaponConfigPath(itemEntry.Id);
        var config = AssetDatabase.LoadAssetAtPath<SOWeaponConfigBase>(desiredPath);
        if (config == null)
        {
            config = FindWeaponConfigAsset(itemEntry.Id);
        }

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SOWeaponConfigBase>();
            AssetDatabase.CreateAsset(config, desiredPath);
        }

        config.WeaponID = itemEntry.Id;
        config.WeaponName = string.IsNullOrWhiteSpace(draft.WeaponName) ? itemEntry.Name : draft.WeaponName;
        config.WeaponType = draft.WeaponType;
        config.Discription = draft.Discription;
        config.WeaponPrefab = draft.WeaponPrefab;
        config.moveSpeedMultiplier = Mathf.Max(0f, draft.MoveSpeedMultiplier);
        config.runSpeedMultiplier = Mathf.Max(0f, draft.RunSpeedMultiplier);
        config.impactEffect = draft.ImpactEffect;

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private SOWeaponConfigBase FindWeaponConfigAsset(int itemId)
    {
        if (itemId <= 0)
        {
            return null;
        }

        var expectedPath = BuildWeaponConfigPath(itemId);
        var config = AssetDatabase.LoadAssetAtPath<SOWeaponConfigBase>(expectedPath);
        if (config != null)
        {
            return config;
        }

        var guids = AssetDatabase.FindAssets($"{WeaponConfigNamePrefix}{itemId} t:SOWeaponConfigBase");
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var loaded = AssetDatabase.LoadAssetAtPath<SOWeaponConfigBase>(path);
            if (loaded != null && loaded.WeaponID == itemId)
            {
                return loaded;
            }
        }

        return null;
    }

    private static string BuildWeaponConfigPath(int itemId)
    {
        return $"{WeaponConfigFolderPath}/{WeaponConfigNamePrefix}{itemId}.asset";
    }

    private WeaponConfigDraft CreateWeaponDraftFromAsset(int itemId, string fallbackWeaponName)
    {
        var draft = CreateDefaultWeaponDraft(fallbackWeaponName);
        if (itemId <= 0)
        {
            return draft;
        }

        var config = FindWeaponConfigAsset(itemId);
        if (config == null)
        {
            return draft;
        }

        draft.WeaponName = string.IsNullOrWhiteSpace(config.WeaponName) ? fallbackWeaponName : config.WeaponName;
        draft.WeaponType = config.WeaponType;
        draft.Discription = config.Discription;
        draft.WeaponPrefab = config.WeaponPrefab;
        draft.MoveSpeedMultiplier = config.moveSpeedMultiplier;
        draft.RunSpeedMultiplier = config.runSpeedMultiplier;
        draft.ImpactEffect = config.impactEffect;
        return draft;
    }

    private static WeaponConfigDraft CreateDefaultWeaponDraft(string defaultWeaponName)
    {
        return new WeaponConfigDraft
        {
            WeaponName = defaultWeaponName,
            WeaponType = WeaponType.Firearm,
            Discription = string.Empty,
            WeaponPrefab = null,
            MoveSpeedMultiplier = 1f,
            RunSpeedMultiplier = 1f,
            ImpactEffect = null
        };
    }

    private static ItemCatalogEntry CreateDefaultItemDraft()
    {
        return new ItemCatalogEntry
        {
            Id = 1,
            Name = string.Empty,
            ResName = string.Empty,
            Size = Vector2Int.one,
            Icon = null,
            MaxStack = 1,
            Category = ItemCategory.Collection,
            Quality = ItemQuality.White,
            Value = 0,
            ContainerConfigId = -1
        };
    }

    private static void CopyItemEntry(ItemCatalogEntry source, ItemCatalogEntry target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.Id = source.Id;
        target.Name = source.Name;
        target.ResName = source.ResName;
        target.Size = new Vector2Int(Mathf.Max(1, source.Size.x), Mathf.Max(1, source.Size.y));
        target.Icon = source.Icon;
        target.MaxStack = Mathf.Max(1, source.MaxStack);
        target.Category = source.Category;
        target.Quality = source.Quality;
        target.Value = Mathf.Max(0, source.Value);
        target.ContainerConfigId = source.ContainerConfigId;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private SOItemCatalog ResolveDefaultCatalog()
    {
        var config = AssetDatabase.LoadAssetAtPath<SOGameConfig>(GameConfigAssetPath);
        if (config != null && config.ItemCatalog != null)
        {
            return config.ItemCatalog;
        }

        var byPath = AssetDatabase.LoadAssetAtPath<SOItemCatalog>(DefaultCatalogAssetPath);
        if (byPath != null)
        {
            return byPath;
        }

        var guids = AssetDatabase.FindAssets("t:SOItemCatalog");
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var catalog = AssetDatabase.LoadAssetAtPath<SOItemCatalog>(path);
            if (catalog != null)
            {
                return catalog;
            }
        }

        return null;
    }

    private void SetStatus(string message)
    {
        statusMessage = $"{DateTime.Now:HH:mm:ss} {message}";
        Repaint();
    }
}
#endif

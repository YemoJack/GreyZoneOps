#if UNITY_EDITOR && ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class ItemConfigDetailWindow : OdinEditorWindow
{
    [Serializable]
    private class ItemConfigDraft
    {
        public int Id;

        public string Name;

        public string ResName;

        public Vector2Int Size = Vector2Int.one;

        public Sprite Icon;

        public int MaxStack = 1;

        public ItemCategory Category = ItemCategory.Collection;

        public ItemQuality Quality = ItemQuality.White;

        public int Value;

        [HideInInspector]
        public bool LockId;
    }

    private Action<int, string> onSavedOrDeleted;
    private SOItemConfig editingAsset;
    [SerializeField, HideInInspector]
    private ItemConfigDraft draft = CreateDefaultDraft();
    private string itemConfigFolderPath;
    private string statusMessage = "Ready.";
    private bool isCreateMode = true;
    private bool awaitingProjectPrefabPick;
    private string initialDraftSnapshot = string.Empty;

    [ShowInInspector, ReadOnly, LabelText("Mode")]
    private string WindowMode => isCreateMode ? "Create" : "Edit";

    [ShowInInspector, ReadOnly, LabelText("Editing Asset")]
    [ShowIf(nameof(HasEditingAsset))]
    private SOItemConfig EditingAsset => editingAsset;

    [ShowInInspector, ReadOnly, MultiLineProperty(2), LabelText("Status")]
    private string Status => statusMessage;

    private bool HasEditingAsset => editingAsset != null;

    [OnInspectorGUI, PropertyOrder(10)]
    private void DrawDraftFields()
    {
        if (draft == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 90f;

            try
            {
                EditorGUI.BeginChangeCheck();

                using (new EditorGUI.DisabledScope(draft.LockId))
                {
                    draft.Id = EditorGUILayout.IntField("Id", draft.Id);
                }

                draft.Name = EditorGUILayout.TextField("Name", draft.Name);
                DrawResNameRow();
                draft.Size = EditorGUILayout.Vector2IntField("Size", draft.Size);
                draft.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", draft.Icon, typeof(Sprite), false);

                bool canEditMaxStack = draft.Category == ItemCategory.Ammo;
                using (new EditorGUI.DisabledScope(!canEditMaxStack))
                {
                    draft.MaxStack = EditorGUILayout.IntField("Max Stack", draft.MaxStack);
                }

                EditorGUI.BeginChangeCheck();
                draft.Category = (ItemCategory)EditorGUILayout.EnumPopup("Category", draft.Category);
                if (EditorGUI.EndChangeCheck())
                {
                    NormalizeDraft();
                }

                draft.Quality = (ItemQuality)EditorGUILayout.EnumPopup("Quality", draft.Quality);
                draft.Value = EditorGUILayout.IntField("Value", draft.Value);

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateUnsavedChangesState();
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }
    }

    public static void OpenForCreate(string folderPath, ItemCategory? presetCategory, Action<int, string> onSavedOrDeleted)
    {
        var window = CreateInstance<ItemConfigDetailWindow>();
        window.InitializeCreateMode(folderPath, presetCategory, onSavedOrDeleted);
        window.titleContent = new GUIContent("Create Item");
        window.minSize = new Vector2(520f, 420f);
        window.ShowUtility();
    }

    public static void OpenForEdit(SOItemConfig asset, string folderPath, Action<int, string> onSavedOrDeleted)
    {
        if (asset == null)
        {
            return;
        }

        var window = CreateInstance<ItemConfigDetailWindow>();
        window.InitializeEditMode(asset, folderPath, onSavedOrDeleted);
        window.titleContent = new GUIContent($"Edit Item {asset.Id}");
        window.minSize = new Vector2(520f, 420f);
        window.ShowUtility();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (draft == null)
        {
            draft = CreateDefaultDraft();
        }

        saveChangesMessage = "You have unsaved Item config changes. Save before closing?";
        UpdateUnsavedChangesState();
    }

    private void OnSelectionChange()
    {
        if (!awaitingProjectPrefabPick || draft == null)
        {
            return;
        }

        var selectedPrefab = Selection.activeObject as GameObject;
        if (selectedPrefab == null || !AssetDatabase.Contains(selectedPrefab))
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedPrefab);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        draft.ResName = selectedPrefab.name;
        awaitingProjectPrefabPick = false;
        SetStatus($"ResName set from prefab: {draft.ResName}");
        UpdateUnsavedChangesState();
        Repaint();
    }

    public override void SaveChanges()
    {
        if (!hasUnsavedChanges)
        {
            base.SaveChanges();
            return;
        }

        if (!TrySaveWithoutClosing())
        {
            return;
        }

        base.SaveChanges();
    }

    public override void DiscardChanges()
    {
        hasUnsavedChanges = false;
        base.DiscardChanges();
    }

    [TitleGroup("Actions"), HorizontalGroup("Actions/Buttons"), Button(ButtonSizes.Medium)]
    private void SaveItem()
    {
        if (!TrySaveWithoutClosing())
        {
            return;
        }
    }

    [TitleGroup("Actions"), HorizontalGroup("Actions/Buttons"), Button(ButtonSizes.Medium), EnableIf(nameof(HasEditingAsset))]
    private void DeleteItem()
    {
        if (editingAsset == null)
        {
            return;
        }

        if (!EditorUtility.DisplayDialog("Delete Item", $"Delete item [{editingAsset.Id}] {editingAsset.Name}?", "Delete", "Cancel"))
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(editingAsset);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        onSavedOrDeleted?.Invoke(0, $"Deleted item asset at {path}.");
        hasUnsavedChanges = false;
        Close();
    }

    private void InitializeCreateMode(string folderPath, ItemCategory? presetCategory, Action<int, string> callback)
    {
        itemConfigFolderPath = string.IsNullOrWhiteSpace(folderPath)
            ? "Assets/Art/Data/InventoryConfig/Item"
            : folderPath;
        onSavedOrDeleted = callback;
        isCreateMode = true;
        editingAsset = null;
        draft = CreateDefaultDraft();
        draft.LockId = false;
        draft.Id = GenerateNextAvailableItemId();
        awaitingProjectPrefabPick = false;
        if (presetCategory.HasValue)
        {
            draft.Category = presetCategory.Value;
        }

        NormalizeDraft();
        ResetUnsavedChangesBaseline();
    }

    private void InitializeEditMode(SOItemConfig asset, string folderPath, Action<int, string> callback)
    {
        itemConfigFolderPath = string.IsNullOrWhiteSpace(folderPath)
            ? "Assets/Art/Data/InventoryConfig/Item"
            : folderPath;
        onSavedOrDeleted = callback;
        isCreateMode = false;
        editingAsset = asset;
        draft = CreateDefaultDraft();
        CopyConfigToDraft(asset, draft);
        draft.LockId = true;
        awaitingProjectPrefabPick = false;
        NormalizeDraft();
        ResetUnsavedChangesBaseline();
    }

    private bool TrySaveWithoutClosing()
    {
        if (!ValidateDraft())
        {
            return false;
        }

        EnsureFolderExists(itemConfigFolderPath);

        bool success = isCreateMode ? CreateAssetInternal() : UpdateAssetInternal();
        if (!success)
        {
            return false;
        }

        hasUnsavedChanges = false;
        return true;
    }

    private bool CreateAssetInternal()
    {
        string assetPath = BuildItemAssetPath(draft.Id);
        var existingAsset = AssetDatabase.LoadAssetAtPath<SOItemConfig>(assetPath);
        if (existingAsset != null)
        {
            SetStatus($"Cannot create item. Asset already exists: {assetPath}");
            return false;
        }

        var asset = CreateInstance<SOItemConfig>();
        CopyDraftToConfig(draft, asset);
        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        onSavedOrDeleted?.Invoke(asset.Id, $"Created item asset: {assetPath}");
        editingAsset = asset;
        isCreateMode = false;
        draft.LockId = true;
        ResetUnsavedChangesBaseline();
        Close();
        return true;
    }

    private bool UpdateAssetInternal()
    {
        if (editingAsset == null)
        {
            SetStatus("Cannot update item. Editing asset is null.");
            return false;
        }

        int oldItemId = editingAsset.Id;
        CopyDraftToConfig(draft, editingAsset);
        RenameAssetToMatchItemId(editingAsset, oldItemId, editingAsset.Id);
        EditorUtility.SetDirty(editingAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        onSavedOrDeleted?.Invoke(editingAsset.Id, $"Updated item id={editingAsset.Id}.");
        ResetUnsavedChangesBaseline();
        Close();
        return true;
    }

    private bool ValidateDraft()
    {
        if (draft == null)
        {
            SetStatus("Validation failed: Draft is null.");
            return false;
        }

        NormalizeDraft();

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

        if (HasDuplicateId(draft.Id))
        {
            SetStatus($"Validation failed: duplicate Item Id {draft.Id}.");
            return false;
        }

        return true;
    }

    private bool HasDuplicateId(int itemId)
    {
        string[] guids = AssetDatabase.FindAssets("t:SOItemConfig");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SOItemConfig asset = AssetDatabase.LoadAssetAtPath<SOItemConfig>(path);
            if (asset == null || asset.Id != itemId)
            {
                continue;
            }

            if (editingAsset != null && asset == editingAsset)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private int GenerateNextAvailableItemId()
    {
        var usedIds = new HashSet<int>();
        string[] guids = AssetDatabase.FindAssets("t:SOItemConfig");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SOItemConfig asset = AssetDatabase.LoadAssetAtPath<SOItemConfig>(path);
            if (asset != null && asset.Id > 0)
            {
                usedIds.Add(asset.Id);
            }
        }

        int candidate = 1;
        while (usedIds.Contains(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    private string BuildItemAssetPath(int itemId)
    {
        return $"{itemConfigFolderPath}/{SOItemConfig.BuildItemConfigKey(itemId)}.asset";
    }

    private void RenameAssetToMatchItemId(SOItemConfig itemAsset, int oldItemId, int newItemId)
    {
        if (itemAsset == null || oldItemId == newItemId || newItemId <= 0)
        {
            return;
        }

        string currentPath = AssetDatabase.GetAssetPath(itemAsset);
        string targetPath = BuildItemAssetPath(newItemId);
        if (string.IsNullOrEmpty(currentPath) || string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var conflictAsset = AssetDatabase.LoadAssetAtPath<SOItemConfig>(targetPath);
        if (conflictAsset != null && conflictAsset != itemAsset)
        {
            SetStatus($"Item asset rename skipped because target already exists: {targetPath}");
            return;
        }

        string error = AssetDatabase.MoveAsset(currentPath, targetPath);
        if (!string.IsNullOrEmpty(error))
        {
            SetStatus($"Failed to move asset: {error}");
        }
    }

    private static ItemConfigDraft CreateDefaultDraft()
    {
        return new ItemConfigDraft
        {
            Id = 1,
            Name = string.Empty,
            ResName = string.Empty,
            Size = Vector2Int.one,
            Icon = null,
            MaxStack = 1,
            Category = ItemCategory.Collection,
            Quality = ItemQuality.White,
            Value = 0
        };
    }

    private void NormalizeDraft()
    {
        if (draft == null)
        {
            return;
        }

        if (draft.Category != ItemCategory.Ammo)
        {
            draft.MaxStack = 1;
            return;
        }

        draft.MaxStack = Mathf.Max(1, draft.MaxStack);
    }

    private void ResetUnsavedChangesBaseline()
    {
        initialDraftSnapshot = BuildDraftSnapshot();
        hasUnsavedChanges = false;
    }

    private void UpdateUnsavedChangesState()
    {
        hasUnsavedChanges = !string.Equals(initialDraftSnapshot, BuildDraftSnapshot(), StringComparison.Ordinal);
    }

    private string BuildDraftSnapshot()
    {
        if (draft == null)
        {
            return string.Empty;
        }

        return string.Join("|",
            draft.Id.ToString(),
            draft.Name ?? string.Empty,
            draft.ResName ?? string.Empty,
            draft.Size.x.ToString(),
            draft.Size.y.ToString(),
            draft.Icon != null ? AssetDatabase.GetAssetPath(draft.Icon) : string.Empty,
            draft.MaxStack.ToString(),
            ((int)draft.Category).ToString(),
            ((int)draft.Quality).ToString(),
            draft.Value.ToString());
    }

    private void DrawResNameRow()
    {
        Rect rect = EditorGUILayout.GetControlRect();
        Rect contentRect = EditorGUI.PrefixLabel(rect, new GUIContent("Res Name"));
        float buttonWidth = 108f;
        float spacing = 6f;
        Rect textRect = new Rect(contentRect.x, contentRect.y, Mathf.Max(60f, contentRect.width - buttonWidth - spacing), contentRect.height);
        Rect buttonRect = new Rect(textRect.xMax + spacing, contentRect.y, buttonWidth, contentRect.height);

        draft.ResName = EditorGUI.TextField(textRect, draft.ResName);

        string buttonLabel = awaitingProjectPrefabPick ? "Cancel Pick" : "Pick Prefab";
        if (GUI.Button(buttonRect, buttonLabel))
        {
            awaitingProjectPrefabPick = !awaitingProjectPrefabPick;
            if (awaitingProjectPrefabPick)
            {
                SetStatus("Click a prefab in the Project window to fill ResName.");
            }
            else
            {
                SetStatus("Prefab picking canceled.");
            }
        }
    }

    private static void CopyConfigToDraft(SOItemConfig source, ItemConfigDraft target)
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
    }

    private static void CopyDraftToConfig(ItemConfigDraft source, SOItemConfig target)
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
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private void SetStatus(string message)
    {
        statusMessage = $"{DateTime.Now:HH:mm:ss} {message}";
        Repaint();
    }
}
#endif

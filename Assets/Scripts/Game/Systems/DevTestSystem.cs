using System.Collections.Generic;
using System.Text;
using QFramework;
using UnityEngine;

public class DevTestSystem : AbstractSystem
{
    // 生成物品测试命令：item 102 / item 102 3
    private const string TestCommandItem = "item";
    // 生成物品的别名命令：spawn 102
    private const string TestCommandSpawn = "spawn";
    // 输出当前玩家容器信息
    private const string TestCommandContainers = "containers";
    // 手动触发保存
    private const string TestCommandSave = "save";
    // 手动触发读档
    private const string TestCommandLoad = "load";
    // 输出帮助信息
    private const string TestCommandHelp = "help";
    // 生成物品到玩家仓库
    private const string TestCommandWarehouse = "warehouse";
    // 生成物品到玩家仓库的别名命令
    private const string TestCommandStash = "stash";
    // 血量测试主命令：health damage/heal/reset
    private const string TestCommandHealth = "health";
    // 血量测试的简写命令
    private const string TestCommandHp = "hp";
    // 提取点测试主命令：extract list/next/leave
    private const string TestCommandExtraction = "extract";
    // 扣血子命令
    private const string TestCommandDamage = "damage";
    // 加血子命令
    private const string TestCommandHeal = "heal";
    // 重置血量子命令
    private const string TestCommandReset = "reset";
    // 列出提取点信息
    private const string TestCommandList = "list";
    // 传送到下一个提取点
    private const string TestCommandNext = "next";
    // 离开当前提取区域
    private const string TestCommandLeave = "leave";

    private Transform testPlayerTransform;
    private int extractionTestIndex;

    protected override void OnInit()
    {
        this.RegisterEvent<EventPlayerSpawned>(OnPlayerSpawned);
        this.RegisterEvent<EventExtractionStarted>(OnExtractionStarted);
        this.RegisterEvent<EventExtractionProgress>(OnExtractionProgress);
        this.RegisterEvent<EventExtractionCancelled>(OnExtractionCancelled);
        this.RegisterEvent<EventExtractionSucceeded>(OnExtractionSucceeded);
    }

    public string ExecuteTestCode(string rawCode)
    {
        string code = string.IsNullOrWhiteSpace(rawCode) ? string.Empty : rawCode.Trim();
        if (string.IsNullOrEmpty(code))
        {
            return BuildTestCommandHelp();
        }

        string[] parts = code.Split(new[] { ' ', '\t', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return BuildTestCommandHelp();
        }

        if (parts.Length == 1)
        {
            if (int.TryParse(parts[0], out int directItemId))
            {
                return SpawnItemById(directItemId, 1);
            }

            string single = parts[0].ToLowerInvariant();
            if (single == TestCommandHelp)
            {
                return BuildTestCommandHelp();
            }

            if (single == TestCommandContainers)
            {
                return BuildContainerSummary();
            }

            if (single == TestCommandSave)
            {
                bool saveOk = this.GetSystem<InventorySystem>()?.SaveGameData() ?? false;
                return $"SaveGameData => {saveOk}";
            }

            if (single == TestCommandLoad)
            {
                bool loadOk = this.GetSystem<InventorySystem>()?.LoadGameData() ?? false;
                return $"LoadGameData => {loadOk}";
            }
        }

        string command = parts[0].ToLowerInvariant();
        if (command == TestCommandItem || command == TestCommandSpawn)
        {
            if (parts.Length < 2)
            {
                return "Usage: item <itemId> [count]";
            }

            if (int.TryParse(parts[1], out int itemId) == false)
            {
                return $"Invalid itemId: {parts[1]}";
            }

            int count = 1;
            if (parts.Length >= 3 && int.TryParse(parts[2], out count) == false)
            {
                return $"Invalid count: {parts[2]}";
            }

            return SpawnItemById(itemId, count);
        }

        if (command == TestCommandWarehouse || command == TestCommandStash)
        {
            if (parts.Length < 2)
            {
                return "Usage: warehouse <itemId> [count]";
            }

            if (int.TryParse(parts[1], out int itemId) == false)
            {
                return $"Invalid itemId: {parts[1]}";
            }

            int count = 1;
            if (parts.Length >= 3 && int.TryParse(parts[2], out count) == false)
            {
                return $"Invalid count: {parts[2]}";
            }

            return AddItemToWarehouse(itemId, count);
        }

        if (command == TestCommandHealth || command == TestCommandHp)
        {
            return ExecuteHealthCommand(parts);
        }

        if (command == TestCommandExtraction)
        {
            return ExecuteExtractionCommand(parts);
        }

        return $"Unknown command: {parts[0]}\n{BuildTestCommandHelp()}";
    }

    public string GetHelpText()
    {
        return BuildTestCommandHelp();
    }

    private void OnPlayerSpawned(EventPlayerSpawned e)
    {
        testPlayerTransform = e.PlayerTransform;
    }

    private void OnExtractionStarted(EventExtractionStarted e)
    {
        Debug.Log($"[ExtractionTest] Started => id={e.ExtractionId}, duration={e.Duration:0.00}s");
    }

    private void OnExtractionProgress(EventExtractionProgress e)
    {
    }

    private void OnExtractionCancelled(EventExtractionCancelled e)
    {
        Debug.Log($"[ExtractionTest] Cancelled => id={e.ExtractionId}");
    }

    private void OnExtractionSucceeded(EventExtractionSucceeded e)
    {
        Debug.Log($"[ExtractionTest] Succeeded => id={e.ExtractionId}");
    }

    private string ExecuteHealthCommand(string[] parts)
    {
        var healthSystem = this.GetSystem<HealthSystem>();
        var health = healthSystem?.GetPlayerHealth();
        if (healthSystem == null || health == null)
        {
            return "HealthSystem not ready.";
        }

        if (parts.Length < 2)
        {
            return "Usage: health damage <value> | health heal <value> | health reset";
        }

        string action = parts[1].ToLowerInvariant();
        if (action == TestCommandReset)
        {
            healthSystem.ResetHealth();
            return $"Player HP reset => {health.CurrentHealth}/{health.MaxHealth}";
        }

        if (parts.Length < 3 || float.TryParse(parts[2], out float value) == false)
        {
            return $"Invalid health value: {(parts.Length >= 3 ? parts[2] : "<missing>")}";
        }

        if (action == TestCommandDamage)
        {
            healthSystem.ApplyDamage(value);
            return $"Player HP -{value:0.##} => {health.CurrentHealth}/{health.MaxHealth}";
        }

        if (action == TestCommandHeal)
        {
            healthSystem.Heal(value);
            return $"Player HP +{value:0.##} => {health.CurrentHealth}/{health.MaxHealth}";
        }

        return $"Unknown health command: {parts[1]}";
    }

    private string ExecuteExtractionCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            return "Usage: extract list | extract next | extract leave";
        }

        string action = parts[1].ToLowerInvariant();
        if (action == TestCommandList)
        {
            return BuildExtractionPointsSummary();
        }

        if (action == TestCommandNext)
        {
            return TeleportPlayerToNextExtractionPoint();
        }

        if (action == TestCommandLeave)
        {
            return TeleportPlayerOutsideExtractionRange();
        }

        return $"Unknown extraction command: {parts[1]}";
    }

    private string BuildExtractionPointsSummary()
    {
        var map = this.GetModel<MapModel>()?.CurrentMap;
        if (map == null)
        {
            return "[ExtractionTest] Current map is null.";
        }

        if (map.extractionPoints == null || map.extractionPoints.Count == 0)
        {
            return "[ExtractionTest] Map has no extraction points.";
        }

        var builder = new StringBuilder();
        for (int i = 0; i < map.extractionPoints.Count; i++)
        {
            var p = map.extractionPoints[i];
            builder.AppendLine($"#{i} id={p.ExtractionId}, name={p.DisplayName}, enabled={p.EnabledOnStart}, type={p.ExtractionType}, trigger={p.TriggerType}, pos={p.Position}, radius={p.Radius:0.00}, box={p.TriggerBoxSize}, duration={p.ExtractDuration:0.00}s");
        }

        return builder.ToString().TrimEnd();
    }

    private string TeleportPlayerToNextExtractionPoint()
    {
        if (!TryGetTestPlayerTransform(out var player))
        {
            return "[ExtractionTest] Player transform not found.";
        }

        var map = this.GetModel<MapModel>()?.CurrentMap;
        if (map == null || map.extractionPoints == null || map.extractionPoints.Count == 0)
        {
            return "[ExtractionTest] No extraction points available.";
        }

        if (extractionTestIndex < 0 || extractionTestIndex >= map.extractionPoints.Count)
        {
            extractionTestIndex = 0;
        }

        var point = map.extractionPoints[extractionTestIndex];
        player.position = point.Position;
        string result = $"[ExtractionTest] Teleport player to extraction #{extractionTestIndex}: id={point.ExtractionId}, enabled={point.EnabledOnStart}, trigger={point.TriggerType}";

        extractionTestIndex = (extractionTestIndex + 1) % map.extractionPoints.Count;
        return result;
    }

    private string TeleportPlayerOutsideExtractionRange()
    {
        if (!TryGetTestPlayerTransform(out var player))
        {
            return "[ExtractionTest] Player transform not found.";
        }

        var map = this.GetModel<MapModel>()?.CurrentMap;
        if (map == null || map.extractionPoints == null || map.extractionPoints.Count == 0)
        {
            player.position += Vector3.right * 10f;
            return "[ExtractionTest] Move player away by +X 10 (no extraction config).";
        }

        var nearest = map.extractionPoints[0];
        var nearestDist = (player.position - nearest.Position).sqrMagnitude;
        for (int i = 1; i < map.extractionPoints.Count; i++)
        {
            var candidate = map.extractionPoints[i];
            var dist = (player.position - candidate.Position).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearest = candidate;
                nearestDist = dist;
            }
        }

        var direction = player.position - nearest.Position;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        var triggerExtent = nearest.TriggerType == MapExtractionTriggerType.Box
            ? Mathf.Max(Mathf.Abs(nearest.TriggerBoxSize.x), Mathf.Max(Mathf.Abs(nearest.TriggerBoxSize.y), Mathf.Abs(nearest.TriggerBoxSize.z))) * 0.5f
            : Mathf.Max(0f, nearest.Radius);

        var safeDistance = triggerExtent + 8f;
        player.position = nearest.Position + direction.normalized * safeDistance;
        return $"[ExtractionTest] Teleport player out of extraction range. nearest={nearest.ExtractionId}, safeDistance={safeDistance:0.00}";
    }

    private bool TryGetTestPlayerTransform(out Transform player)
    {
        player = testPlayerTransform;
        if (player != null)
        {
            return true;
        }

        var runtime = Object.FindObjectOfType<PlayerRuntime>();
        if (runtime != null)
        {
            player = runtime.transform;
            testPlayerTransform = player;
            return true;
        }

        return false;
    }

    private string SpawnItemById(int itemId, int count)
    {
        GameFlowSystem flowSystem = this.GetSystem<GameFlowSystem>();
        GameFlowState state = flowSystem != null ? flowSystem.CurrentState : GameFlowState.None;
        if (state != GameFlowState.InRaid)
        {
            return "item/spawn command can only be used in raid. Use warehouse <itemId> [count] for stash testing out of raid.";
        }

        if (!TryGetTestPlayerTransform(out _))
        {
            return "item/spawn command requires a spawned player in raid.";
        }

        var inventorySystem = this.GetSystem<InventorySystem>();
        if (inventorySystem == null)
        {
            return "InventorySystem is null.";
        }

        if (itemId <= 0)
        {
            return $"Invalid itemId: {itemId}";
        }

        if (count <= 0)
        {
            return $"Invalid count: {count}";
        }

        if (SOItemConfig.TryLoadConfigById(itemId, out var definition) == false || definition == null)
        {
            return $"Item config not found. itemId={itemId}, key={SOItemConfig.BuildItemConfigKey(itemId)}";
        }

        int remaining = count;
        int spawnedCount = 0;
        int maxStack = Mathf.Max(1, definition.MaxStack);

        while (remaining > 0)
        {
            int stackCount = Mathf.Min(remaining, maxStack);
            var itemInstance = new ItemInstance(definition, stackCount);
            inventorySystem.DropItem(itemInstance);

            spawnedCount += stackCount;
            remaining -= stackCount;
        }

        if (spawnedCount <= 0)
        {
            return $"Spawn failed. itemId={itemId}, count={count}.";
        }

        if (remaining > 0)
        {
            return $"Spawn partial. item={definition.Name}({itemId}), success={spawnedCount}, remaining={remaining}, target=world-near-player";
        }

        return $"Spawn success. item={definition.Name}({itemId}), count={spawnedCount}, target=world-near-player";
    }

    private string BuildContainerSummary()
    {
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null)
        {
            return "Inventory model not ready.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Containers:");

        foreach (InventoryContainerType type in System.Enum.GetValues(typeof(InventoryContainerType)))
        {
            string id = model.GetPlayerContainerId(type);
            string name = model.GetPlayerContainerName(type);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            builder.AppendLine($"{type}: {name} ({id})");
        }

        if (builder.Length <= "Containers:".Length + 2)
        {
            builder.AppendLine("No player containers available.");
        }

        builder.AppendLine("Usage:");
        builder.AppendLine("102");
        builder.AppendLine("item 102");
        builder.AppendLine("item 102 3");
        builder.AppendLine("save / load / containers");
        return builder.ToString().TrimEnd();
    }

    private string AddItemToWarehouse(int itemId, int count)
    {
        var persistentModel = this.GetModel<PersistentInventoryModel>();
        if (persistentModel == null)
        {
            return "PersistentInventoryModel is null.";
        }

        if (itemId <= 0)
        {
            return $"Invalid itemId: {itemId}";
        }

        if (count <= 0)
        {
            return $"Invalid count: {count}";
        }

        if (SOItemConfig.TryLoadConfigById(itemId, out var definition) == false || definition == null)
        {
            return $"Item config not found. itemId={itemId}, key={SOItemConfig.BuildItemConfigKey(itemId)}";
        }

        List<ItemInstance> items = persistentModel.GetMutableItems();
        if (items == null)
        {
            return "Persistent warehouse item list is null.";
        }

        int remaining = count;
        int addedCount = 0;
        int maxStack = Mathf.Max(1, definition.MaxStack);

        while (remaining > 0)
        {
            int stackCount = Mathf.Min(remaining, maxStack);
            var itemInstance = new ItemInstance(definition, stackCount)
            {
                AttachedContainer = null
            };

            items.Add(itemInstance);
            addedCount += stackCount;
            remaining -= stackCount;
        }

        persistentModel.ClearWarehouseItemPositions();
        this.GetSystem<PlayerProgressSystem>()?.RefreshProgress();
        return $"Warehouse add success. item={definition.Name}({itemId}), count={addedCount}";
    }

    private string BuildTestCommandHelp()
    {
        return "TestCode commands:\n102\nitem <itemId> [count]\nwarehouse <itemId> [count]\nstash <itemId> [count]\ncontainers\nsave\nload\nhealth damage <value>\nhealth heal <value>\nhealth reset\nextract list\nextract next\nextract leave";
    }
}

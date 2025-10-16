using UnityEngine;
using System.Collections.Generic;

public class Building : MonoBehaviour
{
    // 建筑的原型定义和当前等级
    public BuildingArchetype Archetype { get; private set; }
    public int LevelIndex { get; private set; } = 0;
    public BuildingLevelDef CurrentLevelDef => Archetype.levels[LevelIndex];

    // 资源库存：自身库存和绑定的外部仓库
    public Inventory Inventory { get; private set; }
    public Building assignedStorage;  // 可选：绑定的仓库建筑

    // 建筑的动态统计值（经验、当前人口、当前工人等）
    private Dictionary<string, float> stats = new Dictionary<string, float>();

    // 方便访问的属性
    public int CurrentExp
    {
        get => (int)(stats.ContainsKey("Experience") ? stats["Experience"] : 0);
        set => stats["Experience"] = value;
    }
    public int CurrentPopulation
    {
        get => (int)(stats.ContainsKey("Population") ? stats["Population"] : 0);
        set => stats["Population"] = value;
    }
    public int CurrentWorkers
    {
        get => (int)(stats.ContainsKey("Workers") ? stats["Workers"] : 0);
        set => stats["Workers"] = value;
    }

    /// <summary>
    /// 初始化建筑实例，设置原型和等级，并配置相应功能。
    /// </summary>
    public void Initialize(BuildingArchetype archetype, int startLevel = 0)
    {
        if (archetype == null)
        {
            Debug.LogError("Initialize failed: archetype is null");
            return;
        }
        Archetype = archetype;
        LevelIndex = Mathf.Clamp(startLevel, 0, Archetype.levels.Count - 1);

        // 初始化统计值
        stats.Clear();
        stats["Experience"] = 0;
        stats["Population"] = 0;
        stats["Workers"] = 0;
        // 可根据需要初始化其他统计，如满意度等

        // 配置自身库存（如果该等级提供库存容量）
        if (CurrentLevelDef.InventoryCapacity > 0)
        {
            Inventory = new Inventory(CurrentLevelDef.InventoryCapacity, this);
            ResourceNetwork.RegisterInventory(Inventory);
        }
        else
        {
            Inventory = null;
        }

        // 如有其他需要在建造时执行的效果，可以在此调用相应规则
        // 例如可在这里调用 FireRules(RuleTrigger.OnBuilt) 来执行 OnBuilt 触发的规则
    }

    /// <summary>
    /// 执行与指定触发事件匹配的所有规则。
    /// </summary>
    public void FireRules(RuleTrigger trigger)
    {
        if (Archetype == null) return;
        // 获取当前等级的规则列表
        List<Rule> rules = CurrentLevelDef.rules;
        if (rules == null) return;

        // 遍历规则并逐一检查执行
        foreach (Rule rule in rules)
        {
            rule.CheckAndExecute(this, trigger);
            // 注意：CheckAndExecute内部已处理升级的情形（UpgradeToNextLevel会中断规则执行）
            // 如果发生升级，LevelIndex将改变，因此可以在下次触发时按新规则执行
        }
    }

    /// <summary>
    /// 将建筑升级到指定等级。
    /// </summary>
    public void UpgradeToLevel(int newLevel)
    {
        // 检查新等级有效性
        if (Archetype == null)
        {
            Debug.LogError("Upgrade failed: Building has no Archetype");
            return;
        }
        if (newLevel < 0 || newLevel >= Archetype.levels.Count)
        {
            Debug.LogWarning($"Upgrade failed: invalid level {newLevel}");
            return;
        }
        if (newLevel == LevelIndex)
        {
            return; // 等级未改变
        }

        // 卸载当前等级的库存（如果新等级不再需要库存或容量变小）
        if (Inventory != null)
        {
            int oldCapacity = Inventory.Capacity;
            int newCapacity = Archetype.levels[newLevel].InventoryCapacity;
            if (newCapacity <= 0)
            {
                // 新等级无库存，则移除当前库存
                ResourceNetwork.UnregisterInventory(Inventory);
                Inventory = null;
            }
            else if (newCapacity != oldCapacity)
            {
                // 更新库存容量
                Inventory.Capacity = newCapacity;
                // 如当前存量超过新容量，需要处理溢出（此处简化处理）
                if (Inventory.CurrentAmount > newCapacity)
                {
                    Debug.Log($"Warning: inventory overflow on upgrade of {name}, truncating excess resources.");
                    Inventory.TruncateExcess();  // 将多余物资丢弃或另行处理
                }
            }
        }
        else
        {
            // 旧等级无库存，检查新等级是否增加库存需求
            int newCapacity = Archetype.levels[newLevel].InventoryCapacity;
            if (newCapacity > 0)
            {
                Inventory = new Inventory(newCapacity, this);
                ResourceNetwork.RegisterInventory(Inventory);
            }
        }

        // 更新等级和相关属性
        LevelIndex = newLevel;
        // 重置经验值（升级后经验从0开始累积下一等级）
        CurrentExp = 0;

        // （可选）如果升级改变了提供人口/就业等，可以在此调整 Population 或 Workers 等统计
        // 例如：升级增加住房容量，可以决定是否马上增加当前Population等。

        // 提示：无需直接修改规则列表，FireRules会根据LevelIndex自动使用新等级的规则
    }

    /// <summary>
    /// 调整建筑的某项统计值（用于StatModifier等）。
    /// </summary>
    public void AdjustStat(string statKey, float delta)
    {
        if (!stats.ContainsKey(statKey))
        {
            stats[statKey] = 0;
        }
        stats[statKey] += delta;
    }

    // 示例：在销毁时注销库存
    private void OnDestroy()
    {
        if (Inventory != null)
        {
            ResourceNetwork.UnregisterInventory(Inventory);
        }
    }
}

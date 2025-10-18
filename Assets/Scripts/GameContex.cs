// Assets/Game/Scripts/Runtime/Services.cs
using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public interface IGameContext
{
    ResourceNetwork ResourceNetwork { get; }
    TechTree TechTree { get; }

    CityEnvironment Environment { get; }
}

public class GameContext : MonoBehaviour, IGameContext
{
    private ResourceNetwork resourceNetwork = new ResourceNetwork();
    private TechTree techTree = new TechTree();
    private CityEnvironment environment = new CityEnvironment();

    /// <summary>资源网络：负责仓库注册、库存查询。</summary>
    public ResourceNetwork ResourceNetwork => resourceNetwork;

    /// <summary>科技树：用于校验科技节点。</summary>
    public TechTree TechTree => techTree;

    /// <summary>城市环境：用于处理治安、医疗、美化等光环。</summary>
    public CityEnvironment Environment => environment;

    private void Awake()
    {
        // 兜底初始化，避免在场景中缺失引用。
        if (resourceNetwork == null)
        {
            resourceNetwork = new ResourceNetwork();
        }

        if (techTree == null)
        {
            techTree = new TechTree();
        }

        if (environment == null)
        {
            environment = new CityEnvironment();
        }
    }
}
public class ResourceNetwork
{
    private readonly HashSet<Inventory> storages = new HashSet<Inventory>();

    /// <summary>注册仓库，供全局检索。</summary>
    public void RegisterStorage(Inventory inv)
    {
        if (inv == null)
        {
            return;
        }

        storages.Add(inv);
    }

    /// <summary>移除仓库注册。</summary>
    public void UnregisterStorage(Inventory inv)
    {
        if (inv == null)
        {
            return;
        }

        storages.Remove(inv);
    }

    /// <summary>统计所有仓库的库存总量。</summary>
    public int GetTotalQuantity()
    {
        int total = 0;
        foreach (Inventory storage in storages)
        {
            total += storage.TotalQuantity;
        }

        return total;
    }

    /// <summary>
    /// 获取建筑当前绑定的仓库。
    /// 优先返回显式绑定的仓库，若不存在则回退至自身仓库。
    /// </summary>
    public Inventory GetAssignedStorage(BuildingInstance self)
    {
        if (self == null)
        {
            return null;
        }

        if (self.AssignedStorage != null && self.AssignedStorage.Storage != null)
        {
            return self.AssignedStorage.Storage;
        }

        return self.Storage;
    }
}


public class TechTree
{


    public bool HasNode(string id)
    {
        return true;
    }
}



 
#region 光环

/// <summary>
/// 环境光环的单圈配置。
/// </summary>
[Serializable]
public struct AuraRing
{
    [Min(0)] public int Radius;
    public int Value;
}

/// <summary>
/// 光环类型：治安、医疗、美化。
/// </summary>
public enum AuraCategory
{
    Security,
    Health,
    Beauty
}
/// <summary>
/// 负责统计并查询城市环境类光环的辅助服务。
/// </summary>
public class CityEnvironment
{
    private struct AuraKey : IEquatable<AuraKey>
    {
        public AuraCategory Category;
        public Vector3Int Cell;

        public bool Equals(AuraKey other)
        {
            return Category == other.Category && Cell.Equals(other.Cell);
        }

        public override bool Equals(object obj)
        {
            if (obj is AuraKey other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Category.GetHashCode();
            hash = hash * 31 + Cell.GetHashCode();
            return hash;
        }
    }

    private class AuraRecord
    {
        public AuraCategory Category;
        public Dictionary<Vector3Int, int> CellValues = new Dictionary<Vector3Int, int>();
    }

    private readonly Dictionary<string, AuraRecord> activeAuras = new Dictionary<string, AuraRecord>();
    private readonly Dictionary<AuraKey, int> gridValues = new Dictionary<AuraKey, int>();

    /// <summary>
    /// 应用光环，旧数据会被覆盖。
    /// </summary>
    public void ApplyAura(string sourceId, Vector3 center, bool centerIsCorner, AuraCategory category, IReadOnlyList<AuraRing> rings)
    {
        if (string.IsNullOrEmpty(sourceId))
        {
            return;
        }

        RemoveAura(sourceId);

        if (rings == null || rings.Count == 0)
        {
            return;
        }

        AuraRecord record = new AuraRecord
        {
            Category = category
        };

        for (int i = 0; i < rings.Count; i++)
        {
            AuraRing ring = rings[i];
            if (ring.Radius < 0 || ring.Value <= 0)
            {
                continue;
            }

            List<Vector3Int> cells = CoordinateCalculator.CellsInRadius(center, ring.Radius, centerIsCorner, DistanceMetric.Manhattan, true);
            for (int c = 0; c < cells.Count; c++)
            {
                Vector3Int cell = cells[c];
                if (record.CellValues.TryGetValue(cell, out int existing))
                {
                    if (ring.Value > existing)
                    {
                        record.CellValues[cell] = ring.Value;
                    }
                }
                else
                {
                    record.CellValues.Add(cell, ring.Value);
                }
            }
        }

        activeAuras[sourceId] = record;

        foreach (KeyValuePair<Vector3Int, int> pair in record.CellValues)
        {
            AuraKey key = new AuraKey
            {
                Category = record.Category,
                Cell = pair.Key
            };

            if (gridValues.TryGetValue(key, out int value))
            {
                gridValues[key] = value + pair.Value;
            }
            else
            {
                gridValues.Add(key, pair.Value);
            }
        }
    }

    /// <summary>
    /// 移除指定建筑的光环贡献。
    /// </summary>
    public void RemoveAura(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId))
        {
            return;
        }

        if (!activeAuras.TryGetValue(sourceId, out AuraRecord record))
        {
            return;
        }

        foreach (KeyValuePair<Vector3Int, int> pair in record.CellValues)
        {
            AuraKey key = new AuraKey
            {
                Category = record.Category,
                Cell = pair.Key
            };

            if (!gridValues.TryGetValue(key, out int value))
            {
                continue;
            }

            int reduced = value - pair.Value;
            if (reduced <= 0)
            {
                gridValues.Remove(key);
            }
            else
            {
                gridValues[key] = reduced;
            }
        }

        activeAuras.Remove(sourceId);
    }

    /// <summary>
    /// 查询某个格子的光环总值。
    /// </summary>
    public int GetValue(Vector3Int cell, AuraCategory category)
    {
        AuraKey key = new AuraKey
        {
            Category = category,
            Cell = cell
        };

        if (gridValues.TryGetValue(key, out int value))
        {
            return value;
        }

        return 0;
    }

    /// <summary>遍历所有有光环覆盖的格子。</summary>
    public IEnumerable<Vector3Int> EnumerateActiveCells()
    {
        HashSet<Vector3Int> yielded = new HashSet<Vector3Int>();
        foreach (KeyValuePair<AuraKey, int> pair in gridValues)
        {
            if (pair.Value <= 0)
            {
                continue;
            }

            if (yielded.Add(pair.Key.Cell))
            {
                yield return pair.Key.Cell;
            }
        }
    }

    /// <summary>遍历指定类型光环覆盖的所有格子。</summary>
    public IEnumerable<Vector3Int> EnumerateActiveCells(AuraCategory category)
    {
        foreach (KeyValuePair<AuraKey, int> pair in gridValues)
        {
            if (pair.Key.Category != category || pair.Value <= 0)
            {
                continue;
            }

            yield return pair.Key.Cell;
        }
    }

    /// <summary>根据数值条件筛选格子。</summary>
    public IEnumerable<Vector3Int> EnumerateCells(AuraCategory category, Func<int, bool> predicate)
    {
        if (predicate == null)
        {
            yield break;
        }

        foreach (KeyValuePair<AuraKey, int> pair in gridValues)
        {
            if (pair.Key.Category != category)
            {
                continue;
            }

            int value = pair.Value;
            if (value <= 0)
            {
                continue;
            }

            if (predicate(value))
            {
                yield return pair.Key.Cell;
            }
        }
    }

    /// <summary>判断格子光环是否大于等于指定阈值。</summary>
    public bool MeetsMinimum(Vector3Int cell, AuraCategory category, int minValue)
    {
        return GetValue(cell, category) >= minValue;
    }

    /// <summary>判断格子光环是否小于等于指定阈值。</summary>
    public bool MeetsMaximum(Vector3Int cell, AuraCategory category, int maxValue)
    {
        return GetValue(cell, category) <= maxValue;
    }

    /// <summary>判断格子光环是否等于指定数值。</summary>
    public bool MeetsExact(Vector3Int cell, AuraCategory category, int value)
    {
        return GetValue(cell, category) == value;
    }

    /// <summary>生成“至少为”条件。</summary>
    public Func<Vector3Int, bool> CreateMinimumCondition(AuraCategory category, int minValue)
    {
        return cell => MeetsMinimum(cell, category, minValue);
    }

    /// <summary>生成“至多为”条件。</summary>
    public Func<Vector3Int, bool> CreateMaximumCondition(AuraCategory category, int maxValue)
    {
        return cell => MeetsMaximum(cell, category, maxValue);
    }

    /// <summary>生成“等于”条件。</summary>
    public Func<Vector3Int, bool> CreateExactCondition(AuraCategory category, int value)
    {
        return cell => MeetsExact(cell, category, value);
    }

    /// <summary>根据条件筛选格子。</summary>
    public IEnumerable<Vector3Int> EnumerateCellsSatisfying(params Func<Vector3Int, bool>[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
        {
            yield break;
        }

        foreach (Vector3Int cell in EnumerateActiveCells())
        {
            bool pass = true;
            for (int i = 0; i < conditions.Length; i++)
            {
                Func<Vector3Int, bool> condition = conditions[i];
                if (condition == null)
                {
                    continue;
                }

                if (!condition(cell))
                {
                    pass = false;
                    break;
                }
            }

            if (pass)
            {
                yield return cell;
            }
        }
    }

    /// <summary>根据条件列表筛选格子。</summary>
    public IEnumerable<Vector3Int> EnumerateCellsSatisfying(IReadOnlyList<Func<Vector3Int, bool>> conditions)
    {
        if (conditions == null || conditions.Count == 0)
        {
            yield break;
        }

        foreach (Vector3Int cell in EnumerateActiveCells())
        {
            bool pass = true;
            for (int i = 0; i < conditions.Count; i++)
            {
                Func<Vector3Int, bool> condition = conditions[i];
                if (condition == null)
                {
                    continue;
                }

                if (!condition(cell))
                {
                    pass = false;
                    break;
                }
            }

            if (pass)
            {
                yield return cell;
            }
        }
    }
}




#endregion

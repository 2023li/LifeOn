using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 资源网络：集中管理所有资源的库存、容量和运输覆盖范围。
/// 职责：资源的添加/消耗、仓库容量管理、运输覆盖计算等。
/// </summary>
public class ResourceNetwork
{
    /// <summary>全局资源库存：按资源类型（SupplyDef）存储当前数量。</summary>
    private readonly Dictionary<SupplyDef, int> _resourceAmounts = new Dictionary<SupplyDef, int>();

    /// <summary>总容量上限（由所有仓库提供），以及当前已用容量。</summary>
    private int _totalCapacity;
    private int _usedCapacity;

    /// <summary>已注册的仓库实例及其容量（键：仓库建筑实例，值：该仓库提供的容量）。</summary>
    private readonly Dictionary<BuildingInstance, int> _warehouses = new Dictionary<BuildingInstance, int>();

    /// <summary>按资源记录的生产者列表（谁在生产什么资源）。</summary>
    private readonly Dictionary<SupplyDef, HashSet<BuildingInstance>> _producersByResource = new Dictionary<SupplyDef, HashSet<BuildingInstance>>();

    /// <summary>按资源缓存的覆盖范围（可达格子集合）。</summary>
    private readonly Dictionary<SupplyDef, HashSet<Vector3Int>> _coverageCache = new Dictionary<SupplyDef, HashSet<Vector3Int>>();

    /// <summary>标记哪些资源的覆盖缓存已失效，需要重算。</summary>
    private readonly HashSet<SupplyDef> _dirtyCoverage = new HashSet<SupplyDef>();

    #region 公共API：资源增减

    public int GetAmount(SupplyDef resource)
    {
        if (resource == null)
            return 0;

        return _resourceAmounts.TryGetValue(resource, out var amount) ? amount : 0;
    }



    /// <summary>
    /// 尝试添加指定资源数量到网络中。
    /// 若总容量不足以存放，则不添加并返回 false，同时给出失败原因。
    /// </summary>
    public bool TryAddResource(SupplyDef resource, int amount, out string reason)
    {
        reason = string.Empty;

        if (resource == null || amount <= 0)
        {
            reason = "资源无效或数量必须为正数。";
            return false;
        }

        int capacityNeeded = amount * resource.OccupationUnit;
        int freeCapacity = _totalCapacity - _usedCapacity;

        if (capacityNeeded > freeCapacity)
        {
            reason = $"容量不足：需要 {capacityNeeded}，当前剩余 {freeCapacity}。";
            return false;
        }

        if (_resourceAmounts.TryGetValue(resource, out int current))
        {
            long newAmount = (long)current + amount;
            if (newAmount > int.MaxValue)
            {
                reason = "库存溢出：数量过大。";
                return false;
            }
            _resourceAmounts[resource] = (int)newAmount;
        }
        else
        {
            _resourceAmounts[resource] = amount;
        }

        _usedCapacity += capacityNeeded;
        if (_usedCapacity < 0) _usedCapacity = 0;

        return true;
    }

    /// <summary>
    /// 尝试消耗指定资源数量。
    /// 若库存不足，则不消耗并返回 false，给出原因。
    /// </summary>
    public bool TryConsumeResource(SupplyDef resource, int amount, out string reason)
    {
        reason = string.Empty;

        if (resource == null || amount <= 0)
        {
            reason = "资源无效或数量必须为正数。";
            return false;
        }

        if (!_resourceAmounts.TryGetValue(resource, out int current) || current < amount)
        {
            int available = current > 0 ? current : 0;
            reason = $"库存不足：{resource.DisplayName} 仅有 {available}，需要 {amount}。";
            return false;
        }

        int newAmount = current - amount;
        if (newAmount <= 0)
            _resourceAmounts.Remove(resource);
        else
            _resourceAmounts[resource] = newAmount;

        int capacityFreed = amount * resource.OccupationUnit;
        _usedCapacity -= capacityFreed;
        if (_usedCapacity < 0) _usedCapacity = 0;

        return true;
    }

    #endregion

    #region 公共API：生产者 & 仓库注册



    public int GetFreeCapacity()
    {
        int free = _totalCapacity - _usedCapacity;
        return free > 0 ? free : 0;
    }

    /// <summary>
    /// 注册一个生产指定资源的生产者建筑。
    /// 建议在建筑开始具备生产能力时调用。
    /// </summary>
    public void RegisterProducer(BuildingInstance producer, SupplyDef resource)
    {
        if (producer == null || resource == null)
            return;

        if (!_producersByResource.TryGetValue(resource, out var set))
        {
            set = new HashSet<BuildingInstance>();
            _producersByResource[resource] = set;
        }

        if (set.Add(producer))
        {
            InvalidateCoverage(resource);
        }
    }

    /// <summary>
    /// 注销一个生产者建筑（建筑销毁、停产等）。
    /// </summary>
    public void UnregisterProducer(BuildingInstance producer, SupplyDef resource)
    {
        if (producer == null || resource == null)
            return;

        if (_producersByResource.TryGetValue(resource, out var set))
        {
            if (set.Remove(producer))
            {
                InvalidateCoverage(resource);
                if (set.Count == 0)
                    _producersByResource.Remove(resource);
            }
        }
    }

    /// <summary>
    /// 注册一个仓库建筑，从而增加全局容量，并影响覆盖范围（所有资源）。
    /// </summary>
    public void RegisterWarehouse(BuildingInstance warehouse)
    {
        if (warehouse == null) return;

        int capacity = GetBuildingCapacity(warehouse);
        if (capacity <= 0)
            return; // 不是仓库

        if (_warehouses.TryGetValue(warehouse, out int old))
        {
            if (old == capacity)
                return;

            _warehouses[warehouse] = capacity;
            _totalCapacity += (capacity - old);
        }
        else
        {
            _warehouses.Add(warehouse, capacity);
            _totalCapacity += capacity;
        }

        if (_totalCapacity < 0) _totalCapacity = 0;

        // 仓库位置变化会影响所有资源覆盖
        InvalidateAllCoverage();
    }

    /// <summary>
    /// 移除一个已注册的仓库建筑，减少全局容量，并影响覆盖范围（所有资源）。
    /// </summary>
    public void UnregisterWarehouse(BuildingInstance warehouse)
    {
        if (warehouse == null) return;

        if (_warehouses.TryGetValue(warehouse, out int capacity))
        {
            _warehouses.Remove(warehouse);
            _totalCapacity -= capacity;
            if (_totalCapacity < 0) _totalCapacity = 0;

            // 不强制删超出的资源，由设计决定；此处仅阻止继续增加。
            InvalidateAllCoverage();
        }
    }

    #endregion

    #region 公共API：覆盖 &可达性查询（带缓存）

    /// <summary>
    /// 判断某格子是否能够接收到指定资源。
    /// 结果使用覆盖缓存，如结构有变动才会重算。
    /// </summary>
    public bool CanCellReceive(SupplyDef resource, Vector3Int cell)
    {
        if (resource == null) return false;

        var coverage = GetCoverage(resource);
        return coverage.Contains(cell);
    }

    /// <summary>
    /// 获取指定资源当前可达的所有格子（缓存复用）。
    /// 注意：返回的是内部集合引用，外部请只读使用，不要修改。
    /// </summary>
    public HashSet<Vector3Int> GetCoverage(SupplyDef resource)
    {
        if (resource == null)
            return EmptyHashSet;

        // 无生产者，直接空
        if (!_producersByResource.TryGetValue(resource, out var producers) || producers.Count == 0)
        {
            _coverageCache[resource] = EmptyHashSet;
            _dirtyCoverage.Remove(resource);
            return EmptyHashSet;
        }

        // 有缓存且未标记为脏，则直接返回
        if (_coverageCache.TryGetValue(resource, out var cached) && !_dirtyCoverage.Contains(resource))
            return cached;

        // 需要重算
        var computed = ComputeCoverage(resource, producers);
        _coverageCache[resource] = computed;
        _dirtyCoverage.Remove(resource);
        return computed;
    }

    private static readonly HashSet<Vector3Int> EmptyHashSet = new HashSet<Vector3Int>();

    #endregion

    #region 覆盖计算实现

    /// <summary>
    /// 实际计算覆盖范围：
    /// 从所有生产者出发，以资源运输半径扩散；
    /// 在半径内连到仓库则作为中继点继续扩散，实现“仓库链路扩展”。
    /// </summary>
    private HashSet<Vector3Int> ComputeCoverage(SupplyDef resource, HashSet<BuildingInstance> producers)
    {
        var result = new HashSet<Vector3Int>();
        int radius = resource.BaseTransportationRadius;
        if (radius <= 0)
            return result;

        // BFS 节点：生产者 + 仓库
        var queue = new Queue<BuildingInstance>();
        var visitedWarehouses = new HashSet<BuildingInstance>();

        // 1. 所有生产者作为起点
        foreach (var producer in producers)
        {
            if (producer == null) continue;
            queue.Enqueue(producer);

            var center = ToCell(producer.CenterInGrid);
            MarkCoverage(center, radius, result);
        }

        // 2. 从生产者出发寻找可连接的仓库，中继扩展覆盖
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentCenter = ToCell(current.CenterInGrid);

            // 遍历所有仓库，找到在 radius 内且尚未访问的作为中继
            foreach (var kv in _warehouses)
            {
                var warehouse = kv.Key;
                if (warehouse == null || visitedWarehouses.Contains(warehouse))
                    continue;

                var wCenter = ToCell(warehouse.CenterInGrid);
                int dist = GridDistance(currentCenter, wCenter);
                if (dist <= radius)
                {
                    // 该仓库可由当前节点供给，作为中继
                    visitedWarehouses.Add(warehouse);
                    queue.Enqueue(warehouse);

                    // 以这个仓库为中心继续扩散
                    MarkCoverage(wCenter, radius, result);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 将世界坐标/浮点网格坐标转换为格子坐标（向最近格取整）。
    /// </summary>
    private Vector3Int ToCell(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            0);
    }

    /// <summary>
    /// 标记以某个中心为起点、指定曼哈顿半径内的所有格子为可达。
    /// （如果你有更精细的寻路代价，可在这里替换实现）
    /// </summary>
    private void MarkCoverage(Vector3Int center, int radius, HashSet<Vector3Int> resultSet)
    {
        int cx = center.x;
        int cy = center.y;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
                {
                    resultSet.Add(new Vector3Int(cx + dx, cy + dy, 0));
                }
            }
        }
    }

    /// <summary>
    /// 计算两个格子间的曼哈顿距离。
    /// </summary>
    private int GridDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    #endregion

    #region 工具 & 缓存失效

    /// <summary>
    /// 获取建筑实例的容量（当前等级的 BaseStorageCapacity），非仓库则为0。
    /// </summary>
    private int GetBuildingCapacity(BuildingInstance building)
    {
        if (building == null || building.Def == null)
            return 0;

        var levels = building.Def.Levels;
        if (levels == null || building.LevelIndex < 0 || building.LevelIndex >= building.Def.Levels.Count)
            return 0;

        var levelDef = levels[building.LevelIndex];
        if (levelDef == null)
            return 0;

        return Mathf.Max(0, levelDef.BaseStorageCapacity);
    }

    /// <summary>
    /// 标记某资源的覆盖缓存失效。
    /// </summary>
    private void InvalidateCoverage(SupplyDef resource)
    {
        if (resource == null) return;
        _dirtyCoverage.Add(resource);
    }

    /// <summary>
    /// 标记所有资源的覆盖缓存失效。
    /// 仓库结构变化时调用。
    /// </summary>
    private void InvalidateAllCoverage()
    {
        _coverageCache.Clear();
        _dirtyCoverage.Clear();
    }

    #endregion





    #region 可视化 / 调试

    /// <summary>
    /// 高亮展示指定资源当前的可达范围。
    /// - 使用 ResourceNetwork 的覆盖缓存（GetCoverage）
    /// - 使用 GridSystem 的特效图层高亮
    /// </summary>
    /// <param name="resource">要展示的资源类型</param>
    /// <param name="tile">
    /// 可选：指定高亮用的 Tile。
    /// 若为 null，则使用 GridSystem 中配置的默认高亮 Tile。
    /// </param>
    public void HighlightCoverage(SupplyDef resource, TileBase tile = null)
    {
        var grid = GridSystem.Instance;
        if (grid == null)
        {
            Debug.LogWarning("[ResourceNetwork] HighlightCoverage 调用失败：找不到 GridSystem 实例。");
            return;
        }

        // 未指定资源：理解为清理高亮
        if (resource == null)
        {
            grid.ClearHighlight();
            return;
        }

        var coverage = GetCoverage(resource);

        // 没有覆盖：清理高亮即可，避免残影
        if (coverage == null || coverage.Count == 0)
        {
            grid.ClearHighlight();
            return;
        }

        if (tile != null)
        {
            grid.SetHighlight(coverage, tile);
        }
        else
        {
            // 使用 GridSystem 的默认 visualizationTile
            grid.SetHighlight(coverage);
        }
    }

    #endregion




}

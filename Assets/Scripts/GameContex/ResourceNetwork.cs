using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 资源网络：集中管理所有资源的库存、容量和运输覆盖范围。
/// 容量提供者（仓库）与转运节点（运输建筑）完全解耦。
/// </summary>
public class ResourceNetwork
{
    // ========= 基础数据 =========

    /// <summary>全局资源库存：按资源类型存储当前数量。</summary>
    private readonly Dictionary<SupplyDef, int> _resourceAmounts = new Dictionary<SupplyDef, int>();

    /// <summary>总容量上限（由所有容量提供者提供）与已用容量。</summary>
    private int _totalCapacity;
    private int _usedCapacity;

    /// <summary>
    /// 容量提供者：
    /// 键：建筑实例；值：该建筑当前提供的容量。
    /// 满足 RO_MaxStorageCapacity > 0 即可。
    /// </summary>
    private readonly Dictionary<BuildingInstance, int> _capacityProviders =
        new Dictionary<BuildingInstance, int>();

    /// <summary>
    /// 转运节点：
    /// 仅 CurrentTransportationAbility == true 的建筑。
    /// 是否有容量无关。
    /// </summary>
    private readonly HashSet<BuildingInstance> _transportNodes =
        new HashSet<BuildingInstance>();

    /// <summary>按资源记录的生产者列表。</summary>
    private readonly Dictionary<SupplyDef, HashSet<BuildingInstance>> _producersByResource =
        new Dictionary<SupplyDef, HashSet<BuildingInstance>>();

    /// <summary>按资源缓存的覆盖范围。</summary>
    private readonly Dictionary<SupplyDef, HashSet<Vector3Int>> _coverageCache =
        new Dictionary<SupplyDef, HashSet<Vector3Int>>();

    /// <summary>标记哪些资源的覆盖需要重算。</summary>
    private readonly HashSet<SupplyDef> _dirtyCoverage = new HashSet<SupplyDef>();

    /// <summary>供给链缓存：资源 -> (格子 -> 建筑链)。</summary>
    private readonly Dictionary<SupplyDef, Dictionary<Vector3Int, List<BuildingInstance>>> _chainCache =
        new Dictionary<SupplyDef, Dictionary<Vector3Int, List<BuildingInstance>>>();

    private static readonly HashSet<Vector3Int> EmptyHashSet = new HashSet<Vector3Int>();

    // ========= 资源增减 =========

    public int GetAmount(SupplyDef resource)
    {
        if (resource == null) return 0;
        return _resourceAmounts.TryGetValue(resource, out var v) ? v : 0;
    }

    public int GetFreeCapacity()
    {
        int free = _totalCapacity - _usedCapacity;
        return free > 0 ? free : 0;
    }

    public bool TryAddResource(SupplyDef resource, int amount, out string reason)
    {
        reason = string.Empty;

        if (resource == null || amount <= 0)
        {
            reason = "资源无效或数量必须为正数";
            return false;
        }

        int need = amount * resource.OccupationUnit;
        int free = _totalCapacity - _usedCapacity;
        if (need > free)
        {
            reason = $"容量不足，需要 {need}，仅剩 {free}";
            return false;
        }

        if (_resourceAmounts.TryGetValue(resource, out var current))
        {
            long nv = (long)current + amount;
            if (nv > int.MaxValue)
            {
                reason = "数量过大，超出上限";
                return false;
            }
            _resourceAmounts[resource] = (int)nv;
        }
        else
        {
            _resourceAmounts[resource] = amount;
        }

        _usedCapacity += need;
        if (_usedCapacity < 0) _usedCapacity = 0;
        return true;
    }

    public bool TryConsumeResource(SupplyDef resource, int amount, out string reason)
    {
        reason = string.Empty;

        if (resource == null || amount <= 0)
        {
            reason = "资源无效或数量必须为正数";
            return false;
        }

        if (!_resourceAmounts.TryGetValue(resource, out var current) || current < amount)
        {
            int have = current > 0 ? current : 0;
            reason = $"库存不足：需要 {amount}，仅有 {have}";
            return false;
        }

        int nv = current - amount;
        if (nv <= 0)
            _resourceAmounts.Remove(resource);
        else
            _resourceAmounts[resource] = nv;

        int freed = amount * resource.OccupationUnit;
        _usedCapacity -= freed;
        if (_usedCapacity < 0) _usedCapacity = 0;
        return true;
    }

    // ========= 生产者注册 =========

    public void RegisterProducer(BuildingInstance producer, SupplyDef resource)
    {
        if (producer == null || resource == null) return;

        if (!_producersByResource.TryGetValue(resource, out var set))
        {
            set = new HashSet<BuildingInstance>();
            _producersByResource[resource] = set;
        }

        if (set.Add(producer))
            InvalidateCoverage(resource);
    }

    public void UnregisterProducer(BuildingInstance producer, SupplyDef resource)
    {
        if (producer == null || resource == null) return;

        if (_producersByResource.TryGetValue(resource, out var set)
            && set.Remove(producer))
        {
            InvalidateCoverage(resource);
            if (set.Count == 0)
                _producersByResource.Remove(resource);
        }
    }

    // ========= 容量提供者注册 =========

    /// <summary>注册 / 更新容量提供者（RO_MaxStorageCapacity > 0）。</summary>
    public void RegisterCapacityProvider(BuildingInstance building)
    {
        if (building == null) return;

        int capacity = Mathf.Max(0, building.RO_MaxStorageCapacity);
        if (capacity <= 0)
        {
            UnregisterCapacityProvider(building);
            return;
        }

        if (_capacityProviders.TryGetValue(building, out var old))
        {
            if (old == capacity) return;
            _capacityProviders[building] = capacity;
            _totalCapacity += (capacity - old);
        }
        else
        {
            _capacityProviders[building] = capacity;
            _totalCapacity += capacity;
        }

        if (_totalCapacity < 0) _totalCapacity = 0;
        // 容量变化不影响覆盖，不用刷新覆盖。
    }

    public void UnregisterCapacityProvider(BuildingInstance building)
    {
        if (building == null) return;

        if (_capacityProviders.TryGetValue(building, out var cap))
        {
            _capacityProviders.Remove(building);
            _totalCapacity -= cap;
            if (_totalCapacity < 0) _totalCapacity = 0;
        }
    }

    // ========= 转运节点注册 =========

    /// <summary>注册转运节点（仅根据运输能力）。</summary>
    public void RegisterTransportNode(BuildingInstance building)
    {
        if (building == null || !building.CurrentTransportationAbility)
            return;

        if (_transportNodes.Add(building))
            InvalidateAllCoverage();
    }

    public void UnregisterTransportNode(BuildingInstance building)
    {
        if (building == null) return;

        if (_transportNodes.Remove(building))
            InvalidateAllCoverage();
    }

    /// <summary>当已注册转运点的位置 / 阻力变化时调用。</summary>
    public void NotifyTransportNodeChanged(BuildingInstance building)
    {
        if (building == null) return;
        if (_transportNodes.Contains(building))
            InvalidateAllCoverage();
    }

    // ========= 覆盖查询 =========

    public bool CanCellReceive(SupplyDef resource, Vector3Int cell)
    {
        if (resource == null) return false;
        var coverage = GetCoverage(resource);
        return coverage.Contains(cell);
    }

    public HashSet<Vector3Int> GetCoverage(SupplyDef resource)
    {
        if (resource == null)
            return EmptyHashSet;

        if (!_producersByResource.TryGetValue(resource, out var producers)
            || producers == null || producers.Count == 0)
        {
            _coverageCache[resource] = EmptyHashSet;
            _dirtyCoverage.Remove(resource);
            _chainCache[resource] = new Dictionary<Vector3Int, List<BuildingInstance>>();
            return EmptyHashSet;
        }

        if (_coverageCache.TryGetValue(resource, out var cached)
            && !_dirtyCoverage.Contains(resource))
        {
            return cached;
        }

        var computed = ComputeCoverage(resource, producers);
        _coverageCache[resource] = computed;
        _dirtyCoverage.Remove(resource);
        return computed;
    }

    // ========= 覆盖计算（核心：生产者 + 转运节点） =========

    private HashSet<Vector3Int> ComputeCoverage(SupplyDef resource, HashSet<BuildingInstance> producers)
    {
        var result = new HashSet<Vector3Int>();
        var cellChainMap = new Dictionary<Vector3Int, List<BuildingInstance>>();

        int radius = resource.BaseTransportationRadius;
        int maxDurability = resource.BaseDurability;
        if (radius <= 0 || maxDurability <= 0)
        {
            _chainCache[resource] = new Dictionary<Vector3Int, List<BuildingInstance>>();
            return result;
        }

        var bestCost = new Dictionary<BuildingInstance, int>();
        var queue = new Queue<(BuildingInstance node, int cost, List<BuildingInstance> path)>();

        // 1. 所有生产者作为起点（耗损 0）
        foreach (var producer in producers)
        {
            if (producer == null) continue;

            var startPath = new List<BuildingInstance> { producer };
            queue.Enqueue((producer, 0, startPath));

            var centerCell = ToCell(producer.CurrentCenterInGrid);
            MarkCoverageWithChain(centerCell, radius, startPath, result, cellChainMap);
        }

        // 固定一份转运节点列表
        var nodes = new List<BuildingInstance>(_transportNodes);

        // 2. BFS：只在转运节点之间跳转
        while (queue.Count > 0)
        {
            var (current, costSoFar, pathSoFar) = queue.Dequeue();
            var currentCenter = ToCell(current.CurrentCenterInGrid);

            foreach (var node in nodes)
            {
                if (node == null || node == current)
                    continue;

                int dist = GridDistance(currentCenter, ToCell(node.CurrentCenterInGrid));
                if (dist > radius)
                    continue;

                int resistance = Mathf.Max(1, node.RO_TransportationResistance);
                int newCost = costSoFar + resistance;
                if (newCost > maxDurability)
                    continue;

                if (bestCost.TryGetValue(node, out var prev) && prev <= newCost)
                    continue;

                bestCost[node] = newCost;

                var newPath = new List<BuildingInstance>(pathSoFar) { node };
                queue.Enqueue((node, newCost, newPath));

                var nodeCenter = ToCell(node.CurrentCenterInGrid);
                MarkCoverageWithChain(nodeCenter, radius, newPath, result, cellChainMap);
            }
        }

        _chainCache[resource] = cellChainMap;
        return result;
    }

    private void MarkCoverageWithChain(
        Vector3Int center,
        int radius,
        List<BuildingInstance> chainPath,
        HashSet<Vector3Int> resultSet,
        Dictionary<Vector3Int, List<BuildingInstance>> cellChainMap)
    {
        int cx = center.x;
        int cy = center.y;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius)
                    continue;

                var cell = new Vector3Int(cx + dx, cy + dy, 0);
                resultSet.Add(cell);

                if (!cellChainMap.ContainsKey(cell))
                    cellChainMap[cell] = new List<BuildingInstance>(chainPath);
            }
        }
    }

    public List<BuildingInstance> GetSupplyChainPath(SupplyDef resource, Vector3Int cell)
    {
        if (resource == null) return null;

        var coverage = GetCoverage(resource);
        if (!coverage.Contains(cell))
            return null;

        if (_chainCache.TryGetValue(resource, out var map)
            && map.TryGetValue(cell, out var path))
        {
            return new List<BuildingInstance>(path);
        }

        return null;
    }

    // ========= 工具 & 缓存 =========

    private void InvalidateCoverage(SupplyDef resource)
    {
        if (resource == null) return;
        _dirtyCoverage.Add(resource);
    }

    private void InvalidateAllCoverage()
    {
        _coverageCache.Clear();
        _dirtyCoverage.Clear();
        _chainCache.Clear();
    }

    private Vector3Int ToCell(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            0);
    }

    private int GridDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // ========= 可视化调试 =========

    public void HighlightCoverage(SupplyDef resource, TileBase tile = null)
    {
        var grid = GridSystem.Instance;
        if (grid == null)
        {
            Debug.LogWarning("[ResourceNetwork] HighlightCoverage 失败：GridSystem 实例不存在");
            return;
        }

        if (resource == null)
        {
            grid.ClearHighlight();
            return;
        }

        var coverage = GetCoverage(resource);
        if (coverage == null || coverage.Count == 0)
        {
            grid.ClearHighlight();
            return;
        }

        if (tile != null)
            grid.SetHighlight(coverage, tile);
        else
            grid.SetHighlight(coverage);
    }
}

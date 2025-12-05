
//XY平面单元格计算器
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DistanceMetric
{
    Chebyshev,

    Manhattan,
    Euclidean
}

public enum GridDirection
{
    None,
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
}


public enum ScopeCheckMode
{
    CenterOnly,
    AnyCellOverlap
}

public static class CoordinateCalculator
{


    /// <summary>
    /// 由中心（格坐标）与尺寸 S 枚举占地（S×S）。
    /// 奇数：center 是格心；偶数：center 是拐角。
    /// </summary>
    public static List<Vector3Int> GetBuildingCells(Vector3 centerGridPos, int size)
    {
        if (size <= 0) size = 1;
        int S = size;
        int k = S / 2;

        // 先把传入中心吸附到最近的“整数网点”
        // （奇数时这个整数网点代表格心；偶数时代表拐角）
        int cx = Mathf.RoundToInt(centerGridPos.x);
        int cy = Mathf.RoundToInt(centerGridPos.y);

        int xmin, xmax, ymin, ymax;

        if ((S % 2) == 1)
        {
            // 奇数：中心在格心（整数格心）
            xmin = cx - k; xmax = cx + k;
            ymin = cy - k; ymax = cy + k;
        }
        else
        {
            // 偶数：中心在拐角（整数拐角）
            xmin = cx - k; xmax = cx + k - 1;
            ymin = cy - k; ymax = cy + k - 1;
        }

        var cells = new List<Vector3Int>(S * S);
        for (int y = ymin; y <= ymax; y++)
            for (int x = xmin; x <= xmax; x++)
                cells.Add(new Vector3Int(x, y, 0));

        return cells;
    }


    /// <summary>
    /// 由占地格集合反推出中心、尺寸与中心类型。
    /// 要求：集合为轴对齐、无空洞的实心正方形。
    /// </summary>
    public static bool TryGetCenterFromCells(IReadOnlyCollection<Vector3Int> cells, out Vector2 center, out bool centerIsCorner, out int size)
    {
        center = default;
        centerIsCorner = default;
        size = 0;
        if (cells == null || cells.Count == 0) return false;

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        var set = new HashSet<Vector3Int>(cells);

        foreach (var c in cells)
        {
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y; if (c.y > maxY) maxY = c.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        if (width != height) return false; // 不是正方形
        size = width;

        // 校验实心
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (!set.Contains(new Vector3Int(x, y, 0))) return false;

        int S = size;
        int k = S / 2;

        if (S % 2 == 1)
        {
            // 奇数：中心是格心
            center = new Vector2(minX + k, minY + k);
            centerIsCorner = false;
        }
        else
        {
            // 偶数：中心是拐角
            center = new Vector2(minX + k, minY + k);
            centerIsCorner = true;
        }
        return true;
    }


    /// <summary>
    /// 采样“半径范围”内的格子（以中心为基准）。
    /// includeEdge=true：包含边缘；false：严格内部。
    /// 对 Euclidean，可设置 useEuclideanPlusHalf=true 使用（≤ R + 0.5）的视觉/逻辑更圆润的判定。
    /// </summary>
    public static List<Vector3Int> CellsInRadius(Vector3 center, int radius, bool centerIsCorner, DistanceMetric metric = DistanceMetric.Manhattan, bool includeEdge = true, bool useEuclideanPlusHalf = true, int safetyPadding = 1)
    {
        radius = Mathf.Max(0, radius);

        // 搜索包围盒（粗略，保证包含全部可能点）
        int k = radius + safetyPadding;
        int xmin = Mathf.FloorToInt(center.x) - k;
        int xmax = Mathf.FloorToInt(center.x) + k;
        int ymin = Mathf.FloorToInt(center.y) - k;
        int ymax = Mathf.FloorToInt(center.y) + k;

        var result = new List<Vector3Int>();

        // 欧式距离时，偶数中心（拐角）与格心存在(0.5,0.5)的天然偏移
        Vector2 cellCenterOffset = Vector2.zero;
        if (metric == DistanceMetric.Euclidean && centerIsCorner)
            cellCenterOffset = new Vector2(0.5f, 0.5f);

        // 阈值设置
        float threshold = radius;
        if (metric == DistanceMetric.Euclidean && useEuclideanPlusHalf && includeEdge)
            threshold = radius + 0.5f;
        else if (!includeEdge)
        {
            // 严格内部
            if (metric == DistanceMetric.Euclidean)
                threshold = radius - 0.5f;
            else
                threshold = radius - Mathf.Epsilon;
        }

        for (int y = ymin; y <= ymax; y++)
            for (int x = xmin; x <= xmax; x++)
            {
                // 当前格心（格坐标）
                float cx = x + 0.5f;
                float cy = y + 0.5f;

                float dx = (cx - cellCenterOffset.x) - center.x;
                float dy = (cy - cellCenterOffset.y) - center.y;

                bool inside = false;
                switch (metric)
                {
                    case DistanceMetric.Chebyshev:
                        {
                            float cheb = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                            inside = includeEdge ? (cheb <= radius) : (cheb < radius);
                            break;
                        }
                    case DistanceMetric.Manhattan:
                        {
                            float manh = Mathf.Abs(dx) + Mathf.Abs(dy);
                            inside = includeEdge ? (manh <= radius) : (manh < radius);
                            break;
                        }
                    case DistanceMetric.Euclidean:
                        {
                            float e2 = dx * dx + dy * dy;
                            float thr = threshold;
                            if (!includeEdge && !useEuclideanPlusHalf)
                            {
                                // 严格且不加0.5时：按 R 的平方比较
                                inside = (e2 < (radius * radius));
                            }
                            else
                            {
                                inside = (Mathf.Sqrt(e2) <= thr);
                            }
                            break;
                        }
                }

                if (inside)
                    result.Add(new Vector3Int(x, y, 0));
            }

        return result;
    }


    public static List<Vector3Int> CellsInRadius(IEnumerable<Vector3Int> vector3Ints, int radius,
    DistanceMetric metric = DistanceMetric.Manhattan,
    bool includeEdge = true,
    bool useEuclideanPlusHalf = true,
    int safetyPadding = 1)
{
    if (vector3Ints == null)
        return new List<Vector3Int>();

    // 聚合：以“格心”为基准计算质心（x+0.5, y+0.5）
    long count = 0;
    double sumX = 0, sumY = 0;

    foreach (var v in vector3Ints)
    {
        sumX += (double)v.x + 0.5;
        sumY += (double)v.y + 0.5;
        count++;
    }

    if (count == 0)
        return new List<Vector3Int>();

    float cx = (float)(sumX / count);
    float cy = (float)(sumY / count);
    var center = new Vector3(cx, cy, 0f);

    // 自动推断是否为格点拐角：
    // 若质心刚好落在整点（整数坐标）上，则认为 centerIsCorner = true
    // 允许极小数值误差
    bool IsNearlyInteger(float v)
    {
        const float eps = 1e-5f;
        return Mathf.Abs(v - Mathf.Round(v)) <= eps;
    }

    bool centerIsCorner = IsNearlyInteger(cx) && IsNearlyInteger(cy);

    // 复用原有实现
    return CellsInRadius(center, radius, centerIsCorner, metric, includeEdge, useEuclideanPlusHalf, safetyPadding);
}

    /// <summary>
    /// 可到达的单元格
    /// </summary>
    /// <param name="originCells"></param>
    /// <param name="movePower"></param>
    /// <returns></returns>
    public static List<Vector3Int> GetReachableCellsByMovePower(IEnumerable<Vector3Int> originCells,float movePower)
{
    var result = new List<Vector3Int>();

    if (originCells == null)
        return result;
    if (movePower <= 0f)
        return result;

    var costSoFar = new Dictionary<Vector3Int, float>();
    var frontier = new Queue<Vector3Int>();

    // 1. 把“占地的所有格子”都当作起点，初始消耗为 0
    foreach (var cell in originCells)
    {
        if (costSoFar.ContainsKey(cell))
            continue;

        costSoFar[cell] = 0f;
        frontier.Enqueue(cell);
    }

    // 4 向移动；需要 8 向就把对角也加上
    var dirs = new[]
    {
        new Vector3Int( 1,  0, 0),
        new Vector3Int(-1,  0, 0),
        new Vector3Int( 0,  1, 0),
        new Vector3Int( 0, -1, 0),
    };

    while (frontier.Count > 0)
    {
        var current = frontier.Dequeue();
        float currentCost = costSoFar[current];

        foreach (var d in dirs)
        {
                Vector3Int next = new Vector3Int(current.x + d.x, current.y + d.y, 0);

            // 根据你的 GridSystem 约定调整这里坐标含义
            float resistance = GridSystem.Instance.GetMobileResistance(new Vector3Int(next.x, next.y, 0));

            // 阻力 < 0 或 Infinity 视为不可通行（按你项目约定调）
            if (resistance < 0f || float.IsInfinity(resistance))
                continue;

            float newCost = currentCost + resistance;
            if (newCost > movePower)
                continue;

            if (!costSoFar.TryGetValue(next, out float oldCost) || newCost < oldCost)
            {
                costSoFar[next] = newCost;
                frontier.Enqueue(next);
            }
        }
    }

    // 结果就是所有“成本 ≤ movePower”的格子
    result.AddRange(costSoFar.Keys);
    return result;
}

    public static List<Vector3Int> GetReachableCellsByMovePower(BuildingInstance buildingInstance,float movePower)
    {
        return GetReachableCellsByMovePower(buildingInstance.CurrentOccupy, movePower);
    }


    // --- 请添加到 CoordinateCalculator 类中 ---

    #region 扩展：距离与建筑查询

    /// <summary>
    /// 【核心数学】计算两点在指定度量下的距离
    /// </summary>
    public static float CalculateDistance(Vector3 p1, Vector3 p2, DistanceMetric metric)
    {
        float dx = Mathf.Abs(p1.x - p2.x);
        float dy = Mathf.Abs(p1.y - p2.y);

        switch (metric)
        {
            case DistanceMetric.Chebyshev:
                return Mathf.Max(dx, dy); // 切比雪夫：取最大轴距（适合8方向移动步数）
            case DistanceMetric.Manhattan:
                return dx + dy;           // 曼哈顿：直角折线距离（适合4方向移动步数）
            case DistanceMetric.Euclidean:
                return Mathf.Sqrt(dx * dx + dy * dy); // 欧几里得：直线距离
            default:
                return dx + dy;
        }
    }

    /// <summary>
    /// 【方法1】计算两个建筑实例中心点之间的距离
    /// </summary>
    public static float GetDistance(BuildingInstance a, BuildingInstance b, DistanceMetric metric = DistanceMetric.Euclidean)
    {
        if (a == null || b == null) return float.MaxValue;
        // 使用建筑在网格中的中心坐标进行计算
        return CalculateDistance(a.CurrentCenterInGrid, b.CurrentCenterInGrid, metric);
    }

    /// <summary>
    /// 【方法2】获取指定范围内的所有建筑
    /// </summary>
    /// <param name="center">搜索中心（世界坐标/网格中心）</param>
    /// <param name="radius">搜索半径</param>
    /// <param name="checkMode">
    /// 0 = CenterOnly: 仅判断目标建筑的【中心点】是否在范围内（性能最快，O(N)）
    /// 1 = AnyCellOverlap: 判断目标建筑的【任意占地格子】是否与范围重叠（更精确，适合大建筑，性能较销耗）
    /// </param>
    public static List<BuildingInstance> GetBuildingsInRadius(
        Vector3 center,
        float radius,
        ScopeCheckMode checkMode,
        DistanceMetric metric = DistanceMetric.Manhattan
        )
    {
        var result = new List<BuildingInstance>();
        var allBuildings = BuildingInstance.ActiveInstances; // 获取全局活跃建筑列表

        if (allBuildings == null || allBuildings.Count == 0)
            return result;

        // 模式0：快速中心判定
        if (checkMode == ScopeCheckMode.CenterOnly)
        {
            foreach (var building in allBuildings)
            {
                if (building == null) continue;

                float dist = CalculateDistance(center, building.CurrentCenterInGrid, metric);

                // 针对欧几里得距离的边缘判定，通常保持与 CellsInRadius 一致的松散度
                // 如果需要严格 <= radius，直接写 dist <= radius
                if (dist <= radius)
                {
                    result.Add(building);
                }
            }
        }
        // 模式1：精确重叠判定 (适合范围炮击、光环覆盖等逻辑)
        else if (checkMode == ScopeCheckMode.AnyCellOverlap)
        {
            // 1. 先获取范围内所有的有效格子
            // 注意：这里 centerIsCorner 传 false 还是 true 取决于 center 是否在格点上，通常传 false 即可
            bool centerIsCorner = (center.x % 1 != 0 || center.y % 1 != 0);
            var validCells = CellsInRadius(center, (int)Mathf.Ceil(radius), centerIsCorner, metric, true, true);

            // 转换成 HashSet 加速查询
            var cellSet = new HashSet<Vector3Int>(validCells);

            foreach (var building in allBuildings)
            {
                if (building == null || building.CurrentOccupy == null) continue;

                // 2. 粗筛：如果中心距离远超 (半径 + 建筑最大可能的半宽)，则直接跳过
                // 假设建筑最大边长不超过10，优化性能
                float distCenter = CalculateDistance(center, building.CurrentCenterInGrid, metric);
                if (distCenter > radius + 5f) continue;

                // 3. 细筛：检查该建筑占用的格子是否有任何一个在范围内
                foreach (var occupiedCell in building.CurrentOccupy)
                {
                    if (cellSet.Contains(occupiedCell))
                    {
                        result.Add(building);
                        break; // 只要有一个格子重叠就算命中
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 【方法2重载】获取以某建筑为中心，指定范围内的所有其他建筑
    /// </summary>
    public static List<BuildingInstance> GetBuildingsInRadius(
        BuildingInstance originBuilding,
        float radius,
        ScopeCheckMode checkMode,
        DistanceMetric metric = DistanceMetric.Manhattan
        )
    {
        if (originBuilding == null) return new List<BuildingInstance>();

        var list = GetBuildingsInRadius(originBuilding.CurrentCenterInGrid, radius, checkMode,metric);

        // 通常需要排除自己
        if (list.Contains(originBuilding))
        {
            list.Remove(originBuilding);
        }
        return list;
    }


    /// <summary>
    /// 判断 b2 是否在 b1 的移动能力范围内（考虑地形阻力）
    /// </summary>
    /// <param name="b1">起始建筑</param>
    /// <param name="b2">目标建筑</param>
    /// <param name="movePower">移动力（行动点数/最大消耗）</param>
    /// <param name="checkMode">检测模式</param>
    /// <returns>是否可到达</returns>
    public static bool IsReachable(
        BuildingInstance b1,
        BuildingInstance b2,
        float movePower,
        ScopeCheckMode checkMode)
    {
        if (b1 == null || b2 == null) return false;
        if (movePower <= 0) return false;

        // 1. 获取 b1 能够到达的所有格子列表
        // 注意：如果 movePower 非常大，这里会计算全图，性能开销较大。
        // 如果只是判断两点连通性，通常建议用 A* 寻路配合 Early Exit，但在现有架构下复用此方法最稳妥。
        List<Vector3Int> reachableList = GetReachableCellsByMovePower(b1.CurrentOccupy, movePower);

        // 2. 转换为 HashSet 以优化查找速度
        HashSet<Vector3Int> reachableSet = new HashSet<Vector3Int>(reachableList);

        // 3. 根据模式判定
        if (checkMode == ScopeCheckMode.CenterOnly)
        {
            // 简单判定：将目标中心四舍五入到最近的格子，看该格子是否可达
            // 注意：对于偶数尺寸建筑（中心在x.5），这会选取最近的一个整数格作为代表
            Vector3Int centerCell = Vector3Int.RoundToInt(b2.CurrentCenterInGrid);
            return reachableSet.Contains(centerCell);
        }
        else // ScopeCheckMode.AnyCellOverlap
        {
            // 精确判定：只要目标占据的任何一个格子在可达范围内，即视为可达
            foreach (var cell in b2.CurrentOccupy)
            {
                if (reachableSet.Contains(cell))
                {
                    return true;
                }
            }
            return false;
        }
    }





    #endregion



    #region 扩展：寻路、方向与空间查询

    /// <summary>
    /// 【方法1】获取路径 (A* 算法)
    /// </summary>
    /// <param name="start">起点坐标</param>
    /// <param name="end">终点坐标</param>
    /// <param name="maxCost">最大允许移动消耗（若路径消耗超过此值则返回 null）</param>
    /// <returns>路径点列表（不包含起点，包含终点），不可达返回 null</returns>
    public static List<Vector3Int> GetPath(Vector3Int start, Vector3Int end, float maxCost = float.MaxValue)
    {
        if (start == end) return new List<Vector3Int>();

        var openSet = new List<Vector3Int> { start };
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        var gScore = new Dictionary<Vector3Int, float>();
        gScore[start] = 0;

        var fScore = new Dictionary<Vector3Int, float>();
        fScore[start] = GetHeuristic(start, end);

        while (openSet.Count > 0)
        {
            // 1. 取出 fScore 最小的节点 (模拟优先队列)
            Vector3Int current = openSet[0];
            float lowestF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;

            for (int i = 1; i < openSet.Count; i++)
            {
                var node = openSet[i];
                float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                if (f < lowestF)
                {
                    current = node;
                    lowestF = f;
                }
            }

            if (current == end)
            {
                // 重建路径
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            // 2. 遍历邻居 (4方向)
            foreach (var dir in _directions4)
            {
                Vector3Int neighbor = current + dir;

                // 检查阻力 (这里耦合了 GridSystem)
                float resistance = GridSystem.Instance.GetMobileResistance(neighbor);

                // 阻力 < 0 代表不可通行
                if (resistance < 0) continue;

                float tentativeGScore = gScore[current] + resistance;

                // 超过最大步数限制，剪枝
                if (tentativeGScore > maxCost) continue;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + GetHeuristic(neighbor, end);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // 无法到达
        return null;
    }

    /// <summary>
    /// 【方法2】获取路径消耗
    /// </summary>
    public static float GetPathCost(Vector3Int start, Vector3Int end)
    {
        // 如果只是想知道开销，最准确的方法是跑一次寻路。
        // 为了复用，这里直接调用 GetPath 并计算总阻力。
        // 如果追求极致性能，可以将 GetPath 改写为只返回 float cost 的版本。
        var path = GetPath(start, end);
        if (path == null) return float.MaxValue;

        float cost = 0;
        foreach (var step in path)
        {
            float r = GridSystem.Instance.GetMobileResistance(step);
            if (r > 0) cost += r;
        }
        return cost;
    }

    /// <summary>
    /// 【方法2重载 - 统一逻辑版】计算从 b1 到 b2 的路径消耗
    /// 逻辑：
    /// 1. 起点：b1 占用的【所有格子】都被视为起点，初始 G 值均为 0（免费从建筑任意位置出发）。
    /// 2. 终点：只要触碰到 b2 占用的【任意格子】即视为到达。
    /// 3. 算法：多源多目标 A* (Multi-Source Multi-Target A*)。
    /// </summary>
    public static float GetPathCost(BuildingInstance b1, BuildingInstance b2)
    {
        if (b1 == null || b2 == null) return float.MaxValue;
        if (b1 == b2) return 0f;

        // 1. 准备终点查询表 (HashSet O(1))
        // 用于快速判断当前探测的格子是否属于 b2
        var targetCells = new HashSet<Vector3Int>(b2.CurrentOccupy);

        // 优化：如果两者已经有重叠，消耗直接为 0
        foreach (var cell in b1.CurrentOccupy)
        {
            if (targetCells.Contains(cell)) return 0f;
        }

        // 2. 初始化 A* 数据结构
        // 使用 List 模拟优先队列 (实际项目中建议用 PriorityQueue 优化性能)
        var openSet = new List<Vector3Int>();
        var gScore = new Dictionary<Vector3Int, float>();
        var fScore = new Dictionary<Vector3Int, float>();

        // 3. 计算启发式目标的参考点
        // 为了让 A* 有方向感，我们取 b2 的中心作为计算 H 值的参考
        // (虽然终点是 b2 的任意边缘，但朝中心走通常大方向没错)
        Vector3Int heuristicTarget = Vector3Int.RoundToInt(b2.CurrentCenterInGrid);

        // 4. 【核心修改】：将 b1 的所有格子都作为起点加入 OpenSet
        foreach (var startCell in b1.CurrentOccupy)
        {
            openSet.Add(startCell);
            gScore[startCell] = 0f; // 起步免费
            fScore[startCell] = GetHeuristic(startCell, heuristicTarget);
        }

        // 5. 开始寻路
        while (openSet.Count > 0)
        {
            // --- 模拟优先队列：取出 F 值最小的节点 ---
            int bestIndex = 0;
            float lowestF = fScore.ContainsKey(openSet[0]) ? fScore[openSet[0]] : float.MaxValue;

            for (int i = 1; i < openSet.Count; i++)
            {
                var node = openSet[i];
                float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                if (f < lowestF)
                {
                    lowestF = f;
                    bestIndex = i;
                }
            }

            Vector3Int current = openSet[bestIndex];

            // 【判定到达】：如果当前格子属于 b2，直接返回累计消耗
            if (targetCells.Contains(current))
            {
                return gScore[current];
            }

            // 移除当前节点
            // 优化：Swap Remove (O(1))，因为顺序不重要，只要能遍历即可，但这里是排序列表所以只能用 RemoveAt
            openSet.RemoveAt(bestIndex);

            // --- 遍历邻居 ---
            foreach (var dir in _directions4)
            {
                Vector3Int neighbor = current + dir;

                // 获取移动阻力
                float resistance = GridSystem.Instance.GetMobileResistance(neighbor);

                // 阻力 < 0 代表不可通行
                if (resistance < 0) continue;

                // 计算新 G 值
                // 注意：如果 neighbor 属于 b2，我们仍然加上了进入 b2 这一格的阻力，这通常是合理的
                float tentativeG = gScore[current] + resistance;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + GetHeuristic(neighbor, heuristicTarget);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // 无法到达
        return float.MaxValue;
    }
    /// <summary>
    /// 【方法3】获取方向
    /// </summary>
    public static GridDirection GetDirection(Vector3Int from, Vector3Int to)
    {
        Vector3Int dir = to - from;

        // 简单归一化判断
        if (dir.x == 0 && dir.y > 0) return GridDirection.Up;
        if (dir.x == 0 && dir.y < 0) return GridDirection.Down;
        if (dir.x < 0 && dir.y == 0) return GridDirection.Left;
        if (dir.x > 0 && dir.y == 0) return GridDirection.Right;

        if (dir.x > 0 && dir.y > 0) return GridDirection.UpRight;
        if (dir.x < 0 && dir.y > 0) return GridDirection.UpLeft;
        if (dir.x > 0 && dir.y < 0) return GridDirection.DownRight;
        if (dir.x < 0 && dir.y < 0) return GridDirection.DownLeft;

        return GridDirection.None;
    }

    /// <summary>
    /// 【方法4】获取建筑外围一圈的格子
    /// </summary>
    /// <param name="range">外扩范围（1表示紧贴着的一圈）</param>
    public static List<Vector3Int> GetPerimeterCells(BuildingInstance building, int range = 1)
    {
        var result = new List<Vector3Int>();
        if (building == null || building.CurrentOccupy == null) return result;

        // 1. 计算包围盒
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        // 使用 HashSet 加速“是否属于建筑本体”的判断
        var occupySet = new HashSet<Vector3Int>(building.CurrentOccupy);

        foreach (var cell in building.CurrentOccupy)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        // 2. 遍历外扩后的矩形环
        for (int x = minX - range; x <= maxX + range; x++)
        {
            for (int y = minY - range; y <= maxY + range; y++)
            {
                Vector3Int current = new Vector3Int(x, y, 0);

                // 排除建筑本体内部的格子
                if (occupySet.Contains(current)) continue;

                // 只要在扩展范围内，且不在本体内，即为周边
                // (这里采用了简单的矩形扩展逻辑，如果是通过距离判定圆形范围，需改用 Distance)
                result.Add(current);
            }
        }

        return result;
    }

    /// <summary>
    /// 【方法5】寻找最近的空地 (螺旋搜索)
    /// </summary>
    /// <param name="center">搜索起点</param>
    /// <param name="size">需要的空地尺寸 (size x size)</param>
    /// <param name="maxSearchRadius">最大搜索半径，防止死循环</param>
    public static Vector3Int? FindNearestEmptySpace(Vector3Int center, int size, int maxSearchRadius = 20)
    {
        // 0. 先检查起点本身
        if (IsAreaClear(center, size)) return center;

        // 1. 螺旋遍历
        // 算法：在网格上按螺旋路径移动：右1，下1，左2，上2，右3，下3...
        int x = center.x;
        int y = center.y;
        int step = 1;
        int moved = 0;
        int dirIndex = 0; // 0:Right, 1:Down, 2:Left, 3:Up

        // 对应 Right, Down, Left, Up 的坐标变化
        int[] dx = { 1, 0, -1, 0 };
        int[] dy = { 0, -1, 0, 1 };

        int totalCellsChecked = 0;
        int maxCells = (maxSearchRadius * 2 + 1) * (maxSearchRadius * 2 + 1);

        while (totalCellsChecked < maxCells)
        {
            // 沿当前方向移动 step 步
            for (int i = 0; i < step; i++)
            {
                x += dx[dirIndex];
                y += dy[dirIndex];
                totalCellsChecked++;

                Vector3Int candidate = new Vector3Int(x, y, 0);

                // 检查该位置是否可用
                if (IsAreaClear(candidate, size))
                {
                    return candidate;
                }
            }

            // 换方向
            dirIndex = (dirIndex + 1) % 4;
            moved++;

            // 每换两次方向，步长 +1
            if (moved % 2 == 0)
            {
                step++;
            }
        }

        return null; // 未找到
    }

    // --- 内部辅助方法 ---

    private static readonly Vector3Int[] _directions4 = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0),  // Up
        new Vector3Int(0, -1, 0), // Down
        new Vector3Int(-1, 0, 0), // Left
        new Vector3Int(1, 0, 0)   // Right
    };

    private static float GetHeuristic(Vector3Int a, Vector3Int b)
    {
        // 曼哈顿距离作为启发式函数
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var totalPath = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        // path 包含起点，通常移动逻辑不需要起点，如需移除请 uncomment 下一行
        // if (totalPath.Count > 0) totalPath.RemoveAt(0); 
        return totalPath;
    }

    /// <summary>
    /// 检查以 bottomLeft 为中心（或左下角，取决于 GetBuildingCells 逻辑）的 size*size 区域是否可用
    /// </summary>
    private static bool IsAreaClear(Vector3Int pos, int size)
    {
        // 复用之前的 GetBuildingCells 获取所有需要占用的格子
        // 注意：这里假设 FindNearestEmptySpace 传入的 pos 是指新建筑的“中心点”
        // 如果你定义 pos 为左下角，请调整这里
        var cells = GetBuildingCells(pos, size);

        foreach (var cell in cells)
        {
            // 利用 GridSystem 检查障碍物
            // IsAllowPlacementBuilding 返回 true 代表有障碍(IsAllowPlacementBuilding 命名可能反了？)
            // 或者是 IsAllowPlacementBuilding 返回 true 代表允许放置？
            // 回看 GridSystem 代码： return dic...HasTile(障碍) || ...HasTile(道路);
            // 你的代码里：IsAllowPlacementBuilding 返回 true 意味着 "有障碍" 或者 "有道路" (通常意味着不能建?)
            // 根据你的 GridSystem 代码逻辑：
            // public bool IsAllowPlacementBuilding(Vector3Int coor) { return dic...HasTile...; }
            // 这名字看起来像“是否允许放置”，但实现内容是检查是否有 Tile。
            // 这里的语义有歧义，假设：HasTile 返回 true -> 有东西 -> 不能放。

            if (GridSystem.Instance.IsAllowPlacementBuilding(cell))
            {
                return false; // 有障碍，不可用
            }

            // 还需要检查是否已有建筑实例 (GridSystem 可能只管 Tilemap)
            if (BuildingInstance.TryGetAtCell(cell, out _))
            {
                return false; // 已有建筑，不可用
            }
        }
        return true;
    }

    #endregion

}

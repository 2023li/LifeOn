
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


}

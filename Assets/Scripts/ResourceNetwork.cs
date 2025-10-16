using UnityEngine;
using System.Collections.Generic;

public static class ResourceNetwork
{
    // 全局注册的所有库存列表
    private static List<Inventory> allInventories = new List<Inventory>();

    /// <summary>
    /// 注册一个库存到资源网络中（例如建筑创建时调用）。
    /// </summary>
    public static void RegisterInventory(Inventory inv)
    {
        if (inv != null && !allInventories.Contains(inv))
        {
            allInventories.Add(inv);
        }
    }

    /// <summary>
    /// 从资源网络移除一个库存（例如建筑销毁时调用）。
    /// </summary>
    public static void UnregisterInventory(Inventory inv)
    {
        if (inv != null)
        {
            allInventories.Remove(inv);
        }
    }

    /// <summary>
    /// 在指定位置和半径范围内，查找至少含有amount数量的指定资源的某个库存。返回找到的Inventory，没有则返回null。
    /// </summary>
    public static Inventory FindInventoryWithResource(SupplyType type, int amount, Vector3 position, float radius)
    {
        Inventory found = null;
        float minDistSq = float.MaxValue;
        foreach (Inventory inv in allInventories)
        {
            if (inv == null || !inv.HasResource(type, amount)) continue;
            // 计算与目标位置的距离
            if (radius <= 0)
            {
                // 若半径<=0则不考虑距离
                return inv;
            }
            // 有限半径范围内选取最近的库存
            float distSq = (inv.OwnerBuilding.transform.position - position).sqrMagnitude;
            if (distSq <= radius * radius && distSq < minDistSq)
            {
                minDistSq = distSq;
                found = inv;
            }
        }
        return found;
    }

    /// <summary>
    /// 在指定位置和半径范围内，查找尚有capacitySpace空间的最近库存。用于输出物资时寻找仓库。
    /// </summary>
    public static Inventory FindNearestInventoryWithSpace(Vector3 position, float radius, int requiredSpace)
    {
        Inventory found = null;
        float minDistSq = float.MaxValue;
        foreach (Inventory inv in allInventories)
        {
            if (inv == null) continue;
            int available = inv.Capacity - inv.CurrentAmount;
            if (available < requiredSpace) continue;
            float distSq = (inv.OwnerBuilding.transform.position - position).sqrMagnitude;
            if (radius <= 0 || distSq <= radius * radius)
            {
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    found = inv;
                }
            }
        }
        return found;
    }

    /// <summary>
    /// 尝试在网络中消耗指定建筑附近的一定数量资源。找到则扣减并返回true，否则返回false。
    /// </summary>
    public static bool TryConsumeResource(Building building, SupplyType type, int amount)
    {
        float radius = building.CurrentLevelDef.MaterialFetchingRadius;
        Inventory inv = FindInventoryWithResource(type, amount, building.transform.position, radius);
        if (inv != null)
        {
            // 找到有资源的库存，执行取出
            return inv.RemoveResource(type, amount);
        }
        return false;
    }
}

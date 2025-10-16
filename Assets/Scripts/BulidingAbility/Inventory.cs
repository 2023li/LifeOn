using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    public int Capacity { get; set; }          // 最大容量
    public int CurrentAmount { get; private set; }  // 当前已占用容量
    private Dictionary<SupplyType, int> resourceAmounts;
    public Building OwnerBuilding { get; private set; }

    public Inventory(int capacity, Building owner)
    {
        this.Capacity = capacity;
        this.OwnerBuilding = owner;
        this.CurrentAmount = 0;
        resourceAmounts = new Dictionary<SupplyType, int>();
    }

    /// <summary>
    /// 入库：将指定资源数量添加到库存。成功返回true，容量不足或参数非法返回false。
    /// </summary>
    public bool AddResource(SupplyType type, int amount)
    {
        if (amount <= 0) return false;
        if (CurrentAmount + amount > Capacity)
            return false; // 容量不足

        // 增加资源数量
        if (resourceAmounts.TryGetValue(type, out int existing))
            resourceAmounts[type] = existing + amount;
        else
            resourceAmounts[type] = amount;
        CurrentAmount += amount;
        return true;
    }

    /// <summary>
    /// 出库：从库存中取出指定资源数量。成功返回true，不足或无该资源则返回false。
    /// </summary>
    public bool RemoveResource(SupplyType type, int amount)
    {
        if (amount <= 0) return false;
        if (!resourceAmounts.TryGetValue(type, out int existing))
            return false; // 没有该类型资源
        if (existing < amount)
            return false; // 数量不足

        // 减少资源数量
        int remaining = existing - amount;
        if (remaining == 0)
            resourceAmounts.Remove(type);
        else
            resourceAmounts[type] = remaining;
        CurrentAmount -= amount;
        if (CurrentAmount < 0) CurrentAmount = 0; // 防御性检查
        return true;
    }

    /// <summary>
    /// 检查库存中是否有至少指定数量的某资源。
    /// </summary>
    public bool HasResource(SupplyType type, int amount)
    {
        if (!resourceAmounts.TryGetValue(type, out int existing))
            return false;
        return existing >= amount;
    }

    /// <summary>
    /// 获取指定资源的当前存量（没有则返回0）。
    /// </summary>
    public int GetResourceAmount(SupplyType type)
    {
        return resourceAmounts.TryGetValue(type, out int existing) ? existing : 0;
    }

    /// <summary>
    /// 丢弃多余的物资，使当前存量不超过容量（用于容量降低的情况）。
    /// </summary>
    public void TruncateExcess()
    {
        if (CurrentAmount <= Capacity) return;
        // 简单策略：超出容量部分直接移除（实际可拓展为按某种优先级丢弃）
        int overflow = CurrentAmount - Capacity;
        CurrentAmount = Capacity;
        // 从资源字典中按顺序减少overflow数量
        foreach (var type in new List<SupplyType>(resourceAmounts.Keys))
        {
            if (overflow <= 0) break;
            int qty = resourceAmounts[type];
            if (qty <= overflow)
            {
                resourceAmounts.Remove(type);
                overflow -= qty;
            }
            else
            {
                resourceAmounts[type] = qty - overflow;
                overflow = 0;
            }
        }
        Debug.LogWarning($"Inventory of {OwnerBuilding.name} overflowed, excess resources discarded.");
    }
}

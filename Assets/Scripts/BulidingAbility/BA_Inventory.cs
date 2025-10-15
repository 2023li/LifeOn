using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BA_Inventory : BulidingAbility
{
    public int ActualCapacity;   // 总容量
    public int CurrentCapacity;  // 已占用容量

    Dictionary<SupplyType, int> dic_SupplyAmount;

    protected override void OnAdd()
    {
        dic_SupplyAmount = new Dictionary<SupplyType, int>();
        ActualCapacity = building.Def.Inventory_Capacity;
        CurrentCapacity = 0; // 明确初始化（可省略，默认为0）

        BuildingManager.Instance.Register(this);

    }


    public override void Remove()
    {
        BuildingManager.Instance.Unregister(this);

        Destroy(this);
    }


    /// <summary>
    /// 入库：将 sa.Amount 个 sa.Type 存入仓库。
    /// 成功返回 true；容量不足或参数非法返回 false。
    /// </summary>
    public bool Storage(SupplyAmount sa)
    {
        // 如果你的 SupplyAmount 字段名不是 Type，请把下面的 sa.Type 改成实际字段名
        // 例如 sa.SupplyType / sa.Category 等
        if (sa.Amount <= 0) return false;

        // 判断容量（允许刚好装满）
        if (CurrentCapacity + sa.Amount > ActualCapacity)
            return false;

        // 写入库存
        if (dic_SupplyAmount.TryGetValue(sa.Type, out int cur))
            dic_SupplyAmount[sa.Type] = cur + sa.Amount;
        else
            dic_SupplyAmount[sa.Type] = sa.Amount;

        CurrentCapacity += sa.Amount;
        return true;
    }

    /// <summary>
    /// 出库：从库存中取出 sa.Amount 个 sa.Type。
    /// 成功返回 true；库存不足或参数非法返回 false。
    /// </summary>
    public bool Retrieval(SupplyAmount sa)
    {
        if (sa.Amount <= 0) return false;

        if (!dic_SupplyAmount.TryGetValue(sa.Type, out int cur))
            return false; // 没有该物资

        if (cur < sa.Amount)
            return false; // 数量不足

        int left = cur - sa.Amount;
        if (left == 0)
            dic_SupplyAmount.Remove(sa.Type);
        else
            dic_SupplyAmount[sa.Type] = left;

        CurrentCapacity -= sa.Amount;
        if (CurrentCapacity < 0) CurrentCapacity = 0; // 防御性保护（正常不应发生）

        return true;
    }

 
}

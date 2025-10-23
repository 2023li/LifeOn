using System.Collections.Generic;

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

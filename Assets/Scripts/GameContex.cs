// Assets/Game/Scripts/Runtime/Services.cs
using System.Collections.Generic;
using UnityEngine;

public interface IGameContext
{
    ResourceNetwork ResourceNetwork { get; }
}

public class GameContext : MonoBehaviour, IGameContext
{
    public ResourceNetwork ResourceNetwork => throw new System.NotImplementedException();
}

public class ResourceNetwork
{
    private readonly HashSet<Inventory> storages = new();

    public void RegisterStorage(Inventory inv) => storages.Add(inv);
    public void UnregisterStorage(Inventory inv) => storages.Remove(inv);

    public int GetTotalQuantity()
    {
        int t = 0;
        foreach (var s in storages) t += s.TotalQuantity;
        return t;
    }

    public Inventory GetAssignedStorage(BuildingInstance self)
    {
        // 简化：优先绑定的仓库；没有则返回 null
        return self.AssignedStorage != null ? self.AssignedStorage.Storage : null;
    }
}

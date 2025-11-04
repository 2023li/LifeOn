using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class HasResourceForPopulationByCategory : Condition
{
    public SupplyCategory Category;
    [Min(0)] public float AmountPerCapita = 1f;
    [LabelText("人口为0是忽略")]
    public bool IgnoreIfPopulationZero = true;

    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";

        if (self == null) { why = "建筑为空"; return false; }

        int population = Mathf.Max(0, self.Population);
        if (IgnoreIfPopulationZero && population == 0) return true;

        Inventory inv = GetSupplyInventory(self, ctx);
        if (inv == null) { why = "无供给仓库"; return false; }

        int required = Mathf.CeilToInt(population * AmountPerCapita);
        if (required <= 0) return true;

        int have = 0;
        foreach (var sa in inv.EnumerateContents())
        {
            if (sa.Resource != null && sa.Resource.Category == Category)
            {
                have += Mathf.Max(0, sa.Amount);
                if (have >= required) return true;
            }
        }

        why = "资源不足";
        return false;
    }

    private static Inventory GetSupplyInventory(BuildingInstance self, IGameContext ctx)
    {
        // 首选：指派的供给仓库
        if (self.AssignedStorage != null && self.AssignedStorage.Storage != null)
            return self.AssignedStorage.Storage;

        // 其次：自身是仓库
        if (self.Storage != null)
            return self.Storage;

        // 兜底：兼容你现有的资源网络
        return ctx != null && ctx.ResourceNetwork != null
            ? ctx.ResourceNetwork.GetAssignedStorage(self)
            : null;
    }
}

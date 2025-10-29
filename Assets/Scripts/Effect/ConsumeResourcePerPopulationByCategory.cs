using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ConsumeResourcePerPopulationByCategory : Effect
{
    public SupplyCategory Category;
    [Min(0)] public float AmountPerCapita = 1f;
    public bool IgnoreIfPopulationZero = true;
    public bool LargestStackFirst = true; // true=大堆优先；false=小堆优先

    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (self == null) return;

        int population = Mathf.Max(0, self.Population);
        if (IgnoreIfPopulationZero && population == 0) return;

        Inventory inv = GetSupplyInventory(self, ctx);
        if (inv == null) return;

        int required = Mathf.CeilToInt(population * AmountPerCapita);
        if (required <= 0) return;

        // 收集该类别的库存堆
        var stacks = new List<SupplyAmount>();
        foreach (var sa in inv.EnumerateContents())
        {
            if (sa.Resource != null && sa.Resource.Category == Category && sa.Amount > 0)
            {
                stacks.Add(sa);
            }
        }
        stacks.Sort((a, b) => LargestStackFirst
            ? b.Amount.CompareTo(a.Amount)
            : a.Amount.CompareTo(b.Amount));

        // 生成要扣除的清单
        var toConsume = new List<SupplyAmount>();
        int remaining = required;
        foreach (var s in stacks)
        {
            if (remaining <= 0) break;
            int take = Mathf.Min(remaining, s.Amount);
            toConsume.Add(new SupplyAmount { Resource = s.Resource, Amount = take });
            remaining -= take;
        }

        if (toConsume.Count > 0)
            inv.Consume(toConsume.ToArray());
    }

    private static Inventory GetSupplyInventory(BuildingInstance self, IGameContext ctx)
    {
        if (self.AssignedStorage != null && self.AssignedStorage.Storage != null)
            return self.AssignedStorage.Storage;

        if (self.Storage != null)
            return self.Storage;

        return ctx != null && ctx.ResourceNetwork != null
            ? ctx.ResourceNetwork.GetAssignedStorage(self)
            : null;
    }
}

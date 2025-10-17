using System;
using UnityEngine;

[Serializable]
public abstract class Effect
{
    public abstract void Apply(BuildingInstance self, IGameContext ctx);
}



[Serializable]
public class ConsumeFromAssignedStorage : Effect
{
    public SupplyAmount[] Costs;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        var inv = ctx.ResourceNetwork.GetAssignedStorage(self);
        if (inv == null) return;
        inv.Consume(Costs);
    }
}

[Serializable]
public class ChangePopulation : Effect
{
    public int Delta;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        int max = self.GetMaxPopulation(ctx);
        self.Population = Mathf.Clamp(self.Population + Delta, 0, max);
    }
}

[Serializable]
public class AddExp : Effect
{
    public int Amount = 1;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        self.Exp += Amount;

        Debug.Log("增加经验");
    }


}

[Serializable]
public class UpgradeToNextLevel : Effect
{
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        self.TryUpgrade(ctx);
    }
}

//[Serializable]
//public class AddEnvironmentAura : Effect
//{
//    public int Radius = 5;
//    public int Value = 1;
//    public override void Apply(BuildingInstance self, IGameContext ctx)
//    {
//        ctx.Environment.AddAura(self.InstanceId, self.GridPos, Radius, Value);
//    }
//}

//[Serializable]
//public class RemoveEnvironmentAura : Effect
//{
//    public override void Apply(BuildingInstance self, IGameContext ctx)
//    {
//        ctx.Environment.RemoveAura(self.InstanceId);
//    }
//}


[Serializable]
public class AddToSelfStorage : Effect
{
    public SupplyAmount[] Items;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (self.Storage == null) return;
        self.Storage.Add(Items);
    }
}

[Serializable]
public class AddToAssignedStorage : Effect
{
    public SupplyAmount[] Items;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        var inv = ctx.ResourceNetwork.GetAssignedStorage(self);
        if (inv == null) return;
        inv.Add(Items);
    }
}

// 可选：把自仓部分转运到绑定仓库（每回合）
[Serializable]
public class TransferSelfToAssigned : Effect
{
    public SupplyDef Resource; public int Amount = 10;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (self.Storage == null) return;
        var dst = ctx.ResourceNetwork.GetAssignedStorage(self);
        if (dst == null) return;

        int available = self.Storage.GetAmount(Resource);
        int move = Mathf.Min(Amount, available);
        if (move <= 0) return;

        self.Storage.Consume(new[] { new SupplyAmount { Resource = Resource, Amount = move } });
        dst.Add(Resource, move);
    }
}

/// <summary>按人口从绑定仓库中扣除资源。</summary>
[Serializable]
public class ConsumeResourcePerPopulation : Effect
{
    public SupplyDef Resource;
    [Min(0)] public float AmountPerCapita = 1f;
    public bool IgnoreIfPopulationZero = true;

    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (Resource == null || ctx == null || ctx.ResourceNetwork == null)
        {
            return;
        }

        int population = Mathf.Max(0, self.Population);
        if (IgnoreIfPopulationZero && population == 0)
        {
            return;
        }

        Inventory inventory = ctx.ResourceNetwork.GetAssignedStorage(self);
        if (inventory == null)
        {
            return;
        }

        int required = Mathf.CeilToInt(population * AmountPerCapita);
        if (required <= 0)
        {
            return;
        }

        SupplyAmount[] costs = new SupplyAmount[1];
        costs[0] = new SupplyAmount
        {
            Resource = Resource,
            Amount = required
        };
        inventory.Consume(costs);
    }
}

/// <summary>按照工人数量产出资源。</summary>
[Serializable]
public class AddResourcePerWorker : Effect
{
    public SupplyDef Resource;
    public int AmountPerWorker = 1;
    public bool ToAssignedStorage = false;

    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (Resource == null)
        {
            return;
        }

        int workers = Mathf.Max(0, self.WorkersAssigned);
        if (workers <= 0)
        {
            return;
        }

        Inventory target = self.Storage;
        if (ToAssignedStorage && ctx != null && ctx.ResourceNetwork != null)
        {
            Inventory assigned = ctx.ResourceNetwork.GetAssignedStorage(self);
            if (assigned != null)
            {
                target = assigned;
            }
        }

        if (target == null)
        {
            return;
        }

        int total = workers * Mathf.Max(0, AmountPerWorker);
        target.Add(Resource, total);
    }
}

/// <summary>为建筑应用范围类环境光环。</summary>
[Serializable]
public class ApplyEnvironmentAura : Effect
{
    public AuraCategory Category = AuraCategory.Security;
    public AuraRing[] Rings;

    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (ctx == null || ctx.Environment == null)
        {
            return;
        }

        if (Rings == null || Rings.Length == 0)
        {
            return;
        }

        Vector3 center = self.CenterInGrid;
        bool centerIsCorner = self.CenterIsCorner;
        ctx.Environment.ApplyAura(self.InstanceId, center, centerIsCorner, Category, Rings);
    }
}


/// <summary>根据工人数量动态调整仓库容量。</summary>
[Serializable]
public class AdjustStorageCapacityWithWorkers : Effect
{
    public int LowWorkerThreshold = 3;
    public float LowWorkerMultiplier = 0.5f;
    public int FullWorkerThreshold = 5;
    public float FullWorkerMultiplier = 1.2f;

    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (self.Storage == null || self.Def == null)
        {
            return;
        }

        BuildingLevelDef level = self.Def.Levels[self.LevelIndex];
        int baseCapacity = Mathf.Max(0, level.BaseStorageCapacity);

        float multiplier = 1f;
        if (self.WorkersAssigned >= FullWorkerThreshold)
        {
            multiplier = FullWorkerMultiplier;
        }
        else if (self.WorkersAssigned < LowWorkerThreshold)
        {
            multiplier = LowWorkerMultiplier;
        }

        int newCapacity = Mathf.RoundToInt(baseCapacity * multiplier);
        if (newCapacity < 0)
        {
            newCapacity = 0;
        }

        self.Storage.Capacity = newCapacity;
    }
}

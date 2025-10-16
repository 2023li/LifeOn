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

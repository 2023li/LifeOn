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
    public ResourceAmount[] Costs;
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

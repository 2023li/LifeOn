using System;
using UnityEngine;

[Serializable]
public abstract class Effect
{
    public abstract void Apply(BuildingInstance self, IGameContext ctx);
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

        Debug.Log("增加经验",self);
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



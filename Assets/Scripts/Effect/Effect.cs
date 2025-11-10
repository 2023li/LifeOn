using System;
using UnityEngine;

[Serializable]
public abstract class Effect
{
    public abstract void Apply(BuildingInstance self, IGameContext ctx);
}




[Serializable]
public class Effect_生产资源 : Effect
{
    public SupplyDef Resource;
    public int Amount = 1;

    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        if (self == null || Resource == null || ctx?.ResourceNetwork == null)
        {

            return;
        }

        ResourceNetwork net = ctx.ResourceNetwork;

        // 记录该建筑是这个资源的生产者（供覆盖范围计算使用，幂等）
        net.RegisterProducer(self, Resource);

        if (!net.TryAddResource(Resource, Amount, out string why))
        {
            Debug.LogWarning(
                $"[Effect_生产资源] 建筑 {self.DisplayName} 生产 {Resource.DisplayName} 失败：{why}", self);
        }


        Debug.Log("目前库存："+net.GetAmount(Resource));
    }
}


[Serializable]
public class Effect_改变人口 : Effect
{
    public int Delta;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        int max = self.GetLevelData().GetMaxPopulation(self);
        self.CurrentPopulation = Mathf.Clamp(self.CurrentPopulation + Delta, 0, max);
    }
}


[Serializable]
public class Effect_增加经验 : Effect
{
    public int Amount = 1;
    public override void Apply(BuildingInstance self, IGameContext ctx)
    {
        self.CurrentExp += Amount;
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

        Vector3 center = self.CurrentCenterInGrid;
        bool centerIsCorner = self.CenterIsCorner;
        ctx.Environment.ApplyAura(self.InstanceId, center, centerIsCorner, Category, Rings);
    }
}



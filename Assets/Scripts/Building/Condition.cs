using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public abstract class Condition
{
    public abstract bool Evaluate(BuildingInstance self, IGameContext ctx, out string why);
}






[Serializable]
public class HasResourcesInAssignedStorage : Condition
{
    public SupplyAmount[] Costs;

    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        var inv = ctx.ResourceNetwork.GetAssignedStorage(self);
        if (inv == null) { why = "无可用存储点"; return false; }
        foreach (var c in Costs)
            if (inv.GetAmount(c.Resource) < c.Amount) { why = "存储资源不足"; return false; }
        return true;
    }
}

[Serializable]
public class PopulationAtLeast : Condition
{
    public int Min;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return self.Population >= Min;
    }
}

[Serializable]
public class ExpAtLeast : Condition
{
    public int Min;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return self.Exp >= Min;
    }
}

//[Serializable]
//public class EnvGreaterThan : Condition
//{
//    public int Threshold = 1;
//    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
//    {
//        why = "";
//        int v = ctx.Environment.GetEnvAt(self.GridPos);
//        return v > Threshold;
//    }
//}

/// <summary>
/// 选一：判断“本仓库存货占全网总库存的占比 > 阈值”
/// 注意：如果你要的是“填充率>40%（当前库存/容量）”，请改用下方 FillPercentOver。
/// </summary>
[Serializable]
public class ShareOfNetworkStockOver : Condition
{
    [Range(0, 1f)] public float Threshold = 0.4f;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        var inv = self.Storage; // 当前建筑自己的仓库
        if (inv == null) { why = "非仓库"; return false; }
        int my = inv.TotalQuantity;
        int total = ctx.ResourceNetwork.GetTotalQuantity();
        if (total <= 0) { why = "总库存为0"; return false; }
        float share = (float)my / total;
        return share > Threshold;
    }
}

/// <summary> 选二：判断“填充率>某阈值（当前库存/容量）” </summary>
[Serializable]
public class FillPercentOver : Condition
{
    [Range(0, 1f)] public float Threshold = 0.4f;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        var inv = self.Storage;
        if (inv == null) { why = "非仓库"; return false; }
        if (inv.Capacity <= 0) { why = "容量为0"; return false; }
        float fill = (float)inv.TotalQuantity / inv.Capacity;
        return fill > Threshold;
    }
}


// Rules/Conditions.cs ——追加
[Serializable]
public class WorkersAtLeast : Condition
{
    public int Min;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    { why = ""; return self.WorkersAssigned >= Min; }
}

[Serializable]
public class WorkersLessThan : Condition
{
    public int MaxExclusive;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    { why = ""; return self.WorkersAssigned < MaxExclusive; }
}

[Serializable]
public class WorkersEquals : Condition
{
    public int Count;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    { why = ""; return self.WorkersAssigned == Count; }
}



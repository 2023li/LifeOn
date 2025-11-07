using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



public static class ConditionUtility
{

    /// <summary>
    /// 逐项评估条件列表，若有失败返回 false 并写出失败原因。
    /// </summary>
    /// <param name="conditions">条件集合，允许为空。</param>
    /// <param name="self">当前建筑实例，可为空。</param>
    /// <param name="ctx">游戏上下文，允许为空但可能导致评估失败。</param>
    /// <param name="failedReason">失败原因，为空字符串表示全部通过。</param>
    public static bool TryEvaluateConditions(IEnumerable<Condition> conditions, BuildingInstance self, IGameContext ctx, out string failedReason)
    {
        failedReason = string.Empty;

        if (conditions == null)
        {
            return true;
        }

        foreach (Condition condition in conditions)
        {
            if (condition == null)
            {
                failedReason = "条件配置为空";
                return false;
            }

            try
            {
                if (condition.Evaluate(self, ctx, out string why))
                {
                    continue;
                }

                failedReason = string.IsNullOrWhiteSpace(why)
                    ? $"条件 {condition.GetType().Name} 未通过"
                    : why;
                return false;
            }
            catch (Exception ex)
            {
                failedReason = $"条件 {condition.GetType().Name} 评估异常：{ex.Message}";
                return false;
            }
        }

        return true;
    }
}








[Serializable]
public abstract class Condition
{
    public abstract bool Evaluate(BuildingInstance self, IGameContext ctx, out string why);
}






/// <summary>
/// 条件：尝试从 ResourceNetwork 消耗指定资源。
/// Evaluate 调用成功时，会真实扣除资源。
/// </summary>
[Serializable]
public class TryConsumeResourceCondition : Condition
{
    [LabelText("资源类型")]
    public SupplyDef Resource;

    [LabelText("消耗数量")]
    public int Amount = 1;

    [LabelText("需要在运输范围内")]
    public bool RequireInRange = true;

    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = string.Empty;

        if (Resource == null)
        {
            why = "未指定资源类型。";
            return false;
        }

        if (Amount <= 0)
        {
            why = "消耗数量必须为正数。";
            return false;
        }

        if (ctx == null || ctx.ResourceNetwork == null)
        {
            why = "资源网络未初始化。";
            return false;
        }

        var net = ctx.ResourceNetwork;

        // 检查运输范围
        if (RequireInRange && self != null)
        {
            var cell = new Vector3Int(
                Mathf.RoundToInt(self.CenterInGrid.x),
                Mathf.RoundToInt(self.CenterInGrid.y),
                0);

            if (!net.CanCellReceive(Resource, cell))
            {
                why = $"建筑不在 {Resource.DisplayName} 的运输范围内，无法消耗。";
                return false;
            }
        }

        // 尝试真正扣除资源
        if (!net.TryConsumeResource(Resource, Amount, out var fail))
        {
            // fail 里已经有“库存不足”等具体原因
            why = string.IsNullOrEmpty(fail)
                ? $"消耗 {Resource.DisplayName} 失败。"
                : fail;
            return false;
        }

        // 成功扣除
        return true;
    }
}





[Serializable]
public class NeverNo : Condition
{
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "这个条件永远不满足";
        return false;
    }
}

/// <summary>
/// 仓库是否满了的条件
/// </summary>
public class InventoryNotFullCondition : Condition
{
    [LabelText("目标资源类型")]
    public SupplyDef Resource;

    [LabelText("预期产量数量")]
    public int Amount = 1;

    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";

        if (Resource == null)
        {
            // 未指定资源则不限制
            return true;
        }

        if (ctx?.ResourceNetwork == null)
        {
            // 没有资源网络的情况下，你可以选择返回 false，这里暂定不阻塞
            return true;
        }

        int need = Mathf.Max(1, Amount) * Resource.OccupationUnit;
        int free = ctx.ResourceNetwork.GetFreeCapacity();

        if (free < need)
        {
            why = "仓储容量不足，无法存放更多产出。";
            return false;
        }

        return true;
    }
}


[Serializable]
public class TechUnlockedCondition : Condition
{
    public string TechId;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return ctx != null && ctx.TechTree != null && ctx.TechTree.IsUnlocked(TechId);
    }
}



[Serializable]
public class PopulationLessThan : Condition
{
    public int MaxExclusive;

    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return self.Population < MaxExclusive;
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



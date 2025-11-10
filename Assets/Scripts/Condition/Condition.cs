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
public class C_消耗资源 : Condition
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
                Mathf.RoundToInt(self.CurrentCenterInGrid.x),
                Mathf.RoundToInt(self.CurrentCenterInGrid.y),
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
public class C_永远不 : Condition
{
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "这个条件永远不满足";
        return false;
    }
}


[Serializable]
public class C_需要科技 : Condition
{
    public string TechId;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return ctx != null && ctx.TechTree != null && ctx.TechTree.IsUnlocked(TechId);
    }
}



// Rules/Conditions.cs ——追加
[Serializable]
public class C_工人大于等于 : Condition
{
    public int Min;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    { why = ""; return self.CurrentWorkers >= Min; }
}

[Serializable]
public class C_工人少于 : Condition
{
    public int MaxExclusive;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    { why = ""; return self.CurrentWorkers < MaxExclusive; }
}

[Serializable]
public class WorkersEquals : Condition
{
    public int Count;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    { why = ""; return self.CurrentWorkers == Count; }
}



[Serializable]
public class PopulationLessThan : Condition
{
    public int MaxExclusive;

    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return self.CurrentPopulation < MaxExclusive;
    }
}

[Serializable]
public class PopulationAtLeast : Condition
{
    public int Min;
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "";
        return self.CurrentPopulation >= Min;
    }
}

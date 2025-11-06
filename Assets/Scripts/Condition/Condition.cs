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






[Serializable]
public class NeverNo : Condition
{
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {
        why = "这个条件永远不满足";
        return false;
    }
}

public class InventoryNotFullCondition : Condition
{
    public override bool Evaluate(BuildingInstance self, IGameContext ctx, out string why)
    {

        Debug.LogWarning("InventoryNotFullCondition 未完成 只是占位");
        why = "";
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



using System;
using UnityEngine;

[Serializable]
public abstract class StatModifier
{
    public abstract int Modify(BuildingInstance self, IGameContext ctx, int current);
}



public class Modify_NeedTechnologicalNodes : StatModifier
{
    public string NeedNodesId;

    public int AddValue = 3;

    public override int Modify(BuildingInstance self, IGameContext ctx, int current)
    {
        if (ctx != null && ctx.TechTree != null && ctx.TechTree.HasNode(NeedNodesId))
        {
            return current + AddValue;
        }
        return current;

    }
}




/// <summary>用于配置环境需求的结构体。</summary>
[Serializable]
public struct AuraRequirement
{
    public AuraCategory Category;
    public int MinValue;
}

/// <summary>满足光环需求时，为最大值提供额外加成。</summary>
[Serializable]
public class EnvironmentBonus : StatModifier
{
    public AuraRequirement[] Requirements;
    public int Bonus;

    public override int Modify(BuildingInstance self, IGameContext ctx, int current)
    {
        if (Requirements == null || Requirements.Length == 0)
        {
            return current;
        }

        if (self == null || self.Occupy == null || self.Occupy.Length == 0)
        {
            return current;
        }

        if (ctx == null || ctx.Environment == null)
        {
            return current;
        }

        for (int i = 0; i < Requirements.Length; i++)
        {
            AuraRequirement requirement = Requirements[i];
            bool satisfied = false;

            for (int c = 0; c < self.Occupy.Length; c++)
            {
                Vector3Int cell = self.Occupy[c];
                int value = ctx.Environment.GetValue(cell, requirement.Category);
                if (value >= requirement.MinValue)
                {
                    satisfied = true;
                    break;
                }
            }

            if (!satisfied)
            {
                return current;
            }
        }

        return current + Bonus;
    }
}


//[Serializable]
//public class AddMaxPopIfEnvGreater : StatModifier
//{

//    public int Threshold = 1;
//    public int AddValue = 3;

//    public override int Modify(BuildingInstance self, IGameContext ctx, int current)
//    {
//        int env = ctx.Environment.GetEnvAt(self.GridPos);
//        return env > Threshold ? current + AddValue : current;
//    }
//}

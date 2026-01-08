
using UnityEngine;
using System;

[Serializable]
public class R_填充就业 : Rule
{

    public override string GetRuleName() => $"填补就业人口";

    public override object Clone()
    {
        return new R_填充就业();
    }

    public override void OnAdd(BuildingInstance self)
    {
    }
    public override void OnUpdate(BuildingInstance self, TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.回合结束阶段:
                Debug.Log("执行");
                if (self.Self_CurrentWorkers < self.RO_MaxJobsPosition)
                {
                    Debug.Log("有空位");
                    if (self.Ctx.HumanResourcesNetwork.Unemployed > 0)
                    {
                        self.Self_CurrentWorkers++;

                        Debug.LogWarning("以后需要优化");
                    }


                }
                break;
        }
    }
    public override void OnRemove(BuildingInstance self)
    {
    }

    public override string GetDescription()
    {
        return "x";
    }
}



[Serializable]
public class R_回合结束时获取经验 : Rule
{

    public override string GetRuleName() => $"回合结束时增加{AddExp}exp";

    public int AddExp = 1;



    public override object Clone()
    {
        var r = new R_回合结束时获取经验();
        r.AddExp = AddExp;
        return r;
    }

    public override void OnAdd(BuildingInstance self)
    {
    }
    public override void OnUpdate(BuildingInstance self, TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.结束准备阶段:
                break;
            case TurnPhase.资源消耗阶段:
                break;
            case TurnPhase.资源生产阶段:
                break;
            case TurnPhase.回合结束阶段:
                self.Self_CurrentExp += AddExp;
                break;
            case TurnPhase.开始准备阶段:
                break;
            default:
                break;
        }
    }

    public override void OnRemove(BuildingInstance self)
    {
    }

    public override string GetDescription()
    {
        return "X";
    }
}

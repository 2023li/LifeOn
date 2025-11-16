using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;




[Serializable]
public abstract class Rule:ICloneable
{
    public abstract string GetRuleName();
   



    [LabelText("描述"), MultiLineProperty(3)]
    public string Description;                      // 规则说明


    public abstract void OnAdd(BuildingInstance self);


    public abstract void OnUpdate(BuildingInstance self, TurnPhase phase);

    public abstract void OnRemove(BuildingInstance self);
   

    public abstract object Clone();
  
}


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
                if (self.CurrentWorkers<self.RO_MaxJobsPosition)
                {
                    Debug.Log("有空位");
                    if(self.Ctx.HumanResourcesNetwork.Unemployed > 0)
                    {
                        self.CurrentWorkers++;

                        Debug.LogWarning("以后需要优化");
                    }


                }
                break;
        }
    }
    public override void OnRemove(BuildingInstance self)
    {
    }

  
}



[Serializable]
public class R_回合结束时获取经验 : Rule
{

    public override string GetRuleName() => $"回合结束时增加{AddExp}exp";

    public int AddExp = 1;

   

    public override object Clone()
    {
        var  r =  new R_回合结束时获取经验();
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
                self.CurrentExp += AddExp;
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

   
}

[Serializable]
public class R_野生浆果丛规则:Rule
{

    public override string GetRuleName() { return $"生成{supplyAmount.Count}个浆果"; }

    public List<SupplyAmount> supplyAmount;

    public override object Clone()
    {
        var r =  new R_野生浆果丛规则();
        r.supplyAmount = supplyAmount;
        return r;
    }

    public override void OnAdd(BuildingInstance self)
    {
        foreach (SupplyAmount item in supplyAmount)
        {
            self.Ctx.ResourceNetwork.RegisterProducer(self, item.Resource);
        }

       
    }

    public override void OnRemove(BuildingInstance self)
    {
        foreach (SupplyAmount item in supplyAmount)
        {
            self.Ctx.ResourceNetwork.UnregisterProducer(self, item.Resource);
        }

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

                foreach (SupplyAmount item in supplyAmount)
                {
                    if (!self.Ctx.ResourceNetwork.TryAddResource(item.Resource,item.Amount,out string r))
                    {
                        Debug.Log(r);
                    }
                }

                break;
            case TurnPhase.回合结束阶段:
                break;
            case TurnPhase.开始准备阶段:

                

                break;
        }
    }
}

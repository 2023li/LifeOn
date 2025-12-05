using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BR_测试 : Rule
{
    public override object Clone()
    {
        return new BR_测试();
    }

    public override string GetDescription()
    {
        return "测试建筑描述";
    }

    public override string GetRuleName()
    {
        return "测试建筑规则";
    }

    public override void OnAdd(BuildingInstance self)
    {
        self.AddProduct(SupplyLib.GetSupplyDef(SupplyEnum.SD_测试));
        self.AddProduct(SupplyLib.GetSupplyDef(SupplyEnum.SD_原木));
    }

    public override void OnRemove(BuildingInstance self)
    {
        self.RemoveProduct(SupplyLib.GetSupplyDef(SupplyEnum.SD_测试));
        self.RemoveProduct(SupplyLib.GetSupplyDef(SupplyEnum.SD_原木));
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

                self.BE_TryAddResource(SupplyDef.GetSupplyDef(SupplyEnum.SD_测试),10);

                break;
            case TurnPhase.回合结束阶段:
                break;
            case TurnPhase.开始准备阶段:
                break;
            default:
                break;
        }
    }

  
}

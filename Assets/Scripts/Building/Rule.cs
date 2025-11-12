using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;




[Serializable]
public abstract class Rule:ICloneable
{
    public  virtual string RuleName { get; set; } = "未命名";



    [LabelText("描述"), MultiLineProperty(3)]
    public string Description;                      // 规则说明


    public virtual void OnAdd(BuildingInstance self)
    {

    }

    public virtual void OnUpdate(BuildingInstance self,TurnPhase phase)
    {

    }
    public virtual void OnRemove(BuildingInstance self)
    {

    }

    public abstract object Clone();
  
}



public class R_就业 : Rule
{
    public override object Clone()
    {
        return new R_就业();
    }

    public override void OnUpdate(BuildingInstance self, TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.回合结束阶段:


                break;
        }
    }


}

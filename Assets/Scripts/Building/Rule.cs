using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;


public enum RuleExecuteTime
{
    规则启用时,

    每回合执行,

    拆除时执行,
}

[Serializable]
public abstract class Rule
{
    public  virtual string ElementLabel { get; set; } = "未命名";



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

    
}



public class R_就业 : Rule
{

    public override void OnUpdate(BuildingInstance self, TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.回合结束阶段:


                break;
        }
    }


}

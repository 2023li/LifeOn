using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;


public enum RuleExecuteTime
{
    升级后执行,

    每回合执行,

    拆除时执行,
}

[Serializable]
public class Rule
{


     // —— 元信息（供人看）——
    [LabelText("名称")]
    public string Name;                             // 在列表标题里显示

    [LabelText("描述"), MultiLineProperty(3)]
    public string Description;                      // 规则说明

     public string ElementLabel =>
        string.IsNullOrWhiteSpace(Name)
            ? $"[{Trigger}] 条件{(Conditions?.Count ?? 0)} | 成功{(OnSuccess?.Count ?? 0)} | 失败{(OnFailure?.Count ?? 0)}"
            : Name;


    public RuleExecuteTime ExecutePhase = RuleExecuteTime.每回合执行;

    [ShowIf(nameof(ExecutePhase),nameof(ExecutePerRound))]
    public TurnPhase Trigger = TurnPhase.结束准备阶段;
    [SerializeReference] public List<Condition> Conditions = new();
    [SerializeReference] public List<Effect> OnSuccess = new();
    [SerializeReference] public List<Effect> OnFailure = new();




    private bool ExecutePerRound()
    {
       return ExecutePhase == RuleExecuteTime.每回合执行;
    }

}

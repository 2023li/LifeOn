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



    public virtual void OnAdd(BuildingInstance self)
    {

    }

    public virtual void OnUpdate(BuildingInstance self,TurnPhase phase)
    {

    }
    public virtual void OnRemove(BuildingInstance self)
    {

    }


    [LabelText("执行时机")]
    public RuleExecuteTime ExecutePhase = RuleExecuteTime.每回合执行;


    [ShowIf(nameof(ExecutePhase),nameof(ExecutePerRound))]
    public TurnPhase Trigger = TurnPhase.结束准备阶段;

    [SerializeReference] public List<Condition> Conditions = new();
    [SerializeReference] public List<Effect> OnSuccess = new();
    [SerializeReference] public List<Effect> OnFailure = new();





    /// <summary>
    /// 由外部调用的统一执行入口。
    /// 外部只需要传入当前的时机 + 建筑 + 上下文。
    /// 具体怎么判断、怎么生效，由子类自己决定。
    /// </summary>
    public virtual void Execute(RuleExecuteTime timing,TurnPhase? phase,BuildingInstance self,IGameContext ctx)
    {
        // 默认实现：沿用原本“条件 + 成功/失败效果”的通用逻辑

        if (!ShouldExecute(timing, phase))
            return;

        bool ok = EvaluateConditions(self, ctx, out string _);

        var effects = ok ? OnSuccess : OnFailure;
        ApplyEffects(effects, self, ctx);
    }

    /// <summary>
    /// 是否在本次时机下应该执行（可被子类重写）。
    /// </summary>
    protected virtual bool ShouldExecute(RuleExecuteTime timing, TurnPhase? phase)
    {
        if (ExecutePhase != timing)
            return false;

        if (ExecutePhase == RuleExecuteTime.每回合执行)
        {
            if (!phase.HasValue)
                return false;

            if (Trigger != phase.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 条件判定（需要时子类可重写，或者直接在 Execute 里无视它）。
    /// </summary>
    protected virtual bool EvaluateConditions(
        BuildingInstance self,
        IGameContext ctx,
        out string failedReason)
    {
        failedReason = string.Empty;

        if (Conditions == null || Conditions.Count == 0)
            return true;

        foreach (Condition c in Conditions)
        {
            if (c == null)
                continue;

            if (!c.Evaluate(self, ctx, out failedReason))
            {
                // 有失败就直接返回
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 应用效果，子类可重写以实现更复杂控制。
    /// </summary>
    protected virtual void ApplyEffects(
        List<Effect> effects,
        BuildingInstance self,
        IGameContext ctx)
    {
        if (effects == null)
            return;

        foreach (Effect e in effects)
        {
            e?.Apply(self, ctx);
        }
    }







    private bool ExecutePerRound()
    {
       return ExecutePhase == RuleExecuteTime.每回合执行;
    }

}



public class R_就业 : Rule
{
    
}

using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;



public enum RuleLifecycle
{
    //伴随建筑到销毁
    Persistent = 0,
    //持续一定回合数
    TimeBased = 1,
    //持续到建筑升级
    LevelBase = 2,
}

[Serializable]
public abstract class Rule:ICloneable
{
    [ShowInInspector, MultiLineProperty(3),PropertyOrder(-1), HideLabel]
    [FoldoutGroup("描述")]
    public string DescriptionDisplay => GetDescription();
    public abstract string GetRuleName();

    public RuleLifecycle Lifecycle = RuleLifecycle.Persistent;

    
    [LabelText("持续时间"),ShowIf(nameof(Lifecycle), RuleLifecycle.TimeBased)] 
    public int RemainingRounds = -1;
    

    public abstract string GetDescription();                  // 规则说明


    public abstract void OnAdd(BuildingInstance self);

    public abstract void OnUpdate(BuildingInstance self, TurnPhase phase);

    public abstract void OnRemove(BuildingInstance self);

    public abstract object Clone();
  
}



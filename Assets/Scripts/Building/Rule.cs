using UnityEngine;
using System;
using System.Collections.Generic;


[Serializable]
public class Rule
{
    public TurnPhase Trigger = TurnPhase.结束准备阶段;
    [SerializeReference] public List<Condition> Conditions = new();
    [SerializeReference] public List<Effect> OnSuccess = new();
    [SerializeReference] public List<Effect> OnFailure = new();
}

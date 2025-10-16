using UnityEngine;
using System;
using System.Collections.Generic;


[Serializable]
public class Rule
{
    public TriggerPhase Trigger = TriggerPhase.TurnEnd;
    [SerializeReference] public List<Condition> Conditions = new();
    [SerializeReference] public List<Effect> OnSuccess = new();
    [SerializeReference] public List<Effect> OnFailure = new();
}

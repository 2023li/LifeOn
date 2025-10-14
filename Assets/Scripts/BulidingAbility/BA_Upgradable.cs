using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BA_Upgradable : BulidingAbility
{

    int currentEXP;
    protected override void OnAdd()
    {
        TurnSystem.Instance.OnPhaseProgress += HandleEXP;

    }

    public override void Remove()
    {

        TurnSystem.Instance.OnPhaseProgress -= HandleEXP;

        Destroy(this);
    }

    //完全独立处理
    private void HandleEXP()
    {
        currentEXP++;

        if (currentEXP >= building.Def.NeedEXP)
        {
            building.Construction(building.Def.NextLevel);
        }
    } 

  
}

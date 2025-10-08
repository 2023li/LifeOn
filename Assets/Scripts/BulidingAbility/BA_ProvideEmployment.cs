using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BA_ProvideEmployment : BulidingAbility
{
    protected override void OnAdd()
    {
     
    }

    public override void Remove()
    {
        Destroy(this);
    }

   
}

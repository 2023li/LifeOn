using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


/*
 * 根据building.Def进行生产或是消耗
 * 获取材料需要从附近的仓库中获取 获取范围不大于building.Def.BaseMaterialFetchingRadius (路径计算可参考CoordinateCalculator.cs脚本)
 * 
 */

public class BA_SupplyAndDemand : BulidingAbility
{
    public bool Satisfy { get; set; } = false;

    private BA_Inventory inventory;

    public void SetInventory(BA_Inventory i)
    {
        inventory = i;
    }

    public void Consume()
    {
        Satisfy =false; 

        if (inventory == null)
        {
            return;
        }

        foreach (var item in building.Def.SupplyStacks)
        {
            if (item.Operation==SupplyOperation.Consume)
            {

                //需要确保能够满足要求


            }
        }



        if (Satisfy)
        {
            //进行消耗
        }


    }

    public void Produce()
    {

    }



    protected override void OnAdd()
    {
        BuildingManager.Instance.Register(this);
    }

    public override void Remove()
    {
        Destroy(this);
    }




   
}

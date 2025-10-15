using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;



/// <summary>
/// 负责监听TurnSystem的事件 统一转发给所有的建筑
/// </summary>
public class BuildingManager : Singleton<BuildingManager> 
{



    public List<BA_SupplyAndDemand> _SupplyAndDemands;

    public List<BA_Inventory> _Inventories;

    public void Clear()
    {

    }



    public void Register(BA_SupplyAndDemand sd)
    {
        _SupplyAndDemands.Add(sd);
    }
    public void Unregister(BA_SupplyAndDemand sd)
    {
        if (_SupplyAndDemands.Contains(sd))
        {
            _SupplyAndDemands.Remove(sd);
        }

    }
    public void Register(BA_Inventory inventory)
    {

    }

    public void Unregister(BA_Inventory inventory)
    {
        if (_Inventories.Contains(inventory))
        {
            _Inventories.Remove(inventory);
        }

    }
}

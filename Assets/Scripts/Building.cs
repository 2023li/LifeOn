using Sirenix.OdinInspector;
using UnityEngine;

public class Building : MonoBehaviour
{

    [ShowInInspector]
    [ReadOnly]
    public BuildingDef Def { get; private set; }

    [ShowInInspector]
    [ReadOnly]
    public Vector3Int[] Coordinates {  get; private set; }



    [Button]
    public void Construction(BuildingDef data)
    {
        if (!data) { Debug.LogWarning("传入建筑定义非法"); return; }
        



        Def = data;

        AddAbility();
    }


    private BA_Population ba_Population;
    private BA_ProvideEmployment ba_ProvideEmployment;
    private BA_Upgradable ba_Upgradable;
    private BA_SupplyAndDemand ba_SupplyAndDemand;
    private BA_Inventory ba_Inventory;
    private BA_HaloEffect ba_HaloEffect;
    private void AddAbility()
    {
        if (ba_Population) { ba_Population.Remove(); }
        if (ba_ProvideEmployment) { ba_ProvideEmployment.Remove(); }
        if (ba_Upgradable) { ba_Upgradable.Remove(); }
        if (ba_SupplyAndDemand) { ba_SupplyAndDemand.Remove(); }
        if (ba_Inventory) { ba_Inventory.Remove(); }
        if (ba_HaloEffect) { ba_HaloEffect.Remove(); }



        if (Def.ProvidePopulation){ ba_Population =  BulidingAbility.Add<BA_Population>(this);}
        if (Def.ProvideEmployment) { ba_ProvideEmployment = BulidingAbility.Add<BA_ProvideEmployment>(this);}
        if (Def.ProvideUpgradable) { ba_Upgradable =  BulidingAbility.Add<BA_Upgradable>(this); }
        if (Def.ProvideSupplyAndDemand) { ba_SupplyAndDemand = BulidingAbility.Add<BA_SupplyAndDemand>(this); }
        if (Def.ProvideInventory) { ba_Inventory = BulidingAbility.Add<BA_Inventory>(this); }
        if (Def.ProvideHaloEffect) { ba_HaloEffect =  BulidingAbility.Add<BA_HaloEffect>(this); }
    }
}

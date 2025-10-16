using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;
public class ResourceRouting:MonoSingleton<ResourceRouting>
{

    public ResourceRouting()
    {
        allBuildingDef = new Dictionary<string, BuildingArchetype>();
    }

    Dictionary<string, BuildingArchetype> allBuildingDef;


    public List<BuildingArchetype> teest;

    public List<BuildingArchetype> GetClassAllBuildingDef(BuildingClassify classify)
    {


        return teest;



        var list = new List<BuildingArchetype>();
        foreach (var item in allBuildingDef.Values)
        {
            if (item.classification == classify)
            {
                list.Add(item);
            }
        }

        return list;
    }


}

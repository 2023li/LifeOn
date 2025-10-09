using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;
public class ResourceRouting:MonoSingleton<ResourceRouting>
{
    public ResourceRouting()
    {
        allBuildingDef = new Dictionary<string, BuildingDef>();
    }

    Dictionary<string, BuildingDef> allBuildingDef;


    public List<BuildingDef> teest;

    public List<BuildingDef> GetClassAllBuildingDef(BuildingClassify classify)
    {


        return teest;



        var list = new List<BuildingDef>();
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GlobalResources
{
    金钱,
    科研点,
    //其他的
}

public struct GlobalResourcesAmount
{
    public GlobalResources type;
    public int amount;
}

public class NationalTreasury
{

    private Dictionary<GlobalResources, int> dic_GlobalResourcesAmount;


    public void Init()
    {
        dic_GlobalResourcesAmount = new Dictionary<GlobalResources, int>();
    }


    //返回操作成功或失败
    public bool ChangeAmount(GlobalResourcesAmount gr)
    {
        throw new System.NotImplementedException();
    }




 
}

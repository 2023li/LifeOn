

using System;
using Sirenix.OdinInspector;

public enum SupplyType
{
    Cloth,    // 衣服
    Furniture,// 家具
    Stone,    // 石头
    Clay      // 粘土
}


[Serializable]
public struct SupplyStack
{
    [HorizontalGroup]
    public SupplyType Type;    // 物资类型
    [HorizontalGroup]
    public int Need;       // 数量
}

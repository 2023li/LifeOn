

using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public enum SupplyType
{
    Food,
    Cloth,    // 衣服
    Furniture,// 家具
    Stone,    // 石头
    Clay      // 粘土
}


public enum SupplyOperation
{
    Consume,   // 消耗
    Produce    // 生产
}


[Serializable]
public struct SupplyChange
{
    [HorizontalGroup]
    [LabelText("供需类型")]
    public SupplyOperation Operation;   // 供给或消耗

    [HorizontalGroup]
    [LabelText("物资类型")]
    public SupplyType Type;    // 物资类型

    [HorizontalGroup]
    [LabelText("数量")]
    [FormerlySerializedAs("Need")]
    public int Amount;       // 数量
}


public struct SupplyAmount
{
    [HorizontalGroup]
    [LabelText("物资类型")]
    public SupplyType Type;    // 物资类型

    [HorizontalGroup]
    [LabelText("数量")]
    [FormerlySerializedAs("Need")]
    public int Amount;       // 数量
}


using System;
using Sirenix.OdinInspector;
using UnityEngine;

public struct StructKV<T1,T2>
{
    public T1 Value1;
    public T2 Value2;
}




public enum HaloEffectType
{
    环境,
    治安,
    医疗,
}

[Serializable]
public struct HaloEffectRangeValue
{
    [HorizontalGroup]
    public HaloEffectType Type;

    [HorizontalGroup]
    public short Range;

    [HorizontalGroup]
    [Range(-3, 3)]
    public short Value;
}

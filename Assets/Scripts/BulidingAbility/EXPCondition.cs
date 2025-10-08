using Sirenix.OdinInspector;
using System;



public enum ExpAcquisitionConditionEnum
{
    回合结束时,
    条件1,
    条件2,
    条件3
}

[Serializable]
public struct EXPConditionValue
{
    [HorizontalGroup]
    public ExpAcquisitionConditionEnum Condition;
    [HorizontalGroup]
    public int BaseValue;
}


public interface IEXPCalculate
{
    public bool AcquisitionEXP();
}





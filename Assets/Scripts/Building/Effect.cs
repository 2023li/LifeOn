using UnityEngine;

public abstract class Effect : ScriptableObject
{
    /// <summary>
    /// 对指定建筑应用效果（如修改属性、触发行为）。
    /// </summary>
    public abstract void Apply(Building building);
}

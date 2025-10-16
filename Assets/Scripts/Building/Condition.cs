using UnityEngine;

public abstract class Condition : ScriptableObject
{
    /// <summary>
    /// 判断条件是否满足，传入相关的建筑实例。
    /// </summary>
    public abstract bool Evaluate(Building building);
}

using UnityEngine;

[CreateAssetMenu(fileName = "Cond_EnvironmentValueAtLeast", menuName = "LO/Conditions/EnvironmentValueAtLeast")]
public class EnvironmentValueAtLeast : Condition
{
    [Tooltip("环境参数键名")]
    public string parameterKey;
    [Tooltip("所需的最低环境值")]
    public float minimumValue;

    public override bool Evaluate(Building building)
    {
        // 从全局环境服务获取对应参数的值并与阈值比较
        float currentValue = EnvironmentService.GetValue(parameterKey);
        return currentValue >= minimumValue;
    }
}

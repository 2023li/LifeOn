using UnityEngine;

[CreateAssetMenu(fileName = "Cond_ExperienceAtLeast", menuName = "LO/Conditions/ExperienceAtLeast")]
public class ExperienceAtLeast : Condition
{
    [Tooltip("所需的最低经验值。如果为0，表示使用建筑定义的升级所需经验。")]
    public int minimumExperience = 0;

    public override bool Evaluate(Building building)
    {
        // 获取升级所需经验：优先使用配置的值，否则取建筑当前等级定义中的升级所需经验
        int required = minimumExperience > 0 ? minimumExperience : building.CurrentLevelDef.RequiredExp;
        if (required <= 0)
        {
            return false; // 没有设定需要经验（可能当前等级不可升级）
        }
        // 检查建筑当前经验是否达到要求
        return building.CurrentExp >= required;
    }
}

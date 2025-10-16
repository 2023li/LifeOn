using UnityEngine;

[CreateAssetMenu(fileName = "Cond_PopulationAtLeast", menuName = "LO/Conditions/PopulationAtLeast")]
public class PopulationAtLeast : Condition
{
    [Tooltip("所需的最低人口数量")]
    public int minimumPopulation;

    public override bool Evaluate(Building building)
    {
        // 检查建筑当前人口是否达到要求
        return building.CurrentPopulation >= minimumPopulation;
    }
}

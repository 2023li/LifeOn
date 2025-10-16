using UnityEngine;

[CreateAssetMenu(fileName = "Cond_WorkersAtLeast", menuName = "LO/Conditions/WorkersAtLeast")]
public class WorkersAtLeast : Condition
{
    [Tooltip("所需的最低工人数量")]
    public int minimumWorkers;

    public override bool Evaluate(Building building)
    {
        // 检查建筑当前分配的工人数是否达到要求
        return building.CurrentWorkers >= minimumWorkers;
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AddExp", menuName = "LO/Effects/AddExp")]
public class AddExp : Effect
{
    [Tooltip("增加的经验值数量")]
    public int expAmount;

    public override void Apply(Building building)
    {
        // 增加建筑的经验值
        building.CurrentExp += expAmount;
        // （可选）日志输出
        Debug.Log($"Building {building.name} gains {expAmount} EXP (total EXP = {building.CurrentExp})");
    }
}

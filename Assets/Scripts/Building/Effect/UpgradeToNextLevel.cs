using UnityEngine;

[CreateAssetMenu(fileName = "Effect_UpgradeToNextLevel", menuName = "LO/Effects/UpgradeToNextLevel")]
public class UpgradeToNextLevel : Effect
{
    public override void Apply(Building building)
    {
        // 调用建筑实例的方法执行升级
        building.UpgradeToLevel(building.LevelIndex + 1);
        // （可选）日志输出
        Debug.Log($"Building {building.name} upgraded to Level {building.LevelIndex}");
    }
}

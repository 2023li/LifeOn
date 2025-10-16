using UnityEngine;

[CreateAssetMenu(fileName = "Cond_HasResourcesInStorage", menuName = "LO/Conditions/HasResourcesInAssignedStorage")]
public class HasResourcesInAssignedStorage : Condition
{
    [Tooltip("所需的资源类型和数量列表（全部满足才视为条件成立）")]
    public SupplyAmount[] requiredResources;  // 所需的资源及数量列表

    public override bool Evaluate(Building building)
    {
        // 获取建筑绑定的仓库库存，如果没有绑定仓库则使用资源网络查询
        Inventory sourceInv = null;
        if (building.assignedStorage != null && building.assignedStorage.Inventory != null)
        {
            sourceInv = building.assignedStorage.Inventory;
        }

        // 遍历每种所需资源，检查是否满足数量要求
        foreach (var req in requiredResources)
        {
            bool satisfied = false;
            if (sourceInv != null)
            {
                // 检查绑定仓库中该资源数量
                satisfied = sourceInv.HasResource(req.Type, req.Amount);
            }
            else
            {
                // 没有绑定仓库，则通过资源网络在一定范围内查找可用资源
                float radius = building.CurrentLevelDef.MaterialFetchingRadius;
                // 尝试在资源网络中找到有足够资源的库存
                Inventory found = ResourceNetwork.FindInventoryWithResource(req.Type, req.Amount, building.transform.position, radius);
                satisfied = (found != null);
            }
            if (!satisfied)
            {
                // 任意一种资源不满足，则条件不成立
                return false;
            }
        }
        // 所有列出的资源都满足需求
        return true;
    }
}

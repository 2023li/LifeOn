using UnityEngine;

[CreateAssetMenu(fileName = "Effect_ConsumeFromStorage", menuName = "LO/Effects/ConsumeFromAssignedStorage")]
public class ConsumeFromAssignedStorage : Effect
{
    [Tooltip("要消耗的资源类型")]
    public SupplyType resourceType;
    [Tooltip("消耗的数量")]
    public int amount;

    public override void Apply(Building building)
    {
        // 优先从绑定的仓库取资源，否则通过资源网络尝试获取
        if (building.assignedStorage != null && building.assignedStorage.Inventory != null)
        {
            bool taken = building.assignedStorage.Inventory.RemoveResource(resourceType, amount);
            if (!taken)
            {
                Debug.LogWarning($"Assigned storage lacks {amount} of {resourceType} for {building.name}");
            }
        }
        else
        {
            // 无绑定仓库，尝试从附近任何仓库取所需资源
            bool taken = ResourceNetwork.TryConsumeResource(building, resourceType, amount);
            if (!taken)
            {
                Debug.LogWarning($"No available resource {amount} of {resourceType} for {building.name} in network");
            }
        }
    }
}

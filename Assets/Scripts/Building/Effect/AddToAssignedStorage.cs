using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AddToAssignedStorage", menuName = "LO/Effects/AddToAssignedStorage")]
public class AddToAssignedStorage : Effect
{
    [Tooltip("要存入的资源类型")]
    public SupplyType resourceType;
    [Tooltip("存入的数量")]
    public int amount;

    public override void Apply(Building building)
    {
        // 优先存入绑定的仓库
        if (building.assignedStorage != null && building.assignedStorage.Inventory != null)
        {
            bool stored = building.assignedStorage.Inventory.AddResource(resourceType, amount);
            if (!stored)
            {
                Debug.LogWarning($"Assigned storage full, cannot add {amount} of {resourceType} from {building.name}");
            }
        }
        else
        {
            // 无绑定仓库，则尝试存入附近有空间的仓库
            Inventory inv = ResourceNetwork.FindNearestInventoryWithSpace(building.transform.position, building.CurrentLevelDef.MaterialFetchingRadius, amount);
            if (inv != null)
            {
                bool stored = inv.AddResource(resourceType, amount);
                if (!stored)
                {
                    Debug.LogWarning($"Nearest storage found but failed to store {resourceType}");
                }
            }
            else
            {
                Debug.LogWarning($"No available storage space for {amount} of {resourceType} from {building.name}");
            }
        }
    }
}

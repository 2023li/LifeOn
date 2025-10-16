using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AddToSelfStorage", menuName = "LO/Effects/AddToSelfStorage")]
public class AddToSelfStorage : Effect
{
    [Tooltip("要添加的资源类型")]
    public SupplyType resourceType;
    [Tooltip("添加的数量")]
    public int amount;

    public override void Apply(Building building)
    {
        if (building.Inventory == null)
        {
            Debug.LogWarning($"Building {building.name} has no self-inventory, cannot store {amount} of {resourceType}");
            return;
        }
        // 将资源添加到建筑自身库存
        bool success = building.Inventory.AddResource(resourceType, amount);
        if (!success)
        {
            Debug.LogWarning($"Building {building.name} inventory full, failed to add {amount} of {resourceType}");
        }
    }
}

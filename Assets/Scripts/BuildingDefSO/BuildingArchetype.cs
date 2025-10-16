using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BuildingArchetype", menuName = "LO/Building Archetype")]
public class BuildingArchetype : ScriptableObject
{
    [Header("基础信息")]
    public string BuildingID;               // 建筑唯一ID
    public string BuildingName;             // 建筑名称
    public GameObject BuildingPrefab;       // 建筑实体预制件（可选，不依赖于规则系统）
    public BuildingClassify classification; // 建筑分类（如果有定义枚举）
    public int Size = 1;                    // 占地尺寸（例如网格大小）
    [Header("等级定义列表")]
    public List<BuildingLevelDef> levels;
}

[System.Serializable]
public class BuildingLevelDef
{
    [Header("等级基本属性")]
    public int level;                       // 等级序号（0为初始等级）
 
    public int InventoryCapacity = 0;       // 库存容量（0表示此等级无库存）
    public int BaseMaxPopulation = 0;       // 提供的最大人口容量
    public float BaseProportionWorkingPopulation = 0f; // 基础工作人口比例（适用于住宅）
    public short MaxEmployment = 0;         // 提供的最大就业岗位
    public int BasicSalary = 0;             // 基础薪资（如果适用）
    public int MaterialFetchingRadius = 0;  // 获取原料的半径范围

    [Header("供需配置")]
    public SupplyChange[] SupplyChanges;    // 物资供需列表（消耗或产出）

    [Header("升级配置")]
    public int RequiredExp = 0;             // 升级到下一级所需经验值

    [Header("规则列表")]
    public List<Rule> rules;               // 本等级的规则集合（触发-条件-效果）
}

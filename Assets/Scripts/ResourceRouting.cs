using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;
using Sirenix.OdinInspector;

public class ResourceRouting : MonoSingleton<ResourceRouting>
{
    [LabelText("食物资源定义")]
    [SerializeField] private SupplyDef foodSupply;

    [LabelText("木材资源定义")]
    [SerializeField] private SupplyDef woodSupply;

    [LabelText("建筑定义列表")]
    [SerializeField] private List<BuildingArchetype> buildingDefinitions = new List<BuildingArchetype>();

    private readonly Dictionary<string, BuildingArchetype> allBuildingDef = new Dictionary<string, BuildingArchetype>();
    private bool definitionsBuilt;
    public SupplyDef FoodSupply => foodSupply;
    public SupplyDef WoodSupply => woodSupply;

    protected override void Initialize()
    {
        base.Initialize();
        BuildDefinitionsIfNeeded();
    }

    /// <summary>按分类获取全部建筑定义。</summary>
    public List<BuildingArchetype> GetClassAllBuildingDef(BuildingClassify classify)
    {
        BuildDefinitionsIfNeeded();

        List<BuildingArchetype> list = new List<BuildingArchetype>();
        foreach (KeyValuePair<string, BuildingArchetype> pair in allBuildingDef)
        {
            BuildingArchetype def = pair.Value;
            if (def != null && def.classification == classify)
            {
                list.Add(def);
            }
        }

        return list;
    }

    public BuildingArchetype GetDefinition(string id)
    {
        BuildDefinitionsIfNeeded();

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (allBuildingDef.TryGetValue(id, out BuildingArchetype def))
        {
            return def;
        }

        return null;
    }

    /// <summary>确保运行时的建筑定义只初始化一次。</summary>
    private void BuildDefinitionsIfNeeded()
    {
        if (definitionsBuilt)
        {
            return;
        }

        if (buildingDefinitions == null || buildingDefinitions.Count == 0)
        {
            Debug.LogWarning("[ResourceRouting] 未配置任何建筑定义资产，路由表将为空。", this);
        }
        else
        {
            foreach (BuildingArchetype def in buildingDefinitions)
            {
                RegisterDefinition(def);
            }
        }

        definitionsBuilt = true;
    }

    private void RegisterDefinition(BuildingArchetype def)
    {
        if (def == null || string.IsNullOrEmpty(def.Id))
        {
            return;
        }

        allBuildingDef[def.Id] = def;
    }





}

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 建筑原型生成窗口：
/// 1) 通过 SupplyLib.GetSupplyDef 获取资源；
/// 2) 保存到 Assets/GameData/Buildings/<建筑ID>/<建筑ID>.asset；
/// </summary>
public class BuildingArchetypeCreatorWindow : EditorWindow
{
  

    // 基础保存目录（可按需修改为 "Assets/GameData/Building" 等）
    private const string BaseFolder = "Assets/GameData/Buildings";

    [MenuItem("SSBX/Building Archetype Creator")]
    public static void ShowWindow()
    {
        GetWindow<BuildingArchetypeCreatorWindow>("Building Archetype Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("资源ID配置（通过 SupplyLib 获取）", EditorStyles.boldLabel);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("居民房 (Residential House)", EditorStyles.boldLabel);
        if (GUILayout.Button("生成 居民房 Archetype"))
        {
            CreateResidentialHouseArchetype();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("野生浆果丛 (Wild Berry Bush)", EditorStyles.boldLabel);
        if (GUILayout.Button("生成 浆果丛 Archetype"))
        {
            CreateWildBerryBushArchetype();
        }
    }

    private void CreateResidentialHouseArchetype()
    {
     
        // Create ScriptableObject instance
        BuildingArchetype arch = ScriptableObject.CreateInstance<BuildingArchetype>();
        arch.Id = "building_居民房";
        arch.DisplayName = "居民房";
        arch.Size = 2;
        arch.classification = BuildingClassify.基础;

        // 可见与可建条件
        arch.ShowInBuildPanel = new List<Condition>();
        arch.AllowConstruction = new List<Condition>();

        // Lv 列表
        arch.Levels = new List<BuildingLevelDef>();

        // Level 0
        {
            BuildingLevelDef lvl0 = new BuildingLevelDef();
            lvl0.ExpToNext = 3;
            lvl0.BaseMaxPopulation = 0;
            lvl0.BaseStorageCapacity = 0;
            lvl0.ConditionsForAllowingUpgrades = new List<Condition>();
            lvl0.Rules = new List<Rule>();

            // 回合结束时 +1 exp
            Rule expGainRule = new Rule
            {
                Name = "Lv0 EndTurn Exp",
                Description = "回合结束时 +1 经验",
                Trigger = TurnPhase.回合结束阶段,
                Conditions = new List<Condition>(),
                OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
                OnFailure = new List<Effect>()
            };
            lvl0.Rules.Add(expGainRule);

            arch.Levels.Add(lvl0);
        }

        // Level 1
        {
            BuildingLevelDef lvl1 = new BuildingLevelDef();
            lvl1.ExpToNext = 10;
            lvl1.BaseMaxPopulation = 5;
            lvl1.BaseStorageCapacity = 0;
            lvl1.ConditionsForAllowingUpgrades = new List<Condition>
            {
                new TechUnlockedCondition { TechId = "tech_历法" }
            };
            lvl1.Rules = new List<Rule>();

            // 供养人口规则
            Rule feedRule = new Rule
            {
                Name = "Feeding",
                Description = "消耗食物供养人口，人口增减",
                Trigger = TurnPhase.资源消耗阶段
            };
            feedRule.Conditions = new List<Condition>
            {
                new PopulationAtLeast { Min = 1 },
                new HasResourceForPopulationByCategory
                {
                    Category = SupplyCategory.一级食物,
                    AmountPerCapita = 1f,
                    IgnoreIfPopulationZero = true
                }
            };
            feedRule.OnSuccess = new List<Effect>
            {
                new ConsumeResourcePerPopulationByCategory
                {
                    Category = SupplyCategory.一级食物,
                    AmountPerCapita = 1f,
                    IgnoreIfPopulationZero = true
                },
                new ChangePopulation { Delta = 1 }
            };
            feedRule.OnFailure = new List<Effect>
            {
                new ChangePopulation { Delta = -1 }
            };
            lvl1.Rules.Add(feedRule);

            // 人口满时额外 +1 经验
            Rule fullPopExpRule = new Rule
            {
                Name = "FullPop Exp Bonus",
                Description = "人口已满时，每回合额外 +1 经验",
                Trigger = TurnPhase.回合结束阶段,
                Conditions = new List<Condition> { new PopulationAtLeast { Min = 5 } },
                OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
                OnFailure = new List<Effect>()
            };
            lvl1.Rules.Add(fullPopExpRule);

            arch.Levels.Add(lvl1);
        }

        SaveArchetypeAsset(arch);
    }

    private void CreateWildBerryBushArchetype()
    {
       
        BuildingArchetype arch = ScriptableObject.CreateInstance<BuildingArchetype>();
        arch.Id = "Building_野生浆果丛";
        arch.DisplayName = "野生浆果丛";
        arch.Size = 2;
        arch.classification = BuildingClassify.农业类;

        // 不在建造面板显示，也不允许建造
        arch.ShowInBuildPanel = new List<Condition> { new NeverNo() };
        arch.AllowConstruction = new List<Condition> { new NeverNo() };

        arch.Levels = new List<BuildingLevelDef>();

        // 单一等级（Lv0）
        BuildingLevelDef lvl0 = new BuildingLevelDef
        {
            ExpToNext = -1,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 20,
            ConditionsForAllowingUpgrades = new List<Condition>(),
            Rules = new List<Rule>()
        };

        // 产出规则：库存未满时每回合产出 2 单位浆果
        Rule produceRule = new Rule
        {
            Name = "Berry Production",
            Description = "库存未满时，每回合产出 2 单位浆果",
            Trigger = TurnPhase.资源生产阶段
        };
        produceRule.Conditions = new List<Condition> { new InventoryNotFullCondition() };
        SupplyAmount berryItem = new SupplyAmount { Resource = SupplyLib.GetSupplyDef("Supply_浆果"), Amount = 2 };
        produceRule.OnSuccess = new List<Effect> { new AddToSelfStorage { Items = new[] { berryItem } } };
        produceRule.OnFailure = new List<Effect>();
        lvl0.Rules.Add(produceRule);

        arch.Levels.Add(lvl0);

        SaveArchetypeAsset(arch);
    }

    /// <summary>
    /// 保存到 Assets/GameData/Buildings/&lt;ID&gt;/&lt;ID&gt;.asset
    /// </summary>
    private static void SaveArchetypeAsset(BuildingArchetype arch)
    {
        EnsureFolders(BaseFolder);

        string safeId = SanitizeForFileName(arch.Id);
        string folder = $"{BaseFolder}/{safeId}";
        EnsureFolders(folder);

        string assetPath = $"{folder}/{safeId}.asset";
        string finalPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(arch, finalPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = arch;
        EditorGUIUtility.PingObject(arch);

        Debug.Log($"[BuildingArchetypeCreator] 生成成功：{finalPath}");
    }

    /// <summary>逐级确保目录存在。</summary>
    private static void EnsureFolders(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string parent = current;
            current = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(current))
            {
                AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }
    }

    /// <summary>将非法文件名字符替换为下划线。</summary>
    private static string SanitizeForFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid) name = name.Replace(c, '_');
        return name;
    }
}
#endif

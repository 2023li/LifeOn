using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
// (Assuming necessary using directives for game-specific types like BuildingArchetype, SupplyDef, etc.)
using Sirenix.OdinInspector;



public class BuildingArchetypeCreatorWindow : EditorWindow
{
    // Developer note fields for each building entry
    private string note_Residential = string.Empty;
    private string note_BerryBush = string.Empty;

    // (Optional) References to needed assets like SupplyDef for resources:
    [SerializeField, LabelText("食物资源定义")] private SupplyDef _foodSupply;
    [SerializeField, LabelText("浆果资源定义")] private SupplyDef _berrySupply;

    [MenuItem("SSBX/Building Archetype Creator")]
    public static void ShowWindow()
    {
        GetWindow<BuildingArchetypeCreatorWindow>("Building Archetype Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("居民房 (Residential House)", EditorStyles.boldLabel);
        _foodSupply = (SupplyDef)EditorGUILayout.ObjectField("食物资源定义", _foodSupply, typeof(SupplyDef), false);
        note_Residential = EditorGUILayout.TextField("Note", note_Residential);
        if (GUILayout.Button("Generate 居民房 Archetype"))
        {
            if (_foodSupply == null)
            {
                EditorUtility.DisplayDialog("缺少资源", "请先指定食物资源定义。", "确定");
                return;
            }
            CreateResidentialHouseArchetype();
        }

        GUILayout.Space(10);

        GUILayout.Label("野生浆果丛 (Wild Berry Bush)", EditorStyles.boldLabel);
        _berrySupply = (SupplyDef)EditorGUILayout.ObjectField("浆果资源定义", _berrySupply, typeof(SupplyDef), false);
        note_BerryBush = EditorGUILayout.TextField("Note", note_BerryBush);
        if (GUILayout.Button("Generate 浆果丛 Archetype"))
        {
            if (_berrySupply == null)
            {
                EditorUtility.DisplayDialog("缺少资源", "请先指定浆果资源定义。", "确定");
                return;
            }
            CreateWildBerryBushArchetype();
        }
    }

    /*
     * id：building_居民房
       名称：居民房
       尺寸：2
       分类：基础
       建筑栏显示条件：
       允许建造条件：

       lv0数据：
       升级经验阈值：3
       最大库存：0
       升级条件：经验达到阈值
       Rules：1.回合结束时获得1exp
       lv1数据：
       基础最大人口：5
       升级经验阈值：10
       最大库存：无库存
       升级条件：经验达到阈值；已解锁id为“tech_历法”的节点
       Rules：
       1.回合结束时消耗{人口数}*1的一级食物满足人口+1，不满足人口-1
       2.若人口=5，回合结束时exp+1
     */

    private void CreateResidentialHouseArchetype()
    {
        // Create ScriptableObject instance
        BuildingArchetype arch = ScriptableObject.CreateInstance<BuildingArchetype>();
        arch.Id = "building_居民房";
        arch.DisplayName = "居民房";
        arch.Size = 2;
        arch.classification = BuildingClassify.基础;  // Basic category

        // Build conditions: always visible/allowed (leave lists empty)
        arch.ShowInBuildPanel = new List<Condition>();      // no conditions = always show
        arch.AllowConstruction = new List<Condition>();     // no conditions = always allow

        // Levels configuration
        arch.Levels = new List<BuildingLevelDef>();

        // Level 0 definition
        BuildingLevelDef lvl0 = new BuildingLevelDef();
        lvl0.ExpToNext = 3;
        lvl0.BaseMaxPopulation = 0;       // no population capacity at level 0
        lvl0.BaseStorageCapacity = 0;
        // No special upgrade conditions (exp threshold handled automatically:contentReference[oaicite:8]{index=8}),
        // so leave lvl0.ConditionsForAllowingUpgrades empty.
        lvl0.ConditionsForAllowingUpgrades = new List<Condition>();
        // Define rules for level 0:
        lvl0.Rules = new List<Rule>();
        // Rule: End of turn grants +1 exp
        Rule expGainRule = new Rule
        {
            Name = "Lv0 EndTurn Exp",
            Description = "回合结束时 +1 经验",
            Trigger = TurnPhase.回合结束阶段  // End of turn phase:contentReference[oaicite:9]{index=9}
        };
        expGainRule.Conditions = new List<Condition>(); // no conditions (always triggers)
        // Effect: Add 1 exp to building
        expGainRule.OnSuccess = new List<Effect> { new AddExp { Amount = 1 } };
        expGainRule.OnFailure = new List<Effect>(); // no failure effect
        lvl0.Rules.Add(expGainRule);

        arch.Levels.Add(lvl0);

        // Level 1 definition
        BuildingLevelDef lvl1 = new BuildingLevelDef();
        lvl1.ExpToNext = 10;
        lvl1.BaseMaxPopulation = 5;
        lvl1.BaseStorageCapacity = 0;
        // Upgrade conditions: require tech "历法" unlocked (in addition to exp).
        lvl1.ConditionsForAllowingUpgrades = new List<Condition>();
        // Create a custom Condition that checks TechTree for unlocked tech:
        Condition techReq = new TechUnlockedCondition { TechId = "tech_历法" };
        lvl1.ConditionsForAllowingUpgrades.Add(techReq);
        // (The game will also inherently require Exp >= ExpToNext for upgrade:contentReference[oaicite:10]{index=10}.)

        // Define rules for level 1:
        lvl1.Rules = new List<Rule>();
        // Rule 1: Population feeding at turn end (or resource consumption phase)
        Rule feedRule = new Rule
        {
            Name = "Feeding",
            Description = "消耗食物供养人口，人口增减",
            Trigger = TurnPhase.资源消耗阶段  // Resource consumption phase:contentReference[oaicite:11]{index=11}
        };
        // Conditions: population >=1 and sufficient food available
        feedRule.Conditions = new List<Condition>();
        feedRule.Conditions.Add(new PopulationAtLeast { Min = 1 });  // need at least 1 population:contentReference[oaicite:12]{index=12}
        // Ensure enough Food supply for the population
        feedRule.Conditions.Add(new HasResourceForPopulation
        {
            Resource = _foodSupply,        // SupplyDef for first-level food (e.g., "食物")
            AmountPerCapita = 1f,
            IgnoreIfPopulationZero = true
        });
        // OnSuccess: consume required food and increase population by 1
        feedRule.OnSuccess = new List<Effect>();
        feedRule.OnSuccess.Add(new ConsumeResourcePerPopulation
        {
            Resource = _foodSupply,
            AmountPerCapita = 1f,
            IgnoreIfPopulationZero = true
        });
        feedRule.OnSuccess.Add(new ChangePopulation { Delta = 1 });  // population +1 on success
        // OnFailure: decrease population by 1
        feedRule.OnFailure = new List<Effect> { new ChangePopulation { Delta = -1 } };
        lvl1.Rules.Add(feedRule);

        // Rule 2: Full population exp bonus at end of turn
        Rule fullPopExpRule = new Rule
        {
            Name = "FullPop Exp Bonus",
            Description = "人口已满时，每回合额外+1经验",
            Trigger = TurnPhase.回合结束阶段  // End of turn phase
        };
        fullPopExpRule.Conditions = new List<Condition>();
        fullPopExpRule.Conditions.Add(new PopulationAtLeast { Min = 5 });  // pop >=5 (max at this level):contentReference[oaicite:13]{index=13}
        fullPopExpRule.OnSuccess = new List<Effect> { new AddExp { Amount = 1 } };
        fullPopExpRule.OnFailure = new List<Effect>();  // no effect if condition not met
        lvl1.Rules.Add(fullPopExpRule);

        arch.Levels.Add(lvl1);

        // Finally, create the asset file
        string assetPath = $"Assets/GameData/Buildings/{arch.Id}.asset";
        AssetDatabase.CreateAsset(arch, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Building archetype asset created: {assetPath}");
        // Optionally, select the new asset in Editor
        Selection.activeObject = arch;
    }



    private void CreateWildBerryBushArchetype()
    {
        BuildingArchetype arch = ScriptableObject.CreateInstance<BuildingArchetype>();
        arch.Id = "Building_野生浆果丛";
        arch.DisplayName = "野生浆果丛";
        arch.Size = 2;
        arch.classification = BuildingClassify.农业类;  // Agriculture category

        // Not constructible by player:
        arch.ShowInBuildPanel = new List<Condition> { new NeverNo() };      // never show in build menu:contentReference[oaicite:23]{index=23}
        arch.AllowConstruction = new List<Condition> { new NeverNo() };     // never allow construction

        // Single level (lv0)
        arch.Levels = new List<BuildingLevelDef>();
        BuildingLevelDef lvl0 = new BuildingLevelDef();
        lvl0.ExpToNext = -1;                // no further upgrades (final level):contentReference[oaicite:24]{index=24}
        lvl0.BaseMaxPopulation = 0;         // not a population-holding building
        lvl0.BaseStorageCapacity = 20;      // can store up to 20 berries
        lvl0.ConditionsForAllowingUpgrades = new List<Condition>();  // none (no upgrade)

        // Rule: Produce berries if not full, each production phase
        lvl0.Rules = new List<Rule>();
        Rule produceRule = new Rule
        {
            Name = "Berry Production",
            Description = "库存未满时，每回合产出2单位浆果",
            Trigger = TurnPhase.资源生产阶段   // Resource production phase:contentReference[oaicite:25]{index=25}
        };
        // Condition: inventory not full (< capacity)
        produceRule.Conditions = new List<Condition>();
        produceRule.Conditions.Add(new InventoryNotFullCondition());
        // OnSuccess: add 2 berry units to self storage
        produceRule.OnSuccess = new List<Effect>();
        SupplyAmount berryItem = new SupplyAmount { Resource = _berrySupply, Amount = 2 };
        produceRule.OnSuccess.Add(new AddToSelfStorage { Items = new[] { berryItem } });
        produceRule.OnFailure = new List<Effect>();  // no failure effect
        lvl0.Rules.Add(produceRule);

        arch.Levels.Add(lvl0);

        // Create asset file
        string assetPath = $"Assets/GameData/Buildings/{arch.Id}.asset";
        AssetDatabase.CreateAsset(arch, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Building archetype asset created: {assetPath}");
        Selection.activeObject = arch;
    }
}



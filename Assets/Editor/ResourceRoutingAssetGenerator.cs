using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResourceRoutingAssetGenerator
{
    private const string SupplyFolder = "Assets/GameData/Supplies";
    private const string BuildingFolder = "Assets/GameData/Buildings";

    [MenuItem("Tools/资源路由/生成默认资源定义")]
    public static void GenerateResourceDefinitions()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            EnsureFolder(SupplyFolder);
            EnsureFolder(BuildingFolder);

            SupplyDef foodSupply = CreateOrUpdateSupply(CombineAssetPath(SupplyFolder, "FoodSupply.asset"), "food", "食物");
            SupplyDef woodSupply = CreateOrUpdateSupply(CombineAssetPath(SupplyFolder, "WoodSupply.asset"), "wood", "木材");

            List<BuildingArchetype> buildingAssets = new List<BuildingArchetype>
            {
                CreateOrUpdateResidence(CombineAssetPath(BuildingFolder, "Residence.asset"), foodSupply),
                CreateOrUpdatePoliceStation(CombineAssetPath(BuildingFolder, "PoliceStation.asset")),
                CreateOrUpdateHospital(CombineAssetPath(BuildingFolder, "Hospital.asset")),
                CreateOrUpdateWarehouse(CombineAssetPath(BuildingFolder, "Warehouse.asset")),
                CreateOrUpdateLumberCamp(CombineAssetPath(BuildingFolder, "LumberCamp.asset"), woodSupply)
            };

            AssignToResourceRouting(foodSupply, woodSupply, buildingAssets);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string CombineAssetPath(string folder, string fileName)
    {
        if (string.IsNullOrEmpty(folder))
        {
            return fileName.Replace("\\", "/");
        }

        return $"{folder.TrimEnd('/')}/{fileName}".Replace("\\", "/");
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        return assetPath.Replace("\\", "/");
    }


    private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        assetPath = NormalizeAssetPath(assetPath);

        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.hideFlags = HideFlags.None;
        return asset;
    }

    private static SupplyDef CreateOrUpdateSupply(string assetPath, string id, string displayName)
    {
        SupplyDef supply = LoadOrCreateAsset<SupplyDef>(assetPath);
        supply.Id = id;
        supply.DisplayName = displayName;
        EditorUtility.SetDirty(supply);
        return supply;
    }

    private static BuildingArchetype CreateOrUpdateResidence(string assetPath, SupplyDef foodSupply)
    {
        BuildingArchetype archetype = LoadOrCreateAsset<BuildingArchetype>(assetPath);
        archetype.Id = "residence";
        archetype.DisplayName = "居民房";
        archetype.Size = 2;
        archetype.classification = BuildingClassify.基础;

        BuildingLevelDef level0 = new BuildingLevelDef
        {
            Level = 0,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = 2,
            BaseMaxJobs = 0,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level0.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level1 = new BuildingLevelDef
        {
            Level = 1,
            BaseMaxPopulation = 5,
            BaseStorageCapacity = 0,
            ExpToNext = 10,
            BaseMaxJobs = 0,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level1.Rules.Add(new Rule
        {
            Trigger = TurnPhase.资源消耗阶段,
            Conditions = new List<Condition>
            {
                new PopulationAtLeast { Min = 1 },
                new HasResourceForPopulation { Resource = foodSupply, AmountPerCapita = 1f, IgnoreIfPopulationZero = true }
            },
            OnSuccess = new List<Effect>
            {
                new ConsumeResourcePerPopulation { Resource = foodSupply, AmountPerCapita = 1f, IgnoreIfPopulationZero = true },
                new ChangePopulation { Delta = 2 }
            },
            OnFailure = new List<Effect>
            {
                new ChangePopulation { Delta = -2 }
            }
        });
        level1.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition> { new PopulationAtLeast { Min = 5 } },
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level2 = new BuildingLevelDef
        {
            Level = 2,
            BaseMaxPopulation = 10,
            BaseStorageCapacity = 0,
            ExpToNext = 20,
            BaseMaxJobs = 0,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level2.ConditionalStatModifiers.Add(new EnvironmentBonus
        {
            Bonus = 5,
            Requirements = new[]
            {
                new AuraRequirement { Category = AuraCategory.Beauty, MinValue = 1 },
                new AuraRequirement { Category = AuraCategory.Security, MinValue = 1 }
            }
        });
        level2.Rules.Add(new Rule
        {
            Trigger = TurnPhase.资源消耗阶段,
            Conditions = new List<Condition>
            {
                new PopulationAtLeast { Min = 1 },
                new HasResourceForPopulation { Resource = foodSupply, AmountPerCapita = 1f, IgnoreIfPopulationZero = true }
            },
            OnSuccess = new List<Effect>
            {
                new ConsumeResourcePerPopulation { Resource = foodSupply, AmountPerCapita = 1f, IgnoreIfPopulationZero = true },
                new ChangePopulation { Delta = 2 }
            },
            OnFailure = new List<Effect>
            {
                new ChangePopulation { Delta = -2 }
            }
        });
        level2.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition> { new PopulationAtLeast { Min = 10 } },
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level3 = new BuildingLevelDef
        {
            Level = 3,
            BaseMaxPopulation = 20,
            BaseStorageCapacity = 0,
            ExpToNext = -1,
            BaseMaxJobs = 0,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level3.ConditionalStatModifiers.Add(new EnvironmentBonus
        {
            Bonus = 10,
            Requirements = new[]
            {
                new AuraRequirement { Category = AuraCategory.Beauty, MinValue = 2 },
                new AuraRequirement { Category = AuraCategory.Security, MinValue = 2 },
                new AuraRequirement { Category = AuraCategory.Health, MinValue = 2 }
            }
        });
        level3.Rules.Add(new Rule
        {
            Trigger = TurnPhase.资源消耗阶段,
            Conditions = new List<Condition>
            {
                new PopulationAtLeast { Min = 1 },
                new HasResourceForPopulation { Resource = foodSupply, AmountPerCapita = 1f, IgnoreIfPopulationZero = true }
            },
            OnSuccess = new List<Effect>
            {
                new ConsumeResourcePerPopulation { Resource = foodSupply, AmountPerCapita = 1f, IgnoreIfPopulationZero = true },
                new ChangePopulation { Delta = 2 }
            },
            OnFailure = new List<Effect>
            {
                new ChangePopulation { Delta = -2 }
            }
        });
        level3.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition> { new PopulationAtLeast { Min = 15 } },
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        archetype.Levels = new List<BuildingLevelDef> { level0, level1, level2, level3 };
        EditorUtility.SetDirty(archetype);
        return archetype;
    }

    private static BuildingArchetype CreateOrUpdatePoliceStation(string assetPath)
    {
        BuildingArchetype archetype = LoadOrCreateAsset<BuildingArchetype>(assetPath);
        archetype.Id = "police_station";
        archetype.DisplayName = "警察局";
        archetype.Size = 2;
        archetype.classification = BuildingClassify.基础;

        BuildingLevelDef level1 = new BuildingLevelDef
        {
            Level = 1,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = 6,
            BaseMaxJobs = 4,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level1.Rules.Add(new Rule
        {
            Trigger = TurnPhase.结束准备阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new ApplyEnvironmentAura { Category = AuraCategory.Security, Rings = new[] { new AuraRing { Radius = 8, Value = 1 } } } },
            OnFailure = new List<Effect>()
        });
        level1.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level2 = new BuildingLevelDef
        {
            Level = 2,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = 12,
            BaseMaxJobs = 6,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level2.Rules.Add(new Rule
        {
            Trigger = TurnPhase.结束准备阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new ApplyEnvironmentAura { Category = AuraCategory.Security, Rings = new[] { new AuraRing { Radius = 9, Value = 2 }, new AuraRing { Radius = 15, Value = 1 } } } },
            OnFailure = new List<Effect>()
        });
        level2.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level3 = new BuildingLevelDef
        {
            Level = 3,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = -1,
            BaseMaxJobs = 8,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level3.Rules.Add(new Rule
        {
            Trigger = TurnPhase.结束准备阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new ApplyEnvironmentAura { Category = AuraCategory.Security, Rings = new[] { new AuraRing { Radius = 10, Value = 2 }, new AuraRing { Radius = 18, Value = 2 }, new AuraRing { Radius = 25, Value = 1 } } } },
            OnFailure = new List<Effect>()
        });
        level3.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        archetype.Levels = new List<BuildingLevelDef> { level1, level2, level3 };
        EditorUtility.SetDirty(archetype);
        return archetype;
    }

    private static BuildingArchetype CreateOrUpdateHospital(string assetPath)
    {
        BuildingArchetype archetype = LoadOrCreateAsset<BuildingArchetype>(assetPath);
        archetype.Id = "hospital";
        archetype.DisplayName = "医院";
        archetype.Size = 3;
        archetype.classification = BuildingClassify.基础;

        BuildingLevelDef level1 = new BuildingLevelDef
        {
            Level = 1,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = 8,
            BaseMaxJobs = 4,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level1.Rules.Add(new Rule
        {
            Trigger = TurnPhase.结束准备阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new ApplyEnvironmentAura { Category = AuraCategory.Health, Rings = new[] { new AuraRing { Radius = 8, Value = 2 } } } },
            OnFailure = new List<Effect>()
        });
        level1.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level2 = new BuildingLevelDef
        {
            Level = 2,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = 12,
            BaseMaxJobs = 6,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level2.Rules.Add(new Rule
        {
            Trigger = TurnPhase.结束准备阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new ApplyEnvironmentAura { Category = AuraCategory.Health, Rings = new[] { new AuraRing { Radius = 10, Value = 2 }, new AuraRing { Radius = 14, Value = 1 } } } },
            OnFailure = new List<Effect>()
        });
        level2.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        BuildingLevelDef level3 = new BuildingLevelDef
        {
            Level = 3,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 0,
            ExpToNext = -1,
            BaseMaxJobs = 10,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level3.Rules.Add(new Rule
        {
            Trigger = TurnPhase.结束准备阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new ApplyEnvironmentAura { Category = AuraCategory.Health, Rings = new[] { new AuraRing { Radius = 14, Value = 3 }, new AuraRing { Radius = 20, Value = 2 }, new AuraRing { Radius = 35, Value = 1 } } } },
            OnFailure = new List<Effect>()
        });
        level3.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
            OnFailure = new List<Effect>()
        });

        archetype.Levels = new List<BuildingLevelDef> { level1, level2, level3 };
        EditorUtility.SetDirty(archetype);
        return archetype;
    }

    private static BuildingArchetype CreateOrUpdateWarehouse(string assetPath)
    {
        BuildingArchetype archetype = LoadOrCreateAsset<BuildingArchetype>(assetPath);
        archetype.Id = "warehouse";
        archetype.DisplayName = "仓库";
        archetype.Size = 3;
        archetype.classification = BuildingClassify.基础;

        int[] capacities = { 60, 100, 200, 300, 400, 500 };
        int[] expRequirements = { 2, 5, 5, 5, 5, -1 };

        List<BuildingLevelDef> levels = new List<BuildingLevelDef>();
        for (int i = 0; i < capacities.Length; i++)
        {
            BuildingLevelDef level = new BuildingLevelDef
            {
                Level = i,
                BaseMaxPopulation = 0,
                BaseStorageCapacity = capacities[i],
                ExpToNext = expRequirements[i],
                BaseMaxJobs = 5,
                ConditionalStatModifiers = new List<StatModifier>(),
                Rules = new List<Rule>()
            };

            level.Rules.Add(new Rule
            {
                Trigger = TurnPhase.结束准备阶段,
                Conditions = new List<Condition>(),
                OnSuccess = new List<Effect> { new AdjustStorageCapacityWithWorkers() },
                OnFailure = new List<Effect>()
            });
            level.Rules.Add(new Rule
            {
                Trigger = TurnPhase.回合结束阶段,
                Conditions = new List<Condition>(),
                OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
                OnFailure = new List<Effect>()
            });
            level.Rules.Add(new Rule
            {
                Trigger = TurnPhase.回合结束阶段,
                Conditions = new List<Condition> { new FillPercentOver { Threshold = 0.4f } },
                OnSuccess = new List<Effect> { new AddExp { Amount = 1 } },
                OnFailure = new List<Effect>()
            });

            levels.Add(level);
        }

        archetype.Levels = levels;
        EditorUtility.SetDirty(archetype);
        return archetype;
    }

    private static BuildingArchetype CreateOrUpdateLumberCamp(string assetPath, SupplyDef woodSupply)
    {
        BuildingArchetype archetype = LoadOrCreateAsset<BuildingArchetype>(assetPath);
        archetype.Id = "lumber_camp";
        archetype.DisplayName = "伐木场";
        archetype.Size = 3;
        archetype.classification = BuildingClassify.工业类;

        BuildingLevelDef level = new BuildingLevelDef
        {
            Level = 1,
            BaseMaxPopulation = 0,
            BaseStorageCapacity = 80,
            ExpToNext = -1,
            BaseMaxJobs = 4,
            ConditionalStatModifiers = new List<StatModifier>(),
            Rules = new List<Rule>()
        };
        level.Rules.Add(new Rule
        {
            Trigger = TurnPhase.资源生产阶段,
            Conditions = new List<Condition> { new WorkersAtLeast { Min = 1 } },
            OnSuccess = new List<Effect> { new AddResourcePerWorker { Resource = woodSupply, AmountPerWorker = 2, ToAssignedStorage = false } },
            OnFailure = new List<Effect>()
        });
        level.Rules.Add(new Rule
        {
            Trigger = TurnPhase.回合结束阶段,
            Conditions = new List<Condition>(),
            OnSuccess = new List<Effect> { new TransferSelfToAssigned { Resource = woodSupply, Amount = 10 } },
            OnFailure = new List<Effect>()
        });

        archetype.Levels = new List<BuildingLevelDef> { level };
        EditorUtility.SetDirty(archetype);
        return archetype;
    }

    private static void AssignToResourceRouting(SupplyDef foodSupply, SupplyDef woodSupply, List<BuildingArchetype> definitions)
    {
        ResourceRouting[] routings = Object.FindObjectsOfType<ResourceRouting>(true);
        if (routings == null || routings.Length == 0)
        {
            Debug.LogWarning("未在场景中找到 ResourceRouting 实例。已生成资产，请手动拖拽到组件。");
            return;
        }

        foreach (ResourceRouting routing in routings)
        {
            SerializedObject serializedRouting = new SerializedObject(routing);
            serializedRouting.FindProperty("foodSupply").objectReferenceValue = foodSupply;
            serializedRouting.FindProperty("woodSupply").objectReferenceValue = woodSupply;

            SerializedProperty listProperty = serializedRouting.FindProperty("buildingDefinitions");
            listProperty.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
            {
                listProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            serializedRouting.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(routing);

            if (routing.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(routing.gameObject.scene);
            }
        }
    }
}

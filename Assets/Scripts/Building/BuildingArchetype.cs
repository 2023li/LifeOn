using UnityEngine;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;


public enum BuildingClassify
{
    基础,
    市政,
    工业类,
    农业类,
}


[CreateAssetMenu(fileName = "BuildingArchetype", menuName = "Game/BuildingInstance/Archetype")]
public class BuildingArchetype : ScriptableObject
{
    public string Id;                // "residence", "warehouse", "garden"
    public string DisplayName;       // "居民房"
    public int Size;
    public BuildingInstance BuildingPrefab;    // Addressables/Prefab
    public List<BuildingLevelDef> Levels = new List<BuildingLevelDef>();
    public BuildingClassify classification = BuildingClassify.基础;
    [HideInInspector] public BuildingArchetypeGraph GraphAsset;
}

[Serializable]
public class BuildingLevelDef
{
     
    [Min(0)] public int Level = 0;

    // —— 基础属性（根据建筑不同使用其子集）——
    [LabelText("基础最大人口")]
    public int BaseMaxPopulation;   // 人口上限基础值（居民类）




    [LabelText("仓库容量")]
    public int BaseStorageCapacity;     // 仓库容量（仓库类）



    [LabelText("升级所需经验")]
    public int ExpToNext = -1;      // 升级需要经验；-1 表示最高级



    [LabelText("基础最大岗位")]
    [Min(0)] public int BaseMaxJobs;



    [SerializeReference] public List<StatModifier> ConditionalStatModifiers = new();



    // 规则：回合末拉取资源、人口增减、经验与升级等
    [SerializeReference] public List<Rule> Rules = new();

    [LabelText("等级表现配置")]
    public BuildingLevelViewConfig ViewConfig = new();

}

[Serializable]
public class BuildingLevelViewConfig
{
    [LabelText("默认动画状态")]
    [Tooltip("调用 ApplyLevelState 时直接播放的 Animator 状态名，用于保持该等级的常态表现。")]
    public string DefaultAnimatorState;

    [LabelText("默认动画触发器")]
    [Tooltip("调用 ApplyLevelState 时触发的 Animator Trigger，常用于恢复待机动画。留空表示不触发。")]
    public string DefaultAnimatorTrigger;

    [LabelText("升级动画触发器")]
    [Tooltip("升级瞬间触发的 Animator Trigger，PlayUpgrade 会在 from -> to 时调用。留空表示无需额外触发。")]
    public string UpgradeTrigger;

    [LabelText("常驻粒子预制体")]
    [Tooltip("该等级常驻的粒子或特效预制体，会被实例化到 BuildingView 的粒子父节点下。留空表示不显示。")]
    public GameObject PersistentParticlePrefab;

    [LabelText("升级特效预制体")]
    [Tooltip("升级瞬间播放的一次性特效，PlayUpgrade 会负责实例化。留空表示不播放升级特效。")]
    public GameObject UpgradeEffectPrefab;

    [LabelText("子级预制体替换")]
    [Tooltip("用于切换子模型或装饰的预制体，ApplyLevelState 会销毁旧实例并重新生成。留空则沿用上一等级的外观。")]
    public GameObject ChildPrefab;
}



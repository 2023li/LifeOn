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
    public GameObject ViewPrefab;    // Addressables/Prefab
    public List<BuildingLevelDef> Levels = new List<BuildingLevelDef>();
    public BuildingClassify classification = BuildingClassify.基础;

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
}

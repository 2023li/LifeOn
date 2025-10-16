using UnityEngine;
using System.Collections.Generic;
using System;


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
    public int BuildTimeTurns = 1;   // 施工回合数
    public List<BuildingLevelDef> Levels = new List<BuildingLevelDef>();
    public BuildingClassify classification = BuildingClassify.基础;

}

[Serializable]
public class BuildingLevelDef
{
    [Min(1)] public int Level = 1;

    // —— 基础属性（根据建筑不同使用其子集）——
    public int BaseMaxPopulation;   // 人口上限基础值（居民类）
    public int StorageCapacity;     // 仓库容量（仓库类）
    public int ExpToNext = -1;      // 升级需要经验；-1 表示最高级

    // 条件化属性修饰：例如“环境>1则人口上限+3”
    [SerializeReference] public List<StatModifier> ConditionalStatModifiers = new();

    // 规则：回合末拉取资源、人口增减、经验与升级等
    [SerializeReference] public List<Rule> Rules = new();
}

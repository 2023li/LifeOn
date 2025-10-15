using System;
using Sirenix.OdinInspector;
using UnityEngine;

/*
 * 1.需要每级一个吗？ 需要
 */

[CreateAssetMenu(fileName = "BD_建筑_LV", menuName = "LO/Building Def")]
public class BuildingDef: ScriptableObject
{
    [BoxGroup("基础")]
    [LabelText("建筑唯一ID")]
    public string BuildingOnlyID;

    [BoxGroup("基础")]
    [LabelText("建筑名称")]
    public string BuildingName;

    [BoxGroup("基础")]
    [LabelText("建筑体")]
    [AssetsOnly]
    public Building BuildingPrefab;


    [BoxGroup("基础")]
    [LabelText("分类")]
    public BuildingClassify classification = BuildingClassify.基础;


    [BoxGroup("基础")]
    [LabelText("建筑尺寸")]
    public byte Size = 2;

    [BoxGroup("基础")]
    [LabelText("建筑信息UI预制体")]
    public BuildingInfoPanelBase BuildingInfoPanel;

    [BoxGroup("基础")]
    [LabelText("数量限制")]
    public int QuantityLimit = 999;




    //---------------------------------------人口-------------------------------------------
    [FoldoutGroup("提供人口")]
    [LabelText("提供人口")]
    public bool ProvidePopulation=false;
    [FoldoutGroup("提供人口")]
    [LabelText("基础最大人口")]
    [ShowIf(nameof(ProvidePopulation), true)]
    public int BaseMaxPopulation;


    [FoldoutGroup("提供人口")]
    [LabelText("基础工作人口比例")]
    [ShowIf(nameof(ProvidePopulation), true)]
    public float BaseProportionWorkingPopulation = 0.7f;


    //---------------------------------------库存-------------------------------------------
    [FoldoutGroup("库存")]
    public bool ProvideInventory = false;

    [FoldoutGroup("库存")]
    [LabelText("库存容量")]
    [ShowIf(nameof(ProvideInventory), true)]
    public int Inventory_Capacity = 100;




    //---------------------------------------供需-------------------------------------------
    [FoldoutGroup("供需")]
    public bool ProvideSupplyAndDemand = false;

    [FoldoutGroup("供需")]
    [ShowIf(nameof(ProvideSupplyAndDemand), true)]
    [LabelText("取料距离")]
    public int BaseMaterialFetchingRadius = 5;
    [FoldoutGroup("供需")]
    [ShowIf(nameof(ProvideSupplyAndDemand), true)]
    public SupplyChange[] SupplyStacks;


    //---------------------------------------就业-------------------------------------------
    [FoldoutGroup("就业岗位")]
    [LabelText("提供就业")]
    public bool ProvideEmployment = false;

    [FoldoutGroup("就业岗位")]
    [LabelText("最大岗位")]
    [ShowIf(nameof(ProvideEmployment), true)]
    public short MaxEmployment;

    [FoldoutGroup("就业岗位")]
    [LabelText("基础薪资")]
    [ShowIf(nameof(ProvideEmployment), true)]
    public int BasicSalary;


    //---------------------------------------光环-------------------------------------------

    [FoldoutGroup("光环")]
    [LabelText("光环效果")]
    public bool ProvideHaloEffect = false;

    [FoldoutGroup("光环")]
    [LabelText("影响数值")]
    [ShowIf(nameof(ProvideHaloEffect),true)]
    public HaloEffectRangeValue[] HaloEffectValues;


    //---------------------------------------升级-------------------------------------------
    [FoldoutGroup("升级")]
    [LabelText("可升级的")]
    public bool ProvideUpgradable = false;


    [FoldoutGroup("升级")]
    [ShowIf(nameof(ProvideUpgradable), true)]
    [LabelText("经验获取条件")]
    public EXPConditionValue Upgradable_EXPCondition;

    [FoldoutGroup("升级")]
    [ShowIf(nameof(ProvideUpgradable), true)]
    [LabelText("升级需要的经验")]
    public int NeedEXP = 10;

    [FoldoutGroup("升级")]
    [ShowIf(nameof(ProvideUpgradable), true)]
    [LabelText("升级后的建筑Def")]
    public BuildingDef NextLevel;

  



}





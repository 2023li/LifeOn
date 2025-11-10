using UnityEngine;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;


[Flags]
public enum WarehouseProperties
{
    物资,
    粮食,

}

public enum BuildingClassify
{
    基础,
    市政,
    工业类,
    农业类,

    其他,
}


[CreateAssetMenu(fileName = "BuildingArchetype", menuName = "Game/BuildingInstance/Archetype")]
public class BuildingArchetype : ScriptableObject
{
    [LabelText("建筑唯一ID")]
    public string Id;                // "residence", "warehouse", "garden"
    [LabelText("建筑名称")]
    public string DisplayName;       // "居民房"
    [LabelText("建筑尺寸")]
    public int Size;
    [LabelText("建筑分类")]
    public BuildingClassify classification = BuildingClassify.基础;



    [AssetsOnly,LabelText("建筑预制体")]
    public BuildingInstance BuildingPrefab;

    [LabelText("建筑图标")]
    public Sprite BuildingIcon;




    [AssetsOnly,LabelText("建筑信息UI预制体(简短)")]
    public BuildingBriefPanelBase UIPanelPrefab_Brief;

    [AssetsOnly, LabelText("建筑信息UI预制体(详情)")]
    public BuildingDetailedPanelBase UIPanelPrefab_Detailed;



    [LabelText("简介"), MultiLineProperty(3)]
    public string Introduction;


    //是否在建造面板显示
    [LabelText("显示在建造面板的条件")]
    [SerializeReference] 
    [Tooltip("留空则始终显示")]
    public List<Condition> ShowInBuildPanel;
    //允许建造
    [LabelText("允许建造的条件")]
    [SerializeReference] 
    [Tooltip("留空则始终允许建造")]
    public List<Condition> AllowConstruction;

    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public List<BuildingLevelDef> Levels = new List<BuildingLevelDef>();
   

}

[Serializable]
public class BuildingLevelDef
{
    // —— 基础属性（根据建筑不同使用其子集）——
    [SerializeField,HorizontalGroup("人口"),LabelText("基础最大人口")]
    private int BaseMaxPopulation;   // 人口上限基础值（居民类）
    [SerializeField, HorizontalGroup("人口"), LabelText("启用修饰")]
    private bool EnableStatModifier_MaxPopulation = false;

    [SerializeReference,ShowIf(nameof(EnableStatModifier_MaxPopulation)),LabelText("人口修饰")]
    private List<StatModifier> SM_Population;
    public int GetMaxPopulation(BuildingInstance self)
    {
        int value = BaseMaxPopulation;

        if (EnableStatModifier_MaxPopulation && SM_Population != null)
        {
            for (int i = 0; i < SM_Population.Count; i++)
            {
                var modifier = SM_Population[i];
                if (modifier != null)
                {
                    value = modifier.Modify(self, self.Ctx, value);
                }
            }
        }

        // 最大人口不允许为负，做一下下限保护
        return Mathf.Max(0, value);
    }



    [SerializeField,HorizontalGroup("库存容量"),LabelText("仓库容量"),Tooltip("提供的全局容量增量")]
    private int BaseStorageCapacity;     // 仓库容量（仓库类）
    [SerializeField, HorizontalGroup("库存容量"), LabelText("启用修饰")]
    private bool EnableStatModifier_StorageCapacity = false;
    [SerializeReference, ShowIf(nameof(EnableStatModifier_StorageCapacity)), LabelText("容量修饰")]
    private List<StatModifier> SM_StorageCapacity;
    public int GetStorageCapacity(BuildingInstance self)
    {
        int value = BaseStorageCapacity;

        if (EnableStatModifier_StorageCapacity && SM_StorageCapacity != null)
        {
            for (int i = 0; i < SM_StorageCapacity.Count; i++)
            {
                var modifier = SM_StorageCapacity[i];
                if (modifier != null)
                {
                    value = modifier.Modify(self,self.Ctx, value);
                }
            }
        }

        // 仓库容量同样不允许为负
        return Mathf.Max(0, value);
    }


    [LabelText("升级所需经验"),HorizontalGroup("EXP")]
    private int ExpToNext = -1;      // 升级需要经验；-1 表示最高级
    [LabelText("启用修饰"),HorizontalGroup("EXP")]
    private bool EnableStatModifier_ExpToNext = false;
    [SerializeReference, ShowIf(nameof(EnableStatModifier_ExpToNext)), LabelText("升级经验修饰")]
    private List<StatModifier> SM_ExpToNext;
    /// <summary>
    /// 获取应用修饰后的升级所需经验。
    /// 若基础值为 -1，视为已满级，不再应用修饰，直接返回 -1。
    /// </summary>
    public int GetExpToNext(BuildingInstance self)
    {
        if (ExpToNext < 0)
        {
            return -1;
        }

        int value = ExpToNext;

        if (EnableStatModifier_ExpToNext && SM_ExpToNext != null)
        {
            for (int i = 0; i < SM_ExpToNext.Count; i++)
            {
                var modifier = SM_ExpToNext[i];
                if (modifier != null)
                {
                    value = modifier.Modify(self, self.Ctx, value);
                }
            }
        }

        // 升级经验至少为 1，避免被减成 0 或负数导致升级逻辑异常
        return Mathf.Max(1, value);
    }




    [SerializeField, LabelText("基础最大岗位"), HorizontalGroup("岗位")]
    private int BaseMaxJobs;

    [SerializeField, LabelText("启用修饰"), HorizontalGroup("岗位")]
    private bool EnableStatModifier_MaxJobs = false;
    [SerializeReference, ShowIf(nameof(EnableStatModifier_MaxJobs)), LabelText("岗位数量修饰")]
    private List<StatModifier> SM_MaxJobs;
    /// <summary>
    /// 获取应用修饰后的最大岗位数。
    /// </summary>
    public int GetMaxJobs(BuildingInstance self)
    {
        int value = BaseMaxJobs;

        if (EnableStatModifier_MaxJobs && SM_MaxJobs != null)
        {
            for (int i = 0; i < SM_MaxJobs.Count; i++)
            {
                var modifier = SM_MaxJobs[i];
                if (modifier != null)
                {
                    value = modifier.Modify(self, self.Ctx, value);
                }
            }
        }

        // 岗位数量至少为 0
        return Mathf.Max(0, value);
    }



    [SerializeReference,HorizontalGroup("转运能力"),LabelText("物资转运能力")]
    private Condition TransportationCondition;
    [LabelText("转运代价"), HorizontalGroup("转运能力"),ShowIf("@TransportationCondition != null")]
    private int BaseTransportationResistance = 3;
    [LabelText("启用修饰"), HorizontalGroup("转运能力"), ShowIf("@TransportationCondition != null")]
    private bool EnableStatModifier_TransportationResistance = false;
    [LabelText("转运代价修饰"), ShowIf(nameof(EnableStatModifier_TransportationResistance)),SerializeReference]
    private List<StatModifier> SM_TransportationResistance;

    //转运阻力
    public int GetTransportationResistance(BuildingInstance self)
    {
        if (BaseTransportationResistance < 1)
        {
           
            return 1;
        }

        int value = BaseTransportationResistance;

        if (EnableStatModifier_TransportationResistance && SM_TransportationResistance != null)
        {
            for (int i = 0; i < SM_TransportationResistance.Count; i++)
            {
                var modifier = SM_TransportationResistance[i];
                if (modifier != null)
                {
                    value = modifier.Modify(self, self.Ctx, value);
                }
            }
        }

        // 升级经验至少为 1，避免被减成 0 或负数导致升级逻辑异常
        return Mathf.Max(1, value);
    }

    //转运能力
    public bool TransportationCapacity(BuildingInstance self)
    {
        bool b =  TransportationCondition.Evaluate(self,self.Ctx,out string why);
        if (!b&&!string.IsNullOrEmpty(why))
        {
            Debug.LogWarning(why, self.gameObject);
        }
        return b;

    }





  



    [LabelText("基础岗位吸引力")]
    public float BaseAttractivenessPerJob = 0f;


    [LabelText("等级表现配置")]
    public BuildingLevelViewConfig ViewConfig = new();


    [LabelText("允许升级的条件")]
    [SerializeReference]
    public List<Condition> ConditionsForAllowingUpgrades = new List<Condition>
    {
        //这里需要增加一个默认的条件：建筑经验值大于ExpToNext
    };


    // 规则：回合末拉取资源、人口增减、经验与升级等
    [LabelText("规则")]
     [ListDrawerSettings(
        ShowFoldout = true,
        ShowIndexLabels = true,
        DraggableItems = true,
        ListElementLabelName = nameof(Rule.ElementLabel))]
    [SerializeReference, HideReferenceObjectPicker]
    public List<Rule> Rules = new();

  

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

/*
 * 建筑实例类中的RO_系列属性可以看作某种意义上的 静态属性 
 */


using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public enum BuildingStateValueType
{
    LevelIndex,
    CurrentExp,
    ExpToNext,
    MaxPopulation,
    CurrentPopulation,
    CurrentWorkers,
    MaxStorageCapacity,
    TransportationAbility,
    TransportationResistance,
    就业吸引力,
    产品列表,
    转运流量,
}


[Serializable]
public class BuildingStatModifiers
{
    // 升级所需经验
    public int Base_ExpToNextAdd = 0;
    public float Base_ExpToNextMul = 1f;
    public int Bonus_ExpToNextAdd = 0;
    public float Bonus_ExpToNextMul = 1f;
    public float Final_ExpToNextMul = 1f;

    // 最大人口数
    public int Base_MaxPopulationAdd = 0;
    public float Base_MaxPopulationMul = 1f;
    public int Bonus_MaxPopulationAdd = 0;
    public float Bonus_MaxPopulationMul = 1f;
    public float Final_MaxPopulationMul = 1f;


    // 最大工作岗位数（对应 MaxJobsPosition）
    public int Base_MaxJobsPositionAdd = 0;
    public float Base_MaxJobsPositionMul = 1f;
    public int Bonus_MaxJobsPositionAdd = 0;
    public float Bonus_MaxJobsPositionMul = 1f;
    public float Final_MaxJobsPositionMul = 1f;


    // 最大库存数
    public int Base_StorageCapacityAdd = 0;
    public float Base_StorageCapacityMul = 1f;
    public int Bonus_StorageCapacityAdd = 0;
    public float Bonus_StorageCapacityMul = 1f;
    public float Final_StorageCapacityMul = 1f;

    // 转运疲劳值
    public int Base_TransportationResistanceAdd = 0;
    public float Base_TransportationResistanceMul = 1f;
    public int Bonus_TransportationResistanceAdd = 0;
    public float Bonus_TransportationResistanceMul = 1f;
    public float Final_TransportationResistanceMul = 1f;

    // 转运范围（原 TransportStrength 改为 Radius，补全结构）
    public int Base_TransportRadiusAdd = 0;
    public float Base_TransportRadiusMul = 1f;
    public int Bonus_TransportRadiusAdd = 0;
    public float Bonus_TransportRadiusMul = 1f;
    public float Final_TransportRadiusMul = 1f;

    // 分发范围（原 DistributeStrength 改为 Radius，补全结构）
    public int Base_DistributeRadiusAdd = 0;
    public float Base_DistributeRadiusMul = 1f;
    public int Bonus_DistributeRadiusAdd = 0;
    public float Bonus_DistributeRadiusMul = 1f;
    public float Final_DistributeRadiusMul = 1f;

    // 转运流量 MaxTraffic（遵循统一属性结构：基础增减+基础倍率+额外增减+额外倍率+最终倍率）
    public int Base_MaxTrafficAdd = 0;          // 基础转运流量增减（固定值）
    public float Base_MaxTrafficMul = 1f;      // 基础转运流量倍率（乘法系数）
    public int Bonus_MaxTrafficAdd = 0;        // 额外转运流量增减（奖励/buff 叠加值）
    public float Bonus_MaxTrafficMul = 1f;     // 额外转运流量倍率（奖励/buff 叠加系数）
    public float Final_MaxTrafficMul = 1f;     // 最终转运流量总倍率（汇总所有倍率后的值）

    // 工作吸引力
    public int Base_JobAttractivenessAdd = 0;
    public float Base_JobAttractivenessMul = 1f;
    public int Bonus_JobAttractivenessAdd = 0;
    public float Bonus_JobAttractivenessMul = 1f;
    public float Final_JobAttractivenessMul = 1f;
}






public class BuildingInstance : MonoBehaviour
{
    #region 静态

    private static readonly HashSet<BuildingInstance> _activeInstances = new();

    public static IReadOnlyCollection<BuildingInstance> ActiveInstances => _activeInstances;

    private static Dictionary<Vector3Int, BuildingInstance> _occupyMap = new Dictionary<Vector3Int, BuildingInstance>();
    // 优化后的 TryGetAtCell，复杂度从 O(N) 降为 O(1)
    public static bool TryGetAtCell(Vector3Int cell, out BuildingInstance inst)
    {
        return _occupyMap.TryGetValue(cell, out inst);
    }

    #endregion




    public event Action<BuildingInstance, BuildingStateValueType> OnStateChanged;




    #region 基础 & 状态字段 + 属性


    //----------------------------基础信息-----------------------------------


    [LabelText("实例ID"), ShowInInspector, ReadOnly]
    public string InstanceId { get; private set; } = Guid.NewGuid().ToString("N");

    [ReadOnly, ShowInInspector,LabelText("建筑定义数据")]
    public BuildingArchetype Def { get; set; }
    public string DisplayName =>Def==null? "未知数据" : Def.DisplayName;


    //----------------------------等级-----------------------------------


    [Header("等级"), LabelText("当前等级索引"), ShowInInspector, ReadOnly]
    private int _currentLevelIndex;
    public int CurrentLevelIndex
    {
        get => _currentLevelIndex;
        private set
        {
            _currentLevelIndex = value;
            OnStateChanged?.Invoke(this, BuildingStateValueType.LevelIndex);
        }
    }

    [ShowInInspector, ReadOnly, LabelText("当前经验")]
    private int _currentExp;
    public int CurrentExp
    {
        get => _currentExp;
        set
        {
            _currentExp = value;
        }
    }

    [ShowInInspector, ReadOnly, LabelText("升级所需经验(运行时)")]
    public int RO_ExpToNext
    {
        get
        {
            if (GetLevelData()==null)
            {
                return 0; 
            }
            //计算基础值的修正
            float fBase = (GetLevelData().ExpToNext + statModifiers.Base_ExpToNextAdd) * statModifiers.Base_ExpToNextMul;
            float fBonus = statModifiers.Bonus_ExpToNextMul * statModifiers.Bonus_ExpToNextAdd;
            float f = (fBase + fBonus) * statModifiers.Final_ExpToNextMul;
            return (int)f;
        }

    }


    //----------------------------人口 & 就业-----------------------------------
    [ShowInInspector, ReadOnly, LabelText("运行时最大人口")]
    public int RO_MaxPopulation
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0;
            }

            //计算基础值的修正
            float fBase = (GetLevelData().BaseMaxPopulation + statModifiers.Base_MaxPopulationAdd) * statModifiers.Base_MaxPopulationMul;
            float fBonus = statModifiers.Bonus_MaxPopulationAdd * statModifiers.Bonus_MaxPopulationMul;
            float f = (fBase + fBonus) * statModifiers.Final_MaxPopulationMul;
            return (int)f;
        }

    }
    [ShowInInspector, ReadOnly, LabelText("当前人口")]
    private int _currentPopulation;
    public int CurrentPopulation
    {
        get => _currentPopulation;
        set
        {
            // 1. 计算有效最大值（避免 RO_MaxPopulation 为负数的异常情况）
            int maxValid = Math.Max(RO_MaxPopulation, 0);
            // 2. 钳位 newValue：确保在 [0, maxValid] 范围内（不超上限、不小于0）
            int newValue = Math.Clamp(value, 0, maxValid);
            // 3. 只有值真的变化时，才赋值并触发事件（避免无效调用）
            if (_currentPopulation != newValue)
            {
                _currentPopulation = newValue;
                OnStateChanged?.Invoke(this, BuildingStateValueType.CurrentPopulation);
            }

        }
    }
    [ShowInInspector, ReadOnly, LabelText("当前工人")]
    private int _currentWorkers;
    public int CurrentWorkers
    {

        set
        {
            if (value<=Ctx.HumanResourcesNetwork.Unemployed)
            {
                _currentWorkers = value;
                OnStateChanged?.Invoke(this,BuildingStateValueType.CurrentWorkers);
            }
        }

        get => _currentWorkers;
    }

    [ShowInInspector, ReadOnly, LabelText("运行时最大工作岗位数")]
    public int RO_MaxJobsPosition
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0;
            }

            // 计算基础值的修正（与最大人口数逻辑完全对齐）
            float fBase = (GetLevelData().BaseMaxJobsPosition + statModifiers.Base_MaxJobsPositionAdd) * statModifiers.Base_MaxJobsPositionMul;
            float fBonus = statModifiers.Bonus_MaxJobsPositionAdd * statModifiers.Bonus_MaxJobsPositionMul;
            float f = (fBase + fBonus) * statModifiers.Final_MaxJobsPositionMul;
            return (int)f;
        }
    }


    [ShowInInspector, ReadOnly, LabelText("岗位吸引力")]
    public float RO_JobAttractiveness
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0;
            }

            //计算基础值的修正
            float fBase = (GetLevelData().BaseAttractivenessPerJob + statModifiers.Base_JobAttractivenessAdd) * statModifiers.Base_JobAttractivenessMul;
            float fBonus = statModifiers.Bonus_JobAttractivenessAdd * statModifiers.Bonus_JobAttractivenessMul;
            float f = (fBase + fBonus) * statModifiers.Final_JobAttractivenessMul;
            return f;
        }
    }


    //----------------------------库存与运力-----------------------------------
    [ShowInInspector, ReadOnly, LabelText("运行时库存容量")]
    public int RO_MaxStorageCapacity
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0;
            }

            //计算基础值的修正
            float fBase = (GetLevelData().BaseStorageCapacity + statModifiers.Base_StorageCapacityAdd) * statModifiers.Base_StorageCapacityMul;
            float fBonus = statModifiers.Bonus_StorageCapacityAdd * statModifiers.Bonus_StorageCapacityMul;
            float f = (fBase + fBonus) * statModifiers.Final_StorageCapacityMul;
            return (int)f;
        }
    }

    [ShowInInspector, ReadOnly, LabelText("运行时转运范围")]
    public float RO_TransportRadius
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0f; // 浮点型返回 0f，更规范
            }

            // 完全对齐库存容量的计算逻辑，仅替换字段名
            float fBase = (GetLevelData().BaseTransportRadius + statModifiers.Base_TransportRadiusAdd) * statModifiers.Base_TransportRadiusMul;
            float fBonus = statModifiers.Bonus_TransportRadiusAdd * statModifiers.Bonus_TransportRadiusMul;
            float f = (fBase + fBonus) * statModifiers.Final_TransportRadiusMul;
            return f;
        }
    }

    [ShowInInspector, ReadOnly, LabelText("运行时分发范围")]
    public float RO_DistributeRadius
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0f; // 浮点型返回 0f，更规范
            }

            // 完全对齐库存容量的计算逻辑，仅替换字段名
            float fBase = (GetLevelData().BaseDistributeRadius + statModifiers.Base_DistributeRadiusAdd) * statModifiers.Base_DistributeRadiusMul;
            float fBonus = statModifiers.Bonus_DistributeRadiusAdd * statModifiers.Bonus_DistributeRadiusMul;
            float f = (fBase + fBonus) * statModifiers.Final_DistributeRadiusMul;
            return f;
        }
    }
    [ShowInInspector, ReadOnly, LabelText("最大转运容量")]
    public float RO_MaxTraffic
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0f; // 浮点型返回 0f，更规范
            }

            // 完全对齐 RO_DistributeRadius 计算逻辑，仅替换 MaxTraffic 对应字段名
            float fBase = (GetLevelData().BaseMaxTraffic + statModifiers.Base_MaxTrafficAdd) * statModifiers.Base_MaxTrafficMul;
            float fBonus = statModifiers.Bonus_MaxTrafficAdd * statModifiers.Bonus_MaxTrafficMul;
            float f = (fBase + fBonus) * statModifiers.Final_MaxTrafficMul;
            return f;
        }
    }

    private float _currentTraffic;
    [ShowInInspector, ReadOnly, LabelText("当前转运流量")]
    public float CurrentTraffic
    {
        get => _currentTraffic;
        set
        {
            if (_currentTraffic != value)
            {
                _currentTraffic = value;
                OnStateChanged?.Invoke(this, BuildingStateValueType.转运流量);
            }

        }
    }

    [ShowInInspector, ReadOnly, LabelText("剩余转运容量")]
    public float SurplusTraffic
    {
        get => RO_MaxTraffic - CurrentTraffic;
    }



    [ShowInInspector, ReadOnly, LabelText("参与转运系统")]
    public bool CurrentTransportationAbility
    {
        get => RO_MaxTraffic > 0;      
    }

    [ShowInInspector, ReadOnly, LabelText("转运阻力")]
    public int RO_TransportationResistance
    {
        get
        {
            if (GetLevelData() == null)
            {
                return 0;
            }

            float fBase = (GetLevelData().BaseTransportationResistance + statModifiers.Base_TransportationResistanceAdd) * statModifiers.Base_TransportationResistanceMul;
            float fBonus = statModifiers.Bonus_TransportationResistanceAdd * statModifiers.Bonus_TransportationResistanceMul;
            float f = (fBase + fBonus) * statModifiers.Final_TransportationResistanceMul;
            return Mathf.Max(0, Mathf.RoundToInt(f));
        }
    }



    [ShowInInspector, ReadOnly, LabelText("产品列表")]
    public HashSet<SupplyDef> CurrentProductList { get; private set; } = new HashSet<SupplyDef>(0); //目前只是作为产品源头 没有什么其他作用

    public bool AddProduct(SupplyDef product)
    {
        if (product == null) return false;
        if (CurrentProductList == null) { CurrentProductList = new HashSet<SupplyDef>(); }
        bool a =  CurrentProductList.Add(product);
        if (a)
        {
            OnStateChanged?.Invoke(this, BuildingStateValueType.产品列表);
        }
        return a;
    }
    public bool RemoveProduct(SupplyDef product)
    {
        if (product == null) return false;
        if (CurrentProductList == null) return false;

        bool removed = CurrentProductList.Remove(product);
        if (removed)
        {
            OnStateChanged?.Invoke(this, BuildingStateValueType.产品列表);
        }

        return removed;
    }


    //----------------------------地图占用（这些一般不触发状态事件，如需要也可改同样写法）-----------------------------------

    [ShowInInspector, ReadOnly, LabelText("占用格子")]
    public Vector3Int[] CurrentOccupy { get; private set; } = Array.Empty<Vector3Int>();

    [ShowInInspector, ReadOnly, LabelText("中心坐标(网格)")]
    public Vector3 CurrentCenterInGrid { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("中心是坐标交点")]
    public bool CenterIsCorner { get; private set; }


    //----------------------------上下文 & 视图 & 规则------------------------

    public IGameContext Ctx { get => GameContext.Instance; }

    [ShowInInspector, ReadOnly, LabelText("当前规则列表")]
    public Dictionary<string, Rule> CurrentRules { get; private set; } = new();
    #endregion

    private BuildingStatModifiers statModifiers = new BuildingStatModifiers();

    public void AddRule(string name, Rule rule)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("[BuildingInstance] AddRule 失败：name 为空", this);
            return;
        }
        if (rule == null)
        {
            Debug.LogError($"[BuildingInstance] AddRule 失败：规则对象为空（name={name}）", this);
            return;
        }
        if (CurrentRules.ContainsKey(name))
        {
            return;
        }

        ReadyToAddRule.Add((name, rule));
        rule.OnAdd(this);

    }



    public void RemoveRule(string name)
    {

        if (CurrentRules.TryGetValue(name, out Rule r))
        {
            r.OnRemove(this);
            ReadyToClearedRule.Add(name);
        }

    }
    private void ExecutionRules(TurnPhase phase)
    {
        foreach (var rule in CurrentRules.Values)
        {
            rule.OnUpdate(this, phase);
        }
    }

  


    private void OnEnable()
    {
        _activeInstances.Add(this);


        RegisterToGame();
    }
    private void Start()
    {
        View = transform.GetComponentInChildren<BuildingView>();
        View.Init(this);
    }

    private void OnDisable()
    {
        UnRegisterToGame();


        _activeInstances.Remove(this);
    }



    public void Initialize(BuildingArchetype def, Vector3Int[] occupyCells, Vector3 center, bool centerIsCorner, int startLevelIndex = 0)
    {
        Def = def;
        if (Def?.Levels == null || Def.Levels.Count == 0)
        {
            Debug.LogError("[BuildingInstance] 建筑定义缺少等级数据", this);
            return;
        }

        CurrentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Def.Levels.Count - 1);

        CurrentOccupy = occupyCells ?? Array.Empty<Vector3Int>();
        CurrentCenterInGrid = center;
        CenterIsCorner = centerIsCorner;


        LoadLevelRules(CurrentLevelIndex);
        //第一次需要立刻调用一次
        DelayChangeRuleDic();

    }

    private void RegisterToGame()
    {
        Ctx.HumanResourcesNetwork.Register(this);
        Ctx.ResourceNetwork.Register(this);
        TurnSystem.OnTurnPhaseChange += HandleTurnPhase;

        foreach (var pos in CurrentOccupy) _occupyMap[pos] = this;

    }


    private void UnRegisterToGame()
    {
        Ctx.HumanResourcesNetwork.UnRegister(this);
        Ctx.ResourceNetwork.UnRegister(this);
        TurnSystem.OnTurnPhaseChange -= HandleTurnPhase;

        foreach (var pos in CurrentOccupy) _occupyMap.Remove(pos);
    }


    private void HandleTurnPhase(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.结束准备阶段:
                break;
            case TurnPhase.资源消耗阶段:
                break;
            case TurnPhase.资源生产阶段:
                break;
            case TurnPhase.回合结束阶段:
                TryUpgrade();
                break;
            case TurnPhase.开始准备阶段:
                DelayChangeRuleDic();
                break;
            default:
                Debug.Log($"{phase} 未处理");
                break;
        }

        ExecutionRules(phase);
    }



    private BuildingLevelDef GetLevelData()
    {
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0)
            return null;

        return Def.Levels[Mathf.Clamp(CurrentLevelIndex, 0, Def.Levels.Count - 1)];
    }




    private const string DataRulePrefix = "FromDataRules_";

    private List<string> ReadyToClearedRule = new List<string>();
    private List<(string, Rule)> ReadyToAddRule = new List<(string, Rule)>();
    private void DelayChangeRuleDic()
    {
        foreach (string key in ReadyToClearedRule)
        {
            if (CurrentRules.ContainsKey(key))
            {
                CurrentRules.Remove(key);
            }
        }


        foreach ((string, Rule) item in ReadyToAddRule)
        {
            CurrentRules.Add(item.Item1, item.Item2);
        }


        ReadyToAddRule.Clear();
        ReadyToClearedRule.Clear();
    }

    public bool TryUpgrade()
    {
        // 1) 基础数据校验
        if (Def?.Levels == null || Def.Levels.Count == 0)
            return false;

        // 2) 满级判定
        int lastIndex = Def.Levels.Count - 1;
        if (CurrentLevelIndex >= lastIndex)
            return false;

        // 3) 经验是否足够（优先使用运行时计算的 RO_ExpToNext）
        int expToNext = RO_ExpToNext > 0 ? RO_ExpToNext : Mathf.Max(0, GetLevelData()?.ExpToNext ?? 0);
        if (CurrentExp < expToNext)
            return false;

        // 4) 升级条件判定（通常使用“当前等级”的允许升级条件；若为空可按需替换为“下一等级解锁条件”）
        var currentLevel = GetLevelData();
        var nextLevelIndex = CurrentLevelIndex + 1;
        var nextLevel = Def.Levels[Mathf.Clamp(nextLevelIndex, 0, lastIndex)];

        var conditions = currentLevel?.ConditionsForAllowingUpgrades; // 若你希望检查下一等级的解锁条件，可改为：nextLevel?.ConditionsForAllowingUpgrades 或 nextLevel?.UnlockConditions
        if (conditions != null && !ConditionUtility.TryEvaluateConditions(conditions, this, Ctx, out string reason))
        {
            Debug.LogWarning($"[BuildingInstance] 建筑 {Def.DisplayName} 无法升级：{reason}", this);
            return false;
        }

        // 5) 执行升级（保留多余经验）
        CurrentLevelIndex = nextLevelIndex;
        CurrentExp -= expToNext;
        if (CurrentExp < 0) CurrentExp = 0;

        // 6) 移除旧等级从数据生成的规则
        foreach (var key in CurrentRules.Keys)
        {
            if (key.StartsWith(DataRulePrefix, StringComparison.Ordinal))
            {
                RemoveRule(key);
            }
        }

        LoadLevelRules(CurrentLevelIndex);


        // 8) 通知状态变化（等级变化会影响多项运行时数值，按需补充/裁剪）
        OnStateChanged?.Invoke(this, BuildingStateValueType.LevelIndex);
        OnStateChanged?.Invoke(this, BuildingStateValueType.ExpToNext);
        OnStateChanged?.Invoke(this, BuildingStateValueType.MaxPopulation);
        OnStateChanged?.Invoke(this, BuildingStateValueType.MaxStorageCapacity);
        OnStateChanged?.Invoke(this, BuildingStateValueType.就业吸引力);

        return true;






    }

    private void LoadLevelRules(int levelIndex)
    {
        if (Def?.Levels == null || Def.Levels.Count == 0) return;
        int last = Def.Levels.Count - 1;
        var lvl = Def.Levels[Mathf.Clamp(levelIndex, 0, last)];
        var list = lvl?.Rules;
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var src = list[i];
            var cloned = src?.Clone() as Rule;
            if (cloned == null)
            {
                Debug.LogWarning($"[BuildingInstance] 等级规则为空或克隆失败（index={i}）", this);
                continue;
            }
            string key = MakeDataRuleKey(i, levelIndex, src.GetRuleName());
            AddRule(key, cloned);
        }


        string MakeDataRuleKey(int index, int LevelIndex, string ruleName) { return $"{DataRulePrefix}_{LevelIndex}_{index}_{ruleName}"; }
    }





    public void DestroyBuilding()
    {
        Destroy(gameObject);
    }






    #region 视觉效果


    public BuildingView View {  get; private set; } 







    #endregion


}



public static class BuildingInstanceExtensions
{
    public static bool BE_TryAddResource(this BuildingInstance self,SupplyAmount item)
    {

        return BE_TryAddResource(self,item.Resource,item.Amount);
       
    }

    public static bool BE_TryAddResource(this BuildingInstance self, SupplyDef def,int num)
    {
        if (!self.Ctx.ResourceNetwork.TryAddResource(def, num, out string r))
        {
            Debug.Log(r);
            return false;
        }
        return true;
    }


}

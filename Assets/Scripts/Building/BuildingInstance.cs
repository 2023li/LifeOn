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
    StorageCapacity,
    TransportationAbility,
    TransportationResistance,
    就业吸引力,
}

public class BuildingInstance : MonoBehaviour
{
    #region 静态

    private static readonly HashSet<BuildingInstance> _activeInstances = new();

    public static IReadOnlyCollection<BuildingInstance> ActiveInstances => _activeInstances;

    public static bool TryGetAtCell(Vector3Int cell, out BuildingInstance inst)
    {
        foreach (var candidate in _activeInstances)
        {
            if (candidate == null || candidate.CurrentOccupy == null)
                continue;

            foreach (var occupyCell in candidate.CurrentOccupy)
            {
                if (occupyCell == cell)
                {
                    inst = candidate;
                    return true;
                }
            }
        }

        inst = null;
        return false;
    }

    #endregion

    #region 事件

    /// <summary>
    /// 当状态值变化时触发：(实例，状态类型，旧值，新值)
    /// 所有通过属性修改的受控数值都会调用。
    /// </summary>
    public event Action<BuildingInstance, BuildingStateValueType> OnStateValueChanged;

    private bool SetStateValue<T>(ref T field, T newValue, BuildingStateValueType type)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
            return false;

        T oldValue = field;
        field = newValue;
        OnStateValueChanged?.Invoke(this, type);
        return true;
    }

    #endregion

    #region 基础 & 状态字段 + 属性


    //----------------------------基础信息-----------------------------------


    [LabelText("实例ID"),ShowInInspector, ReadOnly]
    public string InstanceId { get; private set; } = Guid.NewGuid().ToString("N");

    [LabelText("建筑定义数据")]
    public BuildingArchetype Def;
    public string DisplayName =>Def.DisplayName;


    //----------------------------等级-----------------------------------


    [Header("等级"),LabelText("当前等级索引"), ShowInInspector, ReadOnly]
    private int _currentLevelIndex;
    public int CurrrentLevelIndex
    {
        get => _currentLevelIndex;
        private set => SetStateValue(ref _currentLevelIndex, value, BuildingStateValueType.LevelIndex);
    }

    [ShowInInspector, ReadOnly, LabelText("当前经验")]
    private int _currentExp;
    public int CurrentExp
    {
        get => _currentExp;
        set
        {
            // 先更新数值 & 触发状态事件
            if (!SetStateValue(ref _currentExp, value, BuildingStateValueType.CurrentExp))
                return;

            // 每次经验变化后检查是否可升级
            if (IsLevelUpExpEnough())
            {
                TryUpgrade(Ctx);
            }
        }
    }

    [ShowInInspector, ReadOnly, LabelText("升级所需经验(运行时)")]
    private int _runtimeExpToNext;
    public int Runtime_ExpToNext
    {
        get => _runtimeExpToNext;
        private set => SetStateValue(ref _runtimeExpToNext, value, BuildingStateValueType.ExpToNext);
    }


    //----------------------------人口 & 就业-----------------------------------
    [Header("人口&就业"),ShowInInspector, ReadOnly, LabelText("运行时最大人口")]
    private int _runtimeMaxPopulation;
    public int RunTime_MaxPopulation
    {
        get => _runtimeMaxPopulation;
        private set => SetStateValue(ref _runtimeMaxPopulation, value, BuildingStateValueType.MaxPopulation);
    }
    [ShowInInspector, ReadOnly, LabelText("当前人口")]
    private int _currentPopulation;
    public int CurrentPopulation
    {
        get => _currentPopulation;
        set
        {
            int v = Mathf.Max(0, value);
            if (!SetStateValue(ref _currentPopulation, v, BuildingStateValueType.CurrentPopulation))
                return;

            // 保证 workers ≤ population
            if (_currentWorkers > _currentPopulation)
                CurrentWorkers = _currentPopulation; // 走另一 setter

            var hr = Ctx?.HumanResourcesNetwork;
            if (hr == null) return;
            if (_currentPopulation > 0 || _currentWorkers > 0) hr.RegisterOrUpdate(this);
            else hr.Unregister(this);
        }
    }
    [ShowInInspector, ReadOnly, LabelText("当前工人")]
    private int _currentWorkers;
    public int CurrentWorkers
    {
        get => _currentWorkers;
        set
        {
            int v = Mathf.Clamp(value, 0, _currentPopulation);
            if (!SetStateValue(ref _currentWorkers, v, BuildingStateValueType.CurrentWorkers))
                return;

            var hr = Ctx?.HumanResourcesNetwork;
            if (hr == null) return;
            if (_currentPopulation > 0 || _currentWorkers > 0) hr.RegisterOrUpdate(this);
            else hr.Unregister(this);
        }
    }

    [ShowInInspector, ReadOnly, LabelText("岗位吸引力")]
    private float _currentJobAttractiveness;
    public float CurrentJobAttractiveness
    {
        get => _currentJobAttractiveness;
        set => SetStateValue(ref _currentJobAttractiveness, value, BuildingStateValueType.就业吸引力);
    }


    //----------------------------库存与运力-----------------------------------
    [Header("库存与运力"),ShowInInspector, ReadOnly, LabelText("运行时库存容量")]
    private int _runtimeStorageCapacity;
    public int Runtime_StorageCapacity
    {
        get => _runtimeStorageCapacity;
        private set
        {
            if (!SetStateValue(ref _runtimeStorageCapacity, value, BuildingStateValueType.StorageCapacity))
                return;

            if (Ctx?.ResourceNetwork == null)
                return;

            if (_runtimeStorageCapacity > 0)
            {
                Ctx.ResourceNetwork.RegisterCapacityProvider(this);
            }
            else
            {
                Ctx.ResourceNetwork.UnregisterCapacityProvider(this);
            }
        }
    }

    [ShowInInspector, ReadOnly, LabelText("允许转运")]
    private bool _currentTransportationAbility;
    public bool CurrentTransportationAbility
    {
        get => _currentTransportationAbility;
        set
        {
            if (!SetStateValue(ref _currentTransportationAbility, value, BuildingStateValueType.TransportationAbility))
                return;

            if (Ctx?.ResourceNetwork == null)
                return;

            if (_currentTransportationAbility)
            {
                Ctx.ResourceNetwork.RegisterTransportNode(this);
            }
            else
            {
                Ctx.ResourceNetwork.UnregisterTransportNode(this);
            }
        }
    }

    [ShowInInspector, ReadOnly, LabelText("运行时转运阻力")]
    private int _currentTransportationResistance = 5;
    public int CurrentTransportationResistance
    {
        get => _currentTransportationResistance;
        set
        {
            if (!SetStateValue(ref _currentTransportationResistance, value, BuildingStateValueType.TransportationResistance))
                return;

            if (Ctx?.ResourceNetwork != null && CurrentTransportationAbility)
            {
                Ctx.ResourceNetwork.NotifyTransportNodeChanged(this);
            }
        }
    }

    //----------------------------地图占用（这些一般不触发状态事件，如需要也可改同样写法）-----------------------------------

    [ShowInInspector, ReadOnly, LabelText("占用格子")]
    public Vector3Int[] CurrentOccupy { get; private set; } = Array.Empty<Vector3Int>();

    [ShowInInspector, ReadOnly, LabelText("中心坐标(网格)")]
    public Vector3 CurrentCenterInGrid { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("中心是坐标交点")]
    public bool CenterIsCorner { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("尺寸")]
    public int FootprintSize { get; private set; }

    //----------------------------上下文 & 视图 & 规则------------------------

    public IGameContext Ctx { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("当前规则列表")]
    public List<Rule> CurrentRules { get; private set; } = new();

    private readonly List<Rule> _pendingRemoveRules = new(8);

    private BuildingView _view;
    private readonly List<BuildingLevelViewConfig> _viewConfigsCache = new();

    #endregion

    #region Unity 生命周期

    private void OnEnable()
    {
        _activeInstances.Add(this);
        TurnSystem.OnTurnPhaseChange += FirePermanentRules;
    }

    private void OnDisable()
    {
        TurnSystem.OnTurnPhaseChange -= FirePermanentRules;
        _activeInstances.Remove(this);
    }
    
    #endregion

    #region 初始化 & 布局 & 视图

    public void Initialize(BuildingArchetype def, int startLevelIndex = 0)
    {
        Def = def;
        Ctx = GameContext.Instance;

        if (Def?.Levels == null || Def.Levels.Count == 0)
        {
            Debug.LogError("[BuildingInstance] 建筑定义缺少等级数据", this);
            return;
        }

        CurrrentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Def.Levels.Count - 1);

        // 1. 按等级刷新基础数值
        ApplyBaseStatsFromLevel();

        // 2. 注入当前等级静态规则
        RebuildStaticRulesForCurrentLevel(null);

        // 3. 初始化视图
        TryInitView();
    }

    public void ConfigurePlacement(Vector3Int[] occupyCells, Vector3 center, bool centerIsCorner, int footprintSize)
    {
        CurrentOccupy = occupyCells ?? Array.Empty<Vector3Int>();
        CurrentCenterInGrid = center;
        CenterIsCorner = centerIsCorner;
        FootprintSize = footprintSize;
    }

    private BuildingLevelDef GetLevelData()
    {
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0)
            return null;

        return Def.Levels[Mathf.Clamp(CurrrentLevelIndex, 0, Def.Levels.Count - 1)];
    }

    private void ApplyBaseStatsFromLevel()
    {
        var lvl = GetLevelData();
        if (lvl == null)
            return;

        Runtime_ExpToNext = lvl.ExpToNext;
        RunTime_MaxPopulation = lvl.BaseMaxPopulation;
        Runtime_StorageCapacity = lvl.BaseStorageCapacity;
    }

    private void TryInitView()
    {
        if (_view == null)
            _view = GetComponentInChildren<BuildingView>();

        if (_view == null || Def?.Levels == null)
            return;

        _viewConfigsCache.Clear();
        for (int i = 0; i < Def.Levels.Count; i++)
        {
            BuildingLevelDef level = Def.Levels[i];
            _viewConfigsCache.Add(level != null ? level.ViewConfig : null);
        }

        _view.ConfigureLevels(_viewConfigsCache);
        _view.ApplyLevel(CurrrentLevelIndex, CurrrentLevelIndex, playUpgradeAnim: false);
    }

    #endregion

    #region 规则管理

    public void AddRunTimeRule(Rule newRule)
    {
        if (newRule == null)
            return;

        if (!CurrentRules.Contains(newRule))
        {
            CurrentRules.Add(newRule);
            newRule.OnAdd(this);
            // 若 OnAdd 改变了 Storage/运力，会触发 OnStateValueChanged；如需可在 Rule 内手动调用 TryInitStorageIfAny()
        }
    }

    public void RemoveRunTimeRule(Rule rule)
    {
        if (rule == null)
            return;

        if (CurrentRules.Contains(rule) && !_pendingRemoveRules.Contains(rule))
        {
            _pendingRemoveRules.Add(rule);
        }
    }

    private void FlushPendingRemoveRules()
    {
        if (_pendingRemoveRules.Count == 0)
            return;

        foreach (var rule in _pendingRemoveRules)
        {
            if (rule == null)
                continue;

            if (CurrentRules.Remove(rule))
            {
                rule.OnRemove(this);
            }
        }

        _pendingRemoveRules.Clear();
    }

    private void RebuildStaticRulesForCurrentLevel(int? previousLevelIndex)
    {
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0)
            return;

        // 移除旧等级规则
        if (previousLevelIndex.HasValue &&
            previousLevelIndex.Value >= 0 &&
            previousLevelIndex.Value < Def.Levels.Count)
        {
            var prevLvl = Def.Levels[previousLevelIndex.Value];
            if (prevLvl?.Rules != null)
            {
                foreach (var rule in prevLvl.Rules)
                {
                    if (rule != null && CurrentRules.Contains(rule))
                    {
                        rule.OnRemove(this);
                        CurrentRules.Remove(rule);
                    }
                }
            }
        }

        // 添加当前等级静态规则
        var curLvl = GetLevelData();
        if (curLvl?.Rules != null)
        {
            foreach (var rule in curLvl.Rules)
            {
                if (rule == null)
                    continue;

                if (!CurrentRules.Contains(rule))
                {
                    CurrentRules.Add(rule);
                    rule.OnAdd(this);
                }
            }
        }
    }

    #endregion

    #region 等级 & 升级

    public bool IsMaxLevel()
    {
        return Def?.Levels != null &&
               CurrrentLevelIndex >= Def.Levels.Count - 1;
    }

    public bool IsLevelUpExpEnough()
    {
        if (IsMaxLevel())
            return false;

        return Runtime_ExpToNext > 0 && CurrentExp >= Runtime_ExpToNext;
    }

    public bool TryUpgrade(IGameContext ctx)
    {
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0)
            return false;

        var cur = GetLevelData();
        if (cur == null)
            return false;

        int expToNext = Runtime_ExpToNext > 0 ? Runtime_ExpToNext : cur.ExpToNext;
        if (expToNext <= 0 || CurrentExp < expToNext)
            return false;

        var context = ctx ?? Ctx;
        if (!ConditionUtility.TryEvaluateConditions(cur.ConditionsForAllowingUpgrades, this, context, out string reason))
        {
            Debug.LogWarning($"[BuildingInstance] 建筑 {Def.DisplayName} 无法升级：{reason}", this);
            return false;
        }

        if (IsMaxLevel())
            return false;

        int previousIndex = CurrrentLevelIndex;
        CurrrentLevelIndex = Mathf.Min(CurrrentLevelIndex + 1, Def.Levels.Count - 1);
        CurrentExp = 0;

        OnLevelChanged(previousIndex);
        return true;
    }

    private void OnLevelChanged(int previousIndex)
    {
        if (previousIndex == CurrrentLevelIndex)
            return;

        // 1. 刷新基础数值
        ApplyBaseStatsFromLevel();

        // 2. 视图
        _view?.ApplyLevel(previousIndex, CurrrentLevelIndex, playUpgradeAnim: true);

        // 3. 静态规则切换
        RebuildStaticRulesForCurrentLevel(previousIndex);

       
    }

    #endregion

    #region 回合驱动 & 拆除

    private void FirePermanentRules(TurnPhase phase)
    {
        int count = CurrentRules.Count;
        for (int i = 0; i < count; i++)
        {
            CurrentRules[i]?.OnUpdate(this, phase);
        }

        FlushPendingRemoveRules();

        if (IsLevelUpExpEnough())
        {
            TryUpgrade(Ctx);
        }
    }

    private void ExecuteDemolishRules()
    {
        if (CurrentRules.Count > 0)
        {
            foreach (var r in CurrentRules)
            {
                r?.OnRemove(this);
            }

            CurrentRules.Clear();
        }
    }

    public void DestroyBuilding()
    {
        ExecuteDemolishRules();
        Destroy(gameObject);
    }

    #endregion
}

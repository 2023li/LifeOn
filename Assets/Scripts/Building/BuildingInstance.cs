using UnityEngine;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;

public class BuildingInstance : MonoBehaviour
{
    #region 静态
    private static readonly HashSet<BuildingInstance> _activeInstances = new();

    public static IReadOnlyCollection<BuildingInstance> ActiveInstances => _activeInstances;

    public static bool TryGetAtCell(Vector3Int cell, out BuildingInstance inst)
    {
        foreach (BuildingInstance candidate in _activeInstances)
        {
            if (candidate == null || candidate.CurrentOccupy == null)
            {
                continue;
            }

            foreach (Vector3Int occupyCell in candidate.CurrentOccupy)
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






    public string InstanceId { get; private set; } = Guid.NewGuid().ToString("N");

    [LabelText("建筑定义数据")]
    public BuildingArchetype Def;

    [SerializeField, LabelText("建筑表现")]
    private BuildingView _view;

    private readonly List<BuildingLevelViewConfig> _viewConfigsCache = new();

    public string DisplayName => Def != null ? Def.DisplayName : string.Empty;

    [ShowInInspector, ReadOnly, LabelText("等级")]
    public int CurrrentLevelIndex { get; private set; } = 0; // 对应 Def.Levels 索引

    [ShowInInspector, ReadOnly, LabelText("人口")]
    public int CurrentPopulation { get; set; }

    [ShowInInspector, ReadOnly, LabelText("当前工人")]
    public int CurrentWorkers; // 当前分配在该建筑的工人数量

  

    [ShowInInspector, ReadOnly,LabelText("岗位吸引力")]
    public float GetEmploymentAttractiveness()
    {
        /*政府补贴这个行为应该如何定义？
         * 是一个规则吗？
         * 不太像 如果是规则 每回合扣钱可以处理 但是增加的吸引力应该如何处理？
         * 是个一次性的效果
         * 如果拆分为2条规则呢？
         * 
         */
        return GetLevelData().BaseAttractivenessPerJob;
    }
   

    [ShowInInspector, ReadOnly]
    public int CurrentExp { get; set; }

    [ShowInInspector, ReadOnly]
    public BuildingLevelDef CurrentLevelData => Def.Levels[CurrrentLevelIndex];

    [ShowInInspector, ReadOnly, LabelText("占用")]
    public Vector3Int[] CurrentOccupy { get; private set; } // 由放置系统设置

    [ShowInInspector, ReadOnly, LabelText("中心的坐标")]
    public Vector3 CurrentCenterInGrid { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("中心是坐标交点")]
    public bool CenterIsCorner { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("尺寸")]
    public int FootprintSize { get; private set; }

    public IGameContext Ctx { get; private set; }


    #region
    private Dictionary<string, int> tempInt;
    private Dictionary<string, float> tempFloat;
    private Dictionary<string, string> tmepString;

    #endregion


    /// <summary>
    /// 运行时添加的规则（卡牌、Buff、科技等可往这里塞）。
    /// </summary>
    private List<Rule> currentRules;

    #region 运行时规则操作

    /// <summary>
    /// 添加运行时 Rule。
    /// </summary>
    public void AddRunTimeRule(Rule newRule)
    {
        if (newRule == null)
        {
            return;
        }

        currentRules ??= new List<Rule>(10);
        currentRules.Add(newRule);
        if (newRule.ExecutePhase== RuleExecuteTime.规则启用时)
        {
            ExecuteRules(RuleExecuteTime.规则启用时);
        }
    }

    /// <summary>
    /// 移除指定运行时 Rule（可选：当 Buff 结束等）。
    /// </summary>
    public bool RemoveRunTimeRule(Rule rule)
    {
        if (rule == null || currentRules == null)
        {
            return false;
        }

        return currentRules.Remove(rule);
    }

    /// <summary>
    /// 按条件移除一批运行时 Rule，方便做清理。
    /// 例如：一次性规则执行完后，根据自定义标记移除。
    /// </summary>
    public void RemoveRunTimeRules(Predicate<Rule> match)
    {
        if (match == null || currentRules == null)
        {
            return;
        }

        currentRules.RemoveAll(match);
    }

    #endregion

    public void Initialize(BuildingArchetype def)
    {
        Def = def;
        Ctx = GameContext.Instance;
        TryInitStorageIfAny();
        TryInitView();
    }

    private void OnEnable()
    {
        TurnSystem.OnTurnPhaseChange += FirePermanentRules;
        _activeInstances.Add(this);

        if (Def != null && Ctx?.ResourceNetwork != null)
        {
            TryInitStorageIfAny();
        }
    }

    private void OnDisable()
    {
        TurnSystem.OnTurnPhaseChange -= FirePermanentRules;
        _activeInstances.Remove(this);

        if (Ctx?.ResourceNetwork != null)
        {
            Ctx.ResourceNetwork.UnregisterWarehouse(this);
            // 如果以后有生产者注册，也可以在这里顺便注销
        }
    }

    private void TryInitStorageIfAny()
    {
        BuildingLevelDef level = CurrentLevelData;

        // 如果当前建筑有基础存储容量或具备转运能力，则注册为仓库节点
        if (level.GetStorageCapacity(this) > 0 || level.TransportationCapacity(this))
        {
            Ctx.ResourceNetwork.RegisterWarehouse(this);
        }
        else
        {
            Ctx.ResourceNetwork.UnregisterWarehouse(this);
        }
    }

    /// <summary>由建造器配置占地信息，便于环境计算。</summary>
    public void ConfigurePlacement(Vector3Int[] occupyCells, Vector3 center, bool centerIsCorner, int footprintSize)
    {
        CurrentOccupy = occupyCells ?? Array.Empty<Vector3Int>();
        CurrentCenterInGrid = center;
        CenterIsCorner = centerIsCorner;
        FootprintSize = footprintSize;
    }


    /// <summary>
    /// 按执行时机批量执行 当前等级规则 + 运行时规则。
    /// </summary>
    private void ExecuteRules(RuleExecuteTime executeTime, TurnPhase? phase = null)
    {
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0
  )
            return
    ;

        BuildingLevelDef lvl = Def.Levels[CurrrentLevelIndex];

        // 1. 当前等级静态规则
        if (lvl?.Rules != null && lvl.Rules.Count > 0)
        {
            foreach (Rule r in lvl.Rules)
            {
                r?.Execute(executeTime, phase, this, Ctx);
            }
        }

        // 2. 运行时规则
        if (currentRules != null && currentRules.Count > 0)
        {
            var snapshot = currentRules.ToArray(); // 防止执行过程中被修改
            foreach (Rule r in snapshot)
            {
                r?.Execute(executeTime, phase,this, Ctx);
            }
        }
    }

    private void FirePermanentRules(TurnPhase trigger)
    {
        // 1. 执行“每回合执行”的规则（静态 + 运行时）
        ExecuteRules(RuleExecuteTime.每回合执行, trigger);

        // 2. 升级自动化（保持原有逻辑）
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0)
        {
            return;
        }

        BuildingLevelDef lvl = Def.Levels[CurrrentLevelIndex];
        if (lvl.GetExpToNext(this) > 0 && CurrentExp >= lvl.GetExpToNext(this))
        {
            TryUpgrade(Ctx);
        }
    }

    public BuildingLevelDef GetLevelData()
    {
        return Def.Levels[CurrrentLevelIndex];
    }

    public bool TryUpgrade(IGameContext ctx)
    {
        if (Def == null || Def.Levels == null || Def.Levels.Count == 0)
        {
            return false;
        }

        BuildingLevelDef cur = Def.Levels[CurrrentLevelIndex];
        if (cur.GetExpToNext(this) <= 0)
        {
            return false;
        }

        if (CurrentExp < cur.GetExpToNext(this))
        {
            return false;
        }

        IGameContext context = ctx ?? Ctx;
        if (!ConditionUtility.TryEvaluateConditions(cur.ConditionsForAllowingUpgrades, this, context, out string reason))
        {
            string message = string.IsNullOrWhiteSpace(reason) ? "未满足升级条件" : reason;
            Debug.LogWarning($"[BuildingInstance] 建筑 {DisplayName}({Def?.Id}) 无法升级：{message}", this);
            return false;
        }

        int previousIndex = CurrrentLevelIndex;
        CurrrentLevelIndex = Mathf.Min(CurrrentLevelIndex + 1, Def.Levels.Count - 1);
        CurrentExp = 0;

        TryInitStorageIfAny();
        OnLevelChanged(previousIndex, CurrrentLevelIndex, context);

        return true;
    }

    private void OnLevelChanged(int previousIndex, int newIndex, IGameContext ctx)
    {
        if (previousIndex == newIndex)
        {
            return;
        }

        // 切静态外观 +（可选）播放升级动画
        _view?.ApplyLevel(previousIndex, newIndex, playUpgradeAnim: true);

        // 升级成功后，执行“规则启用时”的规则（使用新等级的数据，含运行时规则）
        ExecuteRules(RuleExecuteTime.规则启用时);

        // 仅在 0 -> 1 时进行一次人口收敛与最低基线（保持原有逻辑）
        if (previousIndex == 0 && newIndex == 1)
        {
            int baseline = 2;
            int max = GetLevelData().GetMaxPopulation(this);
            int target = Mathf.Clamp(Mathf.Max(CurrentPopulation, baseline), 0, max);
            CurrentPopulation = target;
        }
    }

    private void TryInitView()
    {
        if (_view == null)
        {
            _view = GetComponentInChildren<BuildingView>();
        }

        if (Def == null|| _view == null)
        {
            return;
        }



        _viewConfigsCache.Clear();
        foreach (BuildingLevelDef level in Def.Levels)
        {
            _viewConfigsCache.Add(level != null ? level.ViewConfig : null);
        }

        _view.ConfigureLevels(_viewConfigsCache);

        // 初始化/加载存档：只切静态外观并进入默认状态，不播放升级动画
        _view.ApplyLevel(CurrrentLevelIndex, CurrrentLevelIndex, playUpgradeAnim: false);

        // 进入当前等级时执行对应规则（静态 + 运行时）
        ExecuteRules(RuleExecuteTime.规则启用时);
    }

    /// <summary>
    /// 外部调用，用于销毁指定建筑实例。
    /// </summary>
    public void DestroyBuilding(BuildingInstance inst)
    {
        if (inst == null)
        {
            return;
        }

        inst.ExecuteDemolishRules();   // 触发拆除规则（退款、返还人口、生成废墟等）
        GameObject.Destroy(inst.gameObject);
    }

    /// <summary>
    /// 外部在真正拆除建筑前调用。
    /// 例如：BuildingManager.DestroyBuilding 时先调用本方法，再 Destroy(gameObject)。
    /// </summary>
    public void ExecuteDemolishRules()
    {
        ExecuteRules(RuleExecuteTime.拆除时执行);
    }
}

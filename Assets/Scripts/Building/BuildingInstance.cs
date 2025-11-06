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
            if (candidate == null || candidate.Occupy == null)
            {
                continue;
            }

            foreach (Vector3Int occupyCell in candidate.Occupy)
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
    public int LevelIndex { get; private set; } = 0; // 对应 Def.Levels 索引

    [ShowInInspector, ReadOnly, LabelText("人口")]
    public int Population { get; set; }

    [ShowInInspector, ReadOnly]
    public int Exp { get; set; }


    [LabelText("提供供给的仓库")]
    public BuildingInstance AssignedStorage;       // 非仓库：从此仓库拉取资源

    [ShowInInspector, ReadOnly, LabelText("占用")]
    public Vector3Int[] Occupy { get; private set; } // 由放置系统设置

    [ShowInInspector, ReadOnly, LabelText("中心的坐标")]
    public Vector3 CenterInGrid { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("中心是坐标交点")]
    public bool CenterIsCorner { get; private set; }

    [ShowInInspector, ReadOnly, LabelText("尺寸")]
    public int FootprintSize { get; private set; }

    private IGameContext _ctx;

    public int WorkersAssigned; // 当前分配在该建筑的工人数量



    public void Initialize(BuildingArchetype def)
    {
        // pointTransform.position = GridSystem.Instance.get
        Def = def;
        _ctx = GameContext.Instance;
        TryInitStorageIfAny();
        TryInitView();
    }

    private void Awake()
    {
       
    }

    private void OnEnable()
    {
        TurnSystem.OnTurnPhaseChange += FireRules;
        _activeInstances.Add(this);
    }



    private void OnDisable()
    {
        TurnSystem.OnTurnPhaseChange -= FireRules;
        _activeInstances.Remove(this);

      
    }

    void TryInitStorageIfAny()
    {
        if (Def == null)
        {
            return;
        }

        BuildingLevelDef level = Def.Levels[LevelIndex];
       
    }

    /// <summary>由建造器配置占地信息，便于环境计算。</summary>
    public void ConfigurePlacement(Vector3Int[] occupyCells, Vector3 center, bool centerIsCorner, int footprintSize)
    {
        Occupy = occupyCells ?? Array.Empty<Vector3Int>();
        CenterInGrid = center;
        CenterIsCorner = centerIsCorner;
        FootprintSize = footprintSize;
    }


    public void FireRules(TurnPhase trigger)
    {
        BuildingLevelDef lvl = Def.Levels[LevelIndex];
        foreach (Rule r in lvl.Rules)
        {
            if (r.Trigger != trigger) { continue; }
            bool ok = true; string why = "";
            foreach (Condition c in r.Conditions)
            {
                if (!c.Evaluate(this, _ctx, out why))
                {
                    ok = false; break;
                }
            }
            List<Effect> effects = ok ? r.OnSuccess : r.OnFailure;

            foreach (Effect e in effects)
            {
                e.Apply(this, _ctx);
            }
        }

        // 升级自动化（也可仅靠规则里放 Upgrade 效果）
        if (lvl.ExpToNext > 0 && Exp >= lvl.ExpToNext)
        {
            TryUpgrade(_ctx);
        }
    }

    public int GetMaxPopulation(IGameContext ctx)
    {
        BuildingLevelDef lvl = Def.Levels[LevelIndex];
        int max = lvl.BaseMaxPopulation;
        foreach (var sm in lvl.ConditionalStatModifiers)
        {
            max = sm.Modify(this, ctx, max);
        }
        return max;
    }

    public bool TryUpgrade(IGameContext ctx)
    {
        BuildingLevelDef cur = Def.Levels[LevelIndex];
        if (cur.ExpToNext <= 0) return false; // 无可升
        if (Exp < cur.ExpToNext) return false;

        IGameContext context = ctx ?? _ctx;
        if (!ConditionUtility.TryEvaluateConditions(cur.ConditionsForAllowingUpgrades, this, context, out string reason))
        {
            string message = string.IsNullOrWhiteSpace(reason) ? "未满足升级条件" : reason;
            Debug.LogWarning($"[BuildingInstance] 建筑 {DisplayName}({Def?.Id}) 无法升级：{message}", this);
            return false;
        }

        int previousIndex = LevelIndex;
        LevelIndex = Mathf.Min(LevelIndex + 1, Def.Levels.Count - 1);
        Exp = 0;

       

        TryInitStorageIfAny();

        OnLevelChanged(previousIndex, LevelIndex, context);
        // 等级变化后的瞬时触发（可选）
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

        // 仅在 0 -> 1 时进行一次人口收敛与最低基线（保持原有逻辑）
        if (previousIndex == 0 && newIndex == 1)
        {
            int baseline = 2;
            int max = GetMaxPopulation(ctx);
            int target = Mathf.Clamp(Mathf.Max(Population, baseline), 0, max);
            Population = target;
        }
    }


    public int GetMaxJobs(IGameContext ctx)
    {
        BuildingLevelDef lvl = Def.Levels[LevelIndex];
        return Mathf.Max(0, lvl.BaseMaxJobs);
    }
    public void AssignWorkers(int count)
    {
        IGameContext ctx = _ctx;
        WorkersAssigned = Mathf.Clamp(count, 0, GetMaxJobs(ctx));
    }

    private void TryInitView()
    {
        if (Def == null)
        {
            return;
        }

        if (_view == null)
        {
            _view = GetComponentInChildren<BuildingView>();
        }

        if (_view == null)
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
        _view.ApplyLevel(LevelIndex, LevelIndex, playUpgradeAnim: false);
    }

}

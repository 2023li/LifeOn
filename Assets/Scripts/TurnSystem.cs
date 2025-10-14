using System;
using UnityEngine;

/// <summary>
/// 回合系统（End→Start 模式）
/// - 每次调用 NextTurn():
///   1) 结算当前回合 T 的所有阶段（就业/产出/…/经济/人口/总结）
///   2) 触发 OnTurnEnd(T)
///   3) 立即进入下一回合 T+1 的 OnTurnStart(T+1) 与 OnPhasePolicies(T+1)
/// - 初次启动时，若没有激活回合，可调用 StartFirstTurn() 或勾选 autoStartOnAwake 以自动触发 T=1 的开始阶段。
/// </summary>
[AddComponentMenu("LifeOn/Turn System")]
public class TurnSystem : MonoBehaviour
{
    // —— 单例（按需使用；也可去掉改为依赖注入） ——
    public static TurnSystem Instance { get; private set; }

    [Header("Startup")]
    [Tooltip("是否在 Awake/Start 后自动触发第一回合的 OnTurnStart & OnPhasePolicies")]
    [SerializeField] private bool autoStartOnAwake = true;

    [Header("Diagnostics")]
    [Tooltip("在 Console 输出每个阶段的日志（便于调试顺序）")]
    [SerializeField] private bool verboseLogging = false;

    /// <summary> 当前激活的回合号（从 1 起计）。例如：刚进入第一回合则为 1。 </summary>
    public int CurrentTurn { get; private set; } = 1;

    /// <summary> 已完成的回合数（等于触发过 OnTurnEnd 的次数）。 </summary>
    public int CompletedTurns { get; private set; } = 0;

    /// <summary> 是否已经进入一个“激活回合”（即已触发 OnTurnStart 但尚未 OnTurnEnd）。</summary>
    public bool HasActiveTurn { get; private set; } = false;

    /// <summary> 当前是否正在进行一次回合结算，防止重入。 </summary>
    public bool IsResolvingTurn { get; private set; } = false;

    // =======================
    // 回合开始与各阶段事件
    // =======================

    /// <summary>
    /// 回合开始（T）：用于清理上回合残留状态、准备UI与数据快照等。
    /// 【监听者举例】CityStateManager（重置临时标记）、HUDController（刷新回合号与提示）
    /// </summary>
    public event Action OnTurnStart;

    /// <summary>
    /// 开始阶段：政策/法令/数值刷新（影响本回合的参数与系数）。
    /// 【监听者举例】PolicyManager（应用政策）、ModifierSystem（刷新全局加成）、EconomyManager（装载本回合税率）
    /// </summary>
    public event Action OnPhasePolicies;

    /// <summary>
    /// 就业阶段（回合制，一次性分配并锁定本回合岗位）：供产出与发薪使用。
    /// 【监听者举例】EmploymentManager（构建工人/岗位快照，调用求解器，写入 CurrentEmployed、生成冻结快照）
    /// </summary>
    public event Action OnPhaseEmployment;

    /// <summary>
    /// 生产阶段：将产出写入本地库存或缓存。
    /// 【监听者举例】SupplyAndDemandSystem/FactorySystem（生产型建筑产出）、BA_SupplyAndDemand（生产分支）
    /// </summary>
    public event Action OnPhaseProduction;

    /// <summary>
    /// 物流/路由阶段：资源在建筑间或城市池中流动（可选）。
    /// 【监听者举例】LogisticsManager、ResourceRouter、MarketDistributor
    /// </summary>
    public event Action OnPhaseLogistics;

    /// <summary>
    /// 消耗阶段：居民/建筑消耗库存，不足则标记缺货。
    /// 【监听者举例】SupplyAndDemandSystem（消费分支：住宅/服务类扣资源，设置 OutOfStock）
    /// </summary>
    public event Action OnPhaseConsumption;

    /// <summary>
    /// 维护/耐久阶段：扣维护费、处理停机/损耗/维修。
    /// 【监听者举例】MaintenanceSystem（维护费与停机）、DurabilitySystem（耐久衰减与修理）
    /// </summary>
    public event Action OnPhaseMaintenance;

    /// <summary>
    /// 进度推进阶段：建筑升级、建造队列、研究、时代演进。
    /// 【监听者举例】ConstructionQueueManager、UpgradeSystem、ResearchManager、EraProgression
    /// </summary>
    public event Action OnPhaseProgress;

    /// <summary>
    /// 事件结算阶段：灾害/外敌/随机事件等造成的影响与伤害。
    /// 【监听者举例】DisasterManager、RaidManager、RandomEventSystem
    /// </summary>
    public event Action OnPhaseEvents;

    /// <summary>
    /// 经济结算阶段：工资、补贴、税收、贸易清算（使用已冻结的就业快照）。
    /// 【监听者举例】EconomyManager（工资与补贴发放、税收与贸易结算）
    /// </summary>
    public event Action OnPhaseEconomy;

    /// <summary>
    /// 人口变化阶段：增长/迁入/迁出/死亡（通常受上一阶段结果影响）。
    /// 【监听者举例】PopulationManager、BA_Population（增长/迁移/死亡结算）
    /// </summary>
    public event Action OnPhasePopulation;

    /// <summary>
    /// 汇总阶段：统计、UI 刷新、自动存档等（本回合结果汇总）。
    /// 【监听者举例】StatsCollector（记录回合统计）、AutosaveSystem（自动存档）、TurnReportUI（显示报表）
    /// </summary>
    public event Action OnPhaseSummary;

    /// <summary>
    /// 回合结束（T 完成）：所有阶段都执行完后触发。
    /// 【监听者举例】UITransitionManager（展示“回合结束”动画/提示）、Analytics（记录一次完成事件）
    /// </summary>
    public event Action OnTurnEnd;

    // ===== Unity 生命周期 =====

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 项目中一般只需要一个 TurnSystem；如存在重复，这里销毁新实例。
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 可选：自动进入第 1 回合的“开始阶段”
        if (autoStartOnAwake && !HasActiveTurn)
            StartFirstTurn();
    }

    // ===== 公共接口 =====

    /// <summary>
    /// 触发第一回合的开始阶段（T=1 的 OnTurnStart & OnPhasePolicies）。
    /// 若已经在一个激活回合中，会被忽略。
    /// </summary>
    public void StartFirstTurn()
    {
        if (HasActiveTurn) return;

        LogPhase("OnTurnStart", CurrentTurn);
        OnTurnStart?.Invoke();

        LogPhase("OnPhasePolicies", CurrentTurn);
        OnPhasePolicies?.Invoke();

        HasActiveTurn = true;
    }

    /// <summary>
    /// 结束当前回合 T（依次触发各结算阶段与 OnTurnEnd），
    /// 并立即开始下一回合 T+1（OnTurnStart → OnPhasePolicies）。
    /// </summary>
    public void NextTurn()
    {
        if (!HasActiveTurn)
        {
            // 尚未进入任何回合：先启动第一回合的开始阶段。
            StartFirstTurn();
            return;
        }
        if (IsResolvingTurn)
        {
            if (verboseLogging) Debug.LogWarning("[TurnSystem] 正在结算回合，忽略重复调用。");
            return;
        }

        IsResolvingTurn = true;

        // —— 结算当前回合 T 的阶段 —— //
        LogPhase("OnPhaseEmployment", CurrentTurn);
        OnPhaseEmployment?.Invoke();

        LogPhase("OnPhaseProduction", CurrentTurn);
        OnPhaseProduction?.Invoke();

        LogPhase("OnPhaseLogistics", CurrentTurn);
        OnPhaseLogistics?.Invoke();

        LogPhase("OnPhaseConsumption", CurrentTurn);
        OnPhaseConsumption?.Invoke();

        LogPhase("OnPhaseMaintenance", CurrentTurn);
        OnPhaseMaintenance?.Invoke();

        LogPhase("OnPhaseProgress", CurrentTurn);
        OnPhaseProgress?.Invoke();

        LogPhase("OnPhaseEvents", CurrentTurn);
        OnPhaseEvents?.Invoke();

        LogPhase("OnPhaseEconomy", CurrentTurn);
        OnPhaseEconomy?.Invoke();

        LogPhase("OnPhasePopulation", CurrentTurn);
        OnPhasePopulation?.Invoke();

        LogPhase("OnPhaseSummary", CurrentTurn);
        OnPhaseSummary?.Invoke();

        // —— 当前回合 T 结束 —— //
        LogPhase("OnTurnEnd", CurrentTurn);
        OnTurnEnd?.Invoke();

        CompletedTurns++;
        HasActiveTurn = false;

        // —— 立刻开始下一回合 T+1 的“开始阶段” —— //
        CurrentTurn++;

        LogPhase("OnTurnStart", CurrentTurn);
        OnTurnStart?.Invoke();

        LogPhase("OnPhasePolicies", CurrentTurn);
        OnPhasePolicies?.Invoke();

        HasActiveTurn = true;
        IsResolvingTurn = false;
    }

    /// <summary>
    /// 连续推进多回合（开发/自动化测试方便）。
    /// </summary>
    public void AdvanceTurns(int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
            NextTurn();
    }

    /// <summary>
    /// 重置回合系统（例如“新游戏/读档前的清场”）。
    /// 注意：不会自动触发 StartFirstTurn。
    /// </summary>
    public void ResetAll(int startTurn = 1)
    {
        if (IsResolvingTurn)
        {
            if (verboseLogging) Debug.LogWarning("[TurnSystem] 结算进行中，无法重置。");
            return;
        }
        CurrentTurn = Mathf.Max(1, startTurn);
        CompletedTurns = 0;
        HasActiveTurn = false;
    }

    // ===== 内部工具 =====

    private void LogPhase(string phaseName, int turn)
    {
        if (verboseLogging)
            Debug.Log($"[TurnSystem] {phaseName} (T{turn})");
    }

#if UNITY_EDITOR
    // 方便在 Inspector 上右键菜单快速测试
    [ContextMenu("Next Turn")]
    private void __EditorNextTurn() => NextTurn();

    [ContextMenu("Start First Turn")]
    private void __EditorStartFirstTurn() => StartFirstTurn();

    [ContextMenu("Advance 5 Turns")]
    private void __EditorAdvance5() => AdvanceTurns(5);
#endif
}

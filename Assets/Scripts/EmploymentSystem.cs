using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[AddComponentMenu("LifeOn/Employment/Employment System")]
public class EmploymentSystem : MonoBehaviour
{
    public static EmploymentSystem Instance { get; private set; }

    [Header("效用权重   U = κ·吸引力 + α·补贴 - β·距离 - θ·换岗")]
    public float Kappa_Attract = 1f;     // 基础吸引力权重 κ
    public float Alpha_Subsidy = 1f;     // 政府补贴权重   α
    public float Beta_Distance = 0.2f;   // 距离惩罚       β
    public float Theta_Switching = 0.5f; // 换岗摩擦       θ

    [Header("搜索与性能")]
    [Min(1)] public int SearchRadius = 30;             // 每名工人只看附近岗位
    [Min(0)] public int RebuildEveryTurns = 1;         // 每 N 回合重建一次候选（1=每回合）
    [Min(0)] public int MaxWorkersPerRebuild = 100000; // 安全上限

    readonly List<WorkerAgent> _workers = new();
    readonly List<JobSlotRuntime> _slots = new();
    int _turnSinceRebuild = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() { TurnSystem.OnTurnPhaseChange += OnTurnPhaseChanged; }
    private void OnDisable() { TurnSystem.OnTurnPhaseChange -= OnTurnPhaseChanged; }

    private void OnTurnPhaseChanged(TurnPhase phase)
    {
        // 在“结束准备阶段”完成就业，保证“资源生产阶段”读取到最新在岗
        if (phase == TurnPhase.结束准备阶段)
        {
            if (_turnSinceRebuild % Mathf.Max(1, RebuildEveryTurns) == 0)
            {
                RebuildWorkersFromPopulation();
                RebuildSlotsFromBuildings();
            }
            _turnSinceRebuild++;

            AssignJobs_Greedy();
            PushAssignmentsToBuildings();
        }
    }

    // ---------- 数据结构 ----------
    [Serializable]
    public class WorkerAgent
    {
        public int Id;
        public Vector3Int HomeCell;
        public int CurrentSlot = -1;
        public float LastUtility = 0f;
    }

    public class JobSlotRuntime
    {
        public BuildingInstance Building;
        public Vector3Int WorkCell;
        public float BaseAttract;              // BuildingLevelDef.BaseAttractivenessPerJob
        public EmploymentSubsidy SubsidyComp;  // 可为空
        public int WorkerId = -1;
        public bool IsFilled => WorkerId >= 0;
        public float Subsidy => Mathf.Max(0f, SubsidyComp ? SubsidyComp.SubsidyPerJob : 0f);
    }

    // ---------- 构建候选 ----------
    void RebuildWorkersFromPopulation()
    {
        _workers.Clear();
        int id = 0;

        // 如你的项目有集中注册：用 BuildingInstance.ActiveInstances；否则用 FindObjectsOfType
        IReadOnlyCollection<BuildingInstance> buildings = BuildingInstance.ActiveInstances ?? GameObject.FindObjectsOfType<BuildingInstance>();

        foreach (var b in buildings)
        {
            int pop = Mathf.Max(0, b.Population);
            if (pop <= 0) continue;

            Vector3Int home = RoundToCell(b.CenterInGrid);
            for (int i = 0; i < pop && _workers.Count < MaxWorkersPerRebuild; i++)
                _workers.Add(new WorkerAgent { Id = id++, HomeCell = home });
        }
    }

    void RebuildSlotsFromBuildings()
    {
        _slots.Clear();
        var buildings = BuildingInstance.ActiveInstances ?? GameObject.FindObjectsOfType<BuildingInstance>();

        foreach (var b in buildings)
        {
            int maxJobs = b.GetMaxJobs(GameContext.Instance);
            if (maxJobs <= 0) continue;

            float baseAttr = 0f;
            var lvl = (b.Def != null && b.Def.Levels != null && b.Def.Levels.Count > b.LevelIndex)
                ? b.Def.Levels[b.LevelIndex] : null;
            if (lvl != null)
            {
                baseAttr = Mathf.Max(0f, lvl.BaseAttractivenessPerJob);
            }


            Vector3Int cell = RoundToCell(b.CenterInGrid);
            var subsidy = b.GetComponent<EmploymentSubsidy>();
            for (int i = 0; i < maxJobs; i++)
            {
                _slots.Add(new JobSlotRuntime
                {
                    Building = b,
                    WorkCell = cell,
                    BaseAttract = baseAttr,
                    SubsidyComp = subsidy
                });
            }
        }
    }

    static Vector3Int RoundToCell(Vector3 gridPos) =>
        new Vector3Int(Mathf.RoundToInt(gridPos.x), Mathf.RoundToInt(gridPos.y), 0);

    static int Manhattan(Vector3Int a, Vector3Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    float Utility(WorkerAgent w, JobSlotRuntime s)
    {
        float u = Kappa_Attract * s.BaseAttract;      // 基础吸引力正项
        u += Alpha_Subsidy * s.Subsidy;               // 政府补贴正项

        int dist = Manhattan(w.HomeCell, s.WorkCell); // 距离负项
        u -= Beta_Distance * dist;

        if (w.CurrentSlot >= 0 && _slots[w.CurrentSlot] != s)
            u -= Theta_Switching;                     // 换岗摩擦负项

        return u;
    }

    // ---------- 贪心匹配（局部最优 + 简洁高效） ----------
    void AssignJobs_Greedy()
    {
        foreach (var s in _slots) s.WorkerId = -1;
        foreach (var w in _workers) { w.CurrentSlot = -1; w.LastUtility = 0f; }

        var proposals = new List<(int workerIdx, int slotIdx, float u)>(_workers.Count);

        for (int wi = 0; wi < _workers.Count; wi++)
        {
            var w = _workers[wi];
            float bestU = float.NegativeInfinity;
            int bestJ = -1;

            for (int sj = 0; sj < _slots.Count; sj++)
            {
                var s = _slots[sj];
                if (Manhattan(w.HomeCell, s.WorkCell) > SearchRadius) continue;

                float u = Utility(w, s);
                if (u > bestU) { bestU = u; bestJ = sj; }
            }

            if (bestJ >= 0 && bestU > 0f)
                proposals.Add((wi, bestJ, bestU));
        }

        proposals.Sort((a, b) => b.u.CompareTo(a.u));

        var workerTaken = new bool[_workers.Count];
        var slotTaken = new bool[_slots.Count];

        foreach (var p in proposals)
        {
            if (workerTaken[p.workerIdx] || slotTaken[p.slotIdx]) continue;

            _slots[p.slotIdx].WorkerId = _workers[p.workerIdx].Id;
            _workers[p.workerIdx].CurrentSlot = p.slotIdx;
            _workers[p.workerIdx].LastUtility = p.u;

            workerTaken[p.workerIdx] = true;
            slotTaken[p.slotIdx] = true;
        }
    }

    // ---------- 写回在岗数（供生产阶段使用） ----------
    void PushAssignmentsToBuildings()
    {
        var counts = new Dictionary<BuildingInstance, int>();
        foreach (var s in _slots)
        {
            if (s.Building == null) continue;
            if (!counts.TryGetValue(s.Building, out int c)) c = 0;
            if (s.IsFilled) c++;
            counts[s.Building] = c;
        }

        var buildings = BuildingInstance.ActiveInstances ?? GameObject.FindObjectsOfType<BuildingInstance>();
        foreach (var b in buildings)
        {
            int c = counts.TryGetValue(b, out int v) ? v : 0;
            b.AssignWorkers(c); // 写回 WorkersAssigned（你的生产效果会读取它）
        }
    }

    // ---------- 对外接口：计算“满岗建议补贴/每岗位” ----------
    // 返回：建议每岗位补贴（满岗），以及“再招1人/2人/…”的阶梯曲线
    public float RecommendSubsidyPerJobFor(BuildingInstance building, out List<float> curve, float epsilon = 0.01f)
    {
        curve = new List<float>();
        if (building == null) return 0f;

        int maxJobs = building.GetMaxJobs(GameContext.Instance);
        int current = Mathf.Clamp(building.WorkersAssigned, 0, maxJobs);
        int need = Mathf.Max(0, maxJobs - current);
        if (need == 0) return 0f;

        float baseAttr = 0f;
        BuildingLevelDef lvl = (building.Def != null && building.Def.Levels != null && building.Def.Levels.Count > building.LevelIndex)
            ? building.Def.Levels[building.LevelIndex] : null;
        if (lvl != null) baseAttr = Mathf.Max(0f, lvl.BaseAttractivenessPerJob);

        Vector3Int workCell = RoundToCell(building.CenterInGrid);

        var demands = new List<float>();

        // 让去该建筑的 U_B >= max(0, bestOther + ε)，反推最小补贴
        foreach (var w in _workers)
        {
            // 其他岗位的最佳效用
            float bestOther = 0f;
            bool hasOther = false;

            foreach (var s in _slots)
            {
                if (s.Building == building) continue;
                if (Manhattan(w.HomeCell, s.WorkCell) > SearchRadius) continue;

                float uOther = Kappa_Attract * s.BaseAttract + Alpha_Subsidy * s.Subsidy
                               - Beta_Distance * Manhattan(w.HomeCell, s.WorkCell);
                if (w.CurrentSlot >= 0 && _slots[w.CurrentSlot] != s) uOther -= Theta_Switching;

                if (!hasOther || uOther > bestOther) { bestOther = uOther; hasOther = true; }
            }

            bestOther = Mathf.Max(0f, bestOther + epsilon);

            int dHere = Manhattan(w.HomeCell, workCell);
            float rhs = bestOther + Beta_Distance * dHere + ((w.CurrentSlot >= 0) ? Theta_Switching : 0f);

            // Subsidy_min = (rhs - κ·BaseAttr) / α
            float sReq = (rhs - Kappa_Attract * baseAttr) / Mathf.Max(0.0001f, Alpha_Subsidy);
            demands.Add(Mathf.Max(0f, sReq));
        }

        demands.Sort();
        if (demands.Count < need) return float.PositiveInfinity; // 候选不足：再怎么补贴也满不了

        for (int i = 0; i < need; i++) curve.Add(demands[i]);
        return curve[curve.Count - 1];
    }
}

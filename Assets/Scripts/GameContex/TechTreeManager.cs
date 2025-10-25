// Assets/Scripts/GameContex/TechTreeManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 科技树运行时管理器：
/// 1) 从 TechTreeAssets 资源初始化节点
/// 2) 记录与查询解锁状态
/// 3) 计算“当前可研究”的节点（依赖满足、未解锁、未在研究中）
/// 4) 维护“当前正在研究”的节点列表
/// 5) 查询研究进度（单个或全部）
/// 额外：开始/取消研究、投入研究点、导入/导出存档状态。
/// </summary>
public class TechTreeManager
{

    public event Action<TechNodeData> ResearchStarted;
 
    public event Action<TechNodeData> ResearchCompleted;

    // —— 数据源（编辑器里配的 ScriptableObject）——
    private TechTreeAssets _tree; // 引用到你的 TechTreeAssets 资源（ScriptableObject）

    // —— 运行时状态 —— 
    private readonly Dictionary<string, TechNodeData> _nodes =
        new Dictionary<string, TechNodeData>(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _unlocked =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ResearchTask> _researching =
        new Dictionary<string, ResearchTask>(StringComparer.OrdinalIgnoreCase);

    /// <summary>可选：记录一个“起始节点ID”（若需要）</summary>
    public string StartingNodeId { get; private set; } = string.Empty;

    //========================== 对外主功能 ==========================




    /// <summary>
    /// 1) 通过 TechTreeAssets 资源初始化科技树。
    /// 可传入一批已解锁的ID（如来自存档）。
    /// </summary>
    public void Init(IEnumerable<string> preUnlockedIds = null, string startingNodeId = null)
    {


        _tree = ResourceRouting.Instance.treeAssets;

        _nodes.Clear();
        foreach (var t in _tree.techList)
        {
            if (t == null || string.IsNullOrWhiteSpace(t.id)) continue;
            _nodes[t.id] = t;
        }

        _unlocked.Clear();
        if (preUnlockedIds != null)
        {
            foreach (var id in preUnlockedIds)
            {
                if (!string.IsNullOrWhiteSpace(id) && _nodes.ContainsKey(id))
                    _unlocked.Add(id);
            }
        }

        _researching.Clear();

        // 设定起始节点（若未指定，则挑第一个“无依赖”的作为起点，若存在）
        StartingNodeId = startingNodeId ?? FindFirstRootId();
    }

    /// <summary>
    /// 2) 查询某节点是否已解锁。
    /// </summary>
    public bool IsUnlocked(string techId)
    {
        if (string.IsNullOrWhiteSpace(techId)) return false;
        return _unlocked.Contains(techId);
    }

    /// <summary>
    /// 3) 获取当前可研究的节点（依赖满足 + 未解锁 + 未在研究中）。
    /// </summary>
    public List<TechNodeData> GetResearchableNodes()
    {
        EnsureTreeBound();

        // TechTreeAssets 已内置“依赖满足 → 可研究”的判定与筛选
        // 参见 TechTreeAssets.AreDependenciesMet / GetAvailableTechs
        var available = _tree.GetAvailableTechs(_unlocked); // 依赖满足但未解锁的列表
        // 过滤掉已经在研究中的
        return available.Where(t => !_researching.ContainsKey(t.id)).ToList();
    }

    /// <summary>
    /// 4) 获取当前正在研究的节点（按加入顺序）。
    /// </summary>
    public List<TechNodeData> GetResearchingNodes()
    {
        return _researching.Values.Select(r => r.Node).ToList();
    }

    /// <summary>
    /// 5) 获取某个节点的当前研究进度（0~1）。
    /// 已解锁返回 1。
    /// 未在研究且未解锁返回 0。
    /// </summary>
    public float GetResearchProgress(string techId)
    {
        if (string.IsNullOrWhiteSpace(techId)) return 0f;
        if (_unlocked.Contains(techId)) return 1f;
        if (_researching.TryGetValue(techId, out var task))
            return task.ProgressRatio;
        return 0f;
    }

    /// <summary>
    /// （便捷）获取所有正在研究节点的进度快照。
    /// </summary>
    public Dictionary<string, float> GetAllResearchProgress()
    {
        var dict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in _researching)
            dict[kv.Key] = kv.Value.ProgressRatio;
        return dict;
    }

    //========================== 进阶/常用操作（可选） ==========================

    /// <summary>
    /// 开始研究指定节点。
    /// - 要求：存在、未解锁、依赖满足、未在研究中
    /// - cost<=0 将直接解锁
    /// </summary>
    public bool StartResearch(string techId)
    {
        EnsureTreeBound();
        if (!_nodes.TryGetValue(techId, out var node)) return false;
        if (_unlocked.Contains(techId)) return false;

        // 依赖满足校验（TechTreeAssets 自带方法）
        if (!_tree.AreDependenciesMet(techId, _unlocked)) return false;

        if (_researching.ContainsKey(techId)) return false;

        if (node.cost <= 0)
        {
            // 零成本：直接解锁
            UnlockInternal(techId);
            return true;
        }

        _researching[techId] = new ResearchTask(node);

        ResearchStarted?.Invoke(node);
        return true;
    }

    /// <summary>
    /// 取消当前的研究（不清空进度；若需要可把 keepProgress 设为 false）。
    /// </summary>
    public bool CancelResearch(string techId, bool keepProgress = true)
    {
        if (!_researching.TryGetValue(techId, out var task)) return false;
        if (!keepProgress)
        {
            task.Accumulated = 0;
        }
        _researching.Remove(techId);
        return true;
    }

    /// <summary>
    /// 为指定节点投入研究点（例如每回合/每秒产出的科研值）。
    /// 当达到或超过 cost 时自动解锁。
    /// 返回：是否已在本次投放后解锁。
    /// </summary>
    public bool ContributeResearch(string techId, int points)
    {
        if (points <= 0) return false;
        if (_unlocked.Contains(techId)) return true; // 已解锁

        if (!_researching.TryGetValue(techId, out var task))
        {
            // 如果依赖满足并且没在研究，允许快捷开始
            if (!StartResearch(techId)) return false;
            if (!_researching.TryGetValue(techId,out task))
            {
                return true;
            }
        }

        task.Accumulated += points;

        if (task.Accumulated >= task.Cost)
        {
            UnlockInternal(techId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 批量平均分配研究点到所有在研项目（适合“被动产出科研点”的场景）。
    /// </summary>
    public void DistributeResearchPoints(int totalPoints)
    {
        if (totalPoints <= 0) return;
        int count = _researching.Count;
        if (count == 0) return;

        int per = Math.Max(1, totalPoints / count);
        // 粗略平均；余数忽略或可做更复杂分配策略
        foreach (var id in _researching.Keys.ToList())
        {
            ContributeResearch(id, per);
        }
    }

    /// <summary>
    /// 直接解锁（用于剧情/奖励等）。
    /// </summary>
    public bool ForceUnlock(string techId)
    {
        if (!_nodes.ContainsKey(techId)) return false;
        UnlockInternal(techId);
        return true;
    }

    /// <summary>
    /// （存档）导出当前状态：已解锁集合 + 正在研究的进度
    /// </summary>
    public SaveData ExportState()
    {
        var sd = new SaveData
        {
            unlocked = _unlocked.ToList(),
            researching = _researching.Values.Select(r => new SaveData.ResearchingItem
            {
                id = r.Node.id,
                accumulated = r.Accumulated
            }).ToList()
        };
        return sd;
    }

    /// <summary>
    /// （读档）导入状态：恢复已解锁与在研进度（需在 Init 之后调用）
    /// </summary>
    public void ImportState(SaveData data)
    {
        if (data == null) return;

        _unlocked.Clear();
        foreach (var id in data.unlocked)
            if (_nodes.ContainsKey(id)) _unlocked.Add(id);

        _researching.Clear();
        if (data.researching != null)
        {
            foreach (var item in data.researching)
            {
                if (!_nodes.TryGetValue(item.id, out var node)) continue;
                if (_unlocked.Contains(item.id)) continue; // 已经解锁则跳过
                var task = new ResearchTask(node) { Accumulated = Math.Max(0, item.accumulated) };
                if (task.Accumulated >= task.Cost)
                {
                    UnlockInternal(item.id);
                }
                else
                {
                    _researching[item.id] = task;
                }
            }
        }
    }

    //========================== 查询/工具 ==========================

    public bool HasNode(string id) => !string.IsNullOrWhiteSpace(id) && _nodes.ContainsKey(id);

    public bool TryGetNode(string id, out TechNodeData node) =>
        _nodes.TryGetValue(id ?? string.Empty, out node);

    public IEnumerable<TechNodeData> GetAllNodes() => _nodes.Values;

    public IReadOnlyCollection<string> GetUnlockedIds() => _unlocked;

    public bool IsResearching(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _researching.ContainsKey(id);
    }

//========================== 内部实现 ==========================

    private void UnlockInternal(string techId)
    {
        if (!_unlocked.Add(techId))
        {
            _researching.Remove(techId);
            return;
        }

        _researching.Remove(techId);
        if (_nodes.TryGetValue(techId,out var node))
        {
            ResearchCompleted?.Invoke(node);
        }

    }

    private void EnsureTreeBound()
    {
        if (_tree == null)
            throw new InvalidOperationException("TechTreeManager 还未 Init，请先调用 Init(TechTreeAssets ...)。");
    }

    private string FindFirstRootId()
    {
        foreach (var kv in _nodes)
        {
            var node = kv.Value;
            if (node.dependencies == null || node.dependencies.Count == 0)
                return node.id;
        }
        return string.Empty;
    }

    //========================== 内部结构体 & 存档结构 ==========================

    private sealed class ResearchTask
    {
        public TechNodeData Node { get; }
        public int Cost { get; }
        public int Accumulated;

        public float ProgressRatio => Cost <= 0 ? 1f : Mathf.Clamp01(Accumulated / (float)Cost);

        public ResearchTask(TechNodeData node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Cost = Math.Max(0, node.cost);
            Accumulated = 0;
        }
    }

    [Serializable]
    public class SaveData
    {
        public List<string> unlocked = new List<string>();

        [Serializable]
        public class ResearchingItem
        {
            public string id;
            public int accumulated;
        }

        public List<ResearchingItem> researching = new List<ResearchingItem>();
    }
}

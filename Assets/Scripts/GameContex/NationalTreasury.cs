using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 国库管理（基于 TreasuryItem）
/// 约定：数量不可为负；新增/删除类型通过创建或移除 TreasuryItem 资源来完成。
/// </summary>
[Serializable]
public class NationalTreasury
{
    [Header("资源目录（放在 Resources 下更方便）")]
    [SerializeField] private TreasuryCatalog catalog;

    [Header("初始已知资源（可空；若留空则从 Catalog 全量初始化为0）")]
    [SerializeField] private List<TreasuryItem> knownItems = new List<TreasuryItem>();

    // 运行时数量表
    private Dictionary<TreasuryItem, int> _amountByItem;

    /// <summary> 初始化：将 knownItems 或 Catalog 中的条目全部置0，避免 KeyNotFound </summary>
    public void Init()
    {
        _amountByItem = new Dictionary<TreasuryItem, int>();

        // 如果没有手动指定 knownItems，则用 Catalog 全量
        var seed = knownItems != null && knownItems.Count > 0
            ? knownItems
            : (catalog != null ? new List<TreasuryItem>(catalog.AllItems) : new List<TreasuryItem>());

        foreach (var t in seed)
        {
            if (t == null) continue;
            _amountByItem[t] = 0;
        }
    }

    private void EnsureInit()
    {
        if (_amountByItem == null) Init();
    }

    // ========================
    //        增 删 改 查
    // ========================

    /// <summary> 查：获取数量（无则视为0） </summary>
    public int Get(TreasuryItem item)
    {
        EnsureInit();
        if (item == null) return 0;
        return _amountByItem.TryGetValue(item, out var v) ? v : 0;
    }

    /// <summary> 改：设置数量（必须 >= 0） </summary>
    public bool Set(TreasuryItem item, int value)
    {
        EnsureInit();
        if (item == null || value < 0) return false;
        _amountByItem[item] = value;
        return true;
    }

    /// <summary> 增：增加正数 </summary>
    public bool Add(TreasuryItem item, int delta)
    {
        if (delta < 0) return false;
        return Change(new TreasuryChange(item, delta));
    }

    /// <summary> 减：消耗正数 </summary>
    public bool Spend(TreasuryItem item, int delta)
    {
        if (delta < 0) return false;
        return Change(new TreasuryChange(item, -delta));
    }

    /// <summary> 删：清空为0 </summary>
    public void Clear(TreasuryItem item)
    {
        EnsureInit();
        if (item == null) return;
        _amountByItem[item] = 0;
    }

    /// <summary> 是否足够 </summary>
    public bool HasEnough(TreasuryItem item, int need)
    {
        if (item == null || need < 0) return false;
        return Get(item) >= need;
    }

    /// <summary>
    /// 核心：按增量变更（支持正负）。若结果为负则失败且不变更。
    /// </summary>
    public bool Change(TreasuryChange change)
    {
        EnsureInit();
        if (change.Item == null) return false;

        var cur = Get(change.Item);
        long next = (long)cur + change.Amount; // 防溢出
        if (next < 0) return false;

        _amountByItem[change.Item] = (int)next;
        return true;
    }

    // ==============
    // 扩展：批量操作
    // ==============

    public bool TryBatchChange(IEnumerable<TreasuryChange> changes)
    {
        EnsureInit();
        if (changes == null) return true;

        // 1) 预演
        var preview = new Dictionary<TreasuryItem, long>();
        foreach (var kv in _amountByItem) preview[kv.Key] = kv.Value;

        foreach (var c in changes)
        {
            if (c.Item == null) return false;
            if (!preview.ContainsKey(c.Item)) preview[c.Item] = 0;
            preview[c.Item] += c.Amount;
            if (preview[c.Item] < 0) return false; // 任一为负则失败
        }

        // 2) 提交
        foreach (var p in preview)
            _amountByItem[p.Key] = (int)p.Value;

        return true;
    }

    public bool TrySpendMany(params TreasuryChange[] costs)
    {
        if (costs == null) return true;
        var list = new List<TreasuryChange>(costs.Length);
        foreach (var c in costs)
        {
            if (c.Item == null || c.Amount < 0) return false; // 传入的cost应为正数
            list.Add(new TreasuryChange(c.Item, -c.Amount));
        }
        return TryBatchChange(list);
    }

    /// <summary> 获取只读快照（用于UI展示/调试） </summary>
    public IReadOnlyDictionary<TreasuryItem, int> GetSnapshot()
    {
        EnsureInit();
        return new Dictionary<TreasuryItem, int>(_amountByItem);
    }

    // ==============
    // 存档/读档（用 Id）
    // ==============

    [Serializable]
    public struct SaveItem
    {
        public string id;   // TreasuryItem.Id
        public int amount;  // 数量
    }

    public List<SaveItem> ToSave()
    {
        EnsureInit();
        var list = new List<SaveItem>(_amountByItem.Count);
        foreach (var kv in _amountByItem)
        {
            if (kv.Key == null) continue;
            list.Add(new SaveItem { id = kv.Key.Id, amount = kv.Value });
        }
        return list;
    }

    /// <summary>
    /// 从存档恢复。需要 catalog 能通过 Id 找回 TreasuryItem。
    /// 未知 Id 将给出告警但不报错。
    /// </summary>
    public void FromSave(IEnumerable<SaveItem> items)
    {
        EnsureInit();
        if (catalog == null)
        {
            Debug.LogError("[NationalTreasury] FromSave失败：Catalog 未绑定。");
            return;
        }
        catalog.BuildIndex();

        foreach (var it in items)
        {
            var item = catalog.FindById(it.id);
            if (item != null)
            {
                _amountByItem[item] = Mathf.Max(0, it.amount);
            }
            else
            {
                Debug.LogWarning($"[NationalTreasury] 未知的 TreasuryItem Id: {it.id}");
            }
        }
    }
}

/// <summary>
/// 变更结构（与原 GlobalResourcesAmount 类似）：Item + 增量
/// </summary>
[Serializable]
public struct TreasuryChange
{
    public TreasuryItem Item;
    public int Amount;
    public TreasuryChange(TreasuryItem item, int amount)
    {
        Item = item;
        Amount = amount;
    }
}

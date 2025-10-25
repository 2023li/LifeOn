using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Treasury Catalog", fileName = "TreasuryCatalog")]
public class TreasuryCatalog : ScriptableObject
{
    [SerializeField] private List<TreasuryItem> allItems = new List<TreasuryItem>();
    public IReadOnlyList<TreasuryItem> AllItems => allItems;

    private Dictionary<string, TreasuryItem> _byId;

    public void BuildIndex()
    {
        _byId = new Dictionary<string, TreasuryItem>();
        foreach (var it in allItems)
        {
            if (it == null) continue;
            if (string.IsNullOrWhiteSpace(it.Id)) continue;
            if (_byId.ContainsKey(it.Id))
            {
                Debug.LogError($"[TreasuryCatalog] Duplicate Id: {it.Id} in {name}");
                continue;
            }
            _byId[it.Id] = it;
        }
    }

    public TreasuryItem FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_byId == null) BuildIndex();
        return _byId.TryGetValue(id, out var it) ? it : null;
    }
}

// Assets/Game/Scripts/Runtime/Inventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    [Serializable] private class Entry { public SupplyDef R; public int Amount; }
    [SerializeField] private List<Entry> entries = new List<Entry>();
    public int Capacity;

    
    public int TotalQuantity
    {
        get { int t = 0; foreach (var e in entries) t += e.Amount; return t; }
    }

    public int GetAmount(SupplyDef r)
    {
        var e = entries.Find(x => x.R == r);
        return e == null ? 0 : e.Amount;
    }

    public void Add(SupplyAmount[] items)
    {
        foreach (var it in items) Add(it.Resource, it.Amount);
    }


    public void Add(SupplyDef r, int amount)
    {
        int free = Mathf.Max(0, Capacity - TotalQuantity);
        int add = Mathf.Min(amount, free);
        if (add <= 0) return;

        var e = entries.Find(x => x.R == r);
        if (e == null) { e = new Entry { R = r, Amount = 0 }; entries.Add(e); }
        e.Amount += add;

        Debug.Log("添加完成");
    }


    public IEnumerable<SupplyAmount> EnumerateContents()
    {
        foreach (var entry in entries)
        {
            if (entry == null || entry.R == null || entry.Amount <= 0)
            {
                continue;
            }

            yield return new SupplyAmount
            {
                Resource = entry.R,
                Amount = entry.Amount
            };
        }
    }


    public void Consume(SupplyAmount[] costs)
    {
        foreach (var c in costs)
        {
            var e = entries.Find(x => x.R == c.Resource);
            if (e != null) e.Amount = Mathf.Max(0, e.Amount - c.Amount);
        }
    }
}

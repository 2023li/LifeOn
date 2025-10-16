// Assets/Game/Scripts/Runtime/Inventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    [Serializable] private class Entry { public ResourceDef R; public int Amount; }
    [SerializeField] private List<Entry> entries = new List<Entry>();
    public int Capacity;

    public int TotalQuantity
    {
        get { int t = 0; foreach (var e in entries) t += e.Amount; return t; }
    }

    public int GetAmount(ResourceDef r)
    {
        var e = entries.Find(x => x.R == r);
        return e == null ? 0 : e.Amount;
    }

    public void Add(ResourceAmount[] items)
    {
        foreach (var it in items) Add(it.Resource, it.Amount);
    }

    public void Add(ResourceDef r, int amount)
    {
        var e = entries.Find(x => x.R == r);
        if (e == null) { e = new Entry { R = r, Amount = 0 }; entries.Add(e); }
        e.Amount = Mathf.Clamp(e.Amount + amount, 0, Capacity);
    }

    public void Consume(ResourceAmount[] costs)
    {
        foreach (var c in costs)
        {
            var e = entries.Find(x => x.R == c.Resource);
            if (e != null) e.Amount = Mathf.Max(0, e.Amount - c.Amount);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class HumanResourcesNetwork
{
    private readonly Dictionary<BuildingInstance, int> _pop = new();
    private readonly Dictionary<BuildingInstance, int> _work = new();
    private int _totalPop, _totalWork;

    public int TotalPopulation => _totalPop;
    public int TotalWorkers => _totalWork;
    public int Unemployed => Mathf.Max(0, _totalPop - _totalWork);

    public void RegisterOrUpdate(BuildingInstance b)
    {
        if (b == null) return;
        int newPop = Mathf.Max(0, b.CurrentPopulation);
        int newWk = Mathf.Clamp(b.CurrentWorkers, 0, newPop);

        if (newPop == 0 && newWk == 0) { Unregister(b); return; }

        int oldPop = _pop.TryGetValue(b, out var p) ? p : 0;
        int oldWk = _work.TryGetValue(b, out var w) ? w : 0;

        _pop[b] = newPop;
        _work[b] = newWk;
        _totalPop += (newPop - oldPop);
        _totalWork += (newWk - oldWk);
        if (_totalPop < 0) _totalPop = 0;
        if (_totalWork < 0) _totalWork = 0;
    }

    public void Unregister(BuildingInstance b)
    {
        if (b == null) return;
        if (_pop.TryGetValue(b, out var pop)) { _totalPop -= pop; _pop.Remove(b); }
        if (_work.TryGetValue(b, out var wk)) { _totalWork -= wk; _work.Remove(b); }
        if (_totalPop < 0) _totalPop = 0;
        if (_totalWork < 0) _totalWork = 0;
    }

    public int TryAssignWorkers(BuildingInstance b, int request)
    {
        if (b == null || request <= 0) return 0;
        int pop = _pop.TryGetValue(b, out var p) ? p : 0;
        int cur = _work.TryGetValue(b, out var w) ? w : 0;

        int room = Mathf.Max(0, pop - cur);
        int take = Mathf.Clamp(request, 0, Mathf.Min(Unemployed, room));
        if (take <= 0) return 0;

        b.CurrentWorkers = cur + take; // 由属性触发 RegisterOrUpdate
        return take;
    }

    public int ReleaseWorkers(BuildingInstance b, int count)
    {
        if (b == null || count <= 0) return 0;
        int cur = _work.TryGetValue(b, out var w) ? w : 0;
        int rel = Mathf.Clamp(count, 0, cur);
        if (rel <= 0) return 0;

        b.CurrentWorkers = cur - rel; // 由属性触发 RegisterOrUpdate
        return rel;
    }

    public (int population, int workers) GetBuildingStats(BuildingInstance b)
    {
        int pop = _pop.TryGetValue(b, out var p) ? p : 0;
        int wk = _work.TryGetValue(b, out var w) ? w : 0;
        return (pop, wk);
    }
}

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


    public void Register(BuildingInstance building)
    {
        if (!_pop.ContainsKey(building))
        {
            _pop.Add(building, building.CurrentPopulation);
        }
        if (!_work.ContainsKey(building))
        {
            _work.Add(building,building.CurrentWorkers);
        }
        building.OnStateChanged += Handle_BuildingStateChange;
    }
    public void UnRegister(BuildingInstance building)
    {
        if (_pop.ContainsKey(building))
        {
            _pop.Remove(building);
        }
        if (_work.ContainsKey(building))
        {
            _work.Remove(building);
        }
        building.OnStateChanged -= Handle_BuildingStateChange;
    }



    private void Handle_BuildingStateChange(BuildingInstance building,BuildingStateValueType type)
    {
        switch (type)
        { 
            case BuildingStateValueType.CurrentPopulation:
                break;
            case BuildingStateValueType.CurrentWorkers:
                break;

        }
    }


}

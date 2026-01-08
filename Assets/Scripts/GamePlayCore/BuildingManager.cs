using System;
using System.Collections.Generic;
using System.Linq;
using Moyo.Unity;
using UnityEditor.VersionControl;
using UnityEngine;



/*
 * buff的叠加类型需要在Rule中自定义 目前采用的是无条件叠加
 * 1.如果我有 5 个“发电站”，它们都提供 BuffId = "PowerSupply"。处于它们共同覆盖范围的建筑，会获得 5 个 "PowerSupply" Rule。这是预期的
 * 
 */

[Serializable]
public class Buff
{
 
    public enum BuffCoverageType
    {
        无视阻力= 0,
        受阻力影响 = 1,
    }

    // 用于逻辑判断 Buff 是否相同（例如 "Power_Tier1"）
    public string BuffId;

    // 提供者实例（源头）
    public BuildingInstance Provider;

    public BuffCoverageType CoverageType = BuffCoverageType.受阻力影响;

    public int Radius;

    // 具体的规则效果（核心逻辑）
    public Rule EffectRule;

    // 缓存该 Buff 覆盖的坐标区域（方便移除时快速索引）
    public List<CubeCoor> CoveredCells;

    public Condition AcceptanceCondition;

    public Buff(string id, BuildingInstance provider, Rule rule, BuffCoverageType coverageType, int radius)
    {
        BuffId = id;
        Provider = provider;
        EffectRule = rule;
        CoveredCells = new List<CubeCoor>();
        CoverageType = coverageType;
        Radius = radius;
    }

    // 辅助判断：是否是同一个来源产生的同一个Buff
    public bool IsSameInstance(Buff other)
    {
        if (other == null) return false;
        return this.Provider == other.Provider && this.BuffId == other.BuffId;
    }

    public List<CubeCoor> CalculateInfluence()
    {
        switch (CoverageType)
        {
            case BuffCoverageType.无视阻力:
                CoveredCells = CoordinateCalculator.CellsInRadius(Provider.Self_CurrentCenterInGrid,Radius);
                break;
            case BuffCoverageType.受阻力影响:
                CoveredCells = CoordinateCalculator.GetReachableCellsByMovePower(Provider, Radius);
                break;
            default:
                Debug.LogWarning($"Buff {CoverageType}覆盖类型未得到处理，默认使用 无视阻力");
                CoveredCells = CoordinateCalculator.CellsInRadius(Provider.Self_CurrentCenterInGrid, Radius);
                break;
        }

        return CoveredCells;
    }

}



public class BuildingManager : Singleton<BuildingManager> 
{
    // 核心数据结构：空间坐标 -> 该坐标上存在的 Buff 列表
    private Dictionary<CubeCoor, List<Buff>> _spatialBuffMap = new Dictionary<CubeCoor, List<Buff>>();

    // 辅助索引：提供者 -> 它发布的所有 Buff（方便该建筑销毁时批量移除）
    private Dictionary<BuildingInstance, List<Buff>> _providerRegistry = new Dictionary<BuildingInstance, List<Buff>>();


    public void Init()
    {
        GridSystem.Instance.OnMapChange += HandleMapChange;
    }

    public void Clear()
    {

    }

    /// <summary>
    /// [提供者调用] 注册一个范围 Buff
    /// </summary>
    /// <param name="provider">提供者建筑</param>
    /// <param name="ruleTemplate">规则模板（会被克隆）</param>
    /// <param name="radius">半径</param>
    /// <param name="buffId">Buff标识ID</param>
    public void RegisterBuffSource(Buff newBuff)
    {
        // 2. 计算覆盖范围 (利用你现有的 CoordinateCalculator)
        // 注意：通常 Buff 是基于建筑中心点向外辐射
        var affectedCells = newBuff.CalculateInfluence();

        // 3. 写入空间映射表
        foreach (var cell in affectedCells)
        {
            if (!_spatialBuffMap.ContainsKey(cell))
            {
                _spatialBuffMap[cell] = new List<Buff>();
            }
            _spatialBuffMap[cell].Add(newBuff);
        }

        // 4. 记录到提供者名下
        if (!_providerRegistry.ContainsKey(newBuff.Provider))
        {
            _providerRegistry[newBuff.Provider] = new List<Buff>();
        }
        _providerRegistry[newBuff.Provider].Add(newBuff);

        // 5. (可选) 通知区域内的建筑刷新，或者等待回合更新时统一拉取
        NotifyBuildingsInCells(affectedCells);
    }



    // 在 BuildingManager.cs 中添加
    public void RefreshBuff(Buff buff)
    {
        // 1. 记录旧范围用于通知
        List<CubeCoor> oldCells = new List<CubeCoor>(buff.CoveredCells);

        // 2. 从空间字典中【完全移除】旧记录
        foreach (var cell in oldCells)
        {
            if (_spatialBuffMap.ContainsKey(cell))
            {
                _spatialBuffMap[cell].Remove(buff);
                if (_spatialBuffMap[cell].Count == 0) _spatialBuffMap.Remove(cell);
            }
        }

        // 3. 重新计算范围
        List<CubeCoor> newCells = buff.CalculateInfluence();

        // 4. 将新记录【写入】空间字典
        foreach (var cell in newCells)
        {
            if (!_spatialBuffMap.ContainsKey(cell))
            {
                _spatialBuffMap[cell] = new List<Buff>();
            }
            _spatialBuffMap[cell].Add(buff);
        }

        // 5. 通知受影响的建筑（包括旧区域失去Buff的，和新区域获得Buff的）
        // 使用 Union 去重，避免同一个建筑被通知两次
        var allAffected = oldCells.Union(newCells).ToList();
        NotifyBuildingsInCells(allAffected);
    }

    // 修改 HandleMapChange 调用
    private void HandleMapChange(CubeCoor coor)
    {
        foreach (var buffs in _providerRegistry.Values)
        {
            // 这里的遍历建议改为 for 循环或拷贝列表，因为 RefreshBuff 可能会修改集合结构(视实现而定，安全起见)
            // 但目前 RefreshBuff 不修改 _providerRegistry，所以 foreach 是安全的
            foreach (var buff in buffs)
            {
                if (CoordinateCalculator.GetDistance(buff.Provider.Self_CurrentCenterInGrid, coor) <= buff.Radius)
                {
                    RefreshBuff(buff); // <--- 改为调用刷新方法
                }
            }
        }
    }





    /// <summary>
    /// [提供者调用] 移除该建筑提供的所有 Buff（例如建筑被销毁、降级、停电）
    /// </summary>
    public void UnregisterBuffsFromProvider(BuildingInstance provider)
    {
        if (provider == null || !_providerRegistry.ContainsKey(provider)) return;

        List<Buff> buffsToRemove = _providerRegistry[provider];

        foreach (var buff in buffsToRemove)
        {
            // 从空间表中清理
            foreach (var cell in buff.CoveredCells)
            {
                if (_spatialBuffMap.ContainsKey(cell))
                {
                    _spatialBuffMap[cell].Remove(buff);
                    // 清理空列表
                    if (_spatialBuffMap[cell].Count == 0)
                        _spatialBuffMap.Remove(cell);
                }
            }
            // 通知受影响的区域刷新
            NotifyBuildingsInCells(buff.CoveredCells);
        }

        _providerRegistry.Remove(provider);
    }


    /// <summary>
    /// [重构] 纯查询方法，不再直接修改 building
    /// </summary>
    public List<Buff> GetBuffsForBuilding(BuildingInstance building)
    {
        List<Buff> result = new List<Buff>();

        // 遍历建筑占用的所有格子
        foreach (var cell in building.Self_CurrentOccupy)
        {
            if (_spatialBuffMap.TryGetValue(cell, out List<Buff> buffsInCell))
            {
                foreach (var buff in buffsInCell)
                {
                    // 1. 排除自己给自己的 Buff (防止无限叠加或逻辑怪圈，看设计需求)
                    if (buff.Provider == building) continue;

                    // 2. 检查 Buff 的接受条件 (Condition)
                    if (buff.AcceptanceCondition != null)
                    {
                       //不满足条件
                       if(!buff.AcceptanceCondition.Evaluate(building, building.Ctx, out string why))
                       {
                            Debug.Log(why);
                            continue;
                       }
                    }

                    // 3. 去重：防止因为占了多个格子而重复添加同一个 Buff 实例
                    if (result.Contains(buff)) continue;

                    /* * 4. [可选] 处理同 ID 互斥逻辑
                 * if (existingBuffIds.Contains(buff.BuffId)) {
                 * // 比较优先级或取舍逻辑
                 * continue;
                 * }
                 * existingBuffIds.Add(buff.BuffId);
                 */

                    result.Add(buff);
                }
            }
        }
        return result;
    }


    // 辅助：通知区域内的建筑脏标记或立即刷新
    private void NotifyBuildingsInCells(List<CubeCoor> cells)
    {
        HashSet<BuildingInstance> dirtyBuildings = new HashSet<BuildingInstance>();

        foreach (CubeCoor cell in cells)
        {
            if (BuildingInstance.TryGetBuildingAtCell(cell, out BuildingInstance target))
            {
                dirtyBuildings.Add(target); // HashSet 自动去重
            }
        }

        foreach (var building in dirtyBuildings)
        {
            building.SyncExternalBuffs();
        }
    }




}

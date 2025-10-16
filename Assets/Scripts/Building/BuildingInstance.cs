using UnityEngine;
using System.Collections.Generic;
using System;

public class BuildingInstance : MonoBehaviour
{
    public string InstanceId { get; private set; } = Guid.NewGuid().ToString("N");
    public BuildingArchetype Def;
    public int LevelIndex { get; private set; } = 0; // 对应 Def.Levels 索引

    public int Population { get; set; }
    public int Exp { get; set; }
    public Inventory Storage { get; private set; } // 仅仓库使用

    public BuildingInstance AssignedStorage;       // 非仓库：从此仓库拉取资源

    public Vector3Int[] Occupy { get; private set; } // 由放置系统设置

    public Vector3 CenterInGrid { get; private set; }

    private IGameContext _ctx;

    public int WorkersAssigned; // 当前分配在该建筑的工人数量

    

    public void Initialize(BuildingArchetype def)
    {
        Def = def;
        _ctx = FindObjectOfType<GameContext>();
        TryInitStorageIfAny();
    }

    private void OnEnable()
    {
        TurnSystem.OnTurnEnd += FireRules;
    }

    private void OnDisable()
    {
        TurnSystem.OnTurnEnd -= FireRules;
        if (Storage != null) _ctx.ResourceNetwork.UnregisterStorage(Storage);
    }

    void TryInitStorageIfAny()
    {
        var lvl = Def.Levels[LevelIndex];
        if (lvl.BaseStorageCapacity > 0)
        {
            Storage = new Inventory { Capacity = lvl.BaseStorageCapacity };
            _ctx.ResourceNetwork.RegisterStorage(Storage);
        }
    }

   

    public void FireRules(TurnPhase trigger)
    {
        BuildingLevelDef lvl = Def.Levels[LevelIndex];
        foreach (Rule r in lvl.Rules)
        {
            if (r.Trigger != trigger) { continue; }
            bool ok = true; string why = "";
            foreach (Condition c in r.Conditions)
            {
                if (!c.Evaluate(this, _ctx, out why))
                {
                    ok = false; break;
                }
            }
            List<Effect> effects = ok ? r.OnSuccess : r.OnFailure;

            foreach (var e in effects)
            {
                e.Apply(this, _ctx);
            }
        }

        // 升级自动化（也可仅靠规则里放 Upgrade 效果）
        if (lvl.ExpToNext > 0 && Exp >= lvl.ExpToNext)
        {
            TryUpgrade(_ctx);
        }
    }

    public int GetMaxPopulation(IGameContext ctx)
    {
        BuildingLevelDef lvl = Def.Levels[LevelIndex];
        int max = lvl.BaseMaxPopulation;
        foreach (var sm in lvl.ConditionalStatModifiers)
        {
            max = sm.Modify(this, ctx, max);
        }
        return max;
    }

    public bool TryUpgrade(IGameContext ctx)
    {
        BuildingLevelDef cur = Def.Levels[LevelIndex];
        if (cur.ExpToNext <= 0) return false; // 无可升
        if (Exp < cur.ExpToNext) return false;

        LevelIndex = Mathf.Min(LevelIndex + 1, Def.Levels.Count - 1);
        Exp = 0;

        // 重新初始化等级相关组件（例如容量变化）
        if (Storage != null)
        {
            ctx.ResourceNetwork.UnregisterStorage(Storage);
        }

        TryInitStorageIfAny();

        // 等级变化后的瞬时触发（可选）
        return true;
    }


    public int GetMaxJobs(IGameContext ctx)
    {
        var lvl = Def.Levels[LevelIndex];
        return Mathf.Max(0, lvl.BaseMaxJobs);
    }
    public void AssignWorkers(int count)
    {
        WorkersAssigned = Mathf.Clamp(count, 0, GetMaxJobs(null));
    }
}

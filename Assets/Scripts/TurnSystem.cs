using System;
using Moyo.Unity;
using UnityEngine;






public enum TriggerPhase
{
    TurnEnd,
    OnConstructionComplete,
    OnPlaced,
    OnRemoved
}



/// <summary>
/// 回合系统（End→Start 模式）
/// - 每次调用 NextTurn():
///   1) 结算当前回合 T 的所有阶段（就业/产出/…/经济/人口/总结）
///   2) 触发 OnTurnEnd(T)
///   3) 立即进入下一回合 T+1 的 OnTurnStart(T+1) 与 OnPhasePolicies(T+1)
/// - 初次启动时，若没有激活回合，可调用 StartFirstTurn() 或勾选 autoStartOnAwake 以自动触发 T=1 的开始阶段。
/// </summary>
[AddComponentMenu("LifeOn/Turn System")]
public class TurnSystem : MonoSingleton<TurnSystem>
{
    public static event Action OnTurnEnd;

    // 供 UI 或系统调用：结束本回合
    public void EndTurn()
    {
        OnTurnEnd?.Invoke();
    }
}

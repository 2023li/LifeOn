using System;
using Moyo.Unity;
using Unity.VisualScripting;
using UnityEngine;






public enum TurnPhase
{

    //
    结束准备阶段,

    //消耗资源
    资源消耗阶段,

    //生产资源
    资源生产阶段,

    //数据结算
    回合结束阶段,




    开始准备阶段
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
    public static event Action<TurnPhase> OnTurnEnd;

    TurnPhase[] phases;
    protected override void Initialize()
    {
        base.Initialize();
        phases = (TurnPhase[])Enum.GetValues(typeof(TurnPhase));
    }

    // 供 UI 或系统调用：结束本回合
    public void EndTurn()
    {
        foreach (TurnPhase Phase in phases)
        {
            OnTurnEnd?.Invoke(Phase);
        }


    }

    

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Moyo.Unity;
using UnityEngine;

public enum TurnPhase
{
    // 在这里已经移除了 动画播放阶段
    结束准备阶段,
    资源消耗阶段,
    资源生产阶段,
    回合结束阶段, //用作数据整理 例如 计数
    开始准备阶段  //对
}
public class TurnSystem
{
    // --- 纯数据逻辑 ---
    public int NumberOfRounds
    {
        get; private set
;
    }

    // 事件
    public event
 Action<TurnPhase> OnTurnPhaseChange;
    // 参数为：当前阻塞的数量
    public event Action<int
> OnTurnBlockCountChanged;

    private
 TurnPhase[] _phases;

    // 【核心修改】使用 Dictionary<object, string>
    // Key: 谁发起的阻塞 (Source)
    // Value: 阻塞的原因 (Reason)
    private readonly Dictionary<object, string> _blockers = new Dictionary<object, string
>();

    public bool IsBlocked => _blockers.Count > 0
;

    // 获取当前所有阻塞原因的列表（用于 UI 显示调试信息）
    public List<string> GetBlockReasons()
 => _blockers.Values.ToList();

    public TurnSystem()
    {
        _phases = (TurnPhase[])Enum.GetValues(
typeof
(TurnPhase));
    }

    public void Reset()
    {
        NumberOfRounds =
0
;
        _blockers.Clear();
        // 重置后记得通知外部，阻塞数变为0
        OnTurnBlockCountChanged?.Invoke(
0
);
    }

    public void EndTurn()
    {
        if
 (IsBlocked)
        {
            // [可选] 可以在这里打印是谁卡住了回合
            foreach (var kvp in
 _blockers)
                Debug.Log(
$"无法结束回合，阻塞源: {kvp.Key}, 原因: {kvp.Value}"
);
            return
;
        }

        foreach (TurnPhase phase in
 _phases)
        {
            OnTurnPhaseChange?.Invoke(phase);

            if
 (phase == TurnPhase.结束准备阶段)
            {
                // 这里的 Key 我们使用一个临时的 object，或者使用 string 本身作为 Key (如果能保证唯一)
                // 推荐使用一个专用的 Token 对象
                AddTimedBlock(
1f, "回合间隙冷却"
);
            }
        }
        NumberOfRounds++;
    }

    #region 阻塞相关 API

    /// <summary>
    /// 注册一个阻塞
    /// </summary>
    /// <param name="source">阻塞源 (通常传 this)</param>
    /// <param name="reason">原因</param>
    public void RegisterBlock(object source, string reason)
    {
        if (source == null) return
;

        bool isNew = !_blockers.ContainsKey(source);

        // 字典特性：如果 Key 存在，会自动更新 Value (原因可能改变了)
        _blockers[source] = reason;

        if(isNew)
        {
            OnTurnBlockCountChanged?.Invoke(_blockers.Count);
            // Debug.Log($"[Turn] {source} 注册了阻塞: {reason}");
        }
    }

    /// <summary>
    /// 移除该对象发起的所有阻塞
    /// </summary>
    /// <param name="source">阻塞源 (通常传 this)</param>
    public void UnregisterBlock(object source)
    {
        if (source == null) return
;

        if(_blockers.Remove(source))
        {
            OnTurnBlockCountChanged?.Invoke(_blockers.Count);
            // Debug.Log($"[Turn] {source} 移除了阻塞");
        }
    }

    /// <summary>
    /// 添加一个定时自动移除的阻塞
    /// </summary>
    public void AddTimedBlock(float duration, string reason)
    {
        // 创建一个临时的“令牌”对象作为 Key，确保唯一性
        object token = new object();

        RegisterBlock(token, reason);

        RemoveBlockDelay(token, duration).Forget();
    }

    private async UniTaskVoid RemoveBlockDelay(object token, float duration)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        UnregisterBlock(token);
    }

    #endregion

    public TurnSystemSaveData Save()
    {
        return new TurnSystemSaveData { currentNumberOfRounds = NumberOfRounds };
    }

    public void Load(TurnSystemSaveData data)
    {
        if (data != null
)
            NumberOfRounds = data.currentNumberOfRounds;
    }

    internal void Clear()
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 回合阻塞信息
/// </summary>
public struct TurnBlock
{
    /// <summary>唯一 ID，用于手动移除</summary>
    public int id;

    /// <summary>阻塞原因（仅用于调试或 UI 显示）</summary>
    public string reason;

    /// <summary>
    /// 若为 null 则表示不会自动移除；
    /// 若有值，则表示在 durationSeconds 秒后自动移除。
    /// </summary>
    public float? durationSeconds;
}

[Serializable]
public class TurnSystemSaveData
{
    public int currentNumberOfRounds;
}

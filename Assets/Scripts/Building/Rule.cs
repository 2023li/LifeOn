using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 触发时机类型枚举，定义规则何时被触发。
/// </summary>
public enum RuleTrigger { OnBuilt, OnTick, OnResourceProduced, OnResourceConsumed /* 等其他事件，可扩展 */ }

/// <summary>
/// 表示一条触发-条件-效果规则。
/// </summary>
[Serializable]
public class Rule
{
    public RuleTrigger trigger;      // 规则触发时机
    public Condition condition;      // 规则条件（可为空表示无条件）
    public List<Effect> effects;     // 规则效果列表

    /// <summary>
    /// 检查当前规则是否在给定触发时机满足条件，若是则执行所有效果。
    /// </summary>
    public void CheckAndExecute(Building building, RuleTrigger currentTrigger)
    {
        if (currentTrigger != trigger) return;  // 触发类型不匹配，不执行

        // 检查条件（如果没有配置条件则视为满足）
        if (condition == null || condition.Evaluate(building))
        {
            // 条件成立，依次执行所有效果
            foreach (Effect effect in effects)
            {
                if (effect == null) continue;
                effect.Apply(building);
                // 若某个效果是升级，则升级后退出当前规则循环，避免旧等级继续执行后续规则
                if (effect is UpgradeToNextLevel)
                {
                    // 一旦升级建筑等级，停止处理本次的剩余规则，等待下个触发再按新等级规则执行
                    break;
                }
            }
        }
    }
}

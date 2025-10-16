using System.Collections.Generic;
using UnityEngine;

public static class EnvironmentService
{
    // 存储各种环境参数的字典
    private static Dictionary<string, float> envValues = new Dictionary<string, float>();

    /// <summary>
    /// 获取指定环境参数的当前值（未设置则返回0）。
    /// </summary>
    public static float GetValue(string key)
    {
        if (envValues.TryGetValue(key, out float val))
            return val;
        return 0f;
    }

    /// <summary>
    /// 设置某环境参数的值。
    /// </summary>
    public static void SetValue(string key, float value)
    {
        envValues[key] = value;
    }

    /// <summary>
    /// 调整（增减）某环境参数的值。
    /// </summary>
    public static void AdjustValue(string key, float delta)
    {
        float current = GetValue(key);
        envValues[key] = current + delta;
    }
}

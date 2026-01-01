using DG.Tweening;
using TMPro;
using UnityEngine;

public static class TMPExtensions
{
    // 定义一个扩展方法
    public static Tweener DOCounter(this TMP_Text target, int fromValue, int toValue, float duration,Ease ease = Ease.OutExpo)
    {
        // 使用 DOVirtual.Float 来驱动
        return DOVirtual.Float(fromValue, toValue, duration, (val) =>
        {
            target.text = Mathf.FloorToInt(val).ToString();
        }).SetTarget(target).SetEase(ease);
    }
}

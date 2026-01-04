using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using Moyo.Unity;

public class UIPanel_Load : PanelBase
{

    public override UILayer Layer => UILayer.Loading;

    [Header("UI Components")]
    [LabelText("进度条")]
    [SerializeField] private Slider progressBar;

    [LabelText("进度文本")]
    [SerializeField] private TMP_Text progressText;

    [LabelText("流程显示文本")]
    [SerializeField] private TMP_Text loadingProcessText;

    [LabelText("提示文本")]
    [SerializeField] private TMP_Text loadingTipText;

    [Header("Localization Data")]
    [LabelText("每次加载显示的流程步数")]
    public int stepsCount = 5;

    [LabelText("流程文案表 (Process Table)")]
    public LocalizedStringTable processStringTable;

    [LabelText("提示文案表 (Tips Table)")]
    public LocalizedStringTable tipsStringTable;

    // 运行时数据
    private List<string> _activeProcessTexts = new List<string>();

   

    public override bool Back(params object[] args)
    {
        // 加载界面通常不允许通过 Back 键关闭
        return true;
    }

    /// <summary>
    /// 初始化 UI 数据（异步加载本地化表格）
    /// 由 LoadTransition 在逻辑开始前调用
    /// </summary>
    public IEnumerator InitializeUIAsync()
    {
        // 1. 加载并显示随机提示 (Tips)
        if (loadingTipText != null && tipsStringTable != null && !tipsStringTable.IsEmpty)
        {
            AsyncOperationHandle<UnityEngine.Localization.Tables.StringTable> handle = tipsStringTable.GetTableAsync();
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var table = handle.Result;
                var allTips = table.Values.Select(v => v.LocalizedValue).Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (allTips.Count > 0)
                {
                    loadingTipText.text = allTips[Random.Range(0, allTips.Count)];
                }
            }
        }

        // 2. 加载并准备流程文案 (Process Flow)
        _activeProcessTexts.Clear();
        if (loadingProcessText != null && processStringTable != null && !processStringTable.IsEmpty)
        {
            var handle = processStringTable.GetTableAsync();
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var table = handle.Result;
                var allProcessTexts = table.Values.Select(v => v.LocalizedValue).Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (allProcessTexts.Count > 0)
                {
                    // 洗牌算法
                    for (int i = 0; i < allProcessTexts.Count; i++)
                    {
                        var temp = allProcessTexts[i];
                        int randomIndex = Random.Range(i, allProcessTexts.Count);
                        allProcessTexts[i] = allProcessTexts[randomIndex];
                        allProcessTexts[randomIndex] = temp;
                    }

                    // 取前 N 个
                    int count = Mathf.Min(stepsCount, allProcessTexts.Count);
                    for (int i = 0; i < count; i++)
                    {
                        _activeProcessTexts.Add(allProcessTexts[i]);
                    }

                    // 立即显示第一句
                    if (_activeProcessTexts.Count > 0) loadingProcessText.text = _activeProcessTexts[0];
                }
            }
        }
    }

    /// <summary>
    /// 更新进度视图
    /// </summary>
    /// <param name="progress">0.0 到 1.0</param>
    public void UpdateProgressView(float progress)
    {
        if (progressBar != null) progressBar.value = progress;
        if (progressText != null) progressText.text = $"{Mathf.FloorToInt(progress * 100)}%";

        // 根据进度更新流程文案
        if (loadingProcessText != null && _activeProcessTexts.Count > 0)
        {
            // 映射 progress (0-1) 到 列表索引 (0 - Count-1)
            int index = Mathf.FloorToInt(progress * _activeProcessTexts.Count);
            index = Mathf.Clamp(index, 0, _activeProcessTexts.Count - 1);
            loadingProcessText.text = _activeProcessTexts[index];
        }
    }

    /// <summary>
    /// 显示“按任意键继续”的提示
    /// </summary>
    public void ShowPressAnyKeyPrompt()
    {
        if (loadingProcessText != null)
        {
            // 这里也可以做成 Localization Key，或者直接写死
            loadingProcessText.text = "Loading Complete. Press any key to continue...";
        }
    }
}

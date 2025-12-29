using System.Collections;
using System.Collections.Generic;
using System.Linq; // 引入 Linq 方便处理列表
using System.Threading.Tasks;
using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 引入 Localization 命名空间
using UnityEngine.Localization;

public class LoadTransition : MonoBehaviour
{
    public static LoadTransition Instance { get; private set; }

    [Header("UI Elements")]
    [LabelText("进度条")]
    public Slider progressBar;

    [LabelText("进度文本")]
    public TMP_Text progressText;

    [Header("Process Text (Flow)")]
    [LabelText("流程显示文本")]
    public TMP_Text loadingProcessText;

    [LabelText("每次加载显示的步数")]
    public int stepsCount = 5;

    // --- 修改点 1: 替换为 LocalizedStringTable ---
    [LabelText("流程文案表 (Process Table)")]
    [Tooltip("请选择包含所有流程文案的 String Table Collection")]
    public LocalizedStringTable processStringTable;

    // 运行时生成的当前流程文案列表
    private List<string> _activeProcessTexts = new List<string>();

    [Header("Tips")]
    [LabelText("提示文本")]
    public TMP_Text loadingTipText;

    // --- 修改点 2: 替换为 LocalizedStringTable ---
    [LabelText("提示文案表 (Tips Table)")]
    [Tooltip("请选择包含所有提示文案的 String Table Collection")]
    public LocalizedStringTable tipsStringTable;

    [Header("Settings")]
    [LabelText("最小加载时间")]
    public float minLoadTime = 1.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- 修改点 3: 将 Start 改为 IEnumerator 以支持异步等待表格加载 ---
    private IEnumerator Start()
    {
        // 1. 隐藏全局 UI
        if (UIManager.Instance != null)
            UIManager.Instance.gameObject.SetActive(false);

        // 2. 初始化文本 (等待表格加载完成)
        yield return InitializeRandomTextsRoutine();

        // 3. 开始加载流程
        if (AppManager.Instance != null && AppManager.Instance.CurrentRequest != null)
        {
            StartCoroutine(ProcessLoadSequence(AppManager.Instance.CurrentRequest));
        }
        else
        {
            Debug.LogError("[LoadTransition] 缺少加载请求！");
        }
    }

    /// <summary>
    /// 协程：加载 String Table 并提取文本
    /// </summary>
    private IEnumerator InitializeRandomTextsRoutine()
    {
        // --- A. 随机提示 (Tips) ---
        if (loadingTipText != null && tipsStringTable != null && !tipsStringTable.IsEmpty)
        {
            // 异步获取表格
            var handle = tipsStringTable.GetTableAsync();
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                var table = handle.Result;
                // 获取所有非空的 Value
                var allTips = table.Values.Select(v => v.LocalizedValue).Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (allTips.Count > 0)
                {
                    loadingTipText.text = allTips[Random.Range(0, allTips.Count)];
                }
            }
        }

        // --- B. 随机流程 (Process Flow) ---
        _activeProcessTexts.Clear();
        if (loadingProcessText != null && processStringTable != null && !processStringTable.IsEmpty)
        {
            // 异步获取表格
            var handle = processStringTable.GetTableAsync();
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                var table = handle.Result;
                // 获取所有非空的 Value
                var allProcessTexts = table.Values.Select(v => v.LocalizedValue).Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (allProcessTexts.Count > 0)
                {
                    // 1. 洗牌
                    for (int i = 0; i < allProcessTexts.Count; i++)
                    {
                        var temp = allProcessTexts[i];
                        int randomIndex = Random.Range(i, allProcessTexts.Count);
                        allProcessTexts[i] = allProcessTexts[randomIndex];
                        allProcessTexts[randomIndex] = temp;
                    }

                    // 2. 取前 N 个
                    int count = Mathf.Min(stepsCount, allProcessTexts.Count);
                    for (int i = 0; i < count; i++)
                    {
                        _activeProcessTexts.Add(allProcessTexts[i]);
                    }

                    // 3. 立即显示第一句
                    if (_activeProcessTexts.Count > 0)
                    {
                        loadingProcessText.text = _activeProcessTexts[0];
                    }
                }
            }
        }
    }

    private IEnumerator ProcessLoadSequence(AppManager.SceneLoadContext request)
    {
        float startTime = Time.time;
        UpdateProgressUI(0f);

        // 阶段 1: 内存清理
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return null;

        // 阶段 2: 预加载
        Task preloadTask = Task.CompletedTask;
        if (request.PreloadAddresses != null && request.PreloadAddresses.Count > 0)
        {
            var tasks = new List<Task>();
            foreach (var addr in request.PreloadAddresses)
                tasks.Add(AssetsManager.Instance.LoadAssetAsync<object>(addr));
            preloadTask = Task.WhenAll(tasks);
        }

        // 阶段 3: 加载场景
        AsyncOperation sceneOp = SceneManager.LoadSceneAsync(request.TargetSceneName);
        sceneOp.allowSceneActivation = false;

        bool isPreloadDone = false;
        while (!sceneOp.isDone)
        {
            if (!isPreloadDone && preloadTask.IsCompleted) isPreloadDone = true;

            float sceneProgress = Mathf.Clamp01(sceneOp.progress / 0.9f);
            float assetsProgress = isPreloadDone ? 1f : 0.5f;
            float timeProgress = Mathf.Clamp01((Time.time - startTime) / minLoadTime);

            float totalProgress = Mathf.Min(sceneProgress, assetsProgress);
            float displayProgress = Mathf.Min(totalProgress, timeProgress);

            UpdateProgressUI(displayProgress);

            if (sceneOp.progress >= 0.9f && isPreloadDone && timeProgress >= 1f)
            {
                UpdateProgressUI(1f);
                yield return new WaitForSeconds(0.2f);

                if (UIManager.Instance != null) UIManager.Instance.gameObject.SetActive(true);
                sceneOp.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private void UpdateProgressUI(float value)
    {
        if (progressBar != null) progressBar.value = value;
        if (progressText != null) progressText.text = $"{Mathf.FloorToInt(value * 100)}%";

        // 更新流程文案
        if (loadingProcessText != null && _activeProcessTexts.Count > 0)
        {
            int index = Mathf.FloorToInt(value * _activeProcessTexts.Count);
            index = Mathf.Clamp(index, 0, _activeProcessTexts.Count - 1);

            loadingProcessText.text = _activeProcessTexts[index];
        }
    }
}

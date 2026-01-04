using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public class LoadTransition : MonoBehaviour
{
    public static LoadTransition Instance { get; private set; }

    public UIPanel_Load uiPanel; // 直接引用 UI 脚本

    [Title("Settings")]
    [LabelText("最小加载时间")]
    public float minLoadTime = 1.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

       
    }

    // 1. 修改为 async void
    private async void Start()
    {
        // 2. 使用 await 替代 .Result，避免死锁
        uiPanel = await UIManager.Instance.ShowPanel<UIPanel_Load>(UIManager.UILayer.Loading);

        // 2. 初始化 UI
        if (uiPanel != null)
        {
            // 如果 InitializeUIAsync 返回的是 Task/UniTask，也加上 await
            // 原代码是 yield return，这里改为 await
            await uiPanel.InitializeUIAsync();
        }

        // 3. 开始加载流程
        if (AppManager.Instance != null && AppManager.Instance.CurrentRequest != null)
        {
            // 因为 Start 变成了 async void，这里需要手动启动协程
            StartCoroutine(ProcessLoadSequence(AppManager.Instance.CurrentRequest));
        }
        else
        {
            Debug.LogError("[LoadTransition] 缺少加载请求！");
        }
    }

    private IEnumerator ProcessLoadSequence(AppManager.SceneLoadContext request)
    {
        float startTime = Time.time;
        if (uiPanel != null) uiPanel.UpdateProgressView(0f);

        // --- 阶段 1: 内存清理 ---
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return null;

        // --- 阶段 2: 资产预加载 ---
        Task preloadTask = Task.CompletedTask;
        if (request.PreloadAddresses != null && request.PreloadAddresses.Count > 0)
        {
            var tasks = new List<Task>();
            foreach (var addr in request.PreloadAddresses)
                tasks.Add(AssetsManager.Instance.LoadAssetAsync<object>(addr));
            preloadTask = Task.WhenAll(tasks);
        }

        // --- 阶段 3: 场景异步加载 ---
        AsyncOperation sceneOp = SceneManager.LoadSceneAsync(request.TargetSceneName);
        sceneOp.allowSceneActivation = false;

        bool isPreloadDone = false;

        // 循环直到满足所有完成条件
        while (true)
        {
            // 检查预加载状态
            if (!isPreloadDone && preloadTask.IsCompleted) isPreloadDone = true;

            // 计算各项进度
            float sceneProgress = Mathf.Clamp01(sceneOp.progress / 0.9f);
            float assetsProgress = isPreloadDone ? 1f : 0.5f;
            float timeProgress = Mathf.Clamp01((Time.time - startTime) / minLoadTime);

            // 综合进度 (取最小值，确保不会提前跳满)
            float totalProgress = Mathf.Min(sceneProgress, assetsProgress);
            float displayProgress = Mathf.Min(totalProgress, timeProgress);

            // 更新 UI
            if (uiPanel != null) uiPanel.UpdateProgressView(displayProgress);

            // 检查结束条件: 场景就绪(>0.9) && 预加载完成 && 动画时间跑完
            if (sceneOp.progress >= 0.9f && isPreloadDone && timeProgress >= 1f)
            {
                break; // 跳出循环，进入等待输入阶段
            }

            yield return null;
        }

        // --- 阶段 4: 加载完成，等待用户输入 ---

        // 强制 UI 显示 100%
        if (uiPanel != null)
        {
            uiPanel.UpdateProgressView(1f);
            uiPanel.ShowPressAnyKeyPrompt();
        }

        Debug.Log("加载完成，请按任意键继续...");

        // 防抖动：等待一帧
        yield return null;

        // 等待输入
        while (!IsAnyInputTriggered()){ yield return null; }

        // 激活场景
        sceneOp.allowSceneActivation = true;
    }

    /// <summary>
    /// 输入检测逻辑 (保留在 Logic 层，因为它是控制什么时候进入下一阶段的开关)
    /// </summary>
    private bool IsAnyInputTriggered()
    {
        bool isKeyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool isPointerPressed = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
        return isKeyboardPressed || isPointerPressed;
    }
}

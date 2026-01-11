using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using System;
using static LOConstant;
using UnityEditor;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;




public class AppManager : MonoSingleton<AppManager>
{



    protected override void Awake()
    {
        base.Awake();
        AppStateEventHandler.Instance.Awake();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }
    // Start is called before the first frame update
    void Start()
    {
        PersistentManager.Instance.LoadAppData();




    }


    public readonly string[] Scenes_Game_PreloadAddresses = new string[]
    {
        //UI游戏界面
        "UIPanel_Main",
        LOConstant.AssetsKey.Address_SupplyLib

    };



    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }
    #region 切换场景
    [Button]
    public void LoadStartScene()
    {
        SceneLoadContext sc = new SceneLoadContext()
        {
            TargetSceneName = LOConstant.SceneName.Start,
            PreloadAddresses = new List<string>()
            {
                "UIPanel_Main"
            },
            OnComplete = async () =>
            {
                await UIManager.Instance.ShowPanel<UIPanel_Main>();
                AppEventArgs.Tiggle(AppEventEnum.场景加载完成);
            }
        };
        LoadScene(sc);
    }
    [Button]
    public void LoadGameScene(Action call = null)
    {
        var context = new SceneLoadContext()
        {
            TargetSceneName = LOConstant.SceneName.Game,

            PreloadAddresses = Scenes_Game_PreloadAddresses,
            OnComplete = async () =>
            {
                await TheGame.Instance.InitGame();

                call?.Invoke();
                AppEventArgs.Tiggle(AppEventEnum.场景加载完成);
                Debug.Log("场景加载完成");
            }
        };
        LoadScene(context);
    }
    #endregion

    public async UniTask EnterWait()
    {
        await UIPanel_GeneralNotice.ShowWait();
    }

    public async UniTask WaitRunTaskMainThread(Action action, float minDuration = 0.5f)
    {
        // 1. 显示 Loading
        await EnterWait();


        // Task B: 最小等待时间的计时器 (忽略 TimeScale 影响)
        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(minDuration), ignoreTimeScale: true);

        action?.Invoke();


        // 3. 等待两者都完成
        await delayTask;

        Debug.Log("主线程任务完成");
        // 4. 无论成功还是报错，最终都会执行这里，确保 Loading 关闭
        ExitWait();

    }

    public async UniTask WaitRunTask(Action action, float minDuration = 0.5f)
    {
        // 1. 显示 Loading
        await EnterWait();

        UniTask workTask = UniTask.RunOnThreadPool(action);


        // Task B: 最小等待时间的计时器 (忽略 TimeScale 影响)
        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(minDuration), ignoreTimeScale: true);

        // 3. 等待两者都完成
        await UniTask.WhenAll(workTask,delayTask);

        Debug.Log("任务完成");
        // 4. 无论成功还是报错，最终都会执行这里，确保 Loading 关闭
        ExitWait();

    }
    public async UniTask BGRunTask(Func<UniTask> action,Action callBack = null)
    {


        if (action != null)
        {
            await action.Invoke();
        }

        callBack?.Invoke();
        Debug.Log("异步任务完成");

    }

    public void ExitWait()
    {
        UIPanel_GeneralNotice.HideWait();
    }



    public class SceneLoadContext
    {
        public string TargetSceneName;
        public IEnumerable<string> PreloadAddresses;
        public Action OnComplete; // 核心：加载完成后的回调
        public bool UseTransition = true;
    }
    public SceneLoadContext CurrentRequest { get; private set; }


    /// <summary>
    /// 加载场景的统一入口
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    /// <param name="onComplete">加载完成后的回调逻辑</param>
    /// <param name="transition">是否使用过渡页</param>
    /// <param name="preloadAssets">需要预加载的资源地址</param>
    public void LoadScene(string sceneName, Action onComplete = null, bool transition = true, params string[] preloadAssets)
    {
        // 1. 构建请求上下文
        CurrentRequest = new SceneLoadContext
        {
            TargetSceneName = sceneName,
            OnComplete = onComplete,
            UseTransition = transition,
            PreloadAddresses = new List<string>(preloadAssets ?? Array.Empty<string>())
        };

        LoadScene(CurrentRequest);
    }
    public void LoadScene(SceneLoadContext sceneLoadContext)
    {
        CurrentRequest = sceneLoadContext;
        // 2. 执行加载
        if (CurrentRequest.UseTransition)
        {
            // 进入过渡场景
            SceneManager.LoadScene(LOConstant.SceneName.Transition);
        }
        else
        {
            // 直接加载（通常用于测试）
            SceneManager.LoadScene(CurrentRequest.TargetSceneName);
            // 注意：直接加载时，Unity的sceneLoaded事件也会触发，所以HandleSceneLoaded会被调用
        }
    }

    // Unity场景加载完成时的系统回调
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果加载的是过渡场景，不执行回调
        if (scene.name == LOConstant.SceneName.Transition) return;

        // 如果当前没有请求，或者加载的场景不是目标场景（防御性编程），跳过
        if (CurrentRequest == null || scene.name != CurrentRequest.TargetSceneName) return;

        Debug.Log($"[AppManager] 场景 {scene.name} 加载完毕，执行回调。");

        // 1. 执行回调
        CurrentRequest.OnComplete?.Invoke();

        // 2. 清理请求，防止重复触发
        CurrentRequest = null;
    }

    internal void ExitApp()
    {
        // 1. 编辑器环境：停止游戏运行
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        // 2. 打包后环境（PC/移动端）：调用退出API
        Application.Quit();
#endif

        // 可选：打印日志，方便调试
        Debug.Log("点击退出游戏，当前环境：" + (Application.isEditor ? "编辑器" : "打包程序"));
    }
}

public enum AppEventEnum
{

    APP数据加载完成,

    游戏加载完成,

    场景加载完成,

    开始游戏,

    游戏进行中,

    结束游戏


}
public struct AppEventArgs
{
    private static AppEventArgs eventArg;

    public AppEventEnum e;

    public static void Tiggle(AppEventEnum e)
    {

        eventArg.e = e;


        MoyoEventManager.TriggerEvent<AppEventArgs>(eventArg);
    }
}

public class AppStateEventHandler : Singleton<AppStateEventHandler>, IMoyoEventListener<AppEventArgs>
{

    protected AppStateEventHandler()
    {
        this.MoyoEventStartListening();
    }
    ~AppStateEventHandler()
    {
        this.MoyoEventStopListening();
    }



    public void OnMoyoEvent(AppEventArgs eventArgs)
    {
        switch (eventArgs.e)
        {
            case AppEventEnum.APP数据加载完成:
                break;
            case AppEventEnum.游戏加载完成:
                break;
            case AppEventEnum.场景加载完成:
                UIManager.Instance.HidePanel<UIPanel_Load>();
                break;
            case AppEventEnum.开始游戏:
                break;
            case AppEventEnum.游戏进行中:
                break;
            case AppEventEnum.结束游戏:
                break;
        }

    }



}



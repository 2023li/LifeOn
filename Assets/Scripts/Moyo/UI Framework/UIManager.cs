using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Moyo.Unity
{

    public enum UILayer
    {
        Background = 0, //背景
        Scene = 1,  //场景中的可点击UI
        Main = 2,   //每个场景的主UI
        Panel = 3,  //例如设置界面等全屏页面
        Popup = 4,  //例如提示框 非全屏界面
        Guide = 5, //指导
        Notice = 6, //通知
        Toast = 7,  //暂时无用
        Loading = 8,
        DebugInfo = 9
    }

    public class UIManager : MonoSingleton<UIManager>, IBackHandler
    {
        #region 配置定义

        [Serializable]
        public class UILayerConfig
        {
            public UILayer layerType;

            public string layerName;

            public int sortOrder;
            //当一个被标记为 isModal = true 的层级上有面板显示时，它会强制屏蔽掉所有“层级更低”的 UI 的点击事件。
            public bool isModal;

            public bool blocksRaycasts;

            [Tooltip("若为false：启用堆栈导航模式。打开新面板时隐藏当前面板；关闭当前面板时自动恢复上一个面板。")]
            public bool allowMultiPanels = true;

            [Tooltip("是否允许通过 Back 键(Esc)关闭此层级的面板")]
            public bool canCloseByBack = true;
        }



        #endregion

        #region 字段与属性

        [SerializeField]
        private UILayerConfig[] layerConfigs = {
            new UILayerConfig { layerType = UILayer.Background, layerName = "Background", sortOrder = 0, isModal = false, blocksRaycasts = false , allowMultiPanels = true,canCloseByBack = false},
            new UILayerConfig { layerType = UILayer.Scene, layerName = "Scene", sortOrder = 1, isModal = false, blocksRaycasts = false , allowMultiPanels = true,canCloseByBack = true},
            new UILayerConfig { layerType = UILayer.Main, layerName = "Main", sortOrder = 2, isModal = false, blocksRaycasts = true, allowMultiPanels = false,canCloseByBack = false },
            new UILayerConfig { layerType = UILayer.Panel, layerName = "Panel", sortOrder = 3, isModal = true, blocksRaycasts = true , allowMultiPanels = true,canCloseByBack = true},
            new UILayerConfig { layerType = UILayer.Popup, layerName = "Popup", sortOrder = 4, isModal = true, blocksRaycasts = true , allowMultiPanels = true,canCloseByBack = true},
            new UILayerConfig { layerType = UILayer.Guide, layerName = "Guide", sortOrder = 5, isModal = true, blocksRaycasts = true , allowMultiPanels = true,canCloseByBack = false},
            new UILayerConfig { layerType = UILayer.Notice, layerName = "Notice", sortOrder = 6, isModal = true, blocksRaycasts = true , allowMultiPanels = true,canCloseByBack = false},
            new UILayerConfig { layerType = UILayer.Toast, layerName = "Toast", sortOrder = 7, isModal = false, blocksRaycasts = false , allowMultiPanels = true,canCloseByBack = true},
            new UILayerConfig { layerType = UILayer.Loading, layerName = "Loading", sortOrder = 8, isModal = true, blocksRaycasts = true , allowMultiPanels = true,canCloseByBack = false},
            new UILayerConfig { layerType = UILayer.DebugInfo, layerName = "DebugInfo", sortOrder = 9, isModal = false, blocksRaycasts = false , allowMultiPanels = true,canCloseByBack = true}
        };

        [LabelText("参考分辨率")]
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920, 1080);

        private Canvas mainCanvas;

        // 核心数据结构
        private Dictionary<UILayer, Transform> layerParents = new Dictionary<UILayer, Transform>();
        private Dictionary<UILayer, CanvasGroup> layerCanvasGroups = new Dictionary<UILayer, CanvasGroup>();

        // activePanels 作为堆栈使用。List 的最后一个元素是当前层级“最上方”的面板。
        private Dictionary<UILayer, List<PanelBase>> activePanels = new Dictionary<UILayer, List<PanelBase>>();

        // 用于快速查找面板实例
        private Dictionary<Type, PanelBase> loadedPanels = new Dictionary<Type, PanelBase>();

        // 内部追踪面板属于哪个层，避免修改 PanelBase
        private Dictionary<PanelBase, UILayer> panelToLayerMap = new Dictionary<PanelBase, UILayer>();

        private UILayer? currentModalLayer = null;

        public int BackPriority => BackPrioritySort.UIPriority;
        public bool TryHandleBack()
        {
            if (BackTopPanel())
            {
                Debug.Log("Back事件UI处理");
                return true;
            }
            Debug.Log("Back事件UI不处理");
            return false;
        }
        public Canvas GetMainCanvas() => mainCanvas;

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeLayers();

        }

        private void OnEnable()
        {

            InputManager.Instance.Register(this);

        }
        private void OnDisable()
        {
            if (InputManager.HasInstance)
            {
                InputManager.Instance.UnRegister(this);
            }
        }




        private void InitializeLayers()
        {
            SetupMainCanvas();
            layerConfigs = layerConfigs.OrderBy(c => c.sortOrder).ToArray();
            foreach (var config in layerConfigs)
            {
                CreateUILayer(config);
            }
        }

        private void SetupMainCanvas()
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                mainCanvas = gameObject.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var canvasScaler = gameObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = canvasReferenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreateUILayer(UILayerConfig config)
        {
            var layerObj = new GameObject(config.layerName);
            layerObj.transform.SetParent(transform);
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localScale = Vector3.one;
            layerObj.transform.SetSiblingIndex(config.sortOrder);

            var rectTransform = layerObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var canvasGroup = layerObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = config.blocksRaycasts;
            canvasGroup.interactable = true;

            layerParents[config.layerType] = layerObj.transform;
            layerCanvasGroups[config.layerType] = canvasGroup;
            activePanels[config.layerType] = new List<PanelBase>();
        }

        #endregion

        #region 打开面板 (ShowPanel)

        // 在 UIManager.cs 中替换原有的 ShowPanel<T> 方法

        public async Task<T> ShowPanel<T>(string address = null, params object[] args) where T : PanelBase
        {
            Type panelType = typeof(T);
            PanelBase targetPanel = null;

            // 1. 尝试从单例缓存中获取
            // 只有单例面板会被存入 loadedPanels，所以这里取到的肯定是可以复用的
            if (loadedPanels.TryGetValue(panelType, out var existingPanel) && existingPanel != null)
            {
                targetPanel = existingPanel;
            }
            else
            {
                // 2. 加载并创建新实例
                targetPanel = await LoadAndCreatePanel<T>(address, args);
                if (targetPanel == null) return null;

                // 【适配修改点】：只有标记为单例的面板，才加入缓存字典
                if (targetPanel.SingletonPanel)
                {
                    // 如果同类型的单例已经存在（极端并发情况），则销毁新建的，使用旧的
                    if (loadedPanels.ContainsKey(panelType))
                    {
                        Destroy(targetPanel.gameObject);
                        targetPanel = loadedPanels[panelType];
                    }
                    else
                    {
                        loadedPanels[panelType] = targetPanel;
                    }
                }
            }

            // 3. 后续逻辑保持不变（设置父节点、层级、堆栈处理）
            UILayer targetLayer = targetPanel.Layer;

            if (!layerParents.ContainsKey(targetLayer))
            {
                Debug.LogError($"面板 {targetPanel.name} 请求的层级 {targetLayer} 未在 UIManager 中初始化！");
                return null;
            }

            if (targetPanel.transform.parent != layerParents[targetLayer])
            {
                targetPanel.transform.SetParent(layerParents[targetLayer], false);
            }

            // 这里需要注意：如果是多实例面板，多个实例都会映射到同一个 layer，这是正确的
            panelToLayerMap[targetPanel] = targetLayer;

            ProcessPanelShow(targetLayer, targetPanel, args);

            if (IsModalLayer(targetLayer))
            {
                SetModalLayer(targetLayer);
            }

            return targetPanel as T;
        }

        private async Task<T> LoadAndCreatePanel<T>(string address = null, params object[] args) where T : PanelBase
        {
            address ??= typeof(T).Name;
            var panelAsset = await AssetsManager.Instance.LoadAssetAsync<GameObject>(address);
            if (panelAsset == null)
            {
                Debug.LogError($"加载UI面板预制体失败：{address}");
                return null;
            }
            var panelObj = Instantiate(panelAsset);
            panelObj.name = typeof(T).Name;
            var panelComponent = panelObj.GetComponent<T>() ?? panelObj.AddComponent<T>();

            // 如果 PanelBase 有初始化方法，可以在此调用
            // panelComponent.OnInit(args); 

            return panelComponent;
        }

        /// <summary>
        /// 核心逻辑：处理面板显示的堆栈行为
        /// </summary>
        private void ProcessPanelShow(UILayer layer, PanelBase newPanel, object[] args)
        {
            UILayerConfig config = GetLayerConfig(layer);
            List<PanelBase> stack = activePanels[layer];

            // 1. 堆栈维护：如果已在栈中，移到末尾（视为重新聚焦）
            if
         (stack.Contains(newPanel))
            {
                stack.Remove(newPanel);
            }

            // 2. 单窗口模式处理：隐藏当前的栈顶面板
            if (!config.allowMultiPanels && stack.Count > 0
        )
            {
                var currentTop = stack[stack.Count - 1
        ];
                // 仅视觉隐藏，不触发出栈逻辑
                if (currentTop != null
         && currentTop != newPanel)
                {
                    // 注意：这里调用的是 PanelBase.Hide，但不走 UIManager.HidePanel 的逻辑
                    currentTop.Hide();
                }
            }

            // 3. 入栈
            stack.Add(newPanel);

            // 4. 视觉调整
            newPanel.transform.SetAsLastSibling();
            // 确保渲染在最前

            // 【关键优化】：不再传递 'this' (UIManager)，直接传 args
            // 这解决了 BuildingConfirmPanel 解析参数时的嵌套数组问题
            newPanel.Show(args);
        }

        #endregion

        #region 关闭面板 (HidePanel)
        public void HidePanel<T>() where T : PanelBase
        {
            Type panelType = typeof(T);

            // 1. 尝试从单例字典中查找
            if (loadedPanels.TryGetValue(panelType, out var panel) && panel != null)
            {
                HidePanel(panel); // 调用基于实例的重载
            }
            else
            {
                // 【适配说明】
                // 如果是多实例面板（SingletonPanel = false），它不会在 loadedPanels 里。
                // 所以 HidePanel<T>() 对多实例面板无效。
                // 开发者必须持有该面板的实例引用，调用 HidePanel(instance)。
                Debug.LogWarning($"HidePanel<{panelType.Name}> 无法找到单例实例。如果是多实例面板，请使用 HidePanel(instance) 直接关闭特定对象。");

                // 可选：如果你希望 HidePanel<T> 暴力关闭所有该类型的面板，可以遍历 activePanels
                // 但这通常不符合预期，建议保持上述 Warning。
            }
        }

        // === 新增部分：支持直接通过实例关闭面板 ===
        public void HidePanel(PanelBase panel)
        {
            if (panel == null) return;

            if (panelToLayerMap.TryGetValue(panel, out var layer))
            {
                // 只有当面板确实在活跃堆栈中时才处理
                if (activePanels.ContainsKey(layer) && activePanels[layer].Contains(panel))
                {
                    ProcessPanelHide(layer, panel);
                }
                else
                {
                    // 如果不在堆栈中（可能已经被隐藏或异常），仅确保视觉隐藏
                    panel.Hide();
                }
            }
            else
            {
                Debug.LogWarning($"尝试关闭未注册层级的面板: {panel.name}");
                panel.Hide();
            }
        }
        // === 新增部分：关闭最顶层的显示面板（用于返回键/Esc） ===
        public void CloseTopPanel()
        {
            // 1. 按照渲染层级从高到低遍历 (Popup > Main > Normal ...)
            // 这样确保优先关闭覆盖在最上面的弹窗
            var sortedConfigs = layerConfigs.OrderByDescending(c => c.sortOrder);

            foreach (var config in sortedConfigs)
            {
                // [可选] 过滤掉不应该被“返回键”关闭的层级
                // 例如：Loading层、Toast层、DebugInfo层 通常不由玩家手动关闭
                if (config.layerType == UILayer.Loading ||
                    config.layerType == UILayer.Toast ||
                    config.layerType == UILayer.DebugInfo ||
                    config.layerType == UILayer.Background)
                {
                    continue;
                }

                // 2. 检查该层是否有活跃面板
                if (activePanels.TryGetValue(config.layerType, out var stack) && stack.Count > 0)
                {
                    // 获取栈顶面板（最后加入的）
                    var topPanel = stack[stack.Count - 1];

                    // 3. 只有当面板当前是“显示中”的状态才关闭
                    // (在堆栈模式下，栈底的面板可能是隐藏的，不能关闭它们，否则逻辑会乱)
                    if (topPanel != null && topPanel.gameObject.activeSelf)
                    {
                        Debug.Log($"[UIManager] CloseTopPanel 关闭了: {topPanel.name}");
                        HidePanel(topPanel);
                        return; // 每次只关闭一个，执行完立即结束
                    }
                }
            }
        }

        /// <summary>
        /// 触发最顶层面板的 Back 逻辑（通常用于绑定物理返回键/ESC键）
        /// </summary>
        public bool BackTopPanel()
        {
            // 1. 按照渲染层级从高到低遍历
            var sortedConfigs = layerConfigs.OrderByDescending(c => c.sortOrder);

            foreach (var config in sortedConfigs)
            {
                // 【优化1】使用配置字段判断，而不是硬编码 layerType
                // 如果你不想改 Config 类，就在这里手动把 UILayer.Main 加进去
                if (!config.canCloseByBack)
                {
                    // 特殊情况：如果这一层是模态层且有显示内容，虽然它不能被关闭，
                    // 但它作为模态层应该"吞掉"输入，防止穿透到下一层？
                    // 策略A（简单）：直接跳过，继续找下一层（当前逻辑）
                    // 策略B（严格）：如果是模态层且可见，直接 return true（拦截），但不执行关闭。

                    // 这里我们采用简单策略，只是跳过 HUD/Background
                    continue;
                }

                if (activePanels.TryGetValue(config.layerType, out var stack) && stack.Count > 0)
                {
                    // 获取栈顶面板
                    var topPanel = stack[stack.Count - 1];

                    if (topPanel != null && topPanel.gameObject.activeSelf)
                    {
                        // 【优化2】处理面板内部逻辑
                        if (topPanel.Back())
                        {
                            return true; // 面板自己处理了（比如弹出了二级确认框）
                        }

                        // 执行关闭
                        Debug.Log($"[UIManager] BackTopPanel 关闭了: {topPanel.name}");
                        HidePanel(topPanel);
                        return true; // 成功关闭一个，拦截输入
                    }
                }
            }

            // 所有层级都没有可关闭的面板，返回 false
            // 这样 InputManager 会继续把事件传给 BackPriority 50 (Building) 或 20 (GamePause)
            return false;
        }

        /// <summary>
        /// 核心逻辑：处理面板关闭后的堆栈恢复
        /// </summary>
        private void ProcessPanelHide(UILayer layer, PanelBase panelToClose)
        {
            // 如果该层级未被跟踪（防御性编程）
            if (!activePanels.TryGetValue(layer, out var stack)) return;

            var config = GetLayerConfig(layer);

            // 1. 视觉隐藏
            // 【关键优化】：不再传递 'this'
            panelToClose.Hide();

            // 2. 出栈
            if (stack.Contains(panelToClose))
            {
                stack.Remove(panelToClose);
            }

            // 3. 自动恢复逻辑 (单窗口模式)
            if (!config.allowMultiPanels && stack.Count > 0)
            {
                var previousPanel = stack[stack.Count - 1];
                // 如果前一个面板存在且目前是隐藏的，则恢复显示
                if (previousPanel != null && !previousPanel.gameObject.activeSelf)
                {
                    // 恢复显示时通常不需要参数，或者你可以缓存之前的参数
                    previousPanel.Show();
                }
            }

            // 4. 刷新模态状态（遮罩/点击阻挡）
            UpdateModalLayerState();


            // 【新增建议】：如果是非单例面板，关闭即销毁
            // 这样避免生成了 100 个 Toast 也就是隐藏在 Hierarchy 里占内存
            if(!panelToClose.SingletonPanel)
            {
                // 延迟一帧或直接销毁，取决于你的动画逻辑
                // 建议调用 DestroyPanel(panelToClose) 来彻底清理
                // 但要注意不要在遍历 activePanels 时直接 Modify 集合，上面逻辑已经 Remove 了，所以这里是安全的
                DestroyPanel(panelToClose);
            }
        }

        public void DestroyPanel(PanelBase panel)
        {
            if (panel == null) return;

            // 1. 先从 UI 堆栈中移除
            if (panelToLayerMap.TryGetValue(panel, out var layer))
            {
                ProcessPanelHide(layer, panel);
                panelToLayerMap.Remove(panel);
            }

            // 2. 【适配修改点】如果是单例，从缓存字典中移除
            Type t = panel.GetType();
            if (loadedPanels.TryGetValue(t, out var cachedPanel) && cachedPanel == panel)
            {
                loadedPanels.Remove(t);
            }

            // 3. 销毁物体
            // 注意：如果是 Addressables 加载，AssetsManager.ReleaseAsset 可能需要具体的 handle 或地址
            // 假设你的 AssetsManager 可以通过 name 或其他方式处理引用计数
            AssetsManager.Instance.ReleaseAsset(panel.name);
            Destroy(panel.gameObject);

            UpdateModalLayerState();
        }

        // 修改原有的泛型版本
        public void DestroyPanel<T>() where T : PanelBase
        {
            // 只处理单例
            if (loadedPanels.TryGetValue(typeof(T), out var panel))
            {
                DestroyPanel(panel);
            }
        }
      
        #endregion

        #region 模态管理

        private void SetModalLayer(UILayer modalLayer)
        {
            if (!IsModalLayer(modalLayer)) return;

            currentModalLayer = modalLayer;
            int modalSortOrder = GetLayerSortOrder(modalLayer);

            foreach (var config in layerConfigs)
            {
                // 只有层级高于或等于模态层的，才允许交互
                bool isInteractable = config.sortOrder >= modalSortOrder;
                SetLayerInteractable(config.layerType, isInteractable);
            }
        }

        private void UpdateModalLayerState()
        {
            UILayer? nextModalLayer = null;

            // 从高到低遍历，找到第一个包含“可见面板”的模态层
            foreach (var config in layerConfigs.OrderByDescending(c => c.sortOrder))
            {
                if (config.isModal && activePanels.ContainsKey(config.layerType))
                {
                    // 检查该层是否有任何面板是 activeSelf (可见) 的
                    if (activePanels[config.layerType].Any(p => p != null && p.gameObject.activeSelf))
                    {
                        nextModalLayer = config.layerType;
                        break;
                    }
                }
            }

            if (nextModalLayer.HasValue)
            {
                SetModalLayer(nextModalLayer.Value);
            }
            else
            {
                // 无模态，恢复所有层
                currentModalLayer = null;
                foreach (var config in layerConfigs)
                {
                    SetLayerInteractable(config.layerType, true);
                }
            }
        }

        private void SetLayerInteractable(UILayer layer, bool isPotentiallyInteractable)
        {
            if (layerCanvasGroups.TryGetValue(layer, out var canvasGroup) &&
                GetLayerConfig(layer) is { } config)
            {
                // 恢复时，要尊重该层级原本是否 blocksRaycasts (比如 Toast 层本来就不阻挡)
                canvasGroup.blocksRaycasts = isPotentiallyInteractable && config.blocksRaycasts;
            }
        }

        #endregion

        #region 辅助方法

        private UILayerConfig GetLayerConfig(UILayer layer)
        {
            return layerConfigs.FirstOrDefault(c => c.layerType == layer);
        }

        private bool IsModalLayer(UILayer layer) => GetLayerConfig(layer)?.isModal ?? false;

        private int GetLayerSortOrder(UILayer layer) => GetLayerConfig(layer)?.sortOrder ?? 0;

        public T GetPanel<T>() where T : PanelBase
        {
            return loadedPanels.TryGetValue(typeof(T), out var panel) ? panel as T : null;
        }

        public bool IsPanelLoaded<T>() where T : PanelBase => loadedPanels.ContainsKey(typeof(T)) && loadedPanels[typeof(T)] != null;

        public bool IsPanelShowing<T>() where T : PanelBase => GetPanel<T>()?.gameObject.activeInHierarchy ?? false;

        public void DestroyAllPanels()
        {
            foreach (var panel in loadedPanels.Values.Where(p => p != null))
            {
                AssetsManager.Instance.ReleaseAsset(panel.name);
                Destroy(panel.gameObject);
            }

            loadedPanels.Clear();
            panelToLayerMap.Clear();

            foreach (var layer in layerConfigs)
            {
                if (activePanels.ContainsKey(layer.layerType))
                {
                    activePanels[layer.layerType].Clear();
                }
            }

            currentModalLayer = null;

            foreach (var config in layerConfigs)
            {
                SetLayerInteractable(config.layerType, true);
            }
        }



        #endregion
    }

}



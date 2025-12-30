using System;
using UnityEngine;
using UnityEngine.UI;
using Moyo.Unity; // 确保引用了你的命名空间

namespace Moyo.Unity
{
    /// <summary>
    /// 建筑放置确认条面板
    /// </summary>
    public class BuildingConfirmPanel : PanelBase
    {
        // 定义参数类，方便外部调用时传递强类型数据
        public class Args
        {
            public Action OnConfirm;
            public Action OnCancel;
            // 如果需要，可以在这里加 position 等其他参数
        }

        private const float DefaultYOffsetMultiplier = 0.6f;

        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private RectTransform rectTransform;
        private Action onConfirm;
        private Action onCancel;

        // 保存 UIManager 引用以便关闭自己
        private UIManager uiManager;

        protected  void Awake()
        {
            rectTransform = transform as RectTransform;
            EnsureCanvasGroup();
            EnsureButtons();

            // 绑定事件
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // --- 核心重构：对接 PanelBase 的 Show 方法 ---
        public override void Show(UIManager manager, params object[] args)
        {
            // 1. 保存 manager 引用
            this.uiManager = manager;

            // 2. 解析参数
            ApplyArgs(args);

            // 3. 确保物体激活（虽然 UIManager 通常会处理，但双重保险没错）
            gameObject.SetActive(true);

            // 4. 处理 CanvasGroup (如果你的 PanelBase 没有做统一动画，这里手动处理)
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 5. 如果需要初始化位置，也可以在这里处理，或者外部调用 SetWorldAnchor
        }

        // --- 核心重构：对接 PanelBase 的 Hide 方法 ---
        public override void Hide(UIManager manager, params object[] args)
        {
            // 1. 禁用交互，防止关闭动画过程中被再次点击
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 2. 这里可以选择做个 DoTween 动画，动画结束再 SetActive(false)
            // 目前简单处理：
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 设置世界坐标锚点，并转换为当前画布的锚点位置。
        /// </summary>
        public void SetWorldAnchor(Vector3 anchorWorldPos)
        {
            // 获取 Canvas：优先尝试 UIManager 的主 Canvas，否则找自身的
            var targetCanvas = uiManager != null ? uiManager.GetMainCanvas() : GetComponentInParent<Canvas>();

            if (rectTransform == null || targetCanvas == null) return;

            var canvasRect = targetCanvas.GetComponent<RectTransform>();

            // 获取摄像机
            // 注意：这里假设 InputManager 存在，如果不存在请回退到 Camera.main
            var cam = Camera.main;
            // var cam = InputManager.Instance != null ? InputManager.Instance.RealCamera : Camera.main;

            // 获取网格偏移 (保持原有逻辑)
            // var grid = GridSystem.Instance;
            // var yOffset = grid != null ? grid.mapGrid.cellSize.y * DefaultYOffsetMultiplier : 0f;
            float yOffset = 1.0f; // 示例值，请替换回你的 GridSystem 逻辑

            var screenPoint = cam != null
                ? cam.WorldToScreenPoint(anchorWorldPos + new Vector3(0f, yOffset, 0f))
                : Vector3.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera,
                out var localPoint);

            rectTransform.anchoredPosition = localPoint;
        }

        private void ApplyArgs(object[] args)
        {
            // 重置委托，防止上次残留
            onConfirm = null;
            onCancel = null;

            if (args == null || args.Length == 0) return;

            // 优先查找强类型参数 Args
            foreach (var arg in args)
            {
                if (arg is Args typedArgs)
                {
                    onConfirm = typedArgs.OnConfirm;
                    onCancel = typedArgs.OnCancel;
                    return; // 找到强类型参数直接返回
                }
            }

            // 兼容旧逻辑：如果直接传了 Action
            foreach (var arg in args)
            {
                if (arg is Action act)
                {
                    if (onConfirm == null) onConfirm = act;
                    else if (onCancel == null) onCancel = act;
                }
            }
        }

        private void OnConfirmClicked()
        {
            onConfirm?.Invoke();

            // 点击确认后，通常应该关闭面板
            CloseSelf();
        }

        private void OnCancelClicked()
        {
            onCancel?.Invoke();

            // 点击取消后，通常应该关闭面板
            CloseSelf();
        }

        private void CloseSelf()
        {
            if (uiManager != null)
            {
                // 使用 UIManager 的标准关闭流程，这样可以维护堆栈逻辑
                uiManager.HidePanel(this);
            }
            else
            {
                // 兜底
                gameObject.SetActive(false);
            }
        }

        #region 初始化辅助
        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void EnsureButtons()
        {
            if (confirmButton != null && cancelButton != null) return;
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (confirmButton == null && btn.name.Contains("Confirm", StringComparison.OrdinalIgnoreCase)) confirmButton = btn;
                if (cancelButton == null && btn.name.Contains("Cancel", StringComparison.OrdinalIgnoreCase)) cancelButton = btn;
            }
        }
        #endregion
    }
}

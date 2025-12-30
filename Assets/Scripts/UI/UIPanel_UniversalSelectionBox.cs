using System;
using System.Threading.Tasks;
using Moyo.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_UniversalSelectionBox : PanelBase
{
    [Header("UI References")]
    [SerializeField] private Button btn_Confirm;
    [SerializeField] private Button btn_Cancel;
    [SerializeField] private TMP_Text txt_LableText;
    [SerializeField] private TMP_Text txt_Description;

    // --- 核心修改：使用成员变量存储当前的回调 ---
    private Action _currentConfirmAction;
    private Action _currentCancelAction;

    protected  void Awake()
    {
       

        // --- 核心修改：只初始化一次监听器 ---
        // 无论面板显示多少次，这里的 AddListener 只运行一次
        if (btn_Confirm != null)
        {
            btn_Confirm.onClick.AddListener(OnConfirmClicked);
        }

        if (btn_Cancel != null)
        {
            btn_Cancel.onClick.AddListener(OnCancelClicked);
        }
    }

    // 静态入口保持不变
    public static async Task ShowBox(string title, string content, Action onConfirm = null, Action onCancel = null)
    {
        var panel = await UIManager.Instance.ShowPanel<UIPanel_UniversalSelectionBox>(UIManager.UILayer.Popup);
        if (panel != null)
        {
            panel.Setup(title, content, onConfirm, onCancel);
        }
    }

    // 设置数据
    private void Setup(string title, string content, Action onConfirm, Action onCancel)
    {
        if (txt_LableText != null) txt_LableText.text = title;
        if (txt_Description != null) txt_Description.text = content;

        // --- 核心修改：直接替换变量引用，不需要操作 UI 事件系统 ---
        _currentConfirmAction = onConfirm;
        _currentCancelAction = onCancel;
    }

    // --- 固定的内部响应方法 ---

    private void OnConfirmClicked()
    {
        // 1. 执行当前存储的逻辑
        _currentConfirmAction?.Invoke();

        // 2. 关闭面板
        ClosePanel();
    }

    private void OnCancelClicked()
    {
        // 1. 执行当前存储的逻辑
        _currentCancelAction?.Invoke();

        // 2. 关闭面板
        ClosePanel();
    }

    private void ClosePanel()
    {
         OnHide();

        // 【重要】清理引用，防止内存泄漏
        // 如果面板被缓存而不销毁，由于 Action 可能会引用外部大的对象（闭包），
        // 不清空会导致外部对象无法被回收。
        _currentConfirmAction = null;
        _currentCancelAction = null;
    }
}

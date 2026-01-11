using System;
using System.Threading.Tasks;
using Moyo.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_UniversalSelectionBox : PanelBase
{
    public class BtnData
    {
        public string btnName;
        public Action btnCall;



        public BtnData() { }
        // 添加一个构造函数方便调用
        public BtnData(string name, Action call)
        {
            btnName = name;
            btnCall = call;
        }
    }

    public override UILayer Layer => UILayer.Popup;

    [Header("UI References")]
    [SerializeField] private Button btn_Template;
    [SerializeField] private RectTransform rt_BtnParent;
    [SerializeField] private TMP_Text txt_LableText; // 注意：通常拼写为 LabelText
    [SerializeField] private TMP_Text txt_Description;

    protected void Awake()
    {
        // 初始时隐藏模板按钮，防止它作为一个不可交互的按钮显示在界面上
        if (btn_Template != null)
        {
            btn_Template.gameObject.SetActive(false);
        }
    }

    // 静态入口保持不变
    public static async Task ShowBox(string title, string content, params BtnData[] btnDatas)
    {
        var panel = await UIManager.Instance.ShowPanel<UIPanel_UniversalSelectionBox>();
        if (panel != null)
        {
            panel.Setup(title, content, btnDatas);
        }
    }

    // 设置数据
    private void Setup(string title, string content, params BtnData[] btnDatas)
    {
        if (txt_LableText != null) txt_LableText.text = title;
        if (txt_Description != null) txt_Description.text = content;

        // 1. 清理旧生成的按钮 (除了模板本身)
        ClearOldButtons();

        // 2. 处理空数据情况：添加默认按钮
        if (btnDatas == null || btnDatas.Length <= 0)
        {
            CreateButton("确定", null);
            return;
        }

        // 3. 遍历生成按钮
        foreach (var btnData in btnDatas)
        {
            CreateButton(btnData.btnName, btnData.btnCall);
        }
    }

    // --- 内部辅助方法 ---

    private void ClearOldButtons()
    {
        if (rt_BtnParent == null) return;

        // 倒序遍历删除，或者收集后删除。
        // 注意：不要删除 btn_Template 本身
        for (int i = rt_BtnParent.childCount - 1; i >= 0; i--)
        {
            Transform child = rt_BtnParent.GetChild(i);
            if (child != btn_Template.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void CreateButton(string btnName, Action onClickAction)
    {
        if (btn_Template == null || rt_BtnParent == null) return;

        // 实例化按钮
        Button newBtn = Instantiate(btn_Template, rt_BtnParent);
        newBtn.gameObject.SetActive(true); // 确保新按钮是激活的

        // 设置文字
        // 假设按钮下有一个 TMP_Text 组件用于显示文字
        TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = btnName;
        }

        // 绑定点击事件
        newBtn.onClick.RemoveAllListeners();
        newBtn.onClick.AddListener(() =>
        {
            // 1. 执行外部传入的逻辑
            onClickAction?.Invoke();

            // 2. 点击任何按钮后，默认行为是关闭窗口
            // 如果你不希望某些按钮关闭窗口，需要修改 BtnData 结构增加一个 bool keepOpen
            ClosePanel();
        });
    }

    // --- 固定的内部响应方法 ---

    private void ClosePanel()
    {
        UIManager.Instance.HidePanel(this);
    }
}

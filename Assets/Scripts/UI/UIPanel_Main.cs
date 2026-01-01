using System;
using System.Collections.Generic;
using DG.Tweening;
using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class UIPanel_Main : PanelBase
{


    public override bool Back(params object[] args)
    {

        Debug.Log("被主面板处理了");

        return true;
    } 

     [SerializeField] private CanvasGroup _popBG;
    private void ShowPopBG()
    {
        // 1. 安全校验：避免空引用报错
        if (_popBG == null)
        {
            Debug.LogWarning("PopBG的CanvasGroup未赋值！");
            return;
        }

        // 2. 先激活目标对象（防止对象未激活导致动画无效）
        _popBG.gameObject.SetActive(true);

        // 3. 杀死该对象上已有的fade动画，避免重复调用导致动画叠加/卡顿
        _popBG.DOKill();

        // 4. 初始化CanvasGroup状态：透明度0，不可交互，不阻挡射线
        _popBG.alpha = 0;
        _popBG.interactable = false;
        _popBG.blocksRaycasts = false;

        // 5. 使用DoTween插值透明度到1
        _popBG.DOFade(1, 0.5f).SetEase(Ease.OutQuad);
    }
    private void HidePopBG()
    {
        if (_popBG == null)
        {
            Debug.LogWarning("PopBG的CanvasGroup未赋值！");
            return;
        }

        _popBG.DOKill();

        _popBG.DOFade(0, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 动画完成后隐藏对象（可选，减少DrawCall）
                _popBG.gameObject.SetActive(false);
            });
    }



    [FoldoutGroup("主菜单按钮")]
    [SerializeField] private Button btn_Continue;
    [FoldoutGroup("主菜单按钮")]
    [SerializeField] private Button btn_NewGame;
    [FoldoutGroup("主菜单按钮")]
    [SerializeField] private Button btn_LoadGame;
    [FoldoutGroup("主菜单按钮")]
    [SerializeField] private Button btn_Setting;
    [FoldoutGroup("主菜单按钮")]
    [SerializeField] private Button btn_Credits;
    [FoldoutGroup("主菜单按钮")]
    [SerializeField] private Button btn_Quit;



   


    protected void Awake()
    {
        btn_Continue.onClick.AddListener(() =>
        {
            PersistentManager.Instance.LoadGame(PersistentManager.Instance.GetLastGameSaveId());
        });

        btn_Setting.onClick.AddListener(async () =>
        {
            await UIManager.Instance.ShowPanel<UIPanel_Setting>(UIManager.UILayer.Main);
        });

        Awake_取名弹窗();
        Awake_确认退出弹窗();
        Awake_语言选择();
    }

    private void OnEnable()
    {

        btn_Continue.gameObject.SetActive(PersistentManager.Instance.HasLastGameSave());
       


    }


    void Start()
    {
        #region 绑定点击事件
        btn_NewGame.onClick.AddListener(()=>
        {
            Show_取名框();
        });



        btn_Quit.onClick.AddListener(() =>
        {
            Show_确认退出弹窗();
        });
        #endregion


        if (PersistentManager.Instance.CurrentAppData.firstStartup)
        {
            Debug.Log("第一次启动游戏");
            Show_语言选择();
        }
        else
        {
            Debug.Log("不是第一次启动");
        }

    }




    #region 确认退出弹窗

  
    [FoldoutGroup("确认退出弹窗"), SerializeField]
    private RectTransform pop_确认退出弹窗;
    [FoldoutGroup("确认退出弹窗"), SerializeField]
    private Button btn_确认退出;
    [FoldoutGroup("确认退出弹窗"), SerializeField]
    private Button btn_取消退出;
    private void Awake_确认退出弹窗()
    {
        btn_确认退出.onClick.AddListener(() => { AppManager.Instance.ExitApp(); });
        btn_取消退出.onClick.AddListener(() => { Hide_确认退出弹窗(); });
    }
    private void Show_确认退出弹窗()
    {
        ShowPopBG();
        pop_确认退出弹窗.gameObject.SetActive(true);
        pop_确认退出弹窗.DOScale(1, 0.5f).From(0).SetEase(Ease.OutBack);


    }
    private void Hide_确认退出弹窗()
    {
        HidePopBG();
        pop_确认退出弹窗.gameObject.SetActive(false);
    }
    #endregion

    #region 取名弹窗
    [FoldoutGroup("取名弹窗"), SerializeField]
    private RectTransform pop_取名框;
    [FoldoutGroup("取名弹窗"), SerializeField]
    private Button btn_确认名称;
    [FoldoutGroup("取名弹窗"), SerializeField]
    private Button btn_取消名称;

    [FoldoutGroup("取名弹窗"), SerializeField]
    private TMP_InputField inputField_GameName;

    private void Awake_取名弹窗()
    {
        btn_确认名称.interactable=false;

        inputField_GameName.onValueChanged.AddListener((str) =>
        {
            btn_确认名称.interactable = !string.IsNullOrEmpty(str);
        });


        btn_确认名称.onClick.AddListener(() =>
        {
            PersistentManager.Instance.GetCurrentGameSave().saveName = inputField_GameName.text;


            AppManager.Instance.LoadGameScene();
        });

        btn_取消名称.onClick.AddListener(() => { Hide_取名弹窗(); });

    }
    private void Show_取名框()
    {
        ShowPopBG();
        pop_取名框.gameObject.SetActive(true);
        pop_取名框.DOScale(1, 0.5f).From(0).SetEase(Ease.OutBack);
    }
    private void Hide_取名弹窗()
    {
        HidePopBG();
        pop_取名框.gameObject.SetActive(false);
    }
    #endregion

    #region 语言选择弹窗
    [FoldoutGroup("语言选择"),SerializeField]
    private Button btn_确认语言;

    [FoldoutGroup("语言选择"),SerializeField]
    private RectTransform pop_语言选择;

    [FoldoutGroup("语言选择"), SerializeField]
    private TMP_Dropdown dropDown_语言选择下拉菜单;
    private void Awake_语言选择()
    {
        btn_确认语言.onClick.AddListener(() =>
        {
            PersistentManager.Instance.CurrentAppData.firstStartup = false;
            PersistentManager.Instance.SaveAppData();
            Hide_语言选择();
        });


        List<string> langList = new List<string>(Enum.GetNames(typeof(AppLanguage)));
        dropDown_语言选择下拉菜单.ClearOptions();
        dropDown_语言选择下拉菜单.AddOptions(langList);
        dropDown_语言选择下拉菜单.onValueChanged.AddListener((index) =>
        {
            AppLanguage current = (AppLanguage)index;
            UnityEngine.Localization.Locale targetLocale = null;
            switch (current)
            {
                case AppLanguage.简体中文:
                    targetLocale = Locale.CreateLocale("zh"); // 同步创建Locale
                    break;
                case AppLanguage.English:
                    targetLocale = Locale.CreateLocale("en");
                    break;
            }

            if (targetLocale != null)
            {
                LocalizationSettings.Instance.SetSelectedLocale(targetLocale);
            }

        });

    }
   
    private void Show_语言选择()
    {
        ShowPopBG();
        pop_语言选择.gameObject.SetActive(true);
        pop_语言选择.DOScale(1, 0.5f).From(0).SetEase(Ease.OutBack);
    }
    private void Hide_语言选择()
    {
        HidePopBG();
        pop_语言选择.gameObject.SetActive(false);
    }

    #endregion











    /// <summary>
    /// 2026
    /// </summary>
    /// <returns>万事顺遂</returns>
    public static string Hi_2026()
    {
        return "所求皆如愿~ (☆▽☆)";
    }








}

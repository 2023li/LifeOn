using System;
using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_Pause : PanelBase
{
    public override UILayer Layer => UILayer.Popup;

    [SerializeField] private Button btn_Continue;
    [SerializeField] private Button btn_LoadGame;
    [SerializeField] private Button btn_Setting;
    [SerializeField] private Button btn_Save;
    [SerializeField] private Button btn_QuickSave;
    [SerializeField] private Button btn_Quit;


    protected void Awake()
    {
        // 绑定按钮关闭自己

        btn_Continue.onClick.AddListener(() => Back());

        btn_LoadGame.onClick.AddListener(() =>
        {

        });

        btn_Setting.onClick.AddListener(async () =>
        {
            await UIManager.Instance.ShowPanel<UIPanel_Setting>();
        });

        btn_Save.onClick.AddListener(() =>
        {

        });
        btn_QuickSave.onClick.AddListener( async () =>
        {

            await AppManager.Instance.WaitRunTaskMainThread(() => { PersistentManager.Instance.SaveGame(); });

        });

        btn_Quit.onClick.AddListener(async () =>
        {
           
        });



    }

    public override bool Back(params object[] args)
    {
        base.Back(args);

        UIManager.Instance.HidePanel(this);
        return true;

    }







}

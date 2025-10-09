using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_Pause : PanelBase, IBackHandler
{

    [AutoBind] public Button btn_Continue;
    [AutoBind] public Button btn_LoadGame;
    [AutoBind] public Button btn_Setting;
    [AutoBind] public Button btn_Save;
    [AutoBind] public Button Quit;


    protected override void Awake()
    {
        base.Awake();

        InputManager.Instance.Register(this);
    }


    public short Priority { get ; set; } = LOConstant.InputPriority.Priority_暂停面板;

    public bool TryHandleBack()
    {
        if (!UIManager.Instance.IsPanelShowing<UIPanel_Pause>())
        {
            return false;
        }
           


        UIManager.Instance.HidePanel<UIPanel_Pause>();

        return true;
    }






}

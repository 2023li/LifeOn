using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_Pause : PanelBase
{

    [SerializeField] private Button btn_Continue;
    [SerializeField] private Button btn_LoadGame;
    [SerializeField] private Button btn_Setting;
    [SerializeField] private Button btn_Save;
    [SerializeField] private Button Quit;

  
    protected  void Awake()
    {
       

      

        // 绑定按钮关闭自己
        if (btn_Continue != null)
            btn_Continue.onClick.AddListener(() => OnHide());

        btn_Save.onClick.AddListener(() =>
        {

        });




    }


    
  





}

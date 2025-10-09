using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_GameMain : PanelBase
{

    [AutoBind] public Button btn_打开建造;

    [AutoBind("建造选择")] public UIItem_BuildingSelection item_建造选择;


    private void Reset()
    {
        this.AutoBindFields();
    }

    protected override void Awake()
    {
        btn_打开建造.onClick.AddListener(() =>
        {
            item_建造选择.Show();
        });
    }



    [Button]
    public void Test()
    {
        item_建造选择.Show();

    }

}

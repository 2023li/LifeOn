using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_GameMain : PanelBase
{

    [AutoBind] public Button btn_打开建造;

    [AutoBind] public Button btn_下回合;

    [AutoBind("建造选择")] public UIItem_BuildingSelection item_建造选择;


    private void Reset()
    {
        this.AutoBindFields();
    }

    protected override void Awake()
    {
        btn_打开建造.onClick.AddListener(() => { item_建造选择.Show(); });


        btn_下回合.onClick.AddListener(() => { TurnSystem.Instance.EndTurn(); });
    }


    #region 顶部HUD
    [AutoBind("text_回合数")] TMP_Text text_TurnText;

    #endregion 

    private RectTransform rt_建筑信息;
    public void ShowBuildingInfo<T>()where T :BuildingInfoPanelBase
    {
        
    }



}

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

    [SerializeField, LabelText("按钮_打开科技面板")]
    private Button btn_打开科技面板;

    [SerializeField,LabelText("按钮_关闭科技面板")]
    private Button btn_关闭科技面板;

    [SerializeField, LabelText("面板_科技面板")]
    private RectTransform panel_科技面板;



    private void Reset()
    {
        this.AutoBindFields();
    }

    protected override void Awake()
    {
        this.AutoBindFields();

        btn_打开建造.onClick.AddListener(() => { item_建造选择.Show(); });


        btn_下回合.onClick.AddListener(() => { TurnSystem.Instance.EndTurn(); });


        btn_打开科技面板.onClick.AddListener(() =>
        {
            panel_科技面板.gameObject.SetActive(true);



        });
        btn_关闭科技面板.onClick.AddListener(() =>
        {
            panel_科技面板.gameObject.SetActive(false);


        });



    }

    private void Start()
    {
        Start_顶部HUD();
    }

    #region 顶部HUD
    [SerializeField,LabelText("文本_回合数")] TMP_Text text_TurnText;

    public void Start_顶部HUD()
    {
        TurnSystem.OnTurnPhaseChange += (p) =>
        {
            if (p==TurnPhase.开始准备阶段)
            {
                text_TurnText.text = TurnSystem.Instance.NumberOfRounds.ToString();
            }
        };
    }

    #endregion 

    private RectTransform rt_建筑信息;
    public void ShowBuildingInfo<T>()where T :BuildingInfoPanelBase
    {
        
    }



}

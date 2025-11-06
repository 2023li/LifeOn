using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_GameMain : PanelBase
{

    [SerializeField, LabelText("Btn_打开建造")] private Button btn_打开建造;

    [SerializeField, LabelText("Btn_结束回合")] private Button btn_下回合;

    [SerializeField, LabelText("Item_建造选择")] private UIItem_BuildingSelection item_建造选择;

    [SerializeField, LabelText("按钮_打开科技面板")]
    private Button btn_打开科技面板;

    [SerializeField,LabelText("按钮_关闭科技面板")]
    private Button btn_关闭科技面板;

    [SerializeField, LabelText("面板_科技面板")]
    private RectTransform panel_科技面板;



    private void Reset()
    {
        
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


        OnAwake_建筑信息();
    }



    private void OnEnable()
    {
        OnEnable_建筑信息();
    }

    private void Start()
    {
        Start_顶部HUD();
        OnStart_建筑信息();
    }

    private void OnDisable()
    {
        OnDisable_建筑信息();
    }

    #region 顶部HUD
    
    [FoldoutGroup("HUD"),SerializeField,LabelText("文本_回合数")] TMP_Text text_TurnText;

    [FoldoutGroup("HUD"), SerializeField, LabelText("img_金币")] Image img_金币;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_金币")] TMP_Text txt_金币;

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




    #region 建筑信息
    [SerializeField] private RectTransform rt_建筑信息;

    private void OnAwake_建筑信息()
    {
        buildingBriefCache = new Dictionary<string, BuildingBriefPanelBase>();
    }

    private void OnStart_建筑信息()
    {
        buildingCommonBrie = ResourceRouting.Instance.GetBuildingCommonBrie();


    }

    private void OnEnable_建筑信息()
    {
        TheGame.Instance.BuildingSelector.Event_SelectedBuilding += ShowBuildingBrief;
    }
    private void OnDisable_建筑信息()
    {
        if (TheGame.HasInstance)
        {
            TheGame.Instance.BuildingSelector.Event_SelectedBuilding -= ShowBuildingBrief;
        }
      
    }


    private BuildingBriefPanelBase buildingCommonBrie;
    private Dictionary<string, BuildingBriefPanelBase> buildingBriefCache;
    private void ShowBuildingBrief(BuildingInstance building)
    {
        if (building == null)
        {
            // 如果没选中建筑，隐藏所有面板
            foreach (var item in buildingBriefCache.Values)
            {
                if (item != null)
                    item.gameObject.SetActive(false);
            }
            return;
        }

        // 1. 先隐藏所有已有面板
        foreach (var item in buildingBriefCache.Values)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }

        // 2. 确定要使用的面板预制体（通用或专用）
        BuildingBriefPanelBase prefab = building.Def.UIPanelPrefab_Brief == null ? buildingCommonBrie : building.Def.UIPanelPrefab_Brief;
        if (prefab == null)
        {
            Debug.LogWarning($"建筑 {building.Def.name} 没有关联的 BriefPanelPrefab，也没有设置通用的兜底");
            return;
        }

        // 3. 根据 panelGuid 查找缓存
        BuildingBriefPanelBase panelInstance;
        if (!buildingBriefCache.TryGetValue(prefab.PanelGuid, out panelInstance) || panelInstance == null)
        {
            // 不存在则实例化
            panelInstance = Instantiate(prefab, rt_建筑信息);
            buildingBriefCache[prefab.PanelGuid] = panelInstance;
        }

        // 4. 显示该面板
        panelInstance.Show(rt_建筑信息, building);
    }


    #endregion



}

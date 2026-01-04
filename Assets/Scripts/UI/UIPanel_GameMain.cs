using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_GameMain : PanelBase
{
    public override bool Back(params object[] args)
    {
        base.Back(args);
        //这个应该由GameManager控制
        // _ = UIManager.Instance.ShowPanel<UIPanel_Pause>(UIManager.UILayer.Main);
        return false;
    }


    public Button btn_打开设置面板;



    private IGameContext ctx;


    [SerializeField, LabelText("Btn_打开建造")] private Button btn_打开建造;

  

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

    [SerializeField,LabelText("btn_打开仓库")]
    private Button btn_打开仓库;
    [SerializeField,LabelText("仓库面板")]
    private UIItem_WarehousePanel warehousePanel;



    #region 结束回合按钮
    [SerializeField, LabelText("Btn_结束回合")]
    [FoldoutGroup("下回合")]
    private Button btn_下回合;

    [SerializeField, LabelText("go_回合结束图标")]
    [FoldoutGroup("下回合")]
    private GameObject go_回合结束图标;

    #endregion


    protected void Awake()
    {
        this.AutoBindFields();

        btn_打开设置面板.onClick.AddListener(() =>
        {
           _ = UIManager.Instance.ShowPanel<UIPanel_Setting>(UIManager.UILayer.Main);
        });


        btn_打开建造.onClick.AddListener(() => { item_建造选择.Show(); });


        btn_下回合.onClick.AddListener(() => { GameContext.Instance.Turn.EndTurn(); });


        btn_打开科技面板.onClick.AddListener(() =>
        {
            panel_科技面板.gameObject.SetActive(true);
        });

        btn_关闭科技面板.onClick.AddListener(() =>
        {
            panel_科技面板.gameObject.SetActive(false);
        });


        btn_打开仓库.onClick.AddListener(() =>
        {
            warehousePanel.Show();
        });

        OnAwake_建筑信息();




    }



    private void OnEnable()
    {
        OnEnable_建筑信息();


        GameContext.Instance.Turn.OnTurnPhaseChange += Handle_PhaseChange;
        GameContext.Instance.Turn.OnTurnBlockCountChanged += Handle_TurnBlock;
    }

   
    private void Start()
    {
        ctx = GameContext.Instance;

        Start_顶部HUD();
        OnStart_建筑信息();

    }

    private void OnDisable()
    {
        OnDisable_建筑信息();
        GameContext.Instance.Turn.OnTurnPhaseChange -= Handle_PhaseChange;
        GameContext.Instance.Turn.OnTurnBlockCountChanged -= Handle_TurnBlock;
    }

  
    private void Handle_PhaseChange(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.结束准备阶段:
               

                break;
            case TurnPhase.资源消耗阶段:
                break;
            case TurnPhase.资源生产阶段:
                break;
            case TurnPhase.回合结束阶段:
                break;
            case TurnPhase.开始准备阶段:

                break;
            default:
                break;
        }
    }


    private void Handle_TurnBlock(int block)
    {
        // 如果你的语义是：有阻塞（block > 0）就【显示图标 + 启用按钮】
        bool hasBlock = block > 0;

        go_回合结束图标.SetActive(hasBlock);



        btn_下回合.gameObject.SetActive(!hasBlock);
    }


    #region 顶部HUD

    [FoldoutGroup("HUD"), SerializeField, LabelText("文本_回合数")] TMP_Text text_TurnText;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_金币")] TMP_Text txt_金币;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_库存")] TMP_Text txt_库存;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_人口")] TMP_Text txt_人口;

    // --- 滚动数值缓存 ---
    private float _display_Gold = 0;
    private float _display_UsedCapacity = 0;
    private float _display_EmployedWorkers = 0;

    // --- Tweener 引用 (防止动画冲突) ---
    private Tweener _tween_Gold;
    private Tweener _tween_Inventory;
    private Tweener _tween_Population;

    public void Start_顶部HUD()
    {
        // 1. 回合数监听 (保持不变)
        GameContext.Instance.Turn.OnTurnPhaseChange += (p) =>
        {
            if (p == TurnPhase.开始准备阶段)
            {
                text_TurnText.text = GameContext.Instance.Turn.NumberOfRounds.ToString();
            }
        };

        // 2. 库存监听
        // 初始化显示
        _display_UsedCapacity = ctx.ResourceNetwork.UsedCapacity;
        RefreshInventoryText(_display_UsedCapacity, ctx.ResourceNetwork.TotalCapacity);

        GameContext.Instance.ResourceNetwork.OnResourceNetworkStateChange += () =>
        {
            // 数据源：已用库存 / 总库存
            Anim_UpdateInventory(ctx.ResourceNetwork.UsedCapacity, ctx.ResourceNetwork.TotalCapacity);
        };

        // 3. 人口监听
        // 初始化显示 (逻辑修正：显示 就业/总人口)
        _display_EmployedWorkers = ctx.HumanResourcesNetwork.TotalWorkers;
        RefreshPopulationText(_display_EmployedWorkers, ctx.HumanResourcesNetwork.TotalPopulation);

        ctx.HumanResourcesNetwork.OnHumanResourcesChange += () =>
        {
            // 数据源：就业人口 / 总人口
            Anim_UpdatePopulation(ctx.HumanResourcesNetwork.TotalWorkers, ctx.HumanResourcesNetwork.TotalPopulation);
        };

        // 4. 金币监听
        UpdateGoldUI(true); // true 表示强制立即刷新，不播动画
        ctx.ResourceNetwork.OnResourceAmountChange += (def) =>
        {
            if (def.Id == SupplyEnum.SD_金币.ToString())
            {
                UpdateGoldUI(false);
            }
        };
    }

    // ================= 库存逻辑 =================
    // 显示格式：已使用库存/总库存
    private void Anim_UpdateInventory(float targetUsed, float targetTotal)
    {
        _tween_Inventory?.Kill();

        // 滚动 "已用库存"
        _tween_Inventory = DOVirtual.Float(_display_UsedCapacity, targetUsed, 0.5f, (val) =>
        {
            _display_UsedCapacity = val;
            RefreshInventoryText(val, targetTotal);
        }).SetEase(Ease.OutQuad);
    }

    private void RefreshInventoryText(float used, float total)
    {
        txt_库存.text = $"{(int)used}/{(int)total}";
    }

    // ================= 人口逻辑 =================
    // 显示格式：就业人口/总人口
    private void Anim_UpdatePopulation(float targetEmployed, float targetTotalPop)
    {
        _tween_Population?.Kill();

        // 滚动 "就业人口"
        _tween_Population = DOVirtual.Float(_display_EmployedWorkers, targetEmployed, 0.5f, (val) =>
        {
            _display_EmployedWorkers = val;
            RefreshPopulationText(val, targetTotalPop);
        }).SetEase(Ease.OutQuad);
    }

    private void RefreshPopulationText(float employed, float totalPop)
    {
        txt_人口.text = $"{(int)employed}/{(int)totalPop}";
    }

    // ================= 金币逻辑 =================
    // 显示格式：金币数量
    public void UpdateGoldUI(bool immediate = false)
    {
        int targetGold = ctx.ResourceNetwork.GetSupplyAmount(SupplyEnum.SD_金币);

        _tween_Gold?.Kill();

        if (immediate)
        {
            _display_Gold = targetGold;
            txt_金币.text = targetGold.ToString();
        }
        else
        {
            _tween_Gold = DOVirtual.Float(_display_Gold, targetGold, 0.8f, (val) =>
            {
                _display_Gold = val;
                txt_金币.text = ((int)val).ToString();
            }).SetEase(Ease.OutExpo);
        }
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


    #region 物流网络相关
   
    #endregion






}

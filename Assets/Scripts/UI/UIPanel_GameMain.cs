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


        btn_下回合.onClick.AddListener(() => { TurnSystem.Instance.EndTurn(); });


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

        TurnSystem.OnTurnPhaseChange += Handle_PhaseChange;
        TurnSystem.OnTurnBlockCountChanged += Handle_TurnBlock;
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
        TurnSystem.OnTurnPhaseChange -= Handle_PhaseChange;
        TurnSystem.OnTurnBlockCountChanged -= Handle_TurnBlock;
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

    [FoldoutGroup("HUD"),SerializeField,LabelText("文本_回合数")] TMP_Text text_TurnText;

    //[FoldoutGroup("HUD"), SerializeField, LabelText("img_金币")] Image img_金币;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_金币")] TMP_Text txt_金币;

    //[FoldoutGroup("HUD"), SerializeField, LabelText("img_库存")] Image img_库存;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_库存")] TMP_Text txt_库存;

    //[FoldoutGroup("HUD"), SerializeField, LabelText("img_人口")] Image img_人口;
    [FoldoutGroup("HUD"), SerializeField, LabelText("txt_人口")] TMP_Text txt_人口;

    // [新增] 2. 用于记录当前显示数值的变量（用于计算滚动起点）
    private float _display_Gold = 0;
    private float _display_UsedCapacity = 0;
    private float _display_TotalWorkers = 0;
    // [新增] 3. 保存Tweener引用，防止快速变化时动画冲突
    private Tweener _tween_Gold;
    private Tweener _tween_Inventory;
    private Tweener _tween_Population;
    public void Start_顶部HUD()
    {
        // 监听回合变更 (保持不变)
        TurnSystem.OnTurnPhaseChange += (p) =>
        {
            if (p == TurnPhase.开始准备阶段)
            {
                text_TurnText.text = TurnSystem.Instance.NumberOfRounds.ToString();
            }
        };

        _display_UsedCapacity = ctx.ResourceNetwork.UsedCapacity;
        UpdateInventoryText(_display_UsedCapacity, ctx.ResourceNetwork.TotalCapacity);

        GameContext.Instance.ResourceNetwork.OnResourceNetworkStateChange += () =>
        {
            // 当数据变化时，执行滚动动画
            Anim_UpdateInventory(ctx.ResourceNetwork.UsedCapacity, ctx.ResourceNetwork.TotalCapacity);
        };

        // --- 人口逻辑修改 ---
        // 初始化
        _display_TotalWorkers = ctx.HumanResourcesNetwork.TotalWorkers;
        UpdatePopulationText(_display_TotalWorkers, ctx.HumanResourcesNetwork.Unemployed);

        ctx.HumanResourcesNetwork.OnHumanResourcesChange += () =>
        {
            // 动画更新
            Anim_UpdatePopulation(ctx.HumanResourcesNetwork.TotalWorkers, ctx.HumanResourcesNetwork.Unemployed);
        };

        // --- 金币逻辑 (假设你有一个获取金币的地方) ---
        // 你的原始代码中没有金币的事件监听，这里我预留一个方法供你外部调用
        UpdateGoldUI();
        ctx.ResourceNetwork.OnResourceAmountChange += (def) =>
        {
            if(def.Id == SupplyEnum.SD_金币.ToString())
            {
                UpdateGoldUI();
            }
        };

    }
    // [新增] 4. 封装库存更新动画
    private void Anim_UpdateInventory(float targetUsed, float targetTotal)
    {
        // 如果上一个动画还在跑，先杀掉，防止冲突
        _tween_Inventory?.Kill();

        // 从 "当前显示的数值" 滚动到 "目标数值"
        _tween_Inventory = DOVirtual.Float(_display_UsedCapacity, targetUsed, 0.5f, (val) =>
        {
            _display_UsedCapacity = val; // 更新中间值
            UpdateInventoryText(val, targetTotal);
        }).SetEase(Ease.OutQuad); // 使用 OutQuad 让动画在结束时变慢，更有质感
    }

    // 辅助方法：只负责更新文本字符串
    private void UpdateInventoryText(float used, float total)
    {
        // (int)used 会丢弃小数，只显示整数
        txt_库存.text = $"库存：{(int)used}/{total}";
    }

    // [新增] 5. 封装人口更新动画
    private void Anim_UpdatePopulation(float targetWorkers, float targetUnemployed)
    {
        _tween_Population?.Kill();

        // 这里的逻辑是：让"总人口"(分子)滚动，"失业人口"(分母)通常我们直接刷新
        // 如果你想让分母也滚动，逻辑会复杂一些，通常滚动分子就很有感觉了
        _tween_Population = DOVirtual.Float(_display_TotalWorkers, targetWorkers, 0.5f, (val) =>
        {
            _display_TotalWorkers = val;
            UpdatePopulationText(val, targetUnemployed);
        }).SetEase(Ease.OutQuad);
    }

    private void UpdatePopulationText(float workers, float unemployed)
    {
        txt_人口.text = $"人口：{(int)workers}/{unemployed}";
    }

    // [新增] 6. 金币更新动画 (供外部调用，例如 UpdateGold(100))
    public void UpdateGoldUI()
    {
        int targetGold = ctx.ResourceNetwork.GetSupplyAmount(SupplyEnum.SD_金币);
        _tween_Gold?.Kill();

        _tween_Gold = DOVirtual.Float(_display_Gold, targetGold, 0.8f, (val) =>
        {
            _display_Gold = val;
            txt_金币.text = ((int)val).ToString(); // 或者 $"金币：{(int)val}"
        }).SetEase(Ease.OutExpo); // 金币通常变化幅度大，用 OutExpo 会更有冲击力
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using Moyo.Unity;
using static BuildingBuilder;

public class BuildingBuilder : MonoSingleton<BuildingBuilder>,IBackHandler,IMoyoEventListener<BuildingEvent>
{


    public struct BuildingEvent
    {
        static BuildingEvent e;

        public BuildingArchetype def;

        public static void Trigger(BuildingArchetype buildingDef)
        {
            e.def = buildingDef;
            MoyoEventManager.TriggerEvent(e);
        }

    }

    public void OnMoyoEvent(BuildingEvent eventType)
    {

        this.EnterBuildMode(eventType.def);

    }




    private enum ConstructionProcess
    {
        None,
        Placing,
        AwaitingConfirmation,
    }

    [ShowInInspector, ReadOnly]
    private ConstructionProcess process;

    [ShowInInspector, ReadOnly]
    private BuildingArchetype currentBuildDef;

    public BuildingInstance BuildingPrefab;
   
    [SerializeField] private TileBase green;
    [SerializeField] private TileBase red;
    [SerializeField] private TileBase crimson; // 深红：占用
    [LabelText("确认面板地址")] [SerializeField] private string confirmPanelAddress = "BuildingConfirmPanel";
    [SerializeField] private UIManager.UILayer confirmPanelLayer = UIManager.UILayer.Popup;

    private BuildingConfirmPanel confirmPanelInstance;
    private bool lastPlacementValid;
    private readonly List<Vector3Int> tempBuildingCells = new List<Vector3Int>(256);
    private Vector3 _confirmAnchorWorld;

    public short Priority { get; set; } = LOConstant.InputPriority.Priority_BuildingBuilder;

    #region 生命周期

    private void OnEnable()
    {
        this.MoyoEventStartListening();

        if (InputManager.Instance == null) return;

        InputManager.Instance.Register(this);

        InputManager.Instance.Building_OnChangeCoordinates += Handle_放置;
        InputManager.Instance.Building_OnConfirmPlacement += Handle_确认放置;
        InputManager.Instance.Building_OnConfirmConstruction += Handle_完成建造;

        // 统一取消
    }

    private void OnDisable()
    {
        this.MoyoEventStopListening();

        if (!InputManager.HasInstance) return;


        InputManager.Instance.Building_OnChangeCoordinates -= Handle_放置;
        InputManager.Instance.Building_OnConfirmPlacement -= Handle_确认放置;
        InputManager.Instance.Building_OnConfirmConstruction -= Handle_完成建造;

    }

    #endregion

    #region 外部 API

    [Button]
    public void EnterBuildMode(BuildingArchetype buildingDef)
    {
        if (buildingDef == null) return;

        currentBuildDef = buildingDef;
        lastPlacementValid = false;
        tempBuildingCells.Clear();
        GridSystem.Instance.ClearHighlight();
        HideConfirmBar();

        process = ConstructionProcess.Placing;

        // 打开建造输入
        InputManager.Instance?.EnableBuildingMap();
    }

    /// <summary>彻底退出建造（可选暴露）</summary>
    [Button]
    public void ExitBuildMode()
    {
        process = ConstructionProcess.None;
        currentBuildDef = null;
        tempBuildingCells.Clear();
        GridSystem.Instance.ClearHighlight();
        HideConfirmBar();
        InputManager.Instance?.DisableBuildingMap();
        Debug.Log(1);
    }

    #endregion

    #region 事件处理

    // 鼠标移动时（来自 InputManager 的转发）
    private void Handle_放置(Vector2 screenMousePos)
    {
        if (process != ConstructionProcess.Placing || currentBuildDef == null) return;

        // 等距网格中的连续坐标（中心）
        var center = GridSystem.Instance.GetScreenPointInGridPos(screenMousePos);

        // 按 S×S 生成占地
        var cells = CoordinateCalculator.GetBuildingCells(center, currentBuildDef.Size);

        tempBuildingCells.Clear();
        tempBuildingCells.AddRange(cells);

        // 分类：占用/空闲
        var occupied = new List<Vector3Int>();
        var free = new List<Vector3Int>();

        foreach (var cell in tempBuildingCells)
        {
            if (GridSystem.Instance.IsOccupy(cell)) occupied.Add(cell);
            else free.Add(cell);
        }

        lastPlacementValid = occupied.Count == 0;

        if (lastPlacementValid)
        {
            GridSystem.Instance.SetHighlight(
                new GridSystem.HighlightSpec(tempBuildingCells, green ?? GridSystem.Instance.visualizationTile)
            );
        }
        else
        {
            GridSystem.Instance.SetHighlight(
                new GridSystem.HighlightSpec(occupied, crimson ?? GridSystem.Instance.visualizationTile),
                new GridSystem.HighlightSpec(free, red ?? GridSystem.Instance.visualizationTile)
            );
        }
    }

    private void Handle_确认放置()
    {
        if (process != ConstructionProcess.Placing || currentBuildDef == null) return;

        if (!lastPlacementValid) return;
        foreach (var cell in tempBuildingCells)
            if (GridSystem.Instance.IsOccupy(cell)) return;

        process = ConstructionProcess.AwaitingConfirmation;

        // 计算确认条锚点（优先用占地中心，兜底用鼠标格）
        _confirmAnchorWorld = GetWorldAnchorFromCells(tempBuildingCells);
        ShowConfirmBarAt(_confirmAnchorWorld);
    }

    public bool TryHandleBack()
    {
        if (process == ConstructionProcess.None)
        {
            return false; 
        }

        Handle_取消();
        return true;
    }
    private void Handle_取消()
    {
        switch (process)
        {
            case ConstructionProcess.Placing:
                // 放置态：彻底退出建造
                ExitBuildMode();
                break;

            case ConstructionProcess.AwaitingConfirmation:
                // 等待确认：退回放置
                process = ConstructionProcess.Placing;
                HideConfirmBar();
                GridSystem.Instance.ClearHighlight();
                break;

            default:
                // 无：不做事（或按需清理）
                break;
        }
    }

    private void Handle_完成建造()
    {
        if (process != ConstructionProcess.AwaitingConfirmation || currentBuildDef == null) return;

        // 二次占用校验（避免竞态）
        foreach (var cell in tempBuildingCells)
        {
            if (GridSystem.Instance.IsOccupy(cell))
            {
                Debug.LogWarning("目标区域已被占用，建造失败，返回放置状态。");
                process = ConstructionProcess.Placing;
                HideConfirmBar();
                return;
            }
        }

        // 落锚点（用于建筑摆放）
        var anchor = _confirmAnchorWorld;

        // 标记占用
        foreach (var cell in tempBuildingCells)
        {
            GridSystem.Instance.SetOccupy(cell);
        }

        // 实例化 & 定位（若你的 BuildingInstance.Construction 内部会定位，可省略下面两行）

        BuildingInstance b = Instantiate(BuildingPrefab);
        b.transform.position = anchor;
        b.Initialize(currentBuildDef);

        Debug.Log("完成建造");
        process = ConstructionProcess.None;
        HideConfirmBar();
        GridSystem.Instance.ClearHighlight();

        // 关闭建造输入
        InputManager.Instance?.DisableBuildingMap();
    }

    private void Handle_取消建造()
    {
        if (process != ConstructionProcess.AwaitingConfirmation) return;

        process = ConstructionProcess.Placing;
        HideConfirmBar();
        GridSystem.Instance.ClearHighlight();
        // 保持在建造模式中，仍允许继续放置
    }

    #endregion

    #region UI：确认条

    private async void ShowConfirmBarAt(Vector3 anchorWorldPos)
    {
        if (UIManager.Instance == null) return;

        var args = new BuildingConfirmPanel.Args
        {
            OnConfirm = Handle_完成建造,
            OnCancel = Handle_取消建造
        };

        var address = string.IsNullOrWhiteSpace(confirmPanelAddress) ? null : confirmPanelAddress;
        var panel = await UIManager.Instance.ShowPanel<BuildingConfirmPanel>(confirmPanelLayer, address, args);
        if (panel == null) return;

        confirmPanelInstance = panel;
        confirmPanelInstance.SetWorldAnchor(anchorWorldPos);
    }

    private void HideConfirmBar()
    {
        if (UIManager.Instance == null) return;

        if (UIManager.Instance.IsPanelLoaded<BuildingConfirmPanel>())
        {
            UIManager.Instance.HidePanel<BuildingConfirmPanel>();
        }

        confirmPanelInstance = null;
    }

    #endregion

    #region 工具

    /// <summary>
    /// 用占地格反推“视觉中心”的世界坐标：奇数尺寸=格心；偶数尺寸=拐角。
    /// 落点会与你高亮逻辑一致。
    /// </summary>
    private Vector3 GetWorldAnchorFromCells(IReadOnlyCollection<Vector3Int> cells)
    {
        // 优先严格用你的 TryGetCenterFromCells（格坐标空间中心）
        if (CoordinateCalculator.TryGetCenterFromCells(cells, out var center, out var isCorner, out var size))
        {
            if (isCorner)
            {
                // 拐角位于整数格线交点：选其左下格的世界原点作为落锚（与 Tile 原点一致）
                var baseCell = new Vector3Int(Mathf.FloorToInt(center.x), Mathf.FloorToInt(center.y), 0);
                return GridSystem.Instance.CellToWorld(baseCell);
            }
            else
            {
                // 格心：取最近格再 + 半格（若 Tile 原点为左下）
                var baseCell = new Vector3Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), 0);
                return GridSystem.Instance.CellToWorld(baseCell) + (Vector3)(GridSystem.Instance.mapGrid.cellSize * 0.5f);
            }
        }

        // 兜底：用当前鼠标所在格
        var cell = GridSystem.Instance.GetMousePosCoordinates();
        return GridSystem.Instance.CellToWorld(cell);
    }

  



    #endregion
}

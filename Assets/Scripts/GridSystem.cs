using System;
using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class GridSystem : MonoSingleton<GridSystem>
{
    public enum Layer
    {
        地图边界, 水, 障碍, 道路, 特效
    }

    protected override bool IsDontDestroyOnLoad => false;


    public Grid mapGrid;
    public Tilemap tilemap_地图边界;
    public Tilemap tilemap_水;
    public Tilemap tilemap_障碍;
    public Tilemap tilemap_道路;
    public Tilemap tilemap_特效;

    [FoldoutGroup("可视化瓦片")]
    [LabelText("默认瓦片")]
    public Tile visualizationTile;

    [FoldoutGroup("可视化瓦片")]
    [LabelText("测试瓦片1")]
    public Tile tile1;


    [FoldoutGroup("可视化瓦片")]
    [LabelText("测试瓦片2")]
    public Tile tile2;


    [FoldoutGroup("可视化瓦片")]
    [LabelText("测试瓦片3")]
    public Tile tile3;


    [FoldoutGroup("可视化瓦片")]
    [LabelText("测试瓦片4")]
    public Tile tile4;









    private Dictionary<Layer, Tilemap> dic_LayerMap;
    private HashSet<Vector3Int> allCells;

    protected override void Awake()
    {
        base.Awake();
        Init();
    }
    public void Init()
    {
        dic_LayerMap = new Dictionary<Layer, Tilemap>()
        {
            {Layer.地图边界,tilemap_地图边界},
            {Layer.水,tilemap_水},
            {Layer.障碍,tilemap_障碍},
            {Layer.道路,tilemap_道路 },
            {Layer.特效,tilemap_特效},
        };


        allCells = new HashSet<Vector3Int>();
        // 遍历 tilemap 的所有已绘制区域
        BoundsInt bounds = tilemap_地图边界.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (tilemap_地图边界.HasTile(pos))
            {
                allCells.Add(pos);
            }
        }
    }



    //------------------------------------读写坐标-------------------------------------
    //获取鼠标/移动端为点击位置
    public Vector3Int GetMousePosCoordinates()
    {
        return GetScreenPointCoordinates(InputManager.Instance.MousePos);
    }

    //获取屏幕点上的坐标
    public Vector3Int GetScreenPointCoordinates(Vector3 pos)
    {
        Vector3 worldPos = InputManager.Instance.RealCamera.ScreenToWorldPoint(pos);

        Vector3 worldPosNoZ = new Vector3(worldPos.x, worldPos.y, 0);

        return mapGrid.WorldToCell(worldPosNoZ);

    }

    public Vector3 GetScreenPointInGridPos(Vector3 pos)
    {
        var cam = InputManager.Instance.RealCamera;
        float dz = mapGrid.transform.position.z - cam.transform.position.z;
        Vector3 sp = new Vector3(pos.x, pos.y, dz);
        Vector3 world = cam.ScreenToWorldPoint(sp);

        //先转本地，再做 LocalToCellInterpolated 得到等距网格的连续坐标
        Vector3 local = mapGrid.WorldToLocal(world);
        Vector3 iso = mapGrid.LocalToCellInterpolated(local);
        iso.z = 0f;
        return iso;
    }

    public Vector3 CellToWorld(Vector3Int coor)
    {
        return mapGrid.CellToWorld(coor);
    }



    /// <summary>
    /// 更精确的方法：考虑网格对齐
    /// </summary>
    /// <param name="gridPositions">网格坐标列表</param>
    /// <returns>精确对齐网格的中心点世界坐标</returns>
    public Vector3 GetCenterFromCellCenters(List<Vector3Int> gridPositions)
    {
        if (gridPositions == null || gridPositions.Count == 0)
        {
            Debug.LogWarning("网格坐标列表为空");
            return Vector3.zero;
        }

        // 获取每个格子的中心世界坐标并累加
        Vector3 sumWorld = Vector3.zero;
        foreach (Vector3Int gridPos in gridPositions)
        {
            // 获取每个格子的中心世界坐标
            Vector3 cellCenterWorld = mapGrid.GetCellCenterWorld(gridPos);
            sumWorld += cellCenterWorld;
        }

        // 计算平均值
        return CommonTools.Vector3NoZ(sumWorld / gridPositions.Count);
    }

    //------------------------------------坐标属性-------------------------------------


    public bool IsOccupy(Vector3Int coor)
    {
        return dic_LayerMap[Layer.障碍].HasTile(coor);
    }
    public void SetOccupy(Vector3Int coor)
    {
        dic_LayerMap[Layer.障碍].SetTile(coor, visualizationTile);
    }








    //------------------------------------可视化-------------------------------------
    private readonly List<Vector3Int> _lastCells = new List<Vector3Int>();
    public struct HighlightSpec
    {
        public readonly IEnumerable<Vector3Int> Coords;
        public readonly TileBase Tile; // 若你只用 Tile，改成 Tile

        public HighlightSpec(IEnumerable<Vector3Int> coords, TileBase tile = null)
        {
            Coords = coords;
            Tile = tile;
        }
    }
    //多种高亮
    public void SetHighlight(params HighlightSpec[] needSetHighlights)
    {
        ClearHighlight();

        if (needSetHighlights == null || needSetHighlights.Length == 0)
            return;

        var map = dic_LayerMap[Layer.特效];

        // 如果同一格被多次指定，后面的覆盖前面的
        var final = new Dictionary<Vector3Int, TileBase>();

        foreach (var spec in needSetHighlights)
        {
            if (spec.Coords == null) continue;

            var tile = spec.Tile ?? visualizationTile; // 允许不传则使用默认高亮Tile
            foreach (var c in spec.Coords)
            {
                if (!allCells.Contains(c)) continue;
                final[c] = tile;
            }
        }

        foreach (var kv in final)
        {
            map.SetTile(kv.Key, kv.Value);
            _lastCells.Add(kv.Key);
        }
    }
    //默认高亮
    public void SetHighlight(IEnumerable<Vector3Int> coords)
    {

        ClearHighlight();

        var map = dic_LayerMap[Layer.特效];

        foreach (var c in coords)
        {
            if (allCells.Contains(c))
            {
                map.SetTile(c, visualizationTile);
                _lastCells.Add(c);
            }
        }

    }
    //指定瓦片的高亮
    public void SetHighlight(IEnumerable<Vector3Int> coords, TileBase tile)
    {
        SetHighlight(new HighlightSpec(coords, tile));
    }



    public void ClearHighlight()
    {
        foreach (var c in _lastCells) tilemap_特效.SetTile(c, null);
        _lastCells.Clear();
    }


    public void ShowAuraHighlight(IGameContext context, AuraCategory category)
    {
        if (context == null || context.Environment == null)
        {
            ClearHighlight();
            return;
        }

        CityEnvironment environment = context.Environment;

        IEnumerable<Vector3Int> level1 = environment.EnumerateCells(category, value => value >= 1);
        IEnumerable<Vector3Int> level2 = environment.EnumerateCells(category, value => value >= 2);
        IEnumerable<Vector3Int> level3 = environment.EnumerateCells(category, value => value >= 3);

        HighlightSpec spec1 = new HighlightSpec(level1, tile1);
        HighlightSpec spec2 = new HighlightSpec(level2, tile2);
        HighlightSpec spec3 = new HighlightSpec(level3, tile3);

        SetHighlight(spec1, spec2, spec3);
    }


    [Button]
    private void Test(AuraCategory category)
    {
        GameContext context = FindObjectOfType<GameContext>();
        ShowAuraHighlight(context, category);
    }


}

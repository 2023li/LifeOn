using System;
using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    public Tile visualizationTile;


    private Dictionary<Layer, Tilemap> dic_LayerMap;
    private HashSet<Vector3Int> allCells;

    protected override void Awake()
    {
        base.Awake();
        Init();

        Halo = new HaloSystem();
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

    private void Update()
    {
        //Test();


    }

    public int r = 4;

    public void Test()
    {
        ClearHighlight();
        var c = GetScreenPointCoordinates(Input.mousePosition);

        var cs = CoordinateCalculator.GetBuildingCells(c, r);

        //SetTile(Layer.特效, cs, visualizationTile);

        SetHighlight(cs);


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


    //------------------------------------坐标属性-------------------------------------
   

    public bool IsOccupy(Vector3Int coor)
    {
        return dic_LayerMap[Layer.障碍].HasTile(coor);
    }
    public void SetOccupy(Vector3Int coor)
    {
        dic_LayerMap[Layer.障碍].SetTile(coor,visualizationTile);
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





    public HaloSystem Halo {  get;private set; }
   


    public class HaloSystem
    {
        public HaloSystem()
        {
            dic_HaloEffectMap = new Dictionary<HaloEffectType, Dictionary<Vector3Int, short>>()
            {
                {HaloEffectType.医疗, new Dictionary<Vector3Int, short>() },
                {HaloEffectType.环境, new Dictionary<Vector3Int, short>() },
                {HaloEffectType.治安, new Dictionary<Vector3Int, short>() },
            };

        }
        private Dictionary<HaloEffectType, Dictionary<Vector3Int, short>> dic_HaloEffectMap;

        //public void AddHaloEffect(IEnumerable<Vector3Int> vector3Ints, HaloEffectRangeValue values) => ModifyEffect(vector3Ints, values, true);

        //public void RemoveEffect(IEnumerable<Vector3Int> vector3Ints, HaloEffectRangeValue values) => ModifyEffect(vector3Ints, values, false);

        //private void ModifyEffect(IEnumerable<Vector3Int> vector3Ints, HaloEffectRangeValue values, bool add)
        //{
        //    var coors = CoordinateCalculator.GetCellsWithinRadius(vector3Ints, values.Range);
        //    var dic = dic_HaloEffectMap[values.Type];

        //    foreach (var c in coors)
        //    {
        //        if (Instance.allCells.Contains(c))
        //        {
        //            short delta = (short)(add ? values.Value : -values.Value);
        //            if (!dic.ContainsKey(c))
        //                dic[c] = delta;
        //            else
        //                dic[c] += delta;

        //            if (dic[c] == 0)
        //                dic.Remove(c);
        //        }
        //    }
        //}


        public short GetHaloValue(HaloEffectType haloEffectType,Vector3Int coor)
        {
            return dic_HaloEffectMap[haloEffectType][coor];
        }
    }

}

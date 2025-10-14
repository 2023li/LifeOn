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





    public HaloSystem Halo { get; private set; }



    public class HaloSystem
    {
        public HaloSystem()
        {
            dic_HaloEffectMap = new Dictionary<HaloEffectType, Dictionary<Vector3Int, int>>()
            {
                {HaloEffectType.医疗, new Dictionary<Vector3Int, int>() },
                {HaloEffectType.环境, new Dictionary<Vector3Int, int>() },
                {HaloEffectType.治安, new Dictionary<Vector3Int, int>() },
            };

        }
        //第一层字典：类型   第二层字典  坐标数值
        private Dictionary<HaloEffectType, Dictionary<Vector3Int, int>> dic_HaloEffectMap;

        public void AddHaloEffect(IEnumerable<Vector3Int> vector3Ints, HaloEffectRangeValue values) => ModifyEffect(vector3Ints, values, true);

        public void RemoveEffect(IEnumerable<Vector3Int> vector3Ints, HaloEffectRangeValue values) => ModifyEffect(vector3Ints, values, false);

        private void ModifyEffect(IEnumerable<Vector3Int> vector3Ints, HaloEffectRangeValue values, bool add)
        {
            var coors = CoordinateCalculator.CellsInRadius(vector3Ints, values.Range);
            var dic = dic_HaloEffectMap[values.Type];

            foreach (var c in coors)
            {
                if (!Instance.allCells.Contains(c)) continue;

                if (add)
                {
                    dic[c] = (short)((dic.TryGetValue(c, out var cur) ? cur : 0) + values.Value);
                }
                else
                {
                    if (!dic.TryGetValue(c, out var cur)) continue; // 没有就不用减
                    var newVal = (short)(cur - values.Value);
                    if (newVal == 0) dic.Remove(c);
                    else dic[c] = newVal;
                }
            }
        }

        private IEnumerable<Vector3Int> ActiveHaloCells() => dic_HaloEffectMap.Values.SelectMany(d => d.Keys).Distinct();

        public int GetHaloValue(HaloEffectType haloEffectType, Vector3Int coor)
        {
            Dictionary<Vector3Int, int> map = dic_HaloEffectMap[haloEffectType];
            return map.TryGetValue(coor, out int v) ? v : 0;
        }


        //大于最小值
        public Func<Vector3Int, bool> HaloAtLeast(HaloEffectType type, short min) => c => GetHaloValue(type, c) >= min;
        //小于最大值
        public Func<Vector3Int, bool> HaloAtMost(HaloEffectType type, short max) => c => GetHaloValue(type, c) <= max;
        //等于
        public Func<Vector3Int, bool> HaloEquals(HaloEffectType type, short value) => c => GetHaloValue(type, c) == value;



        
        public void ShowHaloByType(HaloEffectType type)
        {
            IEnumerable<Vector3Int> level1 = GetCoordinatesSatisfyingAuraConditionFast(HaloAtLeast(type,1));
            IEnumerable<Vector3Int> level2 = GetCoordinatesSatisfyingAuraConditionFast(HaloAtLeast(type, 2));
            IEnumerable<Vector3Int> level3 = GetCoordinatesSatisfyingAuraConditionFast(HaloAtLeast(type, 3));

            HighlightSpec spec1 = new HighlightSpec(level1,Instance.tile1);
            HighlightSpec spec2 = new HighlightSpec(level2, Instance.tile2);
            HighlightSpec spec3 = new HighlightSpec(level3, Instance.tile3);


            Instance.SetHighlight(spec1, spec2, spec3);
        }


        public IEnumerable<Vector3Int> GetCoordinatesSatisfyingAuraConditionFast(params Func<Vector3Int, bool>[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
                yield break;

            foreach (var c in ActiveHaloCells())
            {
                bool pass = true;
                for (int i = 0; i < conditions.Length; i++)
                {
                    Func<Vector3Int, bool> cond = conditions[i];
                    if (cond == null)
                    {
                        continue;
                    }
                    if (!cond(c))
                    {
                        pass = false;
                        break;
                    }
                }
                if (pass)
                {
                    yield return c;
                }
            }
        }

        public IEnumerable<Vector3Int> GetCoordinatesSatisfyingAuraConditionFast(List<Func<Vector3Int, bool>> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                yield break;

            foreach (var c in ActiveHaloCells())
            {
                bool pass = true;
                for (int i = 0; i < conditions.Count; i++)
                {
                    Func<Vector3Int, bool> cond = conditions[i];
                    if (cond == null)
                    {
                        continue;
                    }
                    if (!cond(c))
                    {
                        pass = false;
                        break;
                    }
                }
                if (pass)
                {
                    yield return c;
                }
            }
        }




    }





    [Button]
    private void Test(HaloEffectType t)
    {
        Halo.ShowHaloByType(t);
    }

}

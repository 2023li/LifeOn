using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using Moyo.Unity;
using System;

/// <summary>
/// UI 节点连线的全局管理器。
/// 适用于 2D 项目中存在多个 World-Space Canvas 的情况。
/// 
/// 核心职责：
/// 1. 统一处理跨 Canvas 的射线检测（EventSystem.RaycastAll）。  
/// 2. 管理连线的创建 / 延长 / 删除。  
/// 3. 在拖拽过程中实时更新临时尾端位置，结束时固化拓扑。  
/// 
/// 规则要点（由当前逻辑隐含）：
/// - 只有 start 节点允许“新建”一条线段链（可多条）。  
/// - 非 start 节点只能“延长”自己作为尾节点的已有线。  
/// - 若尾节点存在多条线，则按“鼠标离哪条线更近”来选中延长目标，并加入误触阈值。  
/// - 新建时若 AB 段已存在则不重复创建。  
/// - start 节点拖空（没连到任何目标）视为撤销：删除该 start 最近创建的那条线。  
/// </summary>
public class ConnectionManager : MonoSingleton<ConnectionManager>
{
    #region 画线
    protected override bool IsDontDestroyOnLoad => false;
   

    [Header("Line Settings")]
    [LabelText("线宽")]
    [MinValue(0.0001f)]
    public float lineWidth = 0.02f;

    /// <summary>
    /// 当一个节点作为尾节点挂了多条线时，用于“挑中哪条线进行延长”。
    /// 计算方式： pickRadius = lineWidth * lineFalseTouchDistanceThreshold  
    /// 值越大，越容易选中远处的线；值越小，越不容易误触。
    /// </summary>
    [SerializeField, LabelText("操作线误触阈值(倍数)")]
    [MinValue(0f)]
    private float lineFalseTouchDistanceThreshold = 10f;

    /// <summary>
    /// 当前所有“已固化”的连线集合。
    /// </summary>
    private readonly List<ConnectionLine> _lines = new List<ConnectionLine>();
    public IReadOnlyList<ConnectionLine> Lines => _lines;

    /// <summary>
    /// 正在拖拽中的那条线（可能是新建，也可能是延长）。
    /// </summary>
    private ConnectionLine _activeLine;

    /// <summary>
    /// 当前拖拽模式。
    /// </summary>
    private DragMode _dragMode = DragMode.None;

    /// <summary>
    /// 连线创建计数，用于决定“同一 start 下最新创建的线”。
    /// </summary>
    private int _creationCounter = 0;

    // ====== 每次拖拽临时缓存 ======
    /// <summary>
    /// 拖拽世界坐标换算用的平面（以拖拽起点 Canvas 为基准）。
    /// </summary>
    private Plane _dragPlane;

    /// <summary>
    /// 本次拖拽使用的事件相机（优先 pressEventCamera，否则 main）。 
    /// </summary>
    private Camera _eventCam;

    /// <summary>
    /// 拖拽模式：  
    /// None：未拖拽  
    /// NewFromStart：从 start 新建一条线  
    /// ExtendExisting：延长一条已存在的线  
    /// </summary>
    private enum DragMode
    {
        None,
        NewFromStart,
        ExtendExisting
    }

    protected override void Awake()
    {
        base.Awake();

    }

    /// <summary>
    /// 在运行时创建一份 ConnectionLine 模板对象。  
    /// 通过代码补齐 LineRenderer 的默认设置，保证表现一致。
    /// </summary>
    private ConnectionLine CreateLineInstance()
    {
        var go = new GameObject("ConnectionLine");
        go.transform.SetParent(transform, false);

        var line = go.AddComponent<ConnectionLine>();
        var lr = go.GetComponent<LineRenderer>();

        // 这里把你原来在 CreateLineTemplate 里填的配置都搬过来
        lr.useWorldSpace = true;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.textureMode = LineTextureMode.Tile;
        lr.alignment = LineAlignment.View;
        lr.sortingLayerName = "UI";
        lr.sortingOrder = 10;

        return line;
    }


    #region 拖拽 API（由 UINode 调用）

    /// <summary>
    /// 开始拖拽：  
    /// - 根据起点确定拖拽平面和相机。  
    /// - 决定是新建连线还是延长已有连线。
    /// </summary>
    public void StartDrag(UINode origin, PointerEventData eventData)
    {
        _activeLine = null;
        _dragMode = DragMode.None;

        // 本次拖拽使用的事件相机
        _eventCam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;

        // 以 origin 所在 Canvas 的朝向定义拖拽平面
        // 2D World-Space 通常为 XY 平面，normal 指向 Canvas.forward（一般为 +Z）
        var originCanvas = origin.GetComponentInParent<Canvas>();
        if (originCanvas != null)
            _dragPlane = new Plane(originCanvas.transform.forward, originCanvas.transform.position);
        else
            _dragPlane = new Plane(Vector3.forward, origin.transform.position);

        if (origin.isStart)
        {
            // start 节点：允许无上限新建一条线
            _activeLine = CreateLineInstance();
            _activeLine.Init(origin, origin.CurrentActiveSupplyDef, lineWidth, ++_creationCounter);
            _dragMode = DragMode.NewFromStart;
        }
        else
        {
            // 非 start：只能延长自己作为尾节点的已有线
            List<ConnectionLine> tailLines = _lines.Where(l => l.LastNode == origin).ToList();

            if (tailLines.Count == 1)
            {
                // 只有一条候选线时，直接选中
                _activeLine = tailLines[0];
                _dragMode = DragMode.ExtendExisting;
            }
            else if (tailLines.Count > 1)
            {
                // 多条候选线时：选离鼠标最近的那条
                Vector3 mouseWorld = GetDragWorld(eventData);

                float best = float.MaxValue;
                ConnectionLine bestLine = null;

                foreach (var l in tailLines)
                {
                    float d2 = MinDistanceSqToPolyline(mouseWorld, l.CachedPoints);
                    if (d2 < best)
                    {
                        best = d2;
                        bestLine = l;
                    }
                }

                // 加一个误触阈值，避免鼠标离线太远仍被选中
                float pickRadius = lineWidth * lineFalseTouchDistanceThreshold;
                if (bestLine != null && best <= pickRadius * pickRadius)
                {
                    _activeLine = bestLine;
                    _dragMode = DragMode.ExtendExisting;
                }
            }
        }

        // 初始化临时尾端，用于拖拽时显示“跟随鼠标的尾巴”
        if (_activeLine != null)
            _activeLine.SetTempTail(GetDragWorld(eventData));
    }

    /// <summary>
    /// 拖拽中：实时更新临时尾端位置。
    /// </summary>
    public void Drag(PointerEventData eventData)
    {
        if (_activeLine == null) return;
        _activeLine.SetTempTail(GetDragWorld(eventData));
    }

    /// <summary>
    /// 结束拖拽：  
    /// - 计算鼠标下的目标节点。  
    /// - 根据拖拽模式固化或回滚连线。  
    /// - 最后刷新所有节点槽位与线形。
    /// </summary>
    public void EndDrag(UINode origin, PointerEventData eventData)
    {
        if (_activeLine == null) return;

        UINode target = GetNodeUnderPointer(eventData, origin);

        if (_dragMode == DragMode.NewFromStart)
        {
            if (target != null)
            {
                // 命中了节点
                if (FindLineWithSegment(origin, target) != null)
                {
                    // 这次拖出来的线无效：要先从节点注销，再销毁
                    _activeLine.DetachAll();           // ★ 新增
                    Destroy(_activeLine.gameObject);
                }
                else
                {
                    _activeLine.ClearTempTail();
                    _activeLine.AppendNode(target);
                    _lines.Add(_activeLine);
                }
            }
            else
            {
                // 没命中任何节点：这次新建的临时线需要先撤销注册
                _activeLine.DetachAll();               // ★ 先把刚才新建的那条线从节点上去掉

                // 再按你的原逻辑：拖空视为“撤销上一条真正存在的线”
                DeleteLastLineFromStart(origin);

                Destroy(_activeLine.gameObject);
            }
        }
        else if (_dragMode == DragMode.ExtendExisting)
        {
            if (target != null && target != origin)
            {
                // 防止形成环或重复节点
                if (!_activeLine.nodes.Contains(target))
                {
                    _activeLine.ClearTempTail();
                    _activeLine.AppendNode(target);
                }
                else
                {
                    // 命中已存在节点：仅清掉临时尾，不做修改
                    _activeLine.ClearTempTail();
                }
            }
            else
            {
                // 未命中任何节点：保持原线不变
                _activeLine.ClearTempTail();
            }
        }

        // 重置拖拽状态
        _activeLine = null;
        _dragMode = DragMode.None;

        // 刷新所有节点的“出入口槽位”与所有线的坐标
        RecalculateAllLanes();
        RebuildAllLines();
    }


    #endregion

    #region 拓扑操作（删除/断开/重建）

    /// <summary>
    /// 删除整条线：  
    /// 1) 从关联节点解绑  
    /// 2) 从集合移除  
    /// 3) 销毁对象  
    /// 4) 全局重算/重建
    /// </summary>
    public void DeleteLine(ConnectionLine line)
    {
        if (line == null) return;

        line.DetachAll();
        _lines.Remove(line);
        Destroy(line.gameObject);

        RecalculateAllLanes();
        RebuildAllLines();
    }

    /// <summary>
    /// 规则：若线上的任意一段 AB 被断开，则整条线移除。
    /// </summary>
    public void DisconnectSegment(UINode a, UINode b)
    {
        var line = FindLineWithSegment(a, b);
        if (line != null)
            DeleteLine(line);
    }

    /// <summary>
    /// 删除某 start 节点最新创建的那条线（用于 start 拖空撤销）。
    /// </summary>
    private void DeleteLastLineFromStart(UINode start)
    {
        var candidates = _lines.Where(l => l.startNode == start)
                               .OrderByDescending(l => l.CreationOrder)
                               .ToList();
        if (candidates.Count > 0)
            DeleteLine(candidates[0]);
    }

    /// <summary>
    /// 查找包含线段 AB 的连线（用于判重/断开）。
    /// </summary>
    private ConnectionLine FindLineWithSegment(UINode a, UINode b)
    {
        return _lines.FirstOrDefault(l => l.ContainsSegment(a, b));
    }

    /// <summary>
    /// 收集所有参与连线的节点，重算它们的槽位/车道信息。
    /// </summary>
    private void RecalculateAllLanes()
    {
        var allNodes = new HashSet<UINode>();
        foreach (var line in _lines)
            foreach (var n in line.nodes)
                allNodes.Add(n);

        foreach (var n in allNodes)
            n.RecalculateLanes();
    }

    /// <summary>
    /// 重建所有线的渲染点（比如节点移动后需要更新曲线/折线）。
    /// </summary>
    private void RebuildAllLines()
    {
        foreach (var l in _lines)
            l.RebuildPositions();
    }

    #endregion

    #region 射线检测 / 拖拽世界坐标

    /// <summary>
    /// 跨 Canvas 的 UI 射线检测。  
    /// 要求：  
    /// - 每个 World-Space Canvas 挂 GraphicRaycaster  
    /// - 节点上有可被 Raycast 的 Graphic  
    /// </summary>
    private UINode GetNodeUnderPointer(PointerEventData eventData, UINode exclude)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            var node = r.gameObject.GetComponentInParent<UINode>();
            if (node != null && node != exclude)
                return node;
        }
        return null;
    }

    /// <summary>
    /// 获取拖拽时的世界坐标：  
    /// 1) 若 UI Raycast 命中且提供 worldPosition，则直接用它（跨 Canvas 更准）。  
    /// 2) 否则用屏幕射线与拖拽平面求交点。  
    /// </summary>
    private Vector3 GetDragWorld(PointerEventData eventData)
    {
        // 优先使用 UI 命中提供的 worldPosition
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        if (results.Count > 0)
        {
            var wp = results[0].worldPosition;
            if (wp != Vector3.zero)
                return wp;
        }

        if (_eventCam == null) _eventCam = Camera.main;

        Ray ray = _eventCam.ScreenPointToRay(eventData.position);
        if (_dragPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        // 理论上不会走到这里，兜底返回射线起点
        return ray.origin;
    }

    #endregion

    /// <summary>
    /// 从“线的尾端拖拽手柄”开始拖拽，强制进入延长模式。  
    /// 用于你在 ConnectionLine 上做的可视化拖拽点。
    /// </summary>
    public void StartDragHandle(ConnectionLine line, UINode ownerNode, PointerEventData eventData)
    {
        _activeLine = null;
        _dragMode = DragMode.None;

        _eventCam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;

        // 拖拽平面仍用 ownerNode 所在 Canvas
        var originCanvas = ownerNode.GetComponentInParent<Canvas>();
        if (originCanvas != null)
            _dragPlane = new Plane(originCanvas.transform.forward, originCanvas.transform.position);
        else
            _dragPlane = new Plane(Vector3.forward, ownerNode.transform.position);

        // 强制指定这条线为当前延长线
        _activeLine = line;
        _dragMode = DragMode.ExtendExisting;

        if (_activeLine != null)
            _activeLine.SetTempTail(GetDragWorld(eventData));
    }

    /// <summary>
    /// 计算点 p 到一条折线 poly 的最小距离平方。  
    /// 用于“鼠标离哪条线最近”的判断。
    /// </summary>
    private float MinDistanceSqToPolyline(Vector3 p, IReadOnlyList<Vector3> poly)
    {
        float best = float.MaxValue;
        for (int i = 0; i < poly.Count - 1; i++)
        {
            best = Mathf.Min(best, DistanceSqPointSegment(p, poly[i], poly[i + 1]));
        }
        return best;
    }

    /// <summary>
    /// 点到线段的距离平方（避免开方提升性能）。
    /// </summary>
    private float DistanceSqPointSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector3 proj = a + t * ab;
        return (p - proj).sqrMagnitude;
    }
    #endregion

    #region 与建筑系统集成

   
    //显示 //这个事件需要显示所有的线 显示所有的节点（起始节点添加额外的光环）
    public event Action OnShowTransfer;
    //选择一个资源
    public event Action<SupplyDef> OnSelectSupply;
    public event Action OnHideTransfer;


    public void EnterEditorMode()
    {
        OnShowTransfer?.Invoke();
    }

    public void OnSelect(SupplyDef def)
    {
        OnSelectSupply?.Invoke(def);
    }

    public void ExitEditorMode()
    {
        OnHideTransfer?.Invoke();
    }

    #endregion

}

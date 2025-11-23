using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Global manager for UI node connections.
/// Works with multiple World-Space canvases in a 2D project.
/// - Raycast uses EventSystem.RaycastAll (cross-canvas).
/// - Drag world position computed via a plane defined by the drag origin canvas.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager I { get; private set; }

    [Header("Line Prefab")]
    public ConnectionLine linePrefab;   // Prefab with LineRenderer + ConnectionLine

    [Header("Line Settings")]
    public float lineWidth = 0.02f;


    [LabelText("操作线阈值")]
    private float lineFalseTouchDistanceThreshold = 10f;

    private readonly List<ConnectionLine> _lines = new List<ConnectionLine>();

    private ConnectionLine _activeLine;
    private DragMode _dragMode = DragMode.None;

    private int _creationCounter = 0;

    // Drag plane & camera (per drag)
    private Plane _dragPlane;
    private Camera _eventCam;

    private enum DragMode
    {
        None,
        NewFromStart,
        ExtendExisting
    }

    private void Awake()
    {
        I = this;
    }

    #region Drag API (called by UINode)

    public void StartDrag(UINode origin, PointerEventData eventData)
    {
        _activeLine = null;
        _dragMode = DragMode.None;

        _eventCam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;

        // Define drag plane based on origin node's canvas.
        // For 2D, this plane is typically XY with normal along canvas.forward (often +Z).
        var originCanvas = origin.GetComponentInParent<Canvas>();
        if (originCanvas != null)
            _dragPlane = new Plane(originCanvas.transform.forward, originCanvas.transform.position);
        else
            _dragPlane = new Plane(Vector3.forward, origin.transform.position);

        if (origin.isStart)
        {
            // Rule 1/2: only start node can create a new line; no limit on count
            _activeLine = Instantiate(linePrefab, transform);
            _activeLine.Init(origin, origin.lineMaterial, lineWidth, ++_creationCounter);
            _dragMode = DragMode.NewFromStart;
        }
        else
        {
            var tailLines = _lines.Where(l => l.LastNode == origin).ToList();
            if (tailLines.Count == 1
            )
            {
                _activeLine = tailLines[
            0
            ];
                _dragMode = DragMode.ExtendExisting;
            }
            else if (tailLines.Count > 1
            )
            {
                Vector3 mouseWorld = GetDragWorld(eventData);

                float best = float.MaxValue;
                ConnectionLine bestLine = null
            ;

                foreach (var l in tailLines)
                {
                    float d2 = MinDistanceSqToPolyline(mouseWorld, l.CachedPoints);
                    if (d2 < best)
                    {
                        best = d2;
                        bestLine = l;
                    }
                }

                // 设一个可调阈值，避免远处误选
                float pickRadius = lineWidth * lineFalseTouchDistanceThreshold; // 你可暴露成参数
                if (bestLine != null && best <= pickRadius * pickRadius)
                {
                    _activeLine = bestLine;
                    _dragMode = DragMode.ExtendExisting;
                }
            }
        }

        if (_activeLine != null)
            _activeLine.SetTempTail(GetDragWorld(eventData));
    }

    public void Drag(PointerEventData eventData)
    {
        if (_activeLine == null) return;
        _activeLine.SetTempTail(GetDragWorld(eventData));
    }

    public void EndDrag(UINode origin, PointerEventData eventData)
    {
        if (_activeLine == null) return;

        UINode target = GetNodeUnderPointer(eventData, origin);

        if (_dragMode == DragMode.NewFromStart)
        {
            if (target != null)
            {
                // If AB already exists, do not create duplicate; cancel this temp line
                if (FindLineWithSegment(origin, target) != null)
                {
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
                // Rule 5: start dragged but not connected to any node => delete existing connection
                DeleteLastLineFromStart(origin);
                Destroy(_activeLine.gameObject);
            }
        }
        else if (_dragMode == DragMode.ExtendExisting)
        {
            if (target != null && target != origin)
            {
                // Prevent loops/repeat nodes
                if (!_activeLine.nodes.Contains(target))
                {
                    _activeLine.ClearTempTail();
                    _activeLine.AppendNode(target);
                }
                else
                {
                    _activeLine.ClearTempTail();
                }
            }
            else
            {
                // Not hit any node => keep original line unchanged
                _activeLine.ClearTempTail();
            }
        }

        _activeLine = null;
        _dragMode = DragMode.None;

        RecalculateAllLanes();
        RebuildAllLines();
    }

    #endregion

    #region Topology operations

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
    /// Rule 4: If any segment AB is disconnected, the whole line is removed.
    /// </summary>
    public void DisconnectSegment(UINode a, UINode b)
    {
        var line = FindLineWithSegment(a, b);
        if (line != null)
            DeleteLine(line);
    }

    private void DeleteLastLineFromStart(UINode start)
    {
        var candidates = _lines.Where(l => l.startNode == start)
                               .OrderByDescending(l => l.CreationOrder)
                               .ToList();
        if (candidates.Count > 0)
            DeleteLine(candidates[0]);
    }

    private ConnectionLine FindLineWithSegment(UINode a, UINode b)
    {
        return _lines.FirstOrDefault(l => l.ContainsSegment(a, b));
    }

    private void RecalculateAllLanes()
    {
        var allNodes = new HashSet<UINode>();
        foreach (var line in _lines)
            foreach (var n in line.nodes)
                allNodes.Add(n);

        foreach (var n in allNodes)
            n.RecalculateLanes();
    }

    private void RebuildAllLines()
    {
        foreach (var l in _lines)
            l.RebuildPositions();
    }

    #endregion

    #region Raycast / Drag world position

    /// <summary>
    /// Cross-canvas UI raycast. Requires each World-Space canvas to have GraphicRaycaster,
    /// and nodes to have raycastable Graphics.
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
    /// Get drag world point:
    /// 1) If raycast hits some UI and provides worldPosition, use it.
    /// 2) Otherwise intersect screen ray with drag plane.
    /// </summary>
    private Vector3 GetDragWorld(PointerEventData eventData)
    {
        // Prefer UI hit world position if available
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

        return ray.origin;
    }

    #endregion

    public void StartDragHandle(ConnectionLine line, UINode ownerNode, PointerEventData eventData)
    {
        _activeLine = null;
        _dragMode = DragMode.None;

        _eventCam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;

        // 拖拽平面仍用 ownerNode 所在 canvas
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

    private float MinDistanceSqToPolyline(Vector3 p, IReadOnlyList<Vector3> poly)
    {
        float best = float.MaxValue;
        for (int i = 0; i < poly.Count - 1; i++)
        {
            best = Mathf.Min(best, DistanceSqPointSegment(p, poly[i], poly[i + 1]));
        }
        return best;
    }
    //点到线距离的平方
    private float DistanceSqPointSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector3 proj = a + t * ab;
        return (p - proj).sqrMagnitude;
    }

}

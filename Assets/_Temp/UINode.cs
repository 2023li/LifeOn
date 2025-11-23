using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UINode : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler,IPointerEnterHandler, IPointerExitHandler
{
    [Header("Topology")]
    public bool isStart = false;

    [Header("Line Style (used only if isStart == true)")]
    public Material lineMaterial;

    [Header("左右端口偏移")]
    public float horizontalMargin = 0.5f;   // 经过节点时左右点距节点边缘的额外偏移（世界单位）
    [LabelText("水平排列距离")]
    public float laneSpacing = 0.2f;         // 多条线经过同节点时的水平排列间距（世界单位）

    private RectTransform _rt;

    // 暴露 laneLines 给 manager/handle 用
    public IReadOnlyList<ConnectionLine> LaneLines => _laneLines;

    private readonly List<GameObject> _handles = new
     List<GameObject>();
    public float handleSizeWorld = 0.08f;   // 2D世界单位大小
    public float handleOffsetWorld = 0.12f; // 右侧偏移

    // 所有“占用该节点水平通道”的线（中间或末端拖拽时）
    private readonly List<ConnectionLine> _laneLines = new List<ConnectionLine>();
    private readonly Dictionary<ConnectionLine, int> _laneIndex = new Dictionary<ConnectionLine, int>();

    public RectTransform RectT => _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    #region Drag Forward
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("开始");
        ConnectionManager.I.StartDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ConnectionManager.I.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ConnectionManager.I.EndDrag(this, eventData);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
      //  SpawnHandles();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //ClearHandles();
    }

    #endregion

    #region Lane / Pass-through points

    public void RegisterLaneLine(ConnectionLine line)
    {
        if (_laneLines.Contains(line)) return;
        _laneLines.Add(line);
    }

    public void UnregisterLaneLine(ConnectionLine line)
    {
        _laneLines.Remove(line);
        _laneIndex.Remove(line);
    }

    // Manager 在拓扑变化后调用
    public void RecalculateLanes()
    {
        // 固定排序保证 lane 稳定（可换成按创建时间）
        _laneLines.Sort((a, b) => a.CreationOrder.CompareTo(b.CreationOrder));
        _laneIndex.Clear();
        for (int i = 0; i < _laneLines.Count; i++)
            _laneIndex[_laneLines[i]] = i;
    }

    private float GetLaneYOffset(ConnectionLine line)
    {
        if (!_laneIndex.TryGetValue(line, out int idx))
            idx = 0;

        int count = _laneLines.Count;
        // 居中排列：[-1,0,1] 这种效果
        float centered = idx - (count - 1) * 0.5f;
        return centered * laneSpacing;
    }

    private float HalfWidthWorld()
    {
        // RectTransform width * lossyScale => 世界宽度
        return _rt.rect.width * _rt.lossyScale.x * 0.5f;
    }

    public Vector3 CenterLanePoint(ConnectionLine line)
    {
        return transform.position + _rt.up * GetLaneYOffset(line);
    }

    public Vector3 LeftPoint(ConnectionLine line)
    {
        float hw = HalfWidthWorld();
        return transform.position
               - _rt.right * (hw + horizontalMargin)
               + _rt.up * GetLaneYOffset(line);
    }

    public Vector3 RightPoint(ConnectionLine line)
    {
        float hw = HalfWidthWorld();
        return transform.position
               + _rt.right * (hw + horizontalMargin)
               + _rt.up * GetLaneYOffset(line);
    }

    #endregion



    private void SpawnHandles()
    {
        ClearHandles();

        // 只给“以我为尾端”的线生成 handle
        for (int i = 0; i < _laneLines.Count; i++)
        {
            var line = _laneLines[i];
            if (line.LastNode != this) continue;

            var go = new GameObject($"Handle_{line.CreationOrder}");
            go.transform.SetParent(transform.parent, worldPositionStays: true);

            // 位置：节点右侧 + 对应lane的Y偏移（规则7）
            Vector3 pos = RightPoint(line) + RectT.right * handleOffsetWorld;
            go.transform.position = pos;

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = Vector2.one * handleSizeWorld; // world-space canvas下直接按世界尺寸

            var handle = go.AddComponent<LineHandle>();
            handle.Init(line, this);

            _handles.Add(go);
        }
    }

    private void ClearHandles()
    {
        for (int i = 0; i < _handles.Count; i++)
            if (_handles[i] != null) Destroy(_handles[i]);
        _handles.Clear();
    }
}

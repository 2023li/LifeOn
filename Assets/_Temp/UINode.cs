using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UINode : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler,IPointerEnterHandler, IPointerExitHandler
{
    [Header("拓扑")]
    public bool isStart = false;

    [Header("线材质 (只有在isStart为turn时才会被使用)")]
    public Material lineMaterial;

    [Header("左右端口偏移")]
    public float horizontalMargin = 0.5f;   // 经过节点时左右点距节点边缘的额外偏移（世界单位）
    [LabelText("水平排列距离")]
    public float laneSpacing = 0.2f;         // 多条线经过同节点时的水平排列间距（世界单位）

    private RectTransform _rt;

    // 暴露 laneLines 给 manager/handle 用
    public IReadOnlyList<ConnectionLine> LaneLines => _laneLines;

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
     
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      
    }

    #endregion

    #region Lane / Pass-through points
    /// <summary>
    /// 注册到线
    /// </summary>
    /// <param name="line"></param>
    public void RegisterLaneLine(ConnectionLine line)
    {
        if (_laneLines.Contains(line)) return;
        _laneLines.Add(line);
    }

    /// <summary>
    /// 从线中注销
    /// </summary>
    /// <param name="line"></param>
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
}

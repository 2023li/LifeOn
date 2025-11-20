using System.Collections.Generic;
using UnityEngine;

public class ConnectionLine : MonoBehaviour
{
    public List<ConnectNodeUI> nodes = new();
    public LineRenderer lr;

    // reroute/断开逻辑
    public bool IsRerouting { get; private set; }
    private List<ConnectNodeUI> _removedTail;
    private int _rerouteIndex = -1;

    public void Init(Material mat, float width, int sortingOrder = 0)
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.positionCount = 0;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.numCapVertices = 6;
        lr.numCornerVertices = 6;
        lr.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        RebuildPositions();
    }

    public void AddStart(ConnectNodeUI start)
    {
        nodes.Clear();
        nodes.Add(start);
        start.RegisterLine(this);
        RebuildPositions();
    }

    public void AppendNode(ConnectNodeUI node)
    {
        if (node == null) return;
        if (nodes.Count > 0 && nodes[^1] == node) return;
        if (nodes.Contains(node)) return; // 防止环路（可按需去掉）

        nodes.Add(node);
        node.RegisterLine(this);
        RebuildPositions();
    }

    public int IndexOf(ConnectNodeUI node) => nodes.IndexOf(node);

    // 从中间节点开始拖拽：先砍掉后半段，进入 reroute 状态
    public void BeginReroute(ConnectNodeUI fromNode)
    {
        int idx = IndexOf(fromNode);
        if (idx < 0 || idx >= nodes.Count - 1) return;

        _rerouteIndex = idx;
        _removedTail = nodes.GetRange(idx + 1, nodes.Count - (idx + 1));

        // 反注册 tail
        foreach (var n in _removedTail)
            n.UnregisterLine(this);

        nodes.RemoveRange(idx + 1, nodes.Count - (idx + 1));

        IsRerouting = true;
        RebuildPositions();
    }

    public void EndReroute(ConnectNodeUI target)
    {
        if (!IsRerouting)
        {
            AppendNode(target);
            return;
        }

        // 规则2：如果没连回原 next，则 tail 永久断开
        if (_removedTail != null && _removedTail.Count > 0 && target == _removedTail[0])
        {
            // 连回 c：恢复 c,d...
            foreach (var n in _removedTail)
            {
                nodes.Add(n);
                n.RegisterLine(this);
            }
        }
        else
        {
            // 连到别处/没连到 c：tail 丢弃，若 target 不为空则接新节点
            if (target != null)
                AppendNode(target);
        }

        ClearRerouteState();
    }

    // 松开但没落到任何节点（视为没连到 c）=> tail 已被砍掉且不恢复
    public void CancelReroute()
    {
        if (!IsRerouting) return;
        ClearRerouteState();
        RebuildPositions();
    }

    private void ClearRerouteState()
    {
        IsRerouting = false;
        _removedTail = null;
        _rerouteIndex = -1;

        // 少于2点就销毁
        if (nodes.Count < 2)
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var n in nodes)
            n.UnregisterLine(this);

        Destroy(gameObject);
    }

    private void RebuildPositions()
    {
        if (lr == null) return;
        lr.positionCount = nodes.Count;

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            // 按节点当前 anchor（展开/不展开自动变化）
            Transform anchor = n.GetAnchor(this);
            lr.SetPosition(i, anchor.position);
        }
    }
}
